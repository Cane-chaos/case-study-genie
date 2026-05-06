from typing import TypedDict, Annotated, List, Optional, Any
from langgraph.graph import StateGraph, END
from langgraph.graph.message import add_messages
from langchain_core.messages import AnyMessage, SystemMessage, HumanMessage, AIMessage
from langchain_openai import ChatOpenAI
import json
import re
import unicodedata
import dotenv

from api_agent.services.state_loader import StateLoader

dotenv.load_dotenv(override=True)

try:
    llm = ChatOpenAI(model="gpt-4o", temperature=0.7)
    eval_llm = ChatOpenAI(model="gpt-4o-mini", temperature=0.0) # Dùng model nhanh và rẻ cho việc chạy ngầm
except Exception as e:
    print(f"Lỗi khởi tạo LLM trong Game Agent: {e}")
    llm = None
    eval_llm = None

VI_STOPWORDS = {
    "le", "tan", "khach", "hang", "anh", "chi", "em", "a", "la", "va", "voi",
    "cho", "de", "duoc", "moi", "co", "phai", "can", "hay", "xin", "rat",
    "tiec", "the", "thay", "bang", "tren", "trong", "ung", "dung"
}

POLITE_SUPPORTIVE_TOKENS = {
    "xin", "loi", "mong", "cam", "on", "vui", "long", "giup", "ho", "tro",
    "linh", "dong", "huong", "dan", "thong", "cam", "kiem", "tra", "xac", "minh"
}
AGGRAVATING_TOKENS = {
    "khong", "duoc", "bat", "buoc", "tu", "choi", "cung", "nhac", "ve", "lay",
    "sai", "loi", "phi", "ly", "im", "di", "nhanh", "ke", "mac", "khong", "quan",
    "tam", "phai", "chiu", "trach", "nhiem"
}
INSULT_TOKENS = {
    "ngu", "dot", "vo", "duyen", "lao", "dao", "mat", "day", "khung", "dien",
    "dien", "tham", "te", "rac", "ruoi", "vo", "trach", "nhiem"
}

GLOBAL_INTERRUPTION_CASES = {
    "__global_service_refusal": {
        "_id": "__global_service_refusal",
        "root_operation": "Global interruption",
        "agent_init": {
            "persona_name": "Anh Tuấn",
            "system_prompt": (
                "Bạn là Tuấn, một khách đã đặt phòng trước và đang rất mệt sau chuyến đi dài. "
                "Lễ tân vừa nói khách sạn đóng cửa/không tiếp/không phục vụ bạn mà không xử lý nghiệp vụ. "
                "Bạn cảm thấy bị xúc phạm và cực kỳ tức giận. Hãy phản ứng ngắn gọn, gay gắt, nói rằng cách phục vụ này không chấp nhận được, "
                "dọa khiếu nại hoặc đánh giá xấu, rồi rời đi. Không dùng từ tục tĩu."
            ),
            "base_tension": 1.0,
            "required_tasks": [],
            "passing_threshold": 1.0,
            "ending_type": "bad"
        },
        "transitions": {"possible_resolutions": []},
        "metadata": {
            "end_message": "Lễ tân từ chối phục vụ ngoài quy trình. Kịch bản kết thúc thất bại."
        }
    },
    "__global_personal_insult": {
        "_id": "__global_personal_insult",
        "root_operation": "Global interruption",
        "agent_init": {
            "persona_name": "Anh Tuấn",
            "system_prompt": (
                "Bạn là Tuấn, một khách đang mệt và muốn check-in. Lễ tân vừa xúc phạm cá nhân hoặc nói chuyện thiếu tôn trọng. "
                "Bạn nổi cáu ngay, yêu cầu gặp quản lý, nói sẽ khiếu nại về thái độ phục vụ và không còn muốn tiếp tục trao đổi với lễ tân này. "
                "Phản hồi ngắn, gay gắt nhưng không dùng từ tục tĩu."
            ),
            "base_tension": 1.0,
            "required_tasks": [],
            "passing_threshold": 1.0,
            "ending_type": "bad"
        },
        "transitions": {"possible_resolutions": []},
        "metadata": {
            "end_message": "Khách bị xúc phạm và yêu cầu dừng tương tác. Kịch bản kết thúc thất bại."
        }
    }
}

def _normalize_text(text: str) -> str:
    text = unicodedata.normalize("NFKD", text)
    text = "".join(ch for ch in text if not unicodedata.combining(ch))
    text = text.lower()
    return re.sub(r"[^a-z0-9]+", " ", text).strip()

