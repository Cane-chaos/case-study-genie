# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Type

Unity 6 project using **UI Toolkit** (not uGUI). All UI is built with `.uxml` layout files + `.uss` stylesheets + C# controllers. Do not use GameObject/Canvas/Hierarchy-based UI patterns.

## Build & Run

- Open the project in Unity Editor (Unity Hub or `unity -projectPath .`)
- Play mode runs the `SampleScene` scene
- No CLI build/test commands — all development happens in the Unity Editor

## Project Overview

CaseStudy Engine is a group development project. The system consists of a Unity 6 frontend and a Python FastAPI backend (separate repository) connected via REST/gRPC.

## Architecture

### State Machine

`SimulationManager` is a singleton that owns the global `SimulationState` enum:
```
MainMenu → Designer → Playing
```
Call `SimulationManager.Instance.ChangeState(newState)` to transition. Never create additional instances.

### Event Bus

`EventManager` is a static event aggregator. Subscribe via `EventManager.OnStateChanged += handler`. This decouples UI panels from the state machine.

### UI Pattern (Screen-per-Controller)

Each screen has three matched files:
- `UI/Documents/{Screen}_UI.uxml` — layout (Unity UI Builder or hand-written UXML)
- `UI/Styles/{Screen}_UI.uss` — visual styling (CSS-like)
- `Scripts/UI/{Screen}_Controller.cs` — logic (RegisterCallback for events, UIDocument queries)

Controllers drive the UXML. Do not put game logic in UI code.

### Data Models

`Scripts/Models/CaseModel.cs` defines `CaseStudyData` and `PersonaConfig` — these map to the JSON schema used by the Python backend.

### Key Packages

- **UI Toolkit** (`com.unity.modules.uielements`) — all UI
- **Input System** (`com.unity.inputsystem`) — player input
- **Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`) — API serialization
- **Burst/Collections/Mathematics** — DOTS HPC# (present for future optimization)

## Team Conventions

- **Core files are read-only**: `Scripts/Core/` contains `SimulationManager` and `EventManager` — do not modify these.
- **File naming**: Prefix all task files with the task name, e.g. `Auth_Login.uxml`, `Auth_Controller.cs`.
- **One screen per scope**: Work only in the `.uxml`, `.uss`, and `Controller.cs` for your assigned screen.
- **SimulationManager transitions**: On successful auth, call `SimulationManager.Instance.ChangeState(SimulationState.Designer)`. On "Play", transition to `SimulationState.Playing`.
- **API calls**: Place in `Scripts/Networking/`. Backend is a Python FastAPI service (separate repo).

## Project Structure

```
Assets/_Project/
├── UI/
│   ├── Documents/      (UXML layout files)
│   ├── Styles/         (USS stylesheets)
│   └── PanelSettings/  (Project-wide panel settings)
├── Scripts/
│   ├── Core/           (SimulationManager, EventManager — do not modify)
│   ├── UI/             (Screen controllers)
│   ├── Models/         (CaseStudyData, PersonaConfig)
│   └── Networking/     (API calls to Python backend)
└── Prefabs/
    ├── Characters/     (3D character prefabs)
    └── Environments/   (3D environment prefabs)
```

## Important Notes

- `.uxml`/`MainPanelSettings.asset` live in `UI/PanelSettings/` and configure the root UI Document panel settings.
- Prefabs for 3D characters and environments are in `Prefabs/Characters/` and `Prefabs/Environments/`.
- `mono_crash.*.json` files are profiling artifacts from Burst — safe to ignore or delete.
