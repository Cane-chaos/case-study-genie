# 🚀 CaseStudy Agent API: Overview

Welcome to the **CaseStudy Engine** Backend system. This is the central hub for coordinating authentication, case management, and realistic AI simulation.

---

## 🛠 Tech Stack
- **Framework:** FastAPI
- **Database:** MongoDB Atlas (Async Motor driver)
- **Security:** JWT (JSON Web Tokens) & Bcrypt
- **Runtime:** Python 3.12+

---

## 🔑 Authentication

This is the sole entry point for connecting Unity to the Backend. The system uses JWT to secure sessions.

### 1. Register
- **Endpoint:** `POST /api/register`
- **Body:**
  ```json
  {
    "email": "user@example.com",
    "password": "yourpassword"
  }
  ```
- **Note:** Passwords are encrypted with Bcrypt before being stored in MongoDB. User data is stored in the `member` collection.

### 2. Login
- **Endpoint:** `POST /api/login`
- **Body:** Same as registration.
- **Response:**
  ```json
  {
    "access_token": "eyJhbG...",
    "token_type": "bearer"
  }
  ```
- **Note:** This token is valid for 30 days so users do not need to log in repeatedly on Unity.

---

## 🏗 Case & State Management

The system supports storing entire simulation cases in flexible JSON format. For details on this data structure, please refer to: [STATE_SPEC.md](./STATE_SPEC.md).

---

## ⚡ How to Run the Backend

### 1. Environment Configuration
Create a `.env` file in the root directory with the following:
```env
MONGO_URI=mongodb+srv://...
JWT_SECRET=your_jwt_secret_key
OPENAI_API_KEY=sk-...
```

### 2. Installation and Running
```bash
# Install dependencies
uv sync

# Run server
python api_casestudy/main.py
```
The server will run by default at: `http://localhost:8001`

---

## 🧪 Auto-Generated API Documentation (Swagger UI)
After starting the server, you can access:
👉 `http://localhost:8001/docs` to view and test all APIs directly in the browser.
