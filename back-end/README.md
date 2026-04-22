# 🚀 CaseStudy Agent API: Overview

Chào mừng bạn đến với hệ thống Backend của **CaseStudy Engine**. Đây là trung tâm điều phối xử lý xác thực (Authentication), quản lý kịch bản (Case Management) và mô phỏng thực tế (AI Simulation).

---

## 🛠 Tech Stack
- **Framework:** FastAPI
- **Database:** MongoDB Atlas (Async Motor driver)
- **Security:** JWT (JSON Web Tokens) & Bcrypt
- **Runtime:** Python 3.12+

---

## 🔑 Authentication (Hệ thống Xác thực)

Đây là cổng vào duy nhất để kết nối giữa Unity và Backend. Hệ thống sử dụng JWT để bảo mật các phiên làm việc.

### 1. Đăng ký tài khoản (Register)
- **Endpoint:** `POST /api/register`
- **Body:**
  ```json
  {
    "email": "user@example.com",
    "password": "yourpassword"
  }
  ```
- **Lưu ý:** Mật khẩu sẽ được mã hóa Bcrypt trước khi lưu vào MongoDB. Dữ liệu người dùng được lưu trong Collection `member`.

### 2. Đăng nhập (Login)
- **Endpoint:** `POST /api/login`
- **Body:** Giống như đăng ký.
- **Response:**
  ```json
  {
    "access_token": "eyJhbG...",
    "token_type": "bearer"
  }
  ```
- **Lưu ý:** Token này sẽ có giá trị trong 30 ngày để người dùng không phải đăng nhập lại nhiều lần trên Unity.

---

## 🏗 Case & State Management

Hệ thống hỗ trợ lưu trữ toàn bộ kịch bản mô phỏng dưới dạng JSON linh hoạt. Chi tiết về cấu trúc dữ liệu này, vui lòng xem tại file: [STATE_SPEC.md](./STATE_SPEC.md).

---

## ⚡ Cách chạy Backend

### 1. Cấu hình Môi trường
Tạo file `.env` tại thư mục gốc với các thông tin sau:
```env
MONGO_URI=mongodb+srv://...
JWT_SECRET=your_jwt_secret_key
OPENAI_API_KEY=sk-...
```

### 2. Cài đặt và Chạy
```bash
# Cài đặt thư viện
uv sync

# Chạy server
python api_casestudy/main.py
```
Server sẽ mặc định chạy tại: `http://localhost:8001`

---

## 🧪 Tài liệu API Tự động (Swagger UI)
Sau khi chạy Server, bạn có thể truy cập trực tiếp vào:
👉 `http://localhost:8001/docs` để xem và test thử các API trực tiếp trên trình duyệt.
