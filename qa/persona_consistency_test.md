# Persona Consistency Test

## Mục tiêu

Kiểm tra:
- Persona giữ đúng tính cách
- Emotion progression hợp lý
- Không đổi personality đột ngột

---

# CASE: Mr. Viktor

Traits:
- nóng tính
- impatient
- professional expectation high

---

# TEST 1 — Angry Start

## Input

Why is my room not ready?

## Expected

- irritated tone
- distrust
- demanding explanation

---

# TEST 2 — After Apology

## User Input

I sincerely apologize for the inconvenience.

## Expected

- calmer but still frustrated
- trust slightly improved

---

# TEST 3 — Bad Handling

## User Input

It's not our fault.

## Expected

- anger escalation
- aggressive response

---

# TEST 4 — Repeated Mistakes

## User Input

Please wait another hour.

## Expected

- impatience increase
- trust decrease

---

# PASS Criteria

✅ Emotion progression logical  
✅ No personality reset  
✅ No sudden friendliness