def _meaningful_tokens(text: str) -> set[str]:
    normalized = _normalize_text(text)
    return {
        token
        for token in normalized.split()
        if len(token) > 1 and token not in VI_STOPWORDS
    }

def _get_latest_user_message(messages: List[AnyMessage]) -> str:
    for msg in reversed(messages):
        if isinstance(msg, HumanMessage) or getattr(msg, "type", None) == "human":
            return msg.content
    return ""

def _match_resolution_by_user_message(user_message: str, resolutions: List[dict]) -> Optional[str]:
    user_normalized = _normalize_text(user_message)
    user_tokens = _meaningful_tokens(user_message)
    if not user_tokens:
        return None

    best_next_id = None
    best_score = 0.0
    for resolution in resolutions:
        action = resolution.get("user_action", "")
        next_case_id = resolution.get("next_case_id")
        if not action or not next_case_id:
            continue

        action_normalized = _normalize_text(action)
        action_tokens = _meaningful_tokens(action)
        if not action_tokens:
            continue

        distinctive_keywords = {"vneid"}
        if user_tokens & action_tokens & distinctive_keywords:
            return next_case_id

        # Exact/near-exact containment catches UI buttons or copied action text.
        if action_normalized in user_normalized or user_normalized in action_normalized:
            return next_case_id

        overlap = user_tokens & action_tokens
        action_coverage = len(overlap) / len(action_tokens)
        user_coverage = len(overlap) / len(user_tokens)
        score = (action_coverage * 0.7) + (user_coverage * 0.3)
        if score > best_score:
            best_score = score
            best_next_id = next_case_id

    if best_score >= 0.50:
        print(f"[Evaluator] Rule-based match: {best_next_id} (score={best_score:.2f})")
        return best_next_id
    return None

def _clamp_tension(value: float) -> float:
    return max(0.0, min(1.0, value))

def _estimate_tension_delta(user_message: str, current_tension: float, matched_next_id: Optional[str] = None) -> float:
    tokens = _meaningful_tokens(user_message)
    normalized = _normalize_text(user_message)
    delta = 0.0

    aggravating_hits = tokens & AGGRAVATING_TOKENS
    insult_hits = tokens & INSULT_TOKENS
    supportive_hits = tokens & POLITE_SUPPORTIVE_TOKENS

    if supportive_hits:
        delta -= min(0.18, 0.04 * len(supportive_hits))
    if aggravating_hits:
        delta += min(0.35, 0.07 * len(aggravating_hits))
    if insult_hits:
        delta += 0.45

    if "!" in user_message:
        delta += min(0.18, user_message.count("!") * 0.06)
    if re.search(r"[A-ZÀ-ỸĐ]{4,}", user_message):
        delta += 0.12
    if any(phrase in normalized for phrase in ("khong duoc", "bat buoc", "tu choi", "ve lay")):
        delta += 0.18
    if any(phrase in normalized for phrase in ("xin loi", "mong anh", "mong chi", "vui long", "ho tro")):
        delta -= 0.10

    if matched_next_id:
        if re.search(r"_(?:bad|fail|failure)$", matched_next_id.lower()):
            delta += 0.20
        elif re.search(r"_2_3(?:_|$)", matched_next_id):
            delta += 0.20

    if current_tension >= 0.6 and delta > 0:
        delta *= 1.35
    if current_tension >= 0.8 and delta > 0:
        delta += 0.10

    return max(-0.30, min(0.55, delta))

def _detect_global_interruption(user_message: str) -> tuple[Optional[str], float]:
    normalized = _normalize_text(user_message)
    tokens = _meaningful_tokens(user_message)

    service_refusal_patterns = (
        "khach san dong cua",
        "dong cua khong tiep",
        "khong tiep",
        "khong phuc vu",
        "khong nhan khach",
        "het gio tiep",
        "duoi khach",
        "khong cho vao",
    )
    if any(pattern in normalized for pattern in service_refusal_patterns):
        return "__global_service_refusal", 0.55

    if tokens & INSULT_TOKENS:
        return "__global_personal_insult", 0.55

    return None, 0.0

class GameState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]
    scenario_id: str
    current_node_id: str
    previous_node_id: Optional[str]
    case_data: dict
    next_node_id: Optional[str]
    tension_level: float

