# 🚀 CaseStudy Backend: Overview

Chào mừng bạn đến với hệ thống Backend của **CaseStudy Engine**. Đây là trung tâm điều phối xử lý xác thực (Authentication), quản lý kịch bản (Case Management) và mô phỏng AI đa tác nhân (Multi-Agent Simulation).

---

## 🛠 Tech Stack
- **Framework:** FastAPI, LangGraph
- **Database:** MongoDB Atlas (Async Motor driver)
- **Security:** JWT & Bcrypt
- **Scenario:** Hotel Domain (Mr. Viktor - VIP Guest)

---

## 🔑 Hệ thống Cổng (Ports)

Hệ thống được chia làm hai phần chính để tối ưu hóa hiệu năng:

1. **Backend Auth & CMS (Port 8001)**: Quản lý người dùng, đăng nhập và quản lý kho tài sản (Assets).
2. **Agent API (Port 9000)**: Xử lý logic LangGraph, WebSocket tương tác thời gian thực và mô phỏng NPC.

---

## 🏗 Case & State Management

Dữ liệu kịch bản được lưu trữ dưới dạng JSON linh hoạt, hỗ trợ Unity MCP (Environment & Persona). Chi tiết cấu trúc xem tại: [STATE_SPEC.md](./STATE_SPEC.md).

---

## ⚡ Cách chạy Backend

### 1. Cấu hình Môi trường
Tạo file `.env` tại thư mục `back-end/` với các thông tin sau:
```env
MONGO_URI=mongodb+srv://...
JWT_SECRET=your_jwt_secret_key
OPENAI_API_KEY=sk-...
ELEVENLABS_API_KEY=...
```

### 2. Cài đặt và Chạy
```bash
cd back-end
# Cài đặt thư viện
uv sync

# Chạy Backend Auth (Port 8001)
python api_casestudy/main.py

# Chạy Agent API (Port 9000 - nếu đã triển khai)
# python api_agent/main.py
```

---

## 🧪 Tài liệu API Tự động (Swagger UI)
Sau khi chạy Server, bạn có thể truy cập:
👉 `http://localhost:8001/docs` — API Xác thực và CMS.
👉 `http://localhost:9000/docs` — API Mô phỏng Agent.
