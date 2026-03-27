# 📄 CaseStudy Engine: State Specification (v1.1)

Tài liệu này định nghĩa cấu trúc dữ liệu chung (Schema) để trao đổi thông tin giữa **Unity (Frontend)** và **LangGraph Agent (Backend)**. Đây là "Hợp đồng" duy nhất để cả hai phía có thể làm việc song song mà không bị lệch dữ liệu.

---

## 1. Cấu trúc State Tổng thể (JSON)

Cấu trúc này được dùng để lưu trữ vào MongoDB và dùng làm Payload gửi qua API.

```json
{
  "case_metadata": {
    "case_id": "hotel_reception_001",
    "title": "Xử lý khách hàng VIP giận dữ",
    "difficulty": "Medium",
    "category": "Hospitality"
  },

  "environment": {
    "scene_id": "env_hotel_lobby_luxury",
    "smart_objects": [
      { 
        "id": "obj_ticket_001", 
        "name": "Lá phiếu đặt phòng", 
        "position": [1.2, 0.8, -0.5], 
        "is_required": true 
      },
      { 
        "id": "obj_phone", 
        "name": "Điện thoại bàn", 
        "position": [0.5, 0.8, 0.0], 
        "is_required": false 
      }
    ]
  },

  "persona": {
    "name": "Mr. Viktor",
    "prefab_id": "char_victor_v3",
    "traits": ["Nóng tính", "Coi trọng thời gian", "Thích sự chuyên nghiệp"],
    "voice_style": "Trầm, dứt khoát",
    "initial_trust": 40,
    "goals": ["Được check-in ngay lập tức", "Nhận được lời xin lỗi chân thành"]
  },

  "knowledge_base": {
    "context": "Sảnh khách sạn đang giờ cao điểm, điều hòa đang bị hỏng nhẹ.",
    "policies": [
      { 
        "id": "pol_vip_01", 
        "title": "Quy trình đón khách VIP", 
        "content": "Luôn phải kiểm tra thẻ VIP trước khi chào hỏi..." 
      }
    ]
  },

  "reward_system": {
    "rules": [
      { 
        "trigger": "user_read:obj_ticket_001", 
        "reward": 10, 
        "penalty": -5, 
        "explanation": "Kiểm tra thông tin khách hàng." 
      }
    ],
    "passing_score": 70
  },

  "runtime_state": {
    "current_score": 0,
    "current_event": "event_ingress",
    "history": [],
    "interacted_objects": [],
    "active_memory": "Viktor vừa bước vào và trông rất khó chịu."
  }
}
```

---

## 2. Giải thích các Thành phần Chính

### 🏗️ Environment (Môi trường)
*   **scene_id**: Xác định Unity Scene nào sẽ được nạp.
*   **smart_objects**: Danh sách các vật thể có thể tương tác. Mỗi vật thể có `id` duy nhất để Backend chấm điểm khi người dùng click vào.

### 🎭 Persona (Nhân vật)
*   **traits**: Các từ khóa để Agent (Python) điều chỉnh phong cách trả lời.
*   **initial_trust**: Chỉ số tin tưởng ban đầu, sẽ thay đổi tùy theo cách ứng xử của người dùng.

### 📚 Knowledge Base (Kho tri thức)
*   **policies**: Các quy tắc nghiệp vụ (Y tế, Lễ tân...). Agent sẽ dùng nội dung này để trích dẫn (Citation) và làm căn cứ để trừ điểm nếu học viên làm sai.

### 🏆 Reward System (Hệ thống chấm điểm)
*   Định nghĩa các `trigger` (Hành động kích hoạt) và số điểm cộng/trừ tương ứng.
*   `explanation`: Nội dung này sẽ được hiển thị cho học viên ở trang Tổng kết sau khi kết thúc Simulation.

### 🔄 Runtime State (Trạng thái thực thi)
*   Chứa lịch sử hội thoại (`history`) và các vật thể đã tương tác (`interacted_objects`). Đây là phần dữ liệu thay đổi liên tục sau mỗi lượt chat.

---

## 3. Quy trình làm việc (Multi-Agent Workflow)

Để xử lý State này, Backend sẽ sử dụng 4 Agent chuyên biệt:
1.  **Interpreter Agent**: Phân tích Input từ Unity (Text + Cú click chuột).
2.  **Persona Agent**: Đóng vai nhân vật dựa trên `traits` để phản hồi.
3.  **Chronicler Agent**: Tóm tắt diễn biến vào `active_memory`.
4.  **Judge & Reward Agent**: Đối chiếu hành động của người dùng với `reward_system.rules` để tính điểm.

---

## 4. Lưu ý quan trọng
*   Tất cả các `ID` phải là duy nhất (Unique) trong phạm vi 1 Case.
*   Dữ liệu tọa độ `position` tuân theo hệ trục tọa độ của Unity (Vector3).
