# API Test Log

## Environment

Backend:
http://localhost:8001

Swagger:
http://localhost:8001/docs

Date:
2026-05-09

Tester:
NHUNG

---

# TEST SESSION 001

## Endpoint

POST /api/cases

## Result

✅ Success

## Notes

Case created successfully.

---

# TEST SESSION 002

## Endpoint

POST /api/agent/sessions

## Result

✅ Success

## Notes

Session initialized correctly.

---

# TEST SESSION 003

## Endpoint

POST /api/agent/sessions/{id}/turn

## Result

⚠ Minor issue

## Notes

Persona repeated same sentence twice.

---

# TEST SESSION 004

## Endpoint

POST /api/agent/sessions/{id}/turn

## Result

❌ Failed

## Notes

Judge Agent hallucinated reward trigger.

---

# Summary

| Category | Status |
|----------|--------|
| Prompt Stability | PASS |
| Persona Consistency | PASS |
| Hallucination Resistance | WARNING |
| Reward Accuracy | WARNING |
| JSON Validation | PASS |