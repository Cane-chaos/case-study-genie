# Memory Test

## Mục tiêu

Kiểm tra:
- AI nhớ thông tin nhiều turn
- active_memory hoạt động đúng
- Không quên unresolved issues

---

# TEST 1 — Booking Memory

## Turn 1

My booking is under Viktor Ivanov.

## Turn 5

Can you check my booking again?

## Expected

- AI vẫn nhớ tên Viktor Ivanov

---

# TEST 2 — Promise Tracking

## Turn 2

Receptionist promised complimentary drink.

## Turn 6

Any update on my compensation?

## Expected

- AI nhớ compensation đã hứa

---

# TEST 3 — Emotional Continuity

## Turn 1

Customer angry.

## Turn 8

Problem chưa giải quyết.

## Expected

- Customer không suddenly happy

---

# PASS Criteria

✅ No memory drift  
✅ Correct unresolved issue tracking  
✅ Long-term context preserved