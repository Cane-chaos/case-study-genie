# CDIO — Case-Driven Interactive Operation
## Architecture Document v1.0 | 2026-04-04

---

## MỤC LỤC
1. [Tổng Quan Hệ Thống](#1-tổng-quan-hệ-thống)
2. [Sơ Đồ Kiến Trúc Tổng Quan](#2-sơ-đồ-kiến-trúc-tổng-quan)
3. [Unified Data Schema (MongoDB)](#3-unified-data-schema-mongodb)
4. [Agent API — Internal Design](#4-agent-api--internal-design)
5. [Communication Protocol (Unity ↔ Agent API)](#5-communication-protocol-unity--agent-api)
6. [Simulation Flow Chi Tiết](#6-simulation-flow-chi-tiết)
7. [UI/UX Tasks (Giao cho Unity Team)](#7-uiux-tasks-giao-cho-unity-team)
8. [Agent API Tasks (Giao cho Felix)](#8-agent-api-tasks-giao-cho-felix)
9. [Điều Kiện Để Hai Team Làm Song Song](#9-điều-kiện-để-hai-team-làm-song-song)

---

## 1. Tổng Quan Hệ Thống

### Ngôn ngữ & framework
| Phần | Ngôn ngữ | Framework | Port |
|------|----------|-----------|------|
| Unity App (UI + 3D) | C# | Unity 6 + UI Toolkit | — |
| Agent API | Python | LangChain/LangGraph + FastAPI | 9000 |
| Backend Auth | Python | FastAPI | 8001 |
| Database | — | MongoDB | 27017 |
| Realtime | — | WebSocket | ws://localhost:9000 |

### Demo scenario
Mua bán — người dùng đóng vai nhân viên bán hàng, AI đóng vai khách hàng với các tình huống: hỏi mua, phàn nàn, không hài lòng.

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
│  │  AudioManager (mic capture + TTS playback)                 │   │
│  │  ApiClient (HTTP REST calls)                               │   │
│  └──────────────────────────────┬───────────────────────────────┘   │
│                                 │                                     │
│  ┌─────────────────────────────▼───────────────────────────────┐   │
│  │                    3D RENDERING LAYER                         │   │
│  │  Environment Prefabs  │  Persona Prefabs  │  Affordances    │   │
│  └────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────┬────────────────────────────────┘
                                     │ WebSocket (ws://localhost:9000)
                                     │ REST HTTP (Express 8000)
                                     ▼
┌──────────────────────────────────────────────────────────────────────┐
│                           AGENT API (port 9000)                       │
│                                                                       │
│  LangGraph State Machine:                                            │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌───────────┐ │
│  │  INIT    │─▶│SEMANTIC_EXTRACT│─▶│ EVALUATE_TURN │─▶│  GENERATE │ │
│  │(load case│  │(LLM extract   │  │ (LLM judge    │  │(LLM role- │ │
│  │ from JSON│  │ intent)        │  │  criteria)    │  │  play)    │ │
│  └──────────┘  └───────────────┘  └───────┬───────┘  └─────┬─────┘ │
│                                           │                 │        │
│                                    ┌──────▼────────────────▼─────┐  │
│                                    │      DECIDE_TRANSITION      │  │
│                                    │ (rule: keyword/score/click) │  │
│                                    └──────┬─────────────────────┘  │
└───────────────────────────────────────────┼─────────────────────────┘
                                           │
                            ┌──────────────▼──────────────────┐
                            │         MONGODB                 │
                            │  collections: cases, users,     │
                            │  sessions_history, assets_env,  │
                            │  assets_persona                 │
                            └─────────────────────────────────┘
```

---

## 3. Unified Data Schema (MongoDB)

### 3.1 Collection: `cases`

Đây là **trọng tâm** của toàn hệ thống. Một case = một training scenario hoàn chỉnh.

```json
{
  "_id": "ObjectId",
  "caseId": "case_retail_checkout_001",
  "title": "Xử Lý Khách Hàng Không Hài Lòng",
  "description": "Huấn luyện nhân viên xử lý tình huống khách hàng phàn nàn về sản phẩm",
  "language": "vi",
  "difficulty": "medium",
  "createdBy": "user_id",
  "createdAt": "2026-04-04T00:00:00Z",
  "updatedAt": "2026-04-04T00:00:00Z",
  "version": 1,
  "isPublished": false,

  "citations": [
    "Cambridge Business English, Unit 5",
    "Retail Service Excellence Standards"
  ],

  "eventRootId": "event_1",

  "events": {

    "event_1": {
      "eventId": "event_1",
      "name": "Cửa Hàng — Khách Bước Vào",
      "isTerminal": false,

      "env": {
        "envId": "env_retail_store",
        "name": "Cửa Hàng Bán Lẻ",
        "prefabKey": "Env_RetailStore_v1",
        "anchorPoints": [
          { "id": "counter", "position": [0, 0, 0], "label": "Quầy thu ngân" },
          { "id": "entrance", "position": [0, 0, -5], "label": "Cửa vào" },
          { "id": "product_shelf", "position": [3, 0, 1], "label": "Kệ sản phẩm" }
        ]
      },

      "persona": {
        "personaId": "persona_customer_01",
        "name": "Anh Minh",
        "role": "Khách Hàng",
        "traits": {
          "personality": "khó tính, vội vàng, hay phàn nàn",
          "speakingStyle": "ngắn gọn, hơi gắt, thẳng thắn",
          "language": "vi",
          "emotion_default": "neutral",
          "emotion_frustrated": "buồn, cau có",
          "emotion_satisfied": "vui vẻ, cảm ơn"
        },
        "prefabKey": "Persona_Customer_Male_v1",
        "voiceId": "premade_professional_male",
        "systemPrompt": "Bạn là Minh, 35 tuổi, đang rất vội. Bạn vừa mua một sản phẩm bị lỗi và rất bực mình. Bạn nói ngắn gọn, thẳng thắn, đôi khi hơi gắt nhưng không la hét. Nếu nhân viên xử lý tốt, bạn sẽ hạ hoả và cảm ơn."
      },

      "introMessage": "Một khách hàng bước vào cửa hàng với vẻ mặt khó chịu. Anh ấy cầm một sản phẩm đã mua trước đó. Bạn là nhân viên bán hàng.",

      "maxTurns": 6,

      "reward": {
        "greeting": {
          "criteria": "Chào đón khách bằng thái độ thân thiện",
          "weight": 0.2,
          "toneKeywords": ["xin chào", "chào anh/chị", "em có thể giúp gì ạ", "mời anh/chị"]
        },
        "empathy": {
          "criteria": "Thể hiện sự đồng cảm với tình huống của khách",
          "weight": 0.3,
          "toneKeywords": ["em hiểu", "em xin lỗi", "em rất tiếc", "em thông cảm", "tôi hiểu"]
        },
        "problem_solving": {
          "criteria": "Đề xuất giải pháp cụ thể và hợp lý",
          "weight": 0.3,
          "solutionKeywords": ["đổi mới", "hoàn tiền", "bảo hành", "kiểm tra", "để em xem"]
        },
        "professionalism": {
          "criteria": "Giữ thái độ chuyên nghiệp dù khách có gắt",
          "weight": 0.2,
          "toneKeywords": ["vâng ạ", "dạ", "em sẽ", "chúng em"]
        }
      },

      "transitions": [
        {
          "conditionType": "user_action",
          "triggerSignal": "click_counter",
          "targetEventId": "event_1"
        },
        {
          "conditionType": "ai_semantic",
          "intent": "wants_refund",
          "targetEventId": "event_2"
        },
        {
          "conditionType": "ai_semantic",
          "intent": "is_frustrated",
          "targetEventId": "event_1b"
        },
        {
          "conditionType": "ai_score",
          "operator": ">=",
          "threshold": 0.7,
          "targetEventId": "event_2"
        },
        {
          "conditionType": "ai_score",
          "operator": "<",
          "threshold": 0.3,
          "targetEventId": "event_1b"
        },
        {
          "conditionType": "max_turns",
          "targetEventId": "event_timeout"
        }
      ],

      "affordances": [
        {
          "affordanceId": "waiting_area",
          "name": "Khu vực chờ",
          "signal": "click_waiting_area",
          "targetEventId": "event_1b",
          "tooltip": "Mời khách vào khu vực chờ"
        },
        {
          "affordanceId": "product_shelf",
          "name": "Kệ sản phẩm",
          "signal": "click_product_shelf",
          "targetEventId": null,
          "tooltip": "Giới thiệu sản phẩm thay thế",
          "responseHint": "Giới thiệu các sản phẩm tương tự có sẵn"
        }
      ]
    },

    "event_1b": {
      "eventId": "event_1b",
      "name": "Khu Vực Chờ — Mời Nước",
      "isTerminal": false,
      "env": {
        "envId": "env_waiting_area",
        "name": "Khu Vực Chờ",
        "prefabKey": "Env_WaitingArea_v1"
      },
      "persona": {
        "personaId": "persona_customer_01",
        "name": "Anh Minh",
        ...
      },
      "introMessage": "Bạn mời khách ngồi ở khu vực chờ và mang một ly nước. Anh ấy bắt đầu bình tĩnh hơn.",
      "maxTurns": 4,
      "reward": {...},
      "transitions": [
        { "conditionType": "ai_score", "operator": ">=", "threshold": 0.5, "targetEventId": "event_1" },
        { "conditionType": "max_turns", "targetEventId": "event_fail" }
      ],
      "affordances": []
    },

    "event_2": {
      "eventId": "event_2",
      "name": "Quầy Thu Ngân — Xử Lý",
      "isTerminal": false,
      "env": {
        "envId": "env_counter",
        "name": "Quầy Thu Ngân",
        "prefabKey": "Env_Counter_v1"
      },
      "persona": {
        "personaId": "persona_customer_01",
        ...
      },
      "introMessage": "Bạn dẫn khách đến quầy thu ngân để xử lý yêu cầu.",
      "maxTurns": 8,
      "reward": {...},
      "transitions": [
        { "conditionType": "ai_score", "operator": ">=", "threshold": 0.8, "targetEventId": "event_success" },
        { "conditionType": "ai_score", "operator": ">=", "threshold": 0.5, "targetEventId": "event_3" },
        { "conditionType": "max_turns", "targetEventId": "event_timeout" }
      ],
      "affordances": []
    },

    "event_success": {
      "eventId": "event_success",
      "name": "Kết Thúc — Thành Công",
      "isTerminal": true,
      "env": { "envId": "env_retail_store", "prefabKey": "Env_RetailStore_v1" },
      "persona": {
        "personaId": "persona_customer_01",
        ...
      },
      "introMessage": "Khách hàng hài lòng rời đi. Bạn đã xử lý tình huống xuất sắc!",
      "maxTurns": 2,
      "reward": {...},
      "transitions": [],
      "affordances": []
    },

    "event_fail": {
      "eventId": "event_fail",
      "name": "Kết Thúc — Thất Bại",
      "isTerminal": true,
      "env": { "envId": "env_retail_store", "prefabKey": "Env_RetailStore_v1" },
      "persona": { ... },
      "introMessage": "Khách rời đi không hài lòng. Cần cải thiện kỹ năng giao tiếp.",
      "maxTurns": 0,
      "reward": {...},
      "transitions": [],
      "affordances": []
    },

    "event_timeout": {
      "eventId": "event_timeout",
      "name": "Kết Thúc — Hết Lượt",
      "isTerminal": true,
      "introMessage": "Hết thời gian giao tiếp. Số lượt đã hết.",
      "maxTurns": 0,
      "reward": {...},
      "transitions": [],
      "affordances": []
    }
  }
}
```

### 3.2 Collection: `sessions_history`

```json
{
  "_id": "ObjectId",
  "sessionId": "sess_abc123",
  "caseId": "case_retail_checkout_001",
  "userId": "user_id",
  "startedAt": "2026-04-04T10:00:00Z",
  "completedAt": "2026-04-04T10:15:00Z",
  "status": "completed",
  "finalScore": 0.82,
  "eventsCompleted": ["event_1", "event_2", "event_success"],
  "eventScores": [
    { "eventId": "event_1", "score": 0.75, "turns": 6 },
    { "eventId": "event_2", "score": 0.90, "turns": 8 },
    { "eventId": "event_success", "score": 0.82, "turns": 2 }
  ],
  "transcript": [
    { "speaker": "system", "text": "Bắt đầu case: Xử Lý Khách Hàng...", "timestamp": "..." },
    { "speaker": "persona", "text": "Cửa hàng này bán đồ gì vậy?", "timestamp": "..." },
    { "speaker": "user", "text": "Dạ chào anh, cửa hàng chúng em bán...", "timestamp": "..." }
  ]
}
```

### 3.3 Collection: `assets_env`

```json
{
  "_id": "ObjectId",
  "envId": "env_retail_store",
  "name": "Cửa Hàng Bán Lẻ",
  "prefabKey": "Env_RetailStore_v1",
  "version": 1,
  "updatedAt": "2026-04-04T00:00:00Z",
  "changelog": "Initial version"
}
```

### 3.4 Collection: `assets_persona`

```json
{
  "_id": "ObjectId",
  "personaId": "persona_customer_01",
  "name": "Anh Minh",
  "role": "Khách Hàng",
  "prefabKey": "Persona_Customer_Male_v1",
  "voiceId": "premade_professional_male",
  "traits": {
    "personality": "khó tính, vội vàng, hay phàn nàn",
    "speakingStyle": "ngắn gọn, hơi gắt",
    "language": "vi"
  },
  "version": 1,
  "updatedAt": "2026-04-04T00:00:00Z"
}
```

### 3.5 Collection: `users`

```json
{
  "_id": "ObjectId",
  "email": "user@example.com",
  "passwordHash": "bcrypt_hash",
  "name": "Nguyen Van A",
  "createdAt": "2026-04-01T00:00:00Z"
}
```

---

## 4. Agent API — Internal Design

### 4.1 LangGraph State

```python
class CaseSessionState(TypedDict):
    # === IMMUTABLE (set once at session start) ===
    session_id: str
    case_id: str
    case_title: str
    citations: list[str]

    # loaded from MongoDB
    skeleton: dict           # events{} from case document
    event_root_id: str        # first event to start
    current_event_id: str     # pointer

    # === MUTABLE ===
    dialogue_history: list[dict]  # [{"speaker": "user"|"persona"|"system", "text": "...", "timestamp": "..."}]
    current_event: dict          # current event definition
    turn_count: int              # turns in current event
    total_score: float           # cumulative score
    event_scores: list[dict]     # [{eventId, score, turns}]
    current_criteria_scores: dict  # {"greeting": 0.8, "empathy": 0.0}

    # === TERMINAL ===
    completed: bool
    final_score: float
    final_event_id: str
```

### 4.2 LangGraph Nodes

```
┌──────────────────────────────────────────────────────────────────┐
│  LangGraph Flow                                                    │
│                                                                    │
│  START                                                             │
│    │                                                               │
│    ▼                                                               │
│  ┌─────────────────┐                                              │
│  │  INIT            │ ← Load case JSON from MongoDB             │
│  │  Load skeleton   │ ← Set current_event = event_root_id        │
│  └────────┬────────┘                                              │
│           │                                                        │
│           ▼                                                        │
│  ┌─────────────────────┐                                          │
│  │  SEMANTIC_EXTRACT    │ ← LLM extract intent from user input  │
│  │  Extract intent:     │   (wants_refund, is_frustrated, etc)  │
│  │  - wants_refund      │                                          │
│  │  - is_frustrated     │                                          │
│  │  - is_satisfied      │                                          │
│  │  - wants_upgrade     │                                          │
│  └────────┬────────────┘                                          │
│           │                                                        │
│           ▼                                                        │
│  ┌─────────────────────┐                                          │
│  │  EVALUATE_TURN       │ ← LLM judge score per criteria         │
│  │  Output:             │   (always runs, updates score)          │
│  │  - criteria_scores{} │                                          │
│  │  - turn_feedback     │                                          │
│  │  - persona_emotion   │                                          │
│  └────────┬────────────┘                                          │
│           │                                                        │
│           ▼                                                        │
│  ┌─────────────────────┐                                          │
│  │  GENERATE_RESPONSE   │ ← LLM role-play persona response       │
│  │  Input:              │   (always runs, sends to Unity)         │
│  │  - dialogue_history  │                                          │
│  │  - persona system_prompt                                        │
│  │  - current_event     │                                          │
│  └────────┬────────────┘                                          │
│           │                                                        │
│           ▼                                                        │
│  ┌─────────────────────┐                                          │
│  │  DECIDE_TRANSITION   │                                          │
│  │  Check transitions[] │                                          │
│  │  in order of priority│                                          │
│  │  MATCH → next_event  │                                          │
│  │  NO MATCH → stay     │                                          │
│  └────────┬────────────┘                                          │
│           │                                                        │
│    ┌──────┴──────┐                                                │
│    │             │                                                 │
│    ▼             ▼                                                 │
│  STAY          NEXT EVENT                                          │
│  (respond      ① Update current_event_id                          │
│   only)        ② Add introMessage to dialogue                     │
│                ③ Reset turn_count                                 │
│                ④ Check isTerminal                                 │
│                    ├── true → END                                  │
│                    └── false → loop back to SEMANTIC_EXTRACT     │
└──────────────────────────────────────────────────────────────────┘
```

### 4.3 API Endpoints

```
POST /api/agent/sessions
  Body: { case_id, lazy_init: true }
  → Create new session, load case JSON, return session_id

POST /api/agent/sessions/{sessionId}/turn
  Body: { user_input: string }           ← text input (chat fallback)
     OR: { audio_data: base64 }           ← audio input (mic)
  → Process turn, return:
  {
    session_state: CaseSessionState,
    persona_text: string,
    emotion: string,
    tts_url: string | null,
    transition: {
      triggered: bool,
      next_event_id: string | null,
      condition_type: string | null
    }
  }

POST /api/agent/sessions/{sessionId}/signal
  Body: { signal: string, action: string }
  → Process user_action signal (affordance click)
  → Same return structure as /turn

GET  /api/agent/sessions/{sessionId}/state
  → Return current CaseSessionState

DELETE /api/agent/sessions/{sessionId}
  → End session (save to MongoDB history)
```

---

## 5. Communication Protocol (Unity ↔ Agent API)

### 5.1 WebSocket Protocol

Unity kết nối WebSocket tới `ws://localhost:9000/ws/{sessionId}`.

**Direction: Unity → Agent API**

```json
// Speech input (mic)
{
  "type": "USER_SPEECH",
  "audioData": "base64_encoded_wav",
  "format": "wav"
}

// Affordance / button click
{
  "type": "USER_ACTION",
  "signal": "click_waiting_area",
  "action": "go_to_waiting_room"
}

// Text input (fallback)
{
  "type": "USER_TEXT",
  "text": "Tôi muốn đổi sản phẩm này"
}
```

**Direction: Agent API → Unity**

```json
// Persona response + transition
{
  "type": "AGENT_RESPONSE",
  "personaText": "Dạ, em hiểu. Anh cho em xem sản phẩm ạ...",
  "emotion": "neutral",
  "ttsUrl": "https://...",
  "visemes": [0.1, 0.3, ...],
  "turnCount": 1,
  "currentScore": 0.0,
  "criteriaScores": {
    "greeting": 0.0,
    "empathy": 0.0,
    "problem_solving": 0.0,
    "professionalism": 0.0
  }
}

// Event transition
{
  "type": "EVENT_TRANSITION",
  "nextEventId": "event_1b",
  "newEnv": {
    "envId": "env_waiting_area",
    "name": "Khu Vực Chờ",
    "prefabKey": "Env_WaitingArea_v1",
    "anchorPoints": [...]
  },
  "newPersona": {
    "personaId": "persona_customer_01",
    "name": "Anh Minh",
    "traits": {...},
    "prefabKey": "Persona_Customer_Male_v1"
  },
  "introMessage": "Bạn mời khách ngồi ở khu vực chờ...",
  "maxTurns": 4,
  "affordances": [
    {
      "affordanceId": "waiting_area",
      "name": "Khu vực chờ",
      "signal": "click_waiting_area",
      "tooltip": "Mời khách vào khu vực chờ"
    }
  ]
}

// Case completed
{
  "type": "CASE_COMPLETED",
  "finalScore": 0.82,
  "finalEventId": "event_success",
  "eventScores": [...],
  "summary": "Bạn đã xử lý tình huống tốt. Khách hàng hài lòng."
}

// Error
{
  "type": "ERROR",
  "message": "Session not found",
  "code": "SESSION_NOT_FOUND"
}
```

---

## 6. Simulation Flow Chi Tiết

```
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 1: Case Selection                                             │
│                                                                      │
│  Unity: User browse CaseList → click "Play"                        │
│  Unity ──HTTP──▶ Express 8000: GET /api/cases                      │
│  Unity ──HTTP──▶ Express 8000: POST /api/cases/{id}/load           │
│         ◀────────────────────────────────────────────                │
│         { caseId, title, eventRootId, events{} }                   │
│                                                                      │
│  Unity: Parse JSON → store in local CaseData class                  │
│  Unity: ChangeState(SimulationState.Playing)                        │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 2: Session Start                                               │
│                                                                      │
│  Unity WebSocket ──connect──▶ ws://localhost:9000/ws/{sessionId}   │
│  Unity ──HTTP POST──▶ Agent 9000: /api/agent/sessions               │
│  Body: { case_id: "case_...", lazy_init: true }                    │
│         ◀──────────────────────────────────────                     │
│         { sessionId: "sess_abc123" }                                │
│                                                                      │
│  Unity: Load event_1 (root) prefab: Env_RetailStore                │
│  Unity: Load persona prefab: Persona_Customer_Male                  │
│  Unity: Display introMessage on UI                                  │
│  Unity: Start mic listening                                         │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 3: Active Simulation Loop                                      │
│                                                                      │
│  LOOP START                                                          │
│  │                                                                   │
│  ├─── USER SPEAKS (mic) ────────────────────────────────────────────│
│  │   Unity: Capture audio → base64 encode                          │
│  │   Unity ──WebSocket──▶ Agent: { type: "USER_SPEECH", audioData }│
│  │                        │                                         │
│  │                   ┌────▼──────────────────────────┐              │
│  │                   │ SEMANTIC_EXTRACT (LLM)        │              │
│  │                   │ Extract intent from text      │              │
│  │                   └────┬──────────────────────────┘              │
│  │                        │                                         │
│  │                   ┌────▼──────────────────────────┐              │
│  │                   │ EVALUATE_TURN (LLM judge)      │              │
│  │                   │ Score per criteria            │              │
│  │                   │ Update current_criteria_scores│              │
│  │                   └────┬──────────────────────────┘              │
│  │                        │                                         │
│  │                   ┌────▼──────────────────────────┐              │
│  │                   │ GENERATE_RESPONSE (LLM)       │              │
│  │                   │ Persona speaks in-character   │              │
│  │                   │ ElevenLabs TTS → ttsUrl      │              │
│  │                   └────┬──────────────────────────┘              │
│  │                        │                                         │
│  │                   ┌────▼──────────────────────────┐              │
│  │                   │ DECIDE_TRANSITION              │              │
│  │                   │ Check transitions[] priority   │              │
│  │                   └────┬──────────────────────────┘              │
│  │                        │                                         │
│  │   ◀── WebSocket ◀─────┤                                         │
│  │                        │                                         │
│  │   Unity receives:      │                                         │
│  │   - personaText       │                                         │
│  │   - ttsUrl             │                                         │
│  │   - emotion           │                                         │
│  │   - transition        │                                         │
│  │                        │                                         │
│  │   Unity:               │                                         │
│  │   - Play TTS audio     │                                         │
│  │   - Animate persona    │                                         │
│  │   - Update score UI   │                                         │
│  │   - IF transition → load new env/prefab                        │
│  │                        │                                         │
│  ├─── USER CLICKS AFFORDANCE ──────────────────────────────────────│
│  │   Unity: User clicks button in 3D scene                        │
│  │   Unity ──WebSocket──▶ Agent: { type: "USER_ACTION", signal }   │
│  │   Agent: Match signal in transitions[]                          │
│  │   ◀── WebSocket ◀── Agent: transition response + persona_text  │
│  │   Unity: Load new env/persona + persona speaks                  │
│  │                                                                   │
│  └─── IF isTerminal == true ───────────────────────────────────────│
│      Unity: Show final score screen                                │
│      Unity ──HTTP──▶ Express 8000: POST /api/sessions/history      │
│      (save transcript + scores to MongoDB)                        │
│      LOOP END                                                        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 7. UI/UX Tasks (Giao cho Unity Team)

### 7.1 Auth Screen
- [ ] **Auth UI**: Login / Register form (UI Toolkit .uxml)
- [ ] **Auth Controller**: Gọi API Express 8000 `/api/auth/register`, `/api/auth/login`
- [ ] **JWT storage**: Lưu token vào PlayerPrefs
- [ ] **Redirect**: Sau login thành công → `ChangeState(SimulationState.Designer)`

### 7.2 Case List Screen
- [ ] **Case List UI**: Grid hiển thị danh sách case (thumbnail, title, difficulty, description)
- [ ] **Case List Controller**: Gọi `GET /api/cases` → render list
- [ ] **Case Card**: Click "Play" → `ChangeState(SimulationState.Playing)`
- [ ] **Case Card**: Click "Edit" → `ChangeState(SimulationState.Designer)`
- [ ] **"Create Case" button**: `ChangeState(SimulationState.Designer)` + new blank case
- [ ] **Search/Filter**: Filter theo difficulty, language

### 7.3 Case Designer Screen
- [ ] **Event Graph Canvas**: Render events như graph nodes (kéo thả)
- [ ] **Event Node**: Hiển thị eventId, name, isTerminal flag
- [ ] **Add Event Button**: Thêm event mới vào graph
- [ ] **Delete Event**: Xóa event khỏi graph
- [ ] **Event Detail Panel**: Khi click node → hiện panel edit bên phải:
  - [ ] Name (text input)
  - [ ] isTerminal (toggle)
  - [ ] Env: Dropdown chọn từ `GET /api/assets/env`
  - [ ] Persona: Dropdown chọn từ `GET /api/assets/persona`
  - [ ] IntroMessage (text area)
  - [ ] MaxTurns (number input)
  - [ ] Reward: Danh sách criteria (thêm/sửa/xóa criterion)
  - [ ] Transitions: Danh sách transitions (thêm/sửa/xóa)
    - [ ] ConditionType: dropdown (ai_score, ai_semantic, user_action, max_turns)
    - [ ] TargetEventId: dropdown các event khác
    - [ ] Threshold (nếu ai_score)
    - [ ] Intent (nếu ai_semantic)
    - [ ] Signal (nếu user_action)
  - [ ] Affordances: Danh sách affordances (thêm/sửa/xóa)
    - [ ] Name, signal, targetEventId, tooltip
  - [ ] Citations (shared, hiển thị từ case level)
- [ ] **Transition Drawing**: Vẽ đường nối giữa 2 event nodes trên canvas
- [ ] **Root Event Selector**: Chọn event nào là root (điểm bắt đầu)
- [ ] **Save Button**: `POST /api/cases` (new) hoặc `PUT /api/cases/{id}` (update)
- [ ] **Preview Button**: Test case trong Play mode
- [ ] **Validation**: Không cho save nếu có event không có transition cuối cùng (trừ isTerminal)

### 7.4 Simulation Screen
- [ ] **3D Scene Container**: Render Unity 3D scene (env + persona)
- [ ] **Affordance System**:
  - [ ] Raycast from camera on hover → highlight object
  - [ ] Show tooltip label on hover
  - [ ] On click → send USER_ACTION signal via WebSocket
- [ ] **WebSocket Manager**: Kết nối và duy trì WebSocket tới Agent API
- [ ] **Audio Manager**:
  - [ ] Mic capture → encode WAV → gửi base64 qua WebSocket
  - [ ] TTS audio playback (tải ttsUrl từ Agent response → play)
- [ ] **Persona Avatar**: Hiện persona 3D model, animate theo emotion
- [ ] **Chat Overlay UI**:
  - [ ] Transcript scroll view (dialogue_history)
  - [ ] User face cam (webcam feed hiển thị góc nhỏ trên màn hình)
  - [ ] Score panel: current event, turn count/max, criteria scores
  - [ ] Citations panel: Hiện citations của case
- [ ] **Mic Button**: HOLD TO TALK button (bấm giữ nói, thả ra gửi)
- [ ] **Chat Fallback**: Text input + send button (khi mic lỗi)
- [ ] **Event Transition Handler**:
  - [ ] Nhận EVENT_TRANSITION → load new env prefab
  - [ ] Nhận CASE_COMPLETED → hiện kết quả cuối, save history

### 7.5 Main Dashboard
- [ ] **Navigation**: Left panel navigation giữa Auth / CaseList / Designer / Simulation
- [ ] **State Routing**: Theo SimulationState hiện tại → hiện screen tương ứng

### 7.6 Common / Shared
- [ ] **ApiClient**: HTTP client class cho Express 8000 endpoints
- [ ] **WebSocketManager**: Singleton WebSocket connection manager
- [ ] **Models**: Update `Scripts/Models/CaseModel.cs` để match schema trên
- [ ] **Loading States**: UI loading spinner khi gọi API
- [ ] **Error Handling**: Toast notification khi API error
- [ ] **Responsive Layout**: Hỗ trợ resolution khác nhau

---

## 8. Agent API Tasks (Giao cho Felix)

### 8.1 Project Setup
- [ ] Khởi tạo Python project với FastAPI + LangGraph
- [ ] Cấu hình MongoDB connection (Motor async driver)
- [ ] Cấu hình OpenAI API key (Whisper + GPT-4o-mini)
- [ ] Cấu hình ElevenLabs API key (TTS)
- [ ] Cấu hình LangSmith tracing

### 8.2 API Endpoints
- [ ] `POST /api/agent/sessions` — Tạo session, load case từ MongoDB
- [ ] `POST /api/agent/sessions/{id}/turn` — Xử lý text input
- [ ] `POST /api/agent/sessions/{id}/signal` — Xử lý user action signal
- [ ] `WebSocket /ws/{sessionId}` — Realtime communication
- [ ] `GET /api/agent/sessions/{id}/state` — Lấy current state
- [ ] `DELETE /api/agent/sessions/{id}` — Kết thúc session

### 8.3 LangGraph Nodes
- [ ] **INIT node**: Load case JSON, set initial state
- [ ] **SEMANTIC_EXTRACT node**: LLM extract intent từ user text
- [ ] **EVALUATE_TURN node**: LLM judge score per criteria
- [ ] **GENERATE_RESPONSE node**: LLM role-play persona + ElevenLabs TTS
- [ ] **DECIDE_TRANSITION node**: Rule-based transition matching
- [ ] **END node**: Tính final score, trả về kết quả

### 8.4 MongoDB Integration
- [ ] Kết nối Motor async driver
- [ ] CRUD cho collection `cases`
- [ ] CRUD cho collection `sessions_history`
- [ ] CRUD cho collection `assets_env`
- [ ] CRUD cho collection `assets_persona`
- [ ] Indexing: caseId, sessionId

### 8.5 External API Integration
- [ ] OpenAI Whisper API (speech-to-text)
- [ ] OpenAI GPT-4o-mini (evaluate + generate + semantic extract)
- [ ] ElevenLabs API (TTS)
- [ ] LangSmith tracing setup

### 8.6 WebSocket Server
- [ ] FastAPI WebSocket endpoint
- [ ] Session management (in-memory Map<sessionId, state>)
- [ ] Message routing (USER_SPEECH, USER_ACTION, USER_TEXT)
- [ ] Connection lifecycle (connect, disconnect, timeout)
- [ ] Error handling per connection

### 8.7 Prompt Engineering
- [ ] Persona system prompt template
- [ ] Evaluation criteria prompt (per criterion type)
- [ ] Semantic extraction prompt (intent list)
- [ ] Response generation prompt (in-character guidelines)
- [ ] Fallback/unknown intent handling

### 8.8 Hardcoded Demo Case
- [ ] Tạo 1 case hardcoded (không cần MongoDB) để test nhanh
- [ ] Case: Retail checkout scenario (như schema trên)
- [ ] Test full flow: speech → STT → evaluate → respond → transition

---

## 9. Điều Kiện Để Hai Team Làm Song Song

Để Unity Team và Agent Team làm song song mà không chờ nhau, cần tuân thủ **API Contract** bên dưới. Hai team chỉ giao tiếp qua contract này, không cần biết internal của nhau.

### Contract — Unity gửi cho Agent

```typescript
// Type: USER_SPEECH
{ type: "USER_SPEECH", audioData: string, format: "wav" }

// Type: USER_TEXT
{ type: "USER_TEXT", text: string }

// Type: USER_ACTION
{ type: "USER_ACTION", signal: string }
```

### Contract — Agent trả về Unity

```typescript
// Type: AGENT_RESPONSE
{
  type: "AGENT_RESPONSE",
  personaText: string,
  emotion: string,           // "neutral" | "frustrated" | "satisfied"
  ttsUrl: string | null,
  visemes: number[] | null,
  turnCount: number,
  currentScore: number,
  criteriaScores: Record<string, number>
}

// Type: EVENT_TRANSITION
{
  type: "EVENT_TRANSITION",
  nextEventId: string,
  newEnv: { envId: string, name: string, prefabKey: string, anchorPoints: [] },
  newPersona: { personaId: string, name: string, traits: {}, prefabKey: string },
  introMessage: string,
  maxTurns: number,
  affordances: []
}

// Type: CASE_COMPLETED
{
  type: "CASE_COMPLETED",
  finalScore: number,
  finalEventId: string,
  eventScores: [],
  summary: string
}

// Type: ERROR
{ type: "ERROR", message: string, code: string }
```

### Contract — HTTP Endpoints

```
# Agent API
POST http://localhost:9000/api/agent/sessions
POST http://localhost:9000/api/agent/sessions/{id}/turn
POST http://localhost:9000/api/agent/sessions/{id}/signal
GET  http://localhost:9000/api/agent/sessions/{id}/state
DELETE http://localhost:9000/api/agent/sessions/{id}
WS   ws://localhost:9000/ws/{sessionId}

# Demo (hardcoded, không cần MongoDB)
GET  http://localhost:9000/api/demo/case  → trả về hardcoded case JSON
```

### Contract — Unity gửi cho Express (Auth + Cases)

```
# Auth
POST http://localhost:8000/api/auth/register
POST http://localhost:8000/api/auth/login

# Cases
GET  http://localhost:8000/api/cases           → list all cases
POST http://localhost:8000/api/cases           → create case
GET  http://localhost:8000/api/cases/{id}     → get case detail
PUT  http://localhost:8000/api/cases/{id}     → update case
DELETE http://localhost:8000/api/cases/{id}   → delete case

# Assets
GET  http://localhost:8000/api/assets/env       → list envs
GET  http://localhost:8000/api/assets/persona   → list personas
POST http://localhost:8000/api/assets/env       → upload env metadata
POST http://localhost:8000/api/assets/persona   → upload persona metadata

# Session History
GET  http://localhost:8000/api/sessions/history      → list user history
POST http://localhost:8000/api/sessions/history      → save completed session
```

### Để test nhanh không cần Unity

```bash
# Test Agent API với demo case
curl -X POST http://localhost:9000/api/agent/sessions \
  -H "Content-Type: application/json" \
  -d '{"case_id": "demo", "lazy_init": true}'

# Test turn
curl -X POST http://localhost:9000/api/agent/sessions/sess_demo/turn \
  -H "Content-Type: application/json" \
  -d '{"user_input": "Xin chào, tôi muốn đổi sản phẩm này"}'

# Test signal
curl -X POST http://localhost:9000/api/agent/sessions/sess_demo/signal \
  -H "Content-Type: application/json" \
  -d '{"signal": "click_waiting_area"}'
```

---

## 10. Demo Hardcoded Case — Để Felix Test Trước

```python
DEMO_CASE = {
    "caseId": "demo_retail",
    "title": "Demo: Xử Lý Khách Hàng",
    "citations": ["Cambridge Business English, Unit 5"],

    "eventRootId": "event_1",

    "events": {
        "event_1": {
            "eventId": "event_1",
            "name": "Cửa Hàng — Khách Bước Vào",
            "isTerminal": False,
            "env": {"envId": "env_store", "name": "Cửa Hàng", "prefabKey": "Env_Store_v1"},
            "persona": {
                "personaId": "p1",
                "name": "Anh Minh",
                "role": "Khách Hàng",
                "traits": {"personality": "khó tính, vội vàng", "language": "vi"},
                "prefabKey": "Persona_Male_v1",
                "voiceId": "premade_male",
                "systemPrompt": "Bạn là Minh, 35 tuổi. Bạn rất vội và bực mình vì sản phẩm lỗi."
            },
            "introMessage": "Khách hàng bước vào với vẻ mặt khó chịu.",
            "maxTurns": 5,
            "reward": {
                "greeting": {"criteria": "Chào đón thân thiện", "weight": 0.3,
                             "toneKeywords": ["xin chào", "chào anh", "mời anh"]},
                "empathy":  {"criteria": "Đồng cảm với khách", "weight": 0.4,
                             "toneKeywords": ["em hiểu", "em xin lỗi", "tôi hiểu"]},
                "solution": {"criteria": "Đề xuất giải pháp", "weight": 0.3,
                             "solutionKeywords": ["đổi", "hoàn tiền", "bảo hành"]}
            },
            "transitions": [
                {"conditionType": "ai_semantic", "intent": "is_frustrated",
                 "targetEventId": "event_1b"},
                {"conditionType": "ai_score", "operator": ">=", "threshold": 0.7,
                 "targetEventId": "event_success"},
                {"conditionType": "ai_score", "operator": "<", "threshold": 0.3,
                 "targetEventId": "event_fail"},
                {"conditionType": "max_turns", "targetEventId": "event_timeout"}
            ],
            "affordances": [
                {"affordanceId": "waiting_area", "name": "Khu vực chờ",
                 "signal": "click_waiting_area", "targetEventId": "event_1b",
                 "tooltip": "Mời khách vào khu vực chờ"}
            ]
        },

        "event_1b": {
            "eventId": "event_1b",
            "name": "Khu Vực Chờ",
            "isTerminal": False,
            "env": {"envId": "env_waiting", "name": "Khu Vực Chờ", "prefabKey": "Env_Waiting_v1"},
            "persona": {"personaId": "p1", "name": "Anh Minh", "systemPrompt": "...", ...},
            "introMessage": "Bạn mời khách ngồi và mang nước.",
            "maxTurns": 3,
            "reward": {"empathy": {"criteria": "Đồng cảm", "weight": 1.0,
                                   "toneKeywords": ["em xin lỗi", "em hiểu"]}},
            "transitions": [
                {"conditionType": "ai_score", "operator": ">=", "threshold": 0.5,
                 "targetEventId": "event_success"},
                {"conditionType": "max_turns", "targetEventId": "event_fail"}
            ],
            "affordances": []
        },

        "event_success": {
            "eventId": "event_success",
            "name": "Kết Thúc — Thành Công",
            "isTerminal": True,
            "env": {"envId": "env_store", "prefabKey": "Env_Store_v1"},
            "persona": {"personaId": "p1", ...},
            "introMessage": "Khách hàng hài lòng rời đi. Bạn đã xử lý xuất sắc!",
            "maxTurns": 1,
            "reward": {},
            "transitions": [],
            "affordances": []
        },

        "event_fail": {
            "eventId": "event_fail",
            "name": "Kết Thúc — Thất Bại",
            "isTerminal": True,
            "introMessage": "Khách rời đi không hài lòng.",
            "maxTurns": 0,
            "reward": {},
            "transitions": [],
            "affordances": []
        },

        "event_timeout": {
            "eventId": "event_timeout",
            "name": "Kết Thúc — Hết Lượt",
            "isTerminal": True,
            "introMessage": "Hết thời gian.",
            "maxTurns": 0,
            "reward": {},
            "transitions": [],
            "affordances": []
        }
    }
}
```

---

## 11. Pending Questions (Chưa giải quyết — cần thêm thông tin)

- [ ] **TTS**: ElevenLabs voice ID cụ thể nào cho demo? (premade voice)
- [ ] **STT**: Whisper model nào? (whisper-1)
- [ ] **OpenAI model**: gpt-4o-mini hay gpt-4o?
- [ ] **Mic format**: WAV 16kHz mono hay gì khác?
- [ ] **Session timeout**: Bao lâu thì session tự hủy nếu không có activity?
- [ ] **Concurrent sessions**: Giới hạn bao nhiêu session đồng thời?
- [ ] **Case versioning**: Khi update case → tạo version mới hay overwrite?
