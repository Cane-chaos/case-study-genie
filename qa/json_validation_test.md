# JSON Validation Test

## Mục tiêu

Kiểm tra:
- JSON schema đúng
- Không deserialize error
- ID unique
- Required fields tồn tại

---

# TEST 1 — Missing Persona

## Expected

400 Bad Request

---

# TEST 2 — Duplicate Object ID

## Expected

Validation failed

---

# TEST 3 — Invalid Position Format

## Invalid

"position": "left side"

## Expected

Validation error

---

# TEST 4 — Missing reward_system

## Expected

Schema validation failed

---

# TEST 5 — Invalid trust value

## Invalid

"initial_trust": 999

## Expected

Reject invalid range

---

# PASS Criteria

✅ Proper validation errors  
✅ No backend crash  
✅ Clear error message