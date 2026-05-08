import json
from langchain_openai import ChatOpenAI
from langchain_core.messages import SystemMessage, HumanMessage, AIMessage

from api_agent.services.game_agent import game_app
from api_agent.services.state_loader import StateLoader
from api_agent.prompts.scoring_prompts import SCORING_SYSTEM_PROMPT

try:
    score_llm = ChatOpenAI(model="gpt-4o", temperature=0.2)
except Exception as e:
    print(f"Lỗi khởi tạo LLM cho Scoring: {e}")
    score_llm = None

def get_session_history(session_id: str) -> dict:
    """Lấy trạng thái LangGraph của session."""
    try:
        state = game_app.get_state({"configurable": {"thread_id": session_id}}).values
        return state
    except Exception as e:
        print(f"Lỗi khi lấy state của session {session_id}: {e}")
        return {}

def build_chat_history_text(messages: list) -> str:
    """Chuyển mảng messages thành chuỗi văn bản để AI đọc."""
    history = []
    for msg in messages:
        if isinstance(msg, HumanMessage) or getattr(msg, "type", None) == "human":
            history.append(f"Lễ tân (Người chơi): {msg.content}")
        elif isinstance(msg, AIMessage) or getattr(msg, "type", None) == "ai":
            # Bỏ qua các suy nghĩ nội tâm của AI
            if not msg.content.startswith("[SUY NGHĨ NỘI TÂM]"):
                history.append(f"Khách hàng (AI): {msg.content}")
    return "\n".join(history)

def evaluate_session(session_id: str) -> dict:
    if not score_llm:
        return {"error": "Scoring LLM chưa được cấu hình."}

    state = get_session_history(session_id)
    if not state:
        return {"error": f"Không tìm thấy dữ liệu cho session {session_id}"}

    messages = state.get("messages", [])
    if not messages:
        return {"error": "Lịch sử trò chuyện trống."}

    chat_history_text = build_chat_history_text(messages)
    scenario_id = state.get("scenario_id", "Unknown")
    tension_level = state.get("tension_level", 0.0)
    
    # Lấy ending_type từ current_node_id hoặc state
    case_data = state.get("case_data", {})
    agent_init = case_data.get("agent_init", {})
    ending_type = agent_init.get("ending_type", "none")

    # Tải toàn cảnh nhiệm vụ
    loader = StateLoader()
    full_tasks_context = loader.get_full_scenario_context(scenario_id)

    prompt = SCORING_SYSTEM_PROMPT.format(
        scenario_id=scenario_id,
        full_tasks_context=full_tasks_context,
        chat_history=chat_history_text,
        final_tension=f"{tension_level:.2f}",
        ending_type=ending_type
    )

    try:
        response = score_llm.invoke([SystemMessage(content=prompt)])
        clean_text = response.content.replace("```json", "").replace("```", "").strip()
        eval_result = json.loads(clean_text)
        return eval_result
    except Exception as e:
        print(f"Lỗi khi parse JSON từ kết quả chấm điểm: {e}")
        return {"error": "Lỗi khi chạy logic chấm điểm", "details": str(e)}
