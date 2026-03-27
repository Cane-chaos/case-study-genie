# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project State

The old Python `casestudy/` and `api_casestudy/` packages have been removed. All active development is now in the `front-end/` directory, which is a full-stack JavaScript project (React + Express.js).

## Development Commands

All commands run from `front-end/`:

```bash
cd front-end

# Install dependencies
npm install

# Run Vite dev server (React, port 5173)
npm run dev

# Run Express BFF server (port 8000, hot-reload via nodemon)
npm run start

# Build for production
npm run build
```

All three backend services must run concurrently during development: React (5173), Express BFF (8000), FastAPI (8001), and Agent Server (9000).

## Architecture

### Multi-Process Backend

```
front-end/
├── src/                    # React (Vite) frontend
│   ├── App.jsx             # Router config + ProtectedRoute auth guard
│   ├── Screen/
│   │   ├── Auth/           # Home, Login, Register, ForgotPassword
│   │   └── App/            # CaseList, CaseRunner, AssetStudio, EnvironmentDesigner, PersonaDesigner, User, HistoryDetail
│   ├── components/         # NavBar, SideBar, Footer, LanguageSwitcher
│   └── i18n.js             # i18next setup (en + vi, fallback: vi)
└── server/                 # Express.js BFF
    ├── index.js            # Entry point (port 8000)
    ├── routes/             # authRoutes, userRoutes, caseRoutes, sessionRoutes
    ├── models/             # Mongoose schemas
    ├── middleware/         # JWT auth middleware
    └── config/             # MongoDB connection
```

Four backend services are required:

| Port | Tech | Responsibility |
|------|------|----------------|
| 8000 | Express (Node) | Auth (login/register), case listing, session history storage in MongoDB |
| 8001 | FastAPI (Python) | Auth, case data retrieval, asset design agent (OpenAI GPT-4o-mini) |
| 9000 | Agent Server | Stateful case simulation sessions (LangChain/LangGraph, traced via LangSmith) |
| 5173 | Vite (React) | Frontend dev server |

### Data Storage

- **MongoDB (Mongoose)**: Users, JWT auth, session history — managed by Express (port 8000)
- **Case data**: Served by FastAPI (port 8001) via `GET /api/cases/{caseId}` — returns `{skeleton, personas, context}`

### Auth Flow

JWT stored in `localStorage`. `ProtectedRoute` wrapper in `App.jsx` checks token existence and expiry (decoded client-side) before rendering protected routes. Auth endpoints exist on **both** port 8000 (Express) and port 8001 (FastAPI) — login/register goes to port 8001.

### Key Screens

| Screen | Route | Purpose |
|--------|-------|---------|
| `CaseList` | `/case-list` | Browse cases; fetches from Express `GET /api/cases` |
| `CaseRunner` | `/case-runner/:caseId` | Chat simulation; fetches case from port 8001, runs turns via port 9000 |
| `AssetStudio` | `/asset-studio` | Asset library; syncs from Unity via port 8001 |
| `EnvironmentDesigner` | `/asset-studio/environment-create` | Unity 3D environment builder via WebGL bridge + agent |
| `PersonaDesigner` | `/asset-studio/persona-create` | Avatar/character creator with voice preview |
| `HistoryDetail` | `/history/:sessionId` | Past session transcript replay |
| `User` | `/user` | User profile |

Note: A `/case-designer` route exists in the router but renders a "Coming Soon" placeholder (ReactFlow-based visual case designer not yet built).

### CaseRunner Data Flow

1. `GET http://localhost:8001/api/cases/{caseId}` → `{skeleton, personas, context}`
2. `POST http://localhost:9000/api/agent/sessions` with `{case_id, lazy_init, skip_tts}` → `{sessionId}`
3. `POST http://localhost:9000/api/agent/sessions/{sessionId}/turn` with `{user_input}` → updated `state`
4. On completion: `POST http://localhost:8000/api/sessions/history` to persist transcript

Agent state returned each turn: `{dialogue_history, active_personas, event_summary, current_event, last_score}`. Case ends when `current_event === null` or all canon events pass evaluation.

### Unity WebGL Integration

`EnvironmentDesigner` and `PersonaDesigner` embed Unity via `react-unity-webgl`. Commands are sent via `sendMessage("MCP_Manager", "ExecuteCommandFromWeb", jsonString)`. The design agent (port 8001) interprets natural language prompts into structured JSON actions (`spawn_object`, `set_lighting`, `set_avatar`, etc.) using GPT-4o-mini.

### Case Data Model (Skeleton Schema)

- `skeleton`: Event graph — nodes with `intro_message`, `learning_objective`, `citations`, `max_turns`, `on_success`/`on_failure` transitions, `evaluation_criteria`
- `personas`: Character list with `id`, `name`, `role`, `default_traits`, `voice_profile`, `unity_asset_key`
- `context`: Environment info with `environment_id`, `anchor_points`, `unity_scene_key`

### Planned / In-Progress

Per `Task.md` and design docs:
- **Unity MCP integration**: Agent receives spatial context (`anchor_point`, `subject_focus`, `available_affordances`) from Unity via MCP, emits `signal_output` JSON for lip-sync/animation
- **Parallel node execution**: LangGraph multi-threading so Persona response is not blocked by Policy/Action evaluation
- **Hotel domain**: Single-persona hotel receptionist scenario with static inline data instead of VectorDB lookups
- **Visual case designer**: ReactFlow-based `CaseDesigner` screen (route exists, not yet built)

## Environment Variables

Create `front-end/.env`:
```
# Express BFF (port 8000)
PORT=8000
MONGO_URI=<mongodb connection string>
JWT_SECRET=<secret>

# FastAPI + Agent backend (port 8001)
OPENAI_API_KEY=<key>
OPENAI_MODEL=gpt-4o-mini

# LangSmith tracing (port 9000 agent)
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=<key>
LANGCHAIN_PROJECT=CaseStudy
```

## Key Libraries

- **react-unity-webgl**: Unity WebGL embedding and JS↔Unity messaging
- **ReactFlow** (`reactflow`): Planned for visual case designer
- **i18next / react-i18next**: UI internationalization (en + vi, files in `src/locales/`)
- **Mongoose**: MongoDB ODM for Express server models
- **react-pro-sidebar**: Sidebar navigation component
- **Tailwind CSS**: Utility-first styling with custom primary color palette (`#1EA97C`)
