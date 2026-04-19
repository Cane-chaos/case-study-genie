# 🗂️ CaseStudy Engine — Kế Hoạch Tổng Thể Dự Án

> **Cập nhật lần cuối:** 2026-04-19
> **Trạng thái hiện tại:** Đang phát triển tích cực · Nhánh `be` (Backend) đang được tái cấu trúc

---

## 1. Tổng Quan Dự Án

**CaseStudy Engine** là một hệ thống mô phỏng tình huống nghiệp vụ (case-based simulation) theo thời gian thực.
Học viên tương tác với nhân vật AI (Persona) trong môi trường 3D (Unity WebGL) thông qua giao diện web (React),
được hỗ trợ bởi một backend đa lớp gồm Express.js, FastAPI và LangGraph Agent.

### Mục tiêu cốt lõi
- Tạo ra các kịch bản thực hành nghiệp vụ (lễ tân, y tế, v.v.) một cách linh hoạt, có thể tái tạo bởi nhóm thiết kế.
- AI Agent đóng vai nhân vật có tính cách riêng biệt, phản hồi ngữ cảnh và chấm điểm hành vi học viên.
- Giao diện kéo thả (Designer) cho phép tạo kịch bản mới mà không cần viết code.

---

## 2. Kiến Trúc Hệ Thống

### 2.1 Sơ đồ tổng thể

```
┌──────────────────────────────────────────────────────────┐
│                  NGƯỜI DÙNG (Trình duyệt)                │
│        React + Vite (port 5173) + Tailwind CSS           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Unity WebGL (nhúng qua react-unity-webgl)          │ │
│  │  EnvironmentDesigner | PersonaDesigner              │ │
│  └───────────────────┬─────────────────────────────────┘ │
└──────────────────────┼───────────────────────────────────┘
                       │ REST / WebSocket
     ┌─────────────────┼────────────────────┐
     ▼                 ▼                    ▼
┌─────────┐    ┌──────────────┐    ┌──────────────────┐
│ Express │    │   FastAPI    │    │  Agent Server    │
│  :8000  │    │    :8001     │    │     :9000        │
│ Auth    │    │ Case Data    │    │ LangGraph        │
│ Session │    │ Asset Agent  │    │ Multi-Agent AI   │
│ MongoDB │    │ GPT-4o-mini  │    │ LangSmith Trace  │
└─────────┘    └──────────────┘    └──────────────────┘
     │                │
     └────────────────┘
            │
       ┌────▼────┐
       │ MongoDB │
       │  Atlas  │
       └─────────┘
```

### 2.2 Chi tiết từng service

| Port | Công nghệ | Trách nhiệm |
|:----:|-----------|-------------|
| **5173** | React + Vite | Frontend dev server — giao diện người dùng |
| **8000** | Express.js (Node) | Auth (login/register), lưu lịch sử session vào MongoDB |
| **8001** | FastAPI (Python) | Cung cấp Case data, Asset Design Agent (GPT-4o-mini) |
| **9000** | Agent Server | Điều phối phiên mô phỏng (LangGraph, trace bằng LangSmith) |

---

## 3. Phân Rã Chức Năng (Feature Breakdown)

### Module 1 — 🔑 Authentication (Xác thực)
- Đăng ký / đăng nhập qua email + mật khẩu
- JWT lưu vào `localStorage`, hạn 30 ngày
- `ProtectedRoute` trong React kiểm tra token trước khi vào trang bảo mật
- **Endpoint chính:** `POST /api/register`, `POST /api/login` (port 8001)

### Module 2 — 📋 Case List (Danh sách kịch bản)
- Hiển thị toàn bộ kịch bản khả dụng
- Fetch từ Express: `GET /api/cases` (port 8000)
- **Route:** `/case-list`

### Module 3 — 🎭 Case Runner (Mô phỏng)
- Giao diện chat tương tác với AI Persona trong môi trường 3D
- Nhận phản hồi AI kèm trạng thái cảm xúc / hành động nhân vật
- Chấm điểm tự động theo `reward_system.rules`
- **Luồng dữ liệu:**
  1. `GET :8001/api/cases/{caseId}` → nhận `{skeleton, personas, context}`
  2. `POST :9000/api/agent/sessions` → tạo session → nhận `sessionId`
  3. `POST :9000/api/agent/sessions/{sessionId}/turn` → gửi input → nhận state
  4. `POST :8000/api/sessions/history` → lưu lịch sử sau khi kết thúc
