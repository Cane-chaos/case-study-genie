# CURRENT_EXPECTED

## 1. Current Goal
This project is currently **not targeting the full system** yet.
The immediate objective is to complete **one playable vertical slice** that can be demonstrated clearly.

The current expected scope is:
1. Build **one Unity 3D map**.
2. The player takes the role of a **hotel receptionist**.
3. The AI plays the role of a **customer** who brings a problem based on a scenario.
4. The player solves the problem through:
   - **object interaction mode** (guided / multiple-choice style)
   - **free-text dialogue mode** (open-ended / essay style)
5. The AI core generates responses, branches the scenario, and scores the player based on the chosen action or typed response.

---

## 2. Current Product Direction
The current direction is a **simulation demo**, not a full product suite.

This means the focus is on:
- one 3D environment
- one complete interaction loop
- one AI customer persona
- one scenario with branching
- one scoring flow
- one final feedback screen

The focus is **not** on completing all planned modules such as full authentication, full asset library, full case designer, or multi-map production.

---

## 3. Core Demo Scenario
The current expected demo scenario is:

- Setting: **Hotel lobby / reception desk**
- Player role: **Receptionist**
- AI role: **Customer / VIP guest with a problem**
- Situation example: customer arrives angry because the room is not ready, booking has an issue, or service expectation is not met.

The purpose of the demo is to show:
- roleplay between player and AI customer
- decision making through interaction with work-related objects
- AI branching based on player handling
- score and feedback generation

---

## 4. Gameplay Modes
The current expected system must support **2 modes** inside the simulator.

### Mode A - Guided Interaction Mode
This is the **object-based mode**.
The player interacts with objects in the 3D environment.
Each object represents **one handling direction** or **one problem-solving action**.

This mode should behave like a hidden multiple-choice system inside a 3D simulation.

### Mode B - Free Response Mode
This is the **self-written dialogue mode**.
The player types what they want to say to the customer.
The AI evaluates the response and continues the scenario dynamically.

This mode should behave like an open-ended roleplay evaluation mode.

---

## 5. Object Interaction Design Principle
This is a critical expectation.

In the current scope, objects are **not only decorative props**.
Each important object must represent:
- a business action
- an information source
- or a problem-solving direction

### Example interaction mapping
- **Booking slip** -> verify customer information
- **Reception computer / terminal** -> check booking or room status
- **Desk phone** -> call manager or internal department
- **VIP policy book** -> consult service policy
- **Voucher / compensation item** -> offer compensation
- **Keycard / room tool** -> reassign or activate room solution

So in this design:
- an object is a gameplay decision point
- interaction with an object is equivalent to choosing a handling strategy

---

## 6. Expected Meaning of Smart Objects
Each smart object should carry gameplay meaning beyond its visual representation.

Each object is expected to map to:
- `object_id`
- `interaction_type`
- `intent`
- optional `available_actions`
- optional score/reward trigger

### Suggested examples
- `verify_customer_info`
- `check_room_status`
- `call_manager`
- `call_housekeeping`
- `offer_compensation`
- `follow_vip_policy`
- `reassign_room`

The backend should interpret player interaction through these intents, not only by raw object name.

---

## 7. Current AI Core Expectation
The AI core is expected to do the following:

1. Play the customer persona consistently.
2. Generate reactions based on:
   - current scenario state
   - player choice
   - player text input
   - current trust / emotion / progress
3. Branch the scenario logically.
4. Score the player action.
5. Return feedback that explains why the action is good, weak, or wrong.

The AI should not behave like a random chatbot.
It should behave like a controlled scenario engine.

---

## 8. Current Branching Logic Expectation
The scenario should branch according to player behavior.

At minimum, branching should depend on:
- whether the player verified information first
- whether the player escalated correctly
- whether the player showed empathy
- whether the player chose an appropriate resolution
- whether the player used compensation at the right time

The AI customer state can change across branches such as:
- angry
- impatient
- neutral
- calmer
- satisfied

The system should also track whether the situation is:
- not yet understood
- verified
- being resolved
- escalated
- solved
- mishandled

---

## 9. Current Scoring Expectation
The system should support scoring for both modes.

### For object interaction mode
Scoring should be more structured.
Each action or interaction can trigger a reward or penalty rule.

### For free response mode
Scoring should be rubric-based.
Possible evaluation criteria:
- professionalism
- empathy
- correctness of handling
- clarity of proposed solution

The result does not need to be overly complex at first, but it must clearly show:
- score change
- reason for score
- final result / feedback

---

## 10. Current Unity Scope
Unity is expected to provide:
- one hotel lobby style 3D map
- receptionist area
- customer NPC presence
- clickable smart objects
- simulator UI
- chat / dialogue area
- score or trust display
- final feedback screen

Unity currently does **not need** to fully implement all long-term modules before the simulator demo works.

---

## 11. Current Backend Scope
Backend is expected to provide:
- scenario state handling
- persona-based AI response
- branching logic
- evaluation logic
- score calculation
- history/runtime update

Backend should be able to receive two input forms:
1. object interaction input
2. free-text dialogue input

---

## 12. Current Priority Modules
For the current phase, the priority is:

1. **Simulator**
2. **AI Backend / State Handling**
3. **3D Environment Interaction**

The following modules are lower priority for now and may be simplified or postponed:
- Authentication
- Asset Library
- AI Architect Creator
- Full Case Designer

---

## 13. Current File/Design Priority
If generating code or task suggestions, priority should be given to:
- simulator UI/controller
- smart object model/state definition
- backend request/response flow
- object interaction system
- scenario branching and scoring
- one working demo case

Do not optimize for the full platform first.
Optimize for one strong demo flow first.

---

## 14. What Should Be Avoided Right Now
At the current stage, avoid pushing the project toward unnecessary complexity such as:
- multiple maps
- many personas at once
- a full production content pipeline
- overbuilt architecture before one case works
- redesigning the whole core system
- spreading development equally across all modules

The current expectation is **depth in one scenario**, not breadth across all features.

---

## 15. Final Expected Outcome of This Phase
By the end of the current phase, the project should be able to demonstrate:
- a player standing in a 3D hotel reception context
- an AI customer presenting a problem
- the player solving it either by object interaction or typed dialogue
- the AI reacting dynamically
- the system branching and scoring the experience
- a clear result / feedback screen at the end

This is the current expected milestone.
All planning, coding, prompting, and task decomposition should align with this target first.
