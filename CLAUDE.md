# CLAUDE.md — CDIO CaseStudy Project

## Project Overview

**CDIO** = Case-Driven Interactive Operation. A training simulation platform where users role-play with an AI agent (LangGraph + GPT-4o-mini) in interactive 3D scenarios built in Unity.

**Demo scenario**: Retail customer service — user plays a sales associate, AI plays a frustrated customer. All UI/UX built in Unity 6 with UI Toolkit.

---

## Repository Structure

```
CaseStudy_CDIO/
├── ARCHITECTURE.md              ← MASTER architecture document (v1.0)
├── CaseStydy_Unity_M4/          ← Unity 6 frontend (UI Toolkit + 3D)
│   ├── CLAUDE.md                ← Unity team guide
│   └── Assets/_Project/
│       ├── UI/                  ← UI Toolkit (.uxml + .uss)
│       ├── Scripts/             ← C# logic
│       └── Prefabs/             ← 3D prefabs (Env + Persona)
└── (Agent API)                  ← Python FastAPI + LangGraph (separate repo by Felix)
```

---

## System Architecture

### 4 Services (all on MacBook Pro M4)

| Port | Service | Purpose |
|------|---------|---------|
| 8000 | Express (Node) | Auth (login/register), case CRUD, session history — MongoDB |
| 9000 | FastAPI (Python) | Agent API — LangGraph, WebSocket, STT/TTS |
| 27017 | MongoDB | Data store: cases, users, sessions_history, assets |
| — | Unity Editor | All UI/UX (replaces React) |

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────┐
│  UNITY APP (MacBook Pro M4)                                  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ UI LAYER — UI Toolkit (.uxml + .uss)                   │ │
│  │  Auth / CaseList / Case Designer / Simulation          │ │
│  └────────────────────────────┬───────────────────────────┘ │
│                                │                             │
│  ┌────────────────────────────▼───────────────────────────┐ │
│  │ CORE LAYER — C#                                        │ │
│  │  SimulationManager, EventManager (read-only)            │ │
│  │  ApiClient, WebSocketManager, AudioManager              │ │
│  └────────────────────────────┬───────────────────────────┘ │
│                                │                             │
│  ┌────────────────────────────▼───────────────────────────┐ │
│  │ 3D RENDERING — Unity Prefabs                           │ │
│  │  Prefabs/Environments/  │  Prefabs/Characters/          │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
         │                               │
    HTTP REST                        WebSocket
         │                               │
         ▼                               ▼
┌─────────────────┐          ┌─────────────────────────┐
│  Express 8000    │          │   Agent API 9000         │
│  Auth + Cases    │          │   LangGraph + WebSocket  │
│  MongoDB         │          │   STT (Whisper)          │
└─────────────────┘          │   TTS (ElevenLabs)       │
                              └─────────────────────────┘
```

---

## Key Architecture Decisions

### Case = Event Graph

A case is a **directed graph of events** (nodes). Each event:
- Has **1 env** (3D scene prefab) + **1 persona** (AI character)
- Contains **reward criteria** for scoring
- Has **transitions** to other events (keyword/score/action based)
- May have **affordances** (interactive 3D objects)

### Simulation Flow (Unity ↔ Agent API)

1. User selects case → Unity loads case JSON from Express 8000
2. WebSocket connects to Agent API 9000 → creates session
3. User speaks (mic) or clicks affordance → Unity sends to Agent API
4. Agent: STT → Semantic Extract → Evaluate Score → Generate Response → Decide Transition
5. Agent sends response + transition signal back to Unity via WebSocket
6. Unity: TTS playback, animate persona, load new env/prefab if transition
7. On `isTerminal` event → save history to MongoDB → show final score

---

## Two Teams, Two Tracks

| Team | Working On | Deliverable |
|------|-----------|-------------|
| **Unity UI/UX** | `CaseStydy_Unity_M4/` | Auth, CaseList, Case Designer, Simulation UI, Affordances |
| **Agent (Felix)** | Agent API (separate repo) | LangGraph, WebSocket server, STT/TTS, prompts |

**Contract**: See `ARCHITECTURE.md` Section 9 — API Contract. Two teams only communicate via this contract.

---

## Critical Files

| File | Purpose |
|------|---------|
| `ARCHITECTURE.md` | **MASTER** — full system design, schema, API contract, tasks |
| `CaseStydy_Unity_M4/CLAUDE.md` | Unity team guide |
| `CaseStydy_Unity_M4/Assets/_Project/Scripts/Models/CaseModel.cs` | Unified C# data models |
| `CaseStydy_Unity_M4/Assets/_Project/Scripts/Core/SimulationManager.cs` | State machine — DO NOT MODIFY |
| `CaseStydy_Unity_M4/Assets/_Project/Scripts/Core/EventManager.cs` | Event bus — DO NOT MODIFY |

---

## Development

```bash
# Unity
# Open in Unity Hub / Unity Editor — no CLI needed

# Agent API (by Felix)
cd agent-api && uvicorn main:app --reload --port 9000

# Express BFF (by Felix)
cd front-end && npm run start  # port 8000

# MongoDB
# Ensure running on localhost:27017
```

---

## Important Notes

- All UI is **UI Toolkit** (`.uxml` + `.uss`), NOT uGUI
- Unity 3D prefabs are stored locally; JSON only holds `prefabKey` strings
- Agent API (port 9000) is built by Felix separately — Unity team tests with mock/curl
- `SimulationManager` and `EventManager` in `Scripts/Core/` are **read-only**
- Prefab naming: `Env_*` for environments, `Persona_*` for characters
- Two teams work in parallel via API Contract in `ARCHITECTURE.md` Section 9
