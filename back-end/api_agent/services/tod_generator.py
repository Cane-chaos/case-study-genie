import json
import os
import uuid
import re
from typing import List, Dict, Any, Tuple
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_openai import ChatOpenAI
import dotenv

from api_agent.prompts.tod_prompts import (
    EXPERT_PLANNER_PROMPT,
    EXPERT_CRITIQUE_PROMPT,
    NODE_REFINER_PROMPT,
    DOCUMENT_PARSER_PROMPT
)

dotenv.load_dotenv(override=True)

try:
    llm = ChatOpenAI(model="gpt-4o", temperature=0.7)
except Exception as e:
    print("Warning: Could not initialize OpenAI LLM. Make sure OPENAI_API_KEY is set.")
    llm = None

def _extract_json_array(text: str) -> List[Dict[str, Any]]:
    clean_text = text.replace("```json", "").replace("```", "").strip()
    json_match = re.search(r'\[.*\]', clean_text, re.DOTALL)
    if json_match:
        clean_text = json_match.group(0)
    parsed = json.loads(clean_text)
    if not isinstance(parsed, list):
        raise ValueError("Planner output must be a JSON array.")
    return parsed

def _is_guest_action(action: str) -> bool:
    normalized = action.strip().lower()
    guest_prefixes = (
        "khách ",
        "khach ",
        "người khách ",
        "nguoi khach ",
        "ai khách ",
    )
    return normalized.startswith(guest_prefixes)