- **Route:** `/case-runner/:caseId`

### Module 4 — 🏗️ Asset Studio (Thư viện & Thiết kế)

| Sub-screen | Route | Chức năng |
|------------|-------|-----------|
| Asset Library | `/asset-studio` | Duyệt danh sách Env / Persona |
| Environment Designer | `/asset-studio/environment-create` | Kéo thả + chat AI để dựng môi trường 3D trong Unity WebGL |
| Persona Designer | `/asset-studio/persona-create` | Tạo nhân vật + nghe thử giọng nói |

- Unity giao tiếp qua `sendMessage("MCP_Manager", "ExecuteCommandFromWeb", jsonString)`
- Agent 8001 dịch ngôn ngữ tự nhiên → JSON action (`spawn_object`, `set_lighting`, `set_avatar`...)

### Module 5 — 🕐 History (Lịch sử)
- Xem lại toàn bộ transcript của một phiên mô phỏng đã hoàn thành
- **Route:** `/history/:sessionId`

### Module 6 — 👤 User Profile
- Thông tin tài khoản, cài đặt cá nhân
- **Route:** `/user`

### 🚧 Planned — Case Designer (Chưa xây dựng)
- Giao diện kéo thả dạng node-graph (dùng **ReactFlow**) để thiết kế kịch bản mới
- Route `/case-designer` đã tồn tại nhưng chỉ hiển thị placeholder "Coming Soon"

---

## 4. Mô Hình Dữ Liệu (Data Model)

### 4.1 Case State Schema (Chuẩn giao tiếp Unity ↔ Backend)

```json
{
  "case_metadata": { "case_id", "title", "difficulty", "category" },
  "environment": {
    "scene_id": "unity_scene_key",
    "smart_objects": [{ "id", "name", "position": [x,y,z], "is_required" }]
  },
  "persona": {
    "name", "prefab_id", "traits": [],
    "voice_style", "initial_trust",
    "goals": []
  },
  "knowledge_base": {
    "context": "...",
    "policies": [{ "id", "title", "content" }]
  },
  "reward_system": {
    "rules": [{ "trigger", "reward", "penalty", "explanation" }],
    "passing_score": 70
  },
  "runtime_state": {
    "current_score", "current_event",
    "history": [], "interacted_objects": [], "active_memory"
  }
}
```

### 4.2 Skeleton Schema (Đồ thị sự kiện)
- **Nodes:** `intro_message`, `learning_objective`, `citations`, `max_turns`, `evaluation_criteria`
- **Transitions:** `on_success` → node tiếp theo | `on_failure` → node thay thế

---

## 5. Kiến Trúc AI — Multi-Agent (LangGraph)

Mỗi lượt chat được xử lý qua pipeline 4 agent tuần tự (đang chuyển sang song song):

```
User Input
    │
    ▼
[Interpreter Agent]  — Phân tích hành động + click vật thể từ Unity
    │
    ▼
[Persona Agent]      — Đóng vai nhân vật (dựa trên traits), sinh câu trả lời
    │
    ▼
[Chronicler Agent]   — Tóm tắt diễn biến → cập nhật active_memory
    │
    ▼
[Judge & Reward Agent] — So sánh hành động với reward_system.rules, tính điểm
    │
    ▼
Response → Unity (animation/lip-sync) + React (chat UI + score)
```

---

## 6. Cấu Trúc Thư Mục

