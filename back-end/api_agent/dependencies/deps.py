from fastapi import Depends
from api_agent.services.state_loader import StateLoader

# Dependency để lấy instance của StateLoader (singleton pattern cơ bản)
# Trong tương lai có thể khởi tạo MongoDB connection ở đây rồi truyền vào StateLoader
_state_loader_instance = StateLoader()

def get_state_loader() -> StateLoader:
    return _state_loader_instance
