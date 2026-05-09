# QA Test Suite — CaseStudy Engine

Folder này chứa toàn bộ test scenario cho hệ thống AI simulation.

## Mục tiêu QA

Đảm bảo:
- Agent không hallucinate
- Persona không thoát vai
- Runtime memory ổn định
- Reward system hoạt động đúng
- JSON đúng schema
- API phản hồi đúng format

---

# Các file test

| File | Mục tiêu |
|------|----------|
| hallucination_test.md | Kiểm tra hallucination và prompt injection |
| persona_consistency_test.md | Kiểm tra consistency của persona |
| memory_test.md | Kiểm tra long-term memory |
| judge_reward_test.md | Kiểm tra reward & scoring |
| json_validation_test.md | Kiểm tra schema JSON |
| api_test_log.md | Ghi log test thực tế |

---

# Công cụ sử dụng

- Swagger UI
- Postman
- FastAPI docs
- LangSmith tracing

---

# API Endpoint

Base URL:

http://localhost:8001/docs

---

# Tiêu chí PASS

- JSON parse thành công
- Không break character
- Không hallucinate policy
- Score consistent
- Không crash runtime