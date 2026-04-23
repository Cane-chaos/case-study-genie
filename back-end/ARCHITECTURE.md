# CDIO — Case-Driven Interactive Operation
## Architecture Document v1.1 | 2026-04-23

---

## MỤC LỤC
1. [Tổng Quan Hệ Thống](#1-tổng-quan-hệ-thống)
2. [Sơ Đồ Kiến Trúc Tổng Quan](#2-sơ-đồ-kiến-trúc-tổng-quan)
3. [Unified Data Schema (MongoDB)](#3-unified-data-schema-mongodb)
4. [Multi-Agent Design (Agent API)](#4-multi-agent-design-agent-api)
5. [Communication Protocol (Unity ↔ Agent API)](#5-communication-protocol-unity--agent-api)
6. [Simulation Flow Chi Tiết](#6-simulation-flow-chi-tiết)
7. [Task Roadmap](#7-task-roadmap)

---

## 1. Tổng Quan Hệ Thống

### Ngôn ngữ & framework
| Phần | Ngôn ngữ | Framework | Port |
|------|----------|-----------|------|
| Unity App (UI + 3D) | C# | Unity 6 + UI Toolkit | — |
| Agent API (Core Logic) | Python | LangChain/LangGraph + FastAPI | 9000 |
| Backend API (Auth/CMS) | Python | FastAPI | 8001 |
| Database | — | MongoDB Atlas | 27017 |
| Realtime | — | WebSocket | ws://localhost:9000 |

### Demo scenario
**Hotel Domain**: Người dùng đóng vai nhân viên lễ tân, AI đóng vai Mr. Viktor - một khách hàng VIP đang nóng giận do vấn đề phòng ốc.

---

## 2. Sơ Đồ Kiến Trúc Tổng Quan

```
┌─────────────────────────────────────────────────────────────────────┐
│                          UNITY APP (MacBook Pro M4)                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                        UI LAYER (UI Toolkit)                │   │
│  │  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌─────────────┐  │   │
│  │  │  Auth   │  │ CaseList │  │Designer │  │  Simulation  │  │   │
│  │  │ Screen  │  │  Screen  │  │ Screen  │  │   Screen     │  │   │
│  │  └─────────┘  └──────────┘  └─────────┘  └──────┬──────┘  │   │
│  └──────────────────────────────┬─────────────────────┼────────┘   │
│                                 │                     │             │
│  ┌──────────────────────────────▼─────────────────────▼────────┐   │
│  │                    CORE LAYER (C#)                           │   │
│  │  SimulationManager (state machine: MainMenu/Designer/Play) │   │
│  │  EventManager (static event bus)                             │   │
│  │  WebSocketManager (connect to Agent API)                    │   │
│  │  MCP Bridge (Integration with Environment/Persona)          │   │
│  └──────────────────────────────┬───────────────────────────────┘   │
│                                 │                                     │
│  ┌─────────────────────────────▼───────────────────────────────┐   │
│  │                    3D RENDERING LAYER                         │   │
│  │  Hotel Lobby Prefabs  │  Mr. Viktor Persona  │  Smart Objects │   │
│  └────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────┬────────────────────────────────┘
                                     │ WebSocket (ws://localhost:9000)
                                     │ REST HTTP (FastAPI 8001/9000)
                                     ▼
┌──────────────────────────────────────────────────────────────────────┐
│                           AGENT API (port 9000)                       │
│                                                                       │
│  LangGraph Multi-Agent Orchestrator:                                 │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐        │
│  │ Interpreter  │─────▶│   Persona    │─────▶│  Chronicler  │        │
│  │   Agent      │      │    Agent     │      │    Agent     │        │
│  └──────┬───────┘      └──────┬───────┘      └──────┬───────┘        │
│         │                     │                     │                │
│         └──────────┬──────────┴──────────┬──────────┘                │
│                    ▼                     ▼                           │
│             ┌──────────────┐      ┌──────────────┐                   │
│             │ Judge/Reward │      │    MCP       │                   │
│             │    Agent     │      │ Environment  │                   │
│             └──────────────┘      └──────────────┘                   │
└───────────────────────────────────────────┬─────────────────────────┘
                                           │
                            ┌──────────────▼──────────────────┐
                            │         MONGODB                 │
                            │  collections: cases, users,     │
                            │  sessions_history, assets_env   │
                            └─────────────────────────────────┘
```

---

## 3. Unified Data Schema (MongoDB)
Chi tiết xem tại [STATE_SPEC.md](./STATE_SPEC.md). Cấu trúc tập trung vào `case_metadata`, `environment`, `persona`, `knowledge_base`, và `reward_system`.

---

## 4. Multi-Agent Design (Agent API)

Hệ thống sử dụng LangGraph để điều phối 4 agent chuyên biệt:

1.  **Interpreter Agent**: Phân tích input từ Unity (Text + Tọa độ click chuột trên Smart Objects). Chuyển đổi thành hành động ngữ nghĩa.
2.  **Persona Agent**: Hóa thân vào nhân vật (VD: Mr. Viktor) dựa trên `traits` và `voice_style`. Phản hồi theo ngữ cảnh khách sạn.
3.  **Chronicler Agent**: Tổng hợp diễn biến cuộc hội thoại vào `active_memory` để duy trì ngữ cảnh dài hạn.
4.  **Judge & Reward Agent**: So sánh hành động của người dùng với `reward_system.rules` để tính toán điểm số và cung cấp giải thích (`explanation`).

---

## 5. Communication Protocol (Unity ↔ Agent API)
Duy trì giao thức WebSocket cho thời gian thực và REST cho cấu hình.

### WebSocket Events
- `USER_INPUT`: Gửi text hoặc audio stream từ mic.
- `USER_ACTION`: Gửi ID của Smart Object khi người dùng tương tác trong không gian 3D.
- `AGENT_RESPONSE`: Trả về text, cảm xúc, TTS URL và visemes cho Lip-sync.
- `STATE_UPDATE`: Cập nhật `runtime_state` (điểm số, event hiện tại).

---

## 6. Simulation Flow Chi Tiết

1. **Khởi tạo**: Unity gọi `POST /api/agent/sessions` với `case_id`. Agent API tải cấu hình từ MongoDB.
2. **Vòng lặp tương tác**:
    - Người dùng nói hoặc click vào vật thể (VD: Booking Slip).
    - **Interpreter** xác định hành động: "Người dùng kiểm tra thông tin đặt phòng".
    - **Judge** kiểm tra rule: Nếu đúng quy trình VIP → Cộng 10 điểm.
    - **Persona** phản hồi: "Đúng rồi, đó là thông tin của tôi. Sao vẫn chưa xong?"
    - **Chronicler** ghi nhớ: "Khách vẫn còn sốt ruột mặc dù đã được kiểm tra thông tin".
3. **Kết thúc**: Khi đạt `passing_score` hoặc hết `max_turns`, hệ thống chuyển sang trạng thái tổng kết.

---

## 7. Task Roadmap
Chi tiết theo dõi tại [Task.md](./Task.md).
- **Giai đoạn 1**: Thiết kế Core State & Kiến trúc (Hoàn thành).
- **Giai đoạn 2**: Tích hợp Unity MCP & Environment (Đang thực hiện).
- **Giai đoạn 3**: Xây dựng Hotel Domain & Single Persona (Sắp tới).
- **Giai đoạn 4**: Tối ưu Multi-threading & Real-time Lip-sync.
