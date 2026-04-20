# CURRENT_DEMO_SCOPE
> **Cập nhật lần cuối:** 2026-04-19 — Scope được điều chỉnh: STT là input chính, Smart Object click là Future Implementation.

---

## 1. Mục Tiêu Hiện Tại

Dự án **không nhắm đến full system** ở giai đoạn này.
Mục tiêu trước mắt là hoàn thành **một vertical slice demo có thể chạy end-to-end**:

1. Xây dựng **một map Unity 3D** (sảnh khách sạn).
2. Người chơi vào vai **nhân viên lễ tân**.
3. AI vào vai **khách hàng VIP có vấn đề**.
4. Người chơi **nói bằng giọng nói** (Speech-to-Text) để phản hồi.
5. NPC **phát âm thanh và nhép miệng** (TTS + Lip-sync) để trả lời.
6. Hệ thống **phân nhánh kịch bản** và **chấm điểm** theo hành vi.

---

## 2. Hướng Phát Triển Hiện Tại

Tập trung vào **simulation demo**, không phải full product suite.

- ✅ Một môi trường 3D
- ✅ Một vòng tương tác hoàn chỉnh
- ✅ Một AI Persona (khách VIP)
- ✅ Một kịch bản có phân nhánh
- ✅ Hệ thống chấm điểm
- ✅ Màn hình tổng kết

---

## 3. Demo Scenario

| Yếu tố | Nội dung |
|--------|----------|
| **Bối cảnh** | Sảnh khách sạn / quầy lễ tân |
| **Vai người chơi** | Nhân viên lễ tân (Receptionist) |
| **Vai AI** | Khách VIP có vấn đề (phòng chưa sẵn sàng, đặt phòng lỗi...) |
| **Mục tiêu demo** | Roleplay bằng giọng nói + AI phân nhánh + chấm điểm + NPC lip-sync |

---

## 4. Input / Output Chính (⭐ Core Architecture)

### Input: Speech-to-Text (Ưu tiên cao nhất)

```
[Người chơi nhấn/giữ phím Space hoặc nút Mic]
        ↓
Unity Microphone API — thu âm PCM
        ↓
Convert → WAV bytes
        ↓
POST {BackendBaseUrl}/api/stt  ← FastAPI backend
        ↓
STTResponse { text, language, confidence }
        ↓
Hiển thị text lên UI (subtitle người chơi)
        ↓
CaseService.SendTurn(text) → Agent Server
```

**Cài đặt (SpeechInputController.cs):**
- **Hold-to-Talk:** Giữ `Space` để nói, thả để gửi (mặc định)
- **Toggle:** Nhấn 1 lần để bắt đầu, nhấn lần nữa để dừng
- Hiển thị icon mic khi đang thu âm (`EventManager.OnSTTRecordingStarted`)
- Hiển thị text kết quả (`EventManager.OnSTTResult`)

### Output: NPC Lip-sync + Audio (Ưu tiên cao)

```
[Agent Server trả về AgentTurnResponse]
        ↓
Lấy audio_url từ response
        ↓
NPCOutputController tải audio từ URL
        ↓
AudioSource.Play() — phát giọng NPC
        ↓
Đồng thời: Lip-sync theo amplitude của AudioSource
        ↓
EventManager.TriggerNPCSpeakStart / SpeakEnd
        ↓
Animator trigger theo persona_emotion ("angry", "calm"...)
```

**Cài đặt (NPCOutputController.cs):**
- Gắn vào GameObject của NPC
- Gán `AudioSource`, `Animator`, `SkinnedMeshRenderer` (cho BlendShape)
- Tên BlendShape miệng: `MouthOpen` (mặc định — điều chỉnh theo model)
- Emotion triggers: `Angry`, `Happy`, `Neutral`, `Impatient`

---

## 5. Chế Độ Gameplay

### ✅ Mode A — Speech Dialogue (Đang làm — Ưu tiên)
Người chơi **nói bằng giọng nói** → STT → gửi text lên AI → NPC phản hồi bằng giọng + lip-sync.

### ⚠️ Mode B — Smart Object Click (FUTURE IMPLEMENTATION)
Người chơi click vào vật thể 3D (phiếu đặt phòng, điện thoại...) → intent nghiệp vụ.

> **Lý do hoãn:** Giao diện giọng nói phức tạp hơn và cần ưu tiên hơn cho demo. Smart Object được code skeleton sẵn để implement sau.
>
> **Files liên quan (đã viết, chưa active):**
> - `SmartObjectController.cs` — có header FUTURE IMPLEMENTATION
> - `InteractionManager.cs` — đã disable trong Awake()

---

## 6. Unity Scope Hiện Tại

Unity cần cung cấp:
- Một map hotel lobby 3D
- Khu vực quầy lễ tân
- NPC có Animator + SkinnedMeshRenderer (cho lip-sync)
- **Microphone input UI** (icon mic, text transcription, loading indicator)
- **Chatbox / dialogue area** (hiển thị lịch sử hội thoại)
- Score / trust display
- Màn hình tổng kết sau khi kết thúc

Unity **không cần** trong scope này:
- ~~Smart Object interaction system (click vật thể)~~ → Future
- ~~Full Asset Library UI~~ → Future
- ~~Full Case Designer~~ → Future

---

## 7. Backend Scope Hiện Tại

Backend cần cung cấp:
- **`POST /api/stt`** — Nhận WAV audio, trả về `{ text, language, confidence }`
- Persona-based AI response với `audio_url` trong response
- Phân nhánh kịch bản và chấm điểm
- `AgentTurnResponse` phải có field `audio_url` (hoặc `audio_base64`)
- **Đảm bảo `skip_tts = false`** được xử lý đúng (sinh TTS audio)

---

## 8. Priority Modules

| Ưu tiên | Module |
|---------|--------|
| 🔴 **Cao nhất** | Speech-to-Text input (`SpeechInputController.cs`) |
| 🔴 **Cao nhất** | NPC lip-sync + audio output (`NPCOutputController.cs`) |
| 🔴 **Cao** | AI Backend / State Handling |
| 🔴 **Cao** | Simulator UI (chatbox, score, mic UI) |
| 🟡 **Thấp** | Authentication |
| ⚪ **Hoãn** | Smart Object Click, Asset Library, Case Designer |

> **Nguyên tắc:** Ưu tiên **chiều sâu trong 1 kịch bản hoàn chỉnh** (nói → NPC phản hồi bằng giọng), không dàn trải.

---

## 9. Kết Quả Mong Đợi Cuối Phase

Demo phải thể hiện được:
1. Người chơi đứng trong scene hotel lobby 3D
2. Khách VIP AI xuất hiện và trình bày vấn đề (có giọng nói, NPC nhép miệng)
3. Người chơi **nói vào microphone** → text hiện lên màn hình
4. AI **phản hồi bằng giọng nói** + NPC nhép miệng theo
5. Kịch bản **phân nhánh** dựa trên cách xử lý của người chơi
6. Kết thúc → **màn hình tổng kết** với điểm và nhận xét

---

*Tài liệu này phản ánh scope demo thực tế. Mọi kế hoạch, code và task phân chia phải align với mục tiêu này trước.*