def persona_node(state: GameState):
    """
    Đóng vai Persona để phản hồi User.
    """
    if not llm: return {"messages": []}
    
    case_data = state["case_data"]
    agent_init = case_data.get("agent_init", {})
    system_prompt = agent_init.get("system_prompt")
    
    # Lấy mức độ căng thẳng hiện tại
    tension_level = state.get("tension_level", agent_init.get("base_tension", 0.0))
    escalation_rate = agent_init.get("escalation_rate", 0.1)
    
    # Bọc SystemPrompt vào đầu luồng để nhắc AI về thân phận và hoàn cảnh
    dynamic_prompt = f"""{system_prompt}

[TRẠNG THÁI CẢM XÚC HIỆN TẠI]
Mức độ căng thẳng/vội vã: {tension_level:.1f}/1.0.
- 0.0-0.3: còn lịch sự, hợp tác.
- 0.4-0.6: mệt, sốt ruột, bắt đầu nài nỉ.
- 0.7-0.8: bực rõ, phản ứng ngắn và gay gắt hơn.
- 0.9-1.0: cảm thấy bị xúc phạm hoặc bị đối xử cứng nhắc; nổi cáu ngay, nói rất ngắn, dằn giọng, có thể dọa phàn nàn/bỏ đi.
Nếu lời lễ tân thiếu lễ phép, đổ lỗi, ra lệnh, từ chối cứng nhắc, hoặc làm bạn cảm thấy bị xúc phạm, hãy phản ứng cáu ngay cả khi trước đó đang bình tĩnh. Không giải thích dài dòng như robot."""
    formatted_messages = [SystemMessage(content=dynamic_prompt)] + state["messages"]
    
    response = llm.invoke(formatted_messages)
    return {"messages": [response]}

def evaluator_node(state: GameState):
    """
    Node đánh giá chạy ngầm để xem User đã hoàn thành required_tasks chưa, có rẽ nhánh không, và chấm điểm tension_delta.
    """
    case_data = state.get("case_data", {})
    agent_init = case_data.get("agent_init", {})
    transitions = case_data.get("transitions", {})
    resolutions = transitions.get("possible_resolutions", [])
    
    # Lấy mức độ căng thẳng hiện tại
    current_tension = state.get("tension_level", agent_init.get("base_tension", 0.0))
    
    # Không có rẽ nhánh hoặc là node cuối cùng
    if agent_init.get("ending_type") in {"good", "bad"} or not resolutions:
        return {"next_node_id": None, "tension_level": current_tension}

    latest_user_message = _get_latest_user_message(state["messages"])
    global_next_id, global_delta = _detect_global_interruption(latest_user_message)
    if global_next_id:
        new_tension = _clamp_tension(current_tension + global_delta)
        print(f"[Evaluator] Global interruption: {global_next_id}, tension {current_tension:.2f} -> {new_tension:.2f}")
        return {"next_node_id": global_next_id, "tension_level": new_tension}

    rule_based_next_id = _match_resolution_by_user_message(latest_user_message, resolutions)
    if rule_based_next_id:
        delta = _estimate_tension_delta(latest_user_message, current_tension, rule_based_next_id)
        new_tension = _clamp_tension(current_tension + delta)
        print(f"[Evaluator] Rule-based tension: {current_tension:.2f} -> {new_tension:.2f} (Delta: {delta:.2f})")
        return {"next_node_id": rule_based_next_id, "tension_level": new_tension}
    
    if not eval_llm:
        return {"next_node_id": None, "tension_level": current_tension}
        
    required_tasks = agent_init.get("required_tasks", [])
    
    # Lấy 6 tin nhắn gần nhất để đánh giá
    recent_messages = state["messages"][-6:]
    chat_trace = "\n".join([f"{type(m).__name__}: {m.content}" for m in recent_messages])
    
    eval_prompt = f"""Bạn là Trọng tài hệ thống trong game mô phỏng Lễ tân Khách sạn.
Hãy đọc đoạn hội thoại gần đây giữa Lễ tân (HumanMessage) và Khách hàng (AIMessage).
Lịch sử chat:
{chat_trace}

Nhiệm vụ Lễ tân cần làm (required_tasks):
{json.dumps(required_tasks, ensure_ascii=False)}

Các kết quả rẽ nhánh có thể (possible_resolutions):
{json.dumps(resolutions, ensure_ascii=False)}

Câu hỏi 1: Lễ tân đã làm đúng/đủ các nhiệm vụ yêu cầu chưa? 
Câu hỏi 2: Hành động của Lễ tân khớp với 'user_action' nào trong possible_resolutions nhất?
Câu hỏi 3: Lễ tân trả lời có tốt không? Tính toán `tension_delta` (từ -0.5 đến +0.5). Nếu Lễ tân nói lòng vòng, chậm trễ, từ chối vô lý: tension_delta > 0 (Tăng căng thẳng). Nếu Lễ tân giải quyết nhanh, xoa dịu tốt: tension_delta < 0 (Giảm căng thẳng). Nếu trung bình: tension_delta = 0.
QUY TẮC QUAN TRỌNG: `matched_resolution_id` là lựa chọn nhánh gần nhất với hành động của Lễ tân, kể cả khi hành động đó SAI hoặc chưa hoàn thành required_tasks. Ví dụ nếu possible_resolutions có nhánh "Lễ tân từ chối..." và Lễ tân thật sự từ chối, vẫn phải trả về next_case_id của nhánh đó; không được để null chỉ vì hành động sai.
Hãy trả về JSON:
{{
  "completed": true/false,
  "matched_resolution_id": "<next_case_id hoặc null nếu chưa khớp hoặc chưa đủ>",
  "tension_delta": <float>
}}
CHỈ TRẢ VỀ JSON HỢP LỆ. KHÔNG GIẢI THÍCH."""

    try:
        resp = eval_llm.invoke([SystemMessage(content=eval_prompt)])
        clean_text = resp.content.replace("```json", "").replace("```", "").strip()
        eval_result = json.loads(clean_text)
        
        # Cập nhật tension
        delta = eval_result.get("tension_delta", 0.0)
        heuristic_delta = _estimate_tension_delta(latest_user_message, current_tension, eval_result.get("matched_resolution_id"))
        if abs(heuristic_delta) > abs(delta):
            delta = heuristic_delta
        new_tension = _clamp_tension(current_tension + delta)
        print(f"[Evaluator] Tension thay đổi: {current_tension:.2f} -> {new_tension:.2f} (Delta: {delta})")
        
        if eval_result.get("matched_resolution_id"):
            print(f"[Evaluator] Kích hoạt chuyển cảnh tới: {eval_result['matched_resolution_id']}")
            return {"next_node_id": eval_result["matched_resolution_id"], "tension_level": new_tension}
        
        return {"next_node_id": None, "tension_level": new_tension}
    except Exception as e:
        print(f"[Evaluator] Lỗi đánh giá: {e}")
        
    return {"next_node_id": None, "tension_level": current_tension}

