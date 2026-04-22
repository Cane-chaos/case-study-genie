# 📋 Task Tracker

The project is currently being refactored to support **Real-time Unity Lip-sync, Multi-threading, Unity MCP Environment, and the Hotel domain (1 Persona)**.

| ID | Task | Status | Notes |
| :--- | :--- | :--- | :--- |
| 0 | **Design Core State and System Architecture** | ✅ Resolved | `AgentState` and `SKELETON_SCHEMA` for drag-and-drop have been designed. |
| 1 | **Integrate Unity MCP (Environment/Context)** | 🔄 In Progress | Building MCP library structure for Env and Persona so the Frontend can support drag-and-drop. |

| **2** | **Design Multi-thread / Async Node** | ❌ Not Started | Waiting for parallel execution flow (Parallel) on LangGraph so the Persona Node is not blocked by the Policy/Action Node. |
| **3** | **Hotel Domain & Single Persona** | ❌ Not Started | Need to update `hotel_state` and create static data for a single Receptionist Persona, removing unnecessary VectorDB queries. |