```
case-study-genie/
├── context/                    ← Tài liệu thiết kế & kế hoạch
│   ├── PROJECT_PLANNING.md     ← File này
│   ├── CASESTUDY_ENGINE_GUIDE.md
│   ├── FUNCTIONAL_MAP.md
│   ├── STATE_SPEC.md
│   └── README.md / README(1).md
│
├── front-end/                  ← Full-stack JS (React + Express)
│   ├── src/                    ← React (Vite)
│   │   ├── App.jsx             ← Router + ProtectedRoute
│   │   ├── Screen/
│   │   │   ├── Auth/           ← Home, Login, Register, ForgotPassword
│   │   │   └── App/            ← CaseList, CaseRunner, AssetStudio, ...
│   │   ├── components/         ← NavBar, SideBar, Footer, LanguageSwitcher
│   │   └── i18n.js             ← i18next (en + vi)
│   └── server/                 ← Express.js BFF
│       ├── index.js            ← Entry point (:8000)
│       ├── routes/             ← authRoutes, caseRoutes, sessionRoutes
│       ├── models/             ← Mongoose schemas
│       ├── middleware/         ← JWT auth
│       └── config/             ← MongoDB connection
│
├── api_casestudy/              ← FastAPI (Python) — :8001
│   ├── main.py
│   ├── routers/
│   ├── schemas/
│   ├── services/
│   ├── crud/
│   ├── db/
│   └── core/
│
├── CLAUDE.md                   ← Hướng dẫn cho AI coding assistant
├── GEMINI.md
├── FUNCTIONAL_MAP.md
├── STATE_SPEC.md
├── Task.md                     ← Task tracker hiện tại
├── main.py                     ← Entry point Python
└── pyproject.toml
```

---

## 7. Biến Môi Trường (Environment Variables)

Tạo file `front-end/.env`:

```env
# Express BFF (port 8000)
PORT=8000
MONGO_URI=<mongodb atlas connection string>
JWT_SECRET=<secret key>

# FastAPI + Asset Agent (port 8001)
OPENAI_API_KEY=<openai key>
OPENAI_MODEL=gpt-4o-mini

# LangSmith Tracing (port 9000)
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=<langsmith key>
LANGCHAIN_PROJECT=CaseStudy
```

---

## 8. Hướng Dẫn Khởi Động (Quick Start)

```bash
# 1. Backend Python (FastAPI :8001)
cd case-study-genie
uv sync
python api_casestudy/main.py

# 2. Frontend + Express BFF
cd front-end
npm install

# Terminal 1 — React Dev Server (:5173)
npm run dev

# Terminal 2 — Express BFF (:8000)
npm run start

# 3. Agent Server (:9000) — khởi động riêng theo cấu hình LangGraph
```

---

## 9. Phạm Vi Ưu Tiên Hiện Tại (Current Expected Scope)

> ⚠️ Dự án **không nhắm đến full system** ở giai đoạn này.
> Mục tiêu trước mắt là hoàn thành **một vertical slice có thể demo được**.

### 9.1 Demo Scenario

| Yếu tố | Nội dung |
|--------|----------|
| **Bối cảnh** | Sảnh khách sạn / quầy lễ tân |
| **Vai người chơi** | Nhân viên lễ tân (Receptionist) |
| **Vai AI** | Khách VIP có vấn đề (phòng chưa sẵn sàng, đặt phòng lỗi, dịch vụ không đạt kỳ vọng...) |
| **Mục tiêu demo** | Roleplay + quyết định xử lý tình huống + AI phân nhánh + chấm điểm |

### 9.2 Hai Chế Độ Gameplay

**Mode A — Guided Interaction (Tương tác vật thể):**
- Người chơi click vào các vật thể 3D trong môi trường
- Mỗi vật thể = một hướng xử lý / quyết định nghiệp vụ
- Hoạt động như hệ thống trắc nghiệm ẩn bên trong mô phỏng 3D

**Mode B — Free Response (Hội thoại tự do):**
- Người chơi tự gõ phản hồi với khách hàng AI
- AI đánh giá câu trả lời và tiếp tục kịch bản động
- Chấm điểm theo rubric: tính chuyên nghiệp, sự đồng cảm, độ chính xác, rõ ràng

### 9.3 Smart Objects — Vật Thể Có Nghĩa Nghiệp Vụ

| Vật thể | Intent / Hành động |
|---------|-------------------|
| Phiếu đặt phòng | `verify_customer_info` |
| Máy tính quầy lễ tân | `check_room_status` |
| Điện thoại bàn | `call_manager` / `call_housekeeping` |
| Sách chính sách VIP | `follow_vip_policy` |
| Voucher / bồi thường | `offer_compensation` |
| Thẻ phòng | `reassign_room` |

