# CaseStudy Engine Project

## Overview
CaseStudy is an interactive simulation engine designed for medical and emergency response training. It uses **LangGraph** to coordinate a complex workflow of LLM-based reasoning, semantic retrieval, and state management. The project follows a unique **"3-Layer Memory"** architecture to ensure simulation consistency and realism.

### 🧠 3-Layer Memory Architecture
1.  **Logic Memory (Tầng 1 - Structured):** Defined in `skeleton.json` files. It governs the flow of the simulation using "Canon Events," preconditions, and success/failure transitions.
2.  **Semantic Memory (Tầng 2 - Ngữ nghĩa):** Uses Vector Databases (ChromaDB/Pinecone) to store scene descriptions, persona data (character traits), and policies (medical rules/guidelines).
3.  **Runtime State (Tầng 3 - Active Memory):** Manages the live simulation state, including dialogue history, persona trust levels, and current student actions.

---

## 🛠 Tech Stack
- **Language:** Python 3.12+
- **Orchestration:** LangGraph, LangChain
- **LLMs:** OpenAI (GPT-4o), Google Gemini (via `langchain-google-genai`)
- **Web Framework:** FastAPI, Uvicorn
- **Databases:** 
  - **Vector:** ChromaDB, Pinecone
  - **Document:** MongoDB (PyMongo)
- **Environment Management:** Poetry, uv, Conda
- **Frontend:** Vanilla HTML/JS/CSS (TailwindCSS for some components)

---

## 📂 Project Structure
- `casestudy/agent/`: Core LangGraph implementation.
  - `nodes/`: Individual processing steps (Ingress, Semantic, Policy, Action, etc.).
  - `chains/`: LLM-specific chains for scene narration, persona digests, and responding.
  - `state.py`: Pydantic models for simulation state.
  - `graph.py`: Assembly of the LangGraph workflow.
- `casestudy/app/`: The primary web application.
  - `api/v1/`: API endpoints for managing cases and user sessions.
  - `frontend/`: Static web files (Login, Case Selection, Chat Interface).
- `api_casestudy/`: A specialized API service for agent coordination.
- `casestudy/utils/`: Utilities for DB management, semantic extraction, and document building.
- `casestudy/agent/cases/`: Definition files for specific simulation cases (e.g., `electric_shock_001`).

---

## 🚀 Building and Running

### Prerequisites
- Python 3.12
- Poetry or `uv`
- MongoDB (running locally or via URI in `.env`)
- OpenAI/Google API Keys (configured in `.env`)

### Installation
```bash
poetry install
# or
uv sync
```

### Database Initialization
```bash
python casestudy/utils/create_DB.py
```

### Running the CLI Engine
```bash
python casestudy/main.py --case-id electric_shock_001
```

### Running the Web Applications
1.  **Main Web App (Frontend + Case Management):**
    ```bash
    uvicorn casestudy.app.main:app --reload --port 8000
    ```
2.  **Agent API Service (Agent Coordination):**
    ```bash
    uvicorn api_casestudy.main:app --reload --port 8001
    ```

---

## 🤝 Development Conventions
- **State Management:** Use `RuntimeState` (defined in `casestudy/agent/state.py`) for all simulation data. Avoid direct I/O in LangGraph nodes; use `RuntimeStateStore`.
- **Logic Updates:** Any changes to the simulation flow must be reflected in the `skeleton.json` of the respective case.
- **Semantic Updates:** Use `casestudy/utils/semantic_extract.py` to rebuild vector indexes when scene or policy data changes.
- **Coding Style:** Strictly follow PEP 8; use Pydantic for data validation and type hinting throughout the project.
- **Testing:** Run tests using `pytest`. New features should include corresponding test cases in `casestudy/app/tests/` or root test files.