def validate_tree_skeleton(tree: List[Dict[str, Any]], agent_index: int) -> Tuple[bool, List[str]]:
    errors = []
    if not isinstance(tree, list):
        return False, ["Kết quả trả về không phải là mảng JSON."]

    root_id = "Root"

    def _is_branch_node(nid: str) -> bool:
        if nid == "Root":
            return False
        parts = nid.split("_")
        return len(parts) > 2

    def _node_path_parts(nid: str) -> List[int]:
        if nid == "Root":
            return [0]
        parts = nid.split("_")
        if len(parts) < 2:
            return [-1]
        try:
            return [int(x) for x in parts[1:]]
        except ValueError:
            return [-1]

    node_ids: List[str] = []
    node_by_id: Dict[str, Dict[str, Any]] = {}
    adjacency: Dict[str, List[str]] = {}

    for idx, node in enumerate(tree):
        if not isinstance(node, dict):
            errors.append(f"Phần tử #{idx} không phải object JSON.")
            continue

        node_id = node.get("node_id")
        if not isinstance(node_id, str):
            errors.append(f"Node #{idx} thiếu node_id.")
            continue
        
        node_ids.append(node_id)
        node_by_id[node_id] = node

        tasks = node.get("required_tasks")
        if not isinstance(tasks, list) or len(tasks) != 1 or not isinstance(tasks[0], str) or not tasks[0].strip():
            errors.append(f"{node_id}: required_tasks phải chứa đúng 1 nhiệm vụ không rỗng.")

        ending_type = node.get("ending_type")
        if ending_type not in {"none", "good", "bad"}:
            errors.append(f"{node_id}: ending_type phải là 'none', 'good' hoặc 'bad'.")
        elif _is_branch_node(node_id) and ending_type == "good":
            errors.append(
                f"{node_id}: nhánh xử lý không được kết thúc 'good'; nếu xử lý tốt phải trỏ về node chính tiếp theo của quy trình."
            )

        resolutions = node.get("possible_resolutions")
        if not isinstance(resolutions, list):
            errors.append(f"{node_id}: possible_resolutions phải là mảng.")
            continue

        if ending_type in {"good", "bad"} and resolutions:
            errors.append(f"{node_id}: node kết thúc phải có possible_resolutions rỗng.")
        if node_id == "Root" and ending_type != "none":
            errors.append("Node Root không được phép kết thúc ngay.")
        if ending_type == "none" and not resolutions:
            errors.append(f"{node_id}: node chưa kết thúc phải có ít nhất 1 hướng xử lý.")

        for ridx, resolution in enumerate(resolutions):
            if not isinstance(resolution, dict):
                errors.append(f"{node_id}: resolution #{ridx} không phải object.")
                continue
            user_action = resolution.get("user_action")
            next_node_id = resolution.get("next_node_id")
            if not isinstance(user_action, str) or not user_action.strip():
                errors.append(f"{node_id}: resolution #{ridx} thiếu user_action.")
            elif _is_guest_action(user_action):
                errors.append(f"{node_id}: user_action phải là hành động của lễ tân, không phải của khách: {user_action!r}.")
            if not isinstance(next_node_id, str) or not next_node_id.strip():
                errors.append(f"{node_id}: resolution #{ridx} thiếu next_node_id.")
            else:
                adjacency.setdefault(node_id, []).append(next_node_id)

    id_set = set(node_ids)
    if len(id_set) != len(node_ids):
        errors.append("Có node_id bị trùng.")
    if root_id not in id_set:
        errors.append(f"Thiếu node gốc bắt buộc: {root_id}.")

    for node in tree:
        if not isinstance(node, dict):
            continue
        node_id = node.get("node_id")
        for resolution in node.get("possible_resolutions", []) if isinstance(node.get("possible_resolutions"), list) else []:
            next_node_id = resolution.get("next_node_id") if isinstance(resolution, dict) else None
            if isinstance(next_node_id, str) and next_node_id not in id_set:
                errors.append(f"{node_id}: next_node_id trỏ tới node không tồn tại: {next_node_id}.")

    main_good_terminals = [
        node_id
        for node_id, node in node_by_id.items()
        if node.get("ending_type") == "good" and not _is_branch_node(node_id)
    ]
    if not main_good_terminals:
        errors.append("Thiếu node kết thúc tốt trên trục chính của quy trình.")

    def has_recovery_path_to_main(branch_node_id: str) -> bool:
        branch_parts = _node_path_parts(branch_node_id)
        if len(branch_parts) <= 1:
            return True
        branch_base_step = branch_parts[0]
        seen = set()
        stack = list(adjacency.get(branch_node_id, []))
        while stack:
            next_id = stack.pop()
            if next_id in seen:
                continue
            seen.add(next_id)
            next_parts = _node_path_parts(next_id)
            if len(next_parts) == 1 and next_parts[0] > branch_base_step:
                return True
            next_node = node_by_id.get(next_id)
            if not next_node or next_node.get("ending_type") in {"good", "bad"}:
                continue
            stack.extend(adjacency.get(next_id, []))
        return False

    for node_id, node in node_by_id.items():
        if _is_branch_node(node_id) and node.get("ending_type") == "none":
            if not has_recovery_path_to_main(node_id):
                errors.append(
                    f"{node_id}: nhánh chưa kết thúc phải có ít nhất một đường xử lý tốt quay về node chính phía sau."
                )

    return not errors, errors

def extract_text_from_pdf(pdf_path: str) -> str:
    try:
        import pypdf
        reader = pypdf.PdfReader(pdf_path)
        text = ""
        for page in reader.pages:
            t = page.extract_text()
            if t:
                text += t + "\n"
        return text
    except Exception as e:
        print(f"Error reading PDF {pdf_path}: {e}")
        return ""

def parse_operations(text: str) -> List[str]:
    if not llm: return []
    print("Agent Parser is extracting operations from PDF text...")
    
    # We pass the first 15000 characters to avoid context limit if PDF is huge
    chunk = text[:15000]
    try:
        resp = llm.invoke([
            SystemMessage(content=DOCUMENT_PARSER_PROMPT),
            HumanMessage(content=f"Extract exactly 2 operations from this text:\n{chunk}")
        ])
        
        clean_text = resp.content.replace("```json", "").replace("```", "").strip()
        operations = json.loads(clean_text)
        if isinstance(operations, list):
            return operations
    except Exception as e:
        print(f"Lỗi khi trích xuất nghiệp vụ: {e}")
    return []

def generate_single_tree_skeleton(operation: str, theme: str, root_index: int, context: str = "") -> List[Dict[str, Any]]:
    if not llm: return []
    print(f"\n--- [Agent {root_index}] Planning Tree for Theme: {theme} ---")
    
    max_retries = 3
    current_skeleton_text = ""
    
    for attempt in range(max_retries):
        print(f"[Agent {root_index}] Attempt {attempt + 1}...")
        
        prompt = f"Nghiệp vụ: {operation}\n"
        if context:
            prompt += f"Quy trình chuẩn / Ngữ cảnh tham chiếu:\n{context}\n\n"
        if attempt > 0:
            prompt += f"Feedback từ Critique:\n{current_skeleton_text}\nHãy sửa lại Tree Skeleton theo đúng JSON mảng."
            
        planner_resp = llm.invoke([
            SystemMessage(content=EXPERT_PLANNER_PROMPT.replace("{theme}", theme).replace("{root_index}", str(root_index))),
            HumanMessage(content=prompt)
        ])
        
        skeleton_text = planner_resp.content.replace("```json", "").replace("```", "").strip()
        
        print(f"[Agent {root_index}] Critique is evaluating...")
        critique_resp = llm.invoke([
            SystemMessage(content=EXPERT_CRITIQUE_PROMPT),
            HumanMessage(content=f"Đánh giá Tree Skeleton sau:\n{skeleton_text}")
        ])
        
        critique_feedback = critique_resp.content.strip()
        
        if critique_feedback.lower() in {"approve", "chấp thuận"}:
            print(f"[Agent {root_index}] Critique Approved the Tree Skeleton!")
            try:
                skeleton_json = _extract_json_array(skeleton_text)
                is_valid, validation_errors = validate_tree_skeleton(skeleton_json, root_index)
                if is_valid:
                    return skeleton_json
                print(f"[Agent {root_index}] Local validation rejected the Tree Skeleton:")
                for error in validation_errors:
                    print(f"  - {error}")
                current_skeleton_text = (
                    "Tree Skeleton bị lỗi validation cục bộ:\n"
                    + "\n".join(f"- {error}" for error in validation_errors)
                    + "\nHãy sửa lại và trả về ĐÚNG một JSON Array đầy đủ."
                )
            except Exception as e:
                print(f"[Agent {root_index}] Error parsing approved JSON: {e}")
                current_skeleton_text = "JSON bạn trả về bị lỗi cú pháp. Hãy trả về ĐÚNG chuẩn JSON Array."
        else:
            print(f"[Agent {root_index}] Critique Feedback: {critique_feedback}")
            current_skeleton_text = critique_feedback
            
    print(f"[Agent {root_index}] Failed to get an approved skeleton after max retries.")
    return []

def generate_full_operation(operation_name: str, context: str = "") -> List[Dict[str, Any]]:
    THEMES = [
        "Khách quên mang theo giấy tờ tùy thân (CCCD/Hộ chiếu), cố gắng nài nỉ lễ tân linh động hoặc tỏ ra bất hợp tác."
    ]
    
    import concurrent.futures
    all_nodes = []
    
    print(f"\n🚀 Khởi chạy Agent cho nghiệp vụ: {operation_name}...")
    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as executor:
        futures = []
        for i, theme in enumerate(THEMES):
            root_index = i + 1
            # We want to process each agent's tree entirely inside the thread so it writes to its own folder
            def process_agent(op_name, th, r_index, ctx):
                tree = generate_single_tree_skeleton(op_name, th, r_index, ctx)
                if tree:
                    expand_and_save_nodes(tree, op_name, r_index)
                return tree
            
            futures.append(executor.submit(process_agent, operation_name, theme, root_index, context))
            
        for future in concurrent.futures.as_completed(futures):
            try:
                tree = future.result()
                if tree:
                    all_nodes.extend(tree)
            except Exception as e:
                print(f"Lỗi khi chạy Agent: {e}")
                
    print(f"✅ Đã chạy xong kịch bản. Tổng cộng {len(all_nodes)} nodes được sinh ra.")
    return all_nodes