def transition_node(state: GameState):
    """
    Thực hiện chuyển cảnh: Tải case mới, inject thông báo vào lịch sử để Persona thay đổi thái độ mà không mất trí nhớ.
    """
    next_id = state.get("next_node_id")
    scenario_id = state.get("scenario_id")
    if not next_id or not scenario_id:
        return {}
        
    loader = StateLoader()
    new_case = GLOBAL_INTERRUPTION_CASES.get(next_id)
    if not new_case:
        new_case = loader.fetch_scenario_node(scenario_id, next_id)
    if not new_case:
        print(f"[Transition] Không tìm thấy node mới: {scenario_id}/{next_id}")
        return {"next_node_id": None}
        
    new_system_prompt = new_case.get("agent_init", {}).get("system_prompt", "")
    # Tiêm một tin nhắn ảo vào bộ nhớ để lái AI theo cảm xúc mới
    inject_msg = AIMessage(content=f"[SUY NGHĨ NỘI TÂM] Tình huống vừa thay đổi. Từ bây giờ tôi sẽ ứng xử theo tâm lý mới: {new_system_prompt}")
    
    carried_tension = state.get("tension_level", 0.0)
    new_base_tension = new_case.get("agent_init", {}).get("base_tension", 0.5)
    new_tension = _clamp_tension(max(new_base_tension, carried_tension))
    
    return {
        "previous_node_id": state.get("current_node_id"),
        "current_node_id": next_id,
        "case_data": new_case,
        "next_node_id": None, # Xóa cờ chuyển cảnh
        "tension_level": new_tension,
        "messages": [inject_msg]
    }

def route_after_eval(state: GameState):
    if state.get("next_node_id"):
        return "transition"
    return "persona"

# Xây dựng LangGraph
workflow = StateGraph(GameState)
workflow.add_node("persona", persona_node)
workflow.add_node("evaluator", evaluator_node)
workflow.add_node("transition", transition_node)

workflow.set_entry_point("evaluator")
workflow.add_conditional_edges("evaluator", route_after_eval, {"transition": "transition", "persona": "persona"})
workflow.add_edge("transition", "persona")
workflow.add_edge("persona", END)

from langgraph.checkpoint.memory import MemorySaver
memory = MemorySaver()
game_app = workflow.compile(checkpointer=memory)