### 9.4 Trạng Thái AI Khách Hàng (Branching)

Hệ thống theo dõi trạng thái khách theo từng hành động của người chơi:
- **Cảm xúc:** `angry` → `impatient` → `neutral` → `calmer` → `satisfied`
- **Tình huống:** `not_understood` → `verified` → `resolving` → `escalated` → `solved` / `mishandled`

### 9.5 Module Ưu Tiên Hiện Tại

| Ưu tiên | Module |
|---------|--------|
| 🔴 **Cao** | Simulator (UI + logic) |
| 🔴 **Cao** | AI Backend / State Handling |
| 🔴 **Cao** | 3D Environment & Object Interaction |
| 🟡 **Thấp** | Authentication (có thể đơn giản hóa) |
| ⚪ **Bỏ qua** | Asset Library, Full Case Designer, Multi-map |

> **Nguyên tắc:** Ưu tiên **chiều sâu trong 1 kịch bản**, không dàn trải sang nhiều tính năng.

---

## 10. Trạng Thái Hiện Tại & Roadmap

### ✅ Đã hoàn thành
- Thiết kế `AgentState` và `SKELETON_SCHEMA` tổng thể
- Hệ thống Auth (JWT, bcrypt, MongoDB)
- Case Runner với LangGraph multi-agent cơ bản
- Asset Studio (Library + Environment Designer + Persona Designer)
- Lịch sử phiên mô phỏng (HistoryDetail)
- Giao tiếp Unity WebGL ↔ Web qua MCP bridge

### 🔄 Đang thực hiện
| ID | Nhiệm vụ | Ghi chú |
|----|----------|---------|
| 1 | **Tích hợp Unity MCP** (Env/Context) | Xây dựng cấu trúc thư viện MCP để FE hỗ trợ kéo thả |

### ❌ Chưa bắt đầu
| ID | Nhiệm vụ | Phụ thuộc |
|----|----------|-----------|
| 2 | **Multi-thread / Async LangGraph Node** | Cần Parallel execution — Persona Node không bị block bởi Policy/Action Node |
| 3 | **Hotel Domain + Single Persona** | Cập nhật `hotel_state`, dữ liệu tĩnh cho 1 Persona Receptionist, bỏ VectorDB query |
| 4 | **Case Designer (ReactFlow)** | Route đã có, UI chưa xây dựng |

---

## 10. Quy Ước Git & Làm Việc Nhóm

- **Nhánh chính:** `main` — chỉ merge qua Pull Request (Lead duyệt)
- **Quy tắc đặt tên nhánh:** `feature/Task_<TênTask>` (vd: `feature/Task_Auth`)
- **Commit message:** Rõ ràng, mô tả hành động (vd: `"Hoàn thành UI Login và kết nối API Auth"`)
- **Quy trình:** `checkout -b` → code → `git pull origin main` → `push` → Pull Request
- **Không tự sửa Core:** Chỉ Lead được chỉnh sửa `SimulationManager`, `EventManager`, `UIManager`

---

## 11. Thư Viện Chính

| Thư viện | Phạm vi | Mục đích |
|----------|---------|---------|
| `react-unity-webgl` | Frontend | Nhúng Unity WebGL + gửi lệnh JS→Unity |
| `reactflow` | Frontend | Planned — visual case designer |
| `i18next` / `react-i18next` | Frontend | Đa ngôn ngữ (en + vi) |
| `Tailwind CSS` | Frontend | Styling (primary: `#1EA97C`) |
| `react-pro-sidebar` | Frontend | Sidebar navigation |
| `Mongoose` | Express | MongoDB ODM |
| `LangGraph` | Agent | Điều phối multi-agent workflow |
| `LangSmith` | Agent | Tracing & monitoring LLM calls |
| `FastAPI` | Python BE | REST API + async |
| `motor` | Python BE | Async MongoDB driver |

---

*Tài liệu này được tổng hợp tự động từ toàn bộ nội dung folder `context/`. Cập nhật thủ công khi có thay đổi kiến trúc lớn.*
