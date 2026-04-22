# CaseStudy Unity M4

Project overview (Unity, UI Toolkit + C#):

- `_Project/` — main source folder (distinguished from downloaded plugins).
  - `Scripts/`
    - `Core/`: Managers coordinating the main flow (SimulationManager, EventManager, etc.).
    - `UI/`: UI controllers (UI Toolkit, events, binding).
    - `Models/`: Data classes (CaseStudyData, PersonaConfig...).
    - `Networking/`: API calls to Python Backend.
  - `UI/`
    - `Documents/`: `.uxml` files defining UI layouts.
    - `Styles/`: `.uss` files (CSS for Unity).
  - `Prefabs/`
    - `Environments/`: Hotel lobby, sample rooms.
    - `Characters/`: Mr. Viktor and other characters.

Quick start:
- Open the main scene and press Play: `SimulationManager` holds the state and fires events through `EventManager` to the UI.
- Add UI: create `.uxml` in `UI/Documents`, style with `.uss` in `UI/Styles`, and C# controller in `Scripts/UI`.
- Case data: define in `Scripts/Models`, load/parse, then pass to the UI.
- Connect backend: place API call code in `Scripts/Networking` (REST/gRPC depending on Python backend).

Notes:
- Keep the folder structure consistent to avoid conflicts when importing assets/plugins.
- Environment/character prefabs should reference scripts and styles via UI Toolkit rather than hardcoding.
