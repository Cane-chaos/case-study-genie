# 📄 CaseStudy Engine: State Specification (v1.1)

This document defines the shared data structure (Schema) for information exchange between **Unity (Frontend)** and **LangGraph Agent (Backend)**. This is the sole "contract" so both sides can work in parallel without data drift.

---

## 1. Overall State Structure (JSON)

This structure is used for storage in MongoDB and as the payload sent via API.

```json
{
  "case_metadata": {
    "case_id": "hotel_reception_001",
    "title": "Handling Angry VIP Guest",
    "difficulty": "Medium",
    "category": "Hospitality"
  },

  "environment": {
    "scene_id": "env_hotel_lobby_luxury",
    "smart_objects": [
      {
        "id": "obj_ticket_001",
        "name": "Booking Slip",
        "position": [1.2, 0.8, -0.5],
        "is_required": true
      },
      {
        "id": "obj_phone",
        "name": "Desk Phone",
        "position": [0.5, 0.8, 0.0],
        "is_required": false
      }
    ]
  },

  "persona": {
    "name": "Mr. Viktor",
    "prefab_id": "char_victor_v3",
    "traits": ["Hot-tempered", "Values punctuality", "Prefers professionalism"],
    "voice_style": "Deep, decisive",
    "initial_trust": 40,
    "goals": ["To be checked in immediately", "To receive a sincere apology"]
  },

  "knowledge_base": {
    "context": "The hotel lobby is during peak hours, the air conditioning is slightly broken.",
    "policies": [
      {
        "id": "pol_vip_01",
        "title": "VIP Guest Reception Procedure",
        "content": "Always verify VIP card before greeting..."
      }
    ]
  },

  "reward_system": {
    "rules": [
      {
        "trigger": "user_read:obj_ticket_001",
        "reward": 10,
        "penalty": -5,
        "explanation": "Verified customer information."
      }
    ],
    "passing_score": 70
  },

  "runtime_state": {
    "current_score": 0,
    "current_event": "event_ingress",
    "history": [],
    "interacted_objects": [],
    "active_memory": "Viktor just walked in and looks very uncomfortable."
  }
}
```

---

## 2. Key Component Explanations

### 🏗️ Environment
*   **scene_id**: Identifies which Unity Scene to load.
*   **smart_objects**: List of interactable objects. Each object has a unique `id` so the Backend can score when the user clicks on it.

### 🎭 Persona
*   **traits**: Keywords for the Agent (Python) to adjust response style.
*   **initial_trust**: Initial trust score, which changes based on the user's behavior.

### 📚 Knowledge Base
*   **policies**: Business rules (Medical, Receptionist, etc.). The Agent uses this content for citations and as a basis for deducting points if the trainee makes a mistake.

### 🏆 Reward System
*   Defines `trigger` (activation actions) and corresponding point bonuses/penalties.
*   `explanation`: This content is shown to the trainee on the Summary page after the Simulation ends.

### 🔄 Runtime State
*   Contains conversation history (`history`) and interacted objects (`interacted_objects`). This is the data that changes continuously after each chat turn.

---

## 3. Multi-Agent Workflow

To process this State, the Backend uses 4 specialized Agents:
1.  **Interpreter Agent**: Analyzes input from Unity (Text + Mouse clicks).
2.  **Persona Agent**: Role-plays the character based on `traits` to respond.
3.  **Chronicler Agent**: Summarizes developments into `active_memory`.
4.  **Judge & Reward Agent**: Compares user actions against `reward_system.rules` to calculate scores.

---

## 4. Important Notes
*   All `ID`s must be unique within a Case.
*   Coordinate data in `position` follows the Unity coordinate system (Vector3).
