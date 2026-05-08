import json
import os

class StateLoader:
    def __init__(self, db_collection=None):
        # db_collection would be the MongoDB collection instance
        self.db_collection = db_collection
        
    def fetch_scenario_node(self, scenario_id: str, node_id: str = "Root"):
        """Fetch the node JSON from the folder: data/<scenario_id>/<node_id>.json"""
        if self.db_collection is not None:
            # return self.db_collection.find_one({"scenario_id": scenario_id, "_id": node_id})
            pass
            
        data_dir = os.path.abspath(os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "data"))
        if not os.path.exists(data_dir):
            return None
            
        # Construct path: data/check_in/easy/Root.json
        filepath = os.path.join(data_dir, scenario_id, f"{node_id}.json")
        if not os.path.exists(filepath):
            print(f"File not found: {filepath}")
            return None
            
        try:
            with open(filepath, "r", encoding="utf-8") as f:
                data = json.load(f)
                return data
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
            return None

    def get_unity_init(self, case_data: dict) -> dict:
        """Extract and format data for Unity."""
        if not case_data:
            return {}
            
        return {
            "case_id": case_data.get("_id"),
            "root_operation": case_data.get("root_operation"),
            "agent_init": case_data.get("agent_init") # Return agent_init instead since unity_init is removed
        }
        
    def get_agent_init(self, case_data: dict) -> dict:
        """Extract and format data for LangGraph Agent initialization."""
        if not case_data or "agent_init" not in case_data:
            return {}
            
        return {
            "case_id": case_data.get("_id"),
            "transitions": case_data.get("transitions", {}),
            "context": case_data.get("agent_init")
        }

    def get_full_scenario_context(self, scenario_id: str) -> str:
        """Đọc toàn bộ các file JSON trong thư mục kịch bản và trích xuất tất cả required_tasks để làm context cho Scoring."""
        data_dir = os.path.abspath(os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "data"))
        scenario_dir = os.path.join(data_dir, scenario_id)
        if not os.path.exists(scenario_dir):
            return "Không tìm thấy dữ liệu kịch bản."

        all_tasks = []
        try:
            for filename in os.listdir(scenario_dir):
                if filename.endswith(".json"):
                    filepath = os.path.join(scenario_dir, filename)
                    with open(filepath, "r", encoding="utf-8") as f:
                        data = json.load(f)
                        node_id = data.get("_id", filename)
                        agent_init = data.get("agent_init", {})
                        tasks = agent_init.get("required_tasks", [])
                        if tasks:
                            all_tasks.append(f"Tại {node_id}: {', '.join(tasks)}")
            if all_tasks:
                return "\n".join(all_tasks)
            return "Không có nhiệm vụ cụ thể nào được cấu hình."
        except Exception as e:
            print(f"Lỗi khi đọc toàn bộ kịch bản {scenario_id}: {e}")
            return "Lỗi khi đọc dữ liệu kịch bản."

# Example usage
if __name__ == "__main__":
    loader = StateLoader()
    # Try fetching a random generated case if available
    case = loader.fetch_scenario_node("check_in/scenario_1", "Root")
    if case:
        print("Unity Payload:", json.dumps(loader.get_unity_init(case), indent=2))
        print("Agent Payload:", json.dumps(loader.get_agent_init(case), indent=2))
    else:
        print("No cases found. Run tod_generator.py first.")
