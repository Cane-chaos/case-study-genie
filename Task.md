# 📋 Bảng theo dõi Nhiệm vụ (Task Tracker)

Dự án đang trong quá trình tái cấu trúc để hỗ trợ **Real-time Unity Lip-sync, Multi-threading, Unity MCP Environment, và domain Khách sạn (1 Persona)**.

| ID | Nhiệm vụ | Trạng thái | Ghi chú |
| :--- | :--- | :--- | :--- |
| 0 | **Thiết kế State và Kiến trúc Hệ thống cốt lõi** | ✅ Đã giải quyết | Đã thiết kế xong `AgentState` và `SKELETON_SCHEMA` cho kéo thả. |
| 1 | **Tích hợp Unity MCP (Environment/Context)** | 🔄 Đang giải quyết | Đang xây dựng cấu trúc library MCP cho Env và Persona để Frontend có thể kéo thả. |

| **2** | **Thiết kế Node Multi-thread / Async** | ❌ Chưa giải quyết | Chờ triển khai luồng chạy song song (Parallel) trên LangGraph để Persona Node không bị block bởi Policy/Action Node. |
| **3** | **Domain Khách sạn & Single Persona** | ❌ Chưa giải quyết | Cần cập nhật `hotel_state` và tạo dữ liệu tĩnh cho 1 Persona Lễ tân duy nhất, loại bỏ query VectorDB không cần thiết. |