def expand_and_save_nodes(tree_skeleton: List[Dict[str, Any]], operation_name: str, agent_index: int = 1):
    if not llm or not tree_skeleton: return
    is_valid, validation_errors = validate_tree_skeleton(tree_skeleton, agent_index)
    if not is_valid:
        print(f"[Agent {agent_index}] Không expand vì Tree Skeleton không hợp lệ:")
        for error in validation_errors:
            print(f"  - {error}")
        return
    
    import unicodedata
    
    # Tạo slug cho tên thư mục (bỏ dấu tiếng Việt, thay khoảng trắng bằng _)
    clean_name = unicodedata.normalize('NFKD', operation_name).encode('ASCII', 'ignore').decode('utf-8')
    slug = re.sub(r'[^a-zA-Z0-9]+', '_', clean_name.lower().strip())
    slug = slug.strip('_') # Xóa gạch dưới ở hai đầu nếu có
    
    out_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "..", "data", slug, f"scenario_{agent_index}")
    os.makedirs(out_dir, exist_ok=True)
    
    print(f"\n--- [Phase 2 - Agent {agent_index}] Expanding Nodes for {operation_name} ---")
    print(f"Saving to {out_dir}")
    
    for node in tree_skeleton:
        node_id = node.get("node_id", f"tod_{uuid.uuid4().hex[:8]}")
        print(f"Refining Node: {node_id}")
        
        node_context = json.dumps(node, ensure_ascii=False)
        
        try:
            refiner_resp = llm.invoke([
                SystemMessage(content=NODE_REFINER_PROMPT),
                HumanMessage(content=f"Root Operation: {operation_name}\nNode Context:\n{node_context}")
            ])
            
            final_json_text = refiner_resp.content.replace("```json", "").replace("```", "").strip()
            
            # Find the actual JSON object
            json_match = re.search(r'\{.*\}', final_json_text, re.DOTALL)
            if json_match:
                final_json_text = json_match.group(0)
                
            final_data = json.loads(final_json_text)
            
            # Keep routing-critical fields deterministic; the refiner may paraphrase them incorrectly.
            final_data["_id"] = node_id
            agent_init = final_data.setdefault("agent_init", {})
            agent_init["required_tasks"] = node.get("required_tasks", [])
            agent_init["passing_threshold"] = node.get("passing_threshold", 1.0)
            agent_init["ending_type"] = node.get("ending_type", "none")
            final_data["transitions"] = {
                "possible_resolutions": [
                    {
                        "user_action": res.get("user_action", ""),
                        "next_case_id": res.get("next_node_id", "")
                    }
                    for res in node.get("possible_resolutions", [])
                ]
            }
            
            file_path = os.path.join(out_dir, f"{node_id}.json")
            with open(file_path, "w", encoding="utf-8") as f:
                json.dump(final_data, f, ensure_ascii=False, indent=2)
            print(f"  -> Saved {file_path}")
            
        except Exception as e:
            print(f"  -> Error expanding node {node_id}: {e}")

if __name__ == "__main__":
    pdf_file = "NGHIỆP VỤ ĐĂNG KÝ KHÁCH SẠN CHO KHÁCH LẺ.pdf"
    
    # Đảm bảo đường dẫn tuyệt đối hoặc tương đối đúng với workspace
    base_dir = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
    pdf_path = os.path.join(base_dir, pdf_file)
    
    if not os.path.exists(pdf_path):
        print(f"Không tìm thấy file PDF: {pdf_path}")
    else:
        print(f"Đọc file PDF: {pdf_path}")
        text = extract_text_from_pdf(pdf_path)
        if text:
            operations = parse_operations(text)
            print(f"Tìm thấy các nghiệp vụ: {operations}")
            
            for op in operations:
                combined_skeleton = generate_full_operation(op)
                # Note: expand_and_save_nodes is now called internally per agent
