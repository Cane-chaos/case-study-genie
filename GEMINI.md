# 🚀 CaseStudy Agent API: Overview

Chào mừng đến với hệ thống Backend của **CaseStudy Engine**. Đây là trung tâm điều phối xác thực, quản lý case và mô phỏng AI đa tác nhân (Multi-Agent).

---

## 🛠 Tech Stack
- **Framework:** FastAPI, LangGraph
- **Database:** MongoDB Atlas (Async Motor driver)
- **Security:** JWT & Bcrypt
- **Scenario:** Hotel Domain (Mr. Viktor - VIP Guest)

---

## 🔑 Cấu hình Cổng (Ports)

Hệ thống chia làm hai thành phần chính:

1. **Backend Auth & CMS (Port 8001)**: Xử lý đăng ký, đăng nhập và quản lý tài sản (Assets).
2. **Agent API (Port 9000)**: Xử lý logic mô phỏng thời gian thực, WebSocket và LangGraph Multi-Agent.

---

## 🏗 Case & State Management

Dữ liệu mô phỏng được định nghĩa theo cấu trúc JSON linh hoạt, hỗ trợ Unity MCP.
Chi tiết xem tại: [STATE_SPEC.md](./back-end/STATE_SPEC.md).

---

## ⚡ Cách Chạy Backend

### 1. Cấu hình Môi trường
Tạo file `.env` trong thư mục `back-end/` với các biến sau:
```env
MONGO_URI=mongodb+srv://...
JWT_SECRET=your_jwt_secret_key
OPENAI_API_KEY=sk-...
ELEVENLABS_API_KEY=...
```

### 2. Cài đặt và Khởi chạy
```bash
cd back-end
# Cài đặt dependencies (sử dụng uv)
uv sync

# Chạy Backend Auth (Port 8001)
python api_casestudy/main.py

# Chạy Agent API (Port 9000 - nếu đã triển khai)
# python api_agent/main.py
```

---

## 🧪 Tài liệu API (Swagger UI)
Sau khi chạy server, truy cập:
👉 `http://localhost:8001/docs` để xem và test các API xác thực.
👉 `http://localhost:9000/docs` để xem các API mô phỏng (Agent).

---

## 📘 Tài liệu Kiến trúc
Để hiểu sâu hơn về luồng dữ liệu và Multi-Agent workflow, tham khảo:
[ARCHITECTURE.md](./back-end/ARCHITECTURE.md)
