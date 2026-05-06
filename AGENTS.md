# Repository Guidelines

## Project Structure & Module Organization

This repository combines a Python FastAPI backend and a Unity 6 client.

- `back-end/` contains backend code, documentation, case data, and generated schema assets.
- `back-end/api_casestudy/` is the auth and case-management API. Routers live in `routers/`, database setup in `db/`, configuration in `core/`, and service logic in `services/`.
- `back-end/api_agent/` contains the simulation/agent API with routers, services, prompts, and dependencies.
- `back-end/data/` stores case-tree JSON files named like `Agent1_Root.json` and `Agent3_Node_3_2.json`.
- `CaseStudy_Unity_M4/` is the Unity project. Main project code is under `Assets/_Project/`.
- `Assets/_Project/Scripts/` is split into `Core/`, `Models/`, `Networking/`, and `UI/`. UI Toolkit assets belong in `Assets/_Project/UI/`.

## Build, Test, and Development Commands

Backend commands run from `back-end/`:

```bash
uv sync
python api_casestudy/main.py
python api_agent/main.py
```

`uv sync` installs Python dependencies from `pyproject.toml` and `uv.lock`. `python api_casestudy/main.py` starts the auth API on port `8001`. `python api_agent/main.py` starts the agent API on port `9000` when configured.

Unity development is done in Unity Editor `6000.4.0f1`. Open `CaseStudy_Unity_M4/`, load `Assets/Scenes/SampleScene.unity`, and press Play.

## Coding Style & Naming Conventions

Use 4-space indentation for Python and C#. Keep FastAPI route handlers thin and move reusable behavior into `services/`. Store secrets in `back-end/.env`; do not hardcode connection strings or API keys.

Unity UI uses UI Toolkit only: pair each screen as `UI/Documents/{Screen}_UI.uxml`, `UI/Styles/{Screen}_UI.uss`, and `Scripts/UI/{Screen}_Controller.cs`. Keep API calls in `Scripts/Networking/`. Avoid modifying core Unity managers unless the state flow requires it.

## Testing Guidelines

No formal test suite is currently committed. For backend changes, at minimum run the relevant API locally and check `/healthz` and `/docs`. Add future Python tests under `back-end/tests/` using `test_*.py` naming. For Unity changes, verify Play Mode in `SampleScene` and include screenshots or recordings for UI changes.

## Commit & Pull Request Guidelines

Recent commits use short imperative summaries, sometimes with a prefix such as `chore:`. Prefer messages like `Add auth service validation` or `chore: move backend files`.

Pull requests should include a brief summary, affected areas (`back-end`, Unity, or both), setup/configuration notes, and manual test results. Link related tasks or issues. Include screenshots for Unity UI changes and API examples for backend endpoint changes.
