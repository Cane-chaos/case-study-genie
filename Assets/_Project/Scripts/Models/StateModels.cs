using System;
using System.Collections.Generic;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════
// CaseStudy Engine — Data Models
// Map 1:1 với JSON schema trong STATE_SPEC.md
// Dùng cho serialization/deserialization giữa Unity ↔ Backend
// ═══════════════════════════════════════════════════════════════

// ───────────────────────────────────────────
// CASE METADATA
// ───────────────────────────────────────────

[Serializable]
public class CaseMetadata
{
    public string case_id;
    public string title;
    public string difficulty;   // "Easy", "Medium", "Hard"
    public string category;     // "Hospitality", "Healthcare"...
}

// ───────────────────────────────────────────
// ENVIRONMENT
// ───────────────────────────────────────────

[Serializable]
public class SmartObject
{
    public string id;           // Ví dụ: "obj_ticket_001"
    public string name;         // Ví dụ: "Lá phiếu đặt phòng"
    public float[] position;    // [x, y, z] — tọa độ Unity Vector3
    public bool is_required;    // Có bắt buộc tương tác không

    // ── Mở rộng cho gameplay (từ current_expected) ──
    public string interaction_type;     // "click", "pickup", "examine"
    public string intent;               // "verify_customer_info", "check_room_status"...
    public string[] available_actions;  // Danh sách hành động khả dụng

    /// <summary>Convert mảng float[3] sang Unity Vector3.</summary>
    public Vector3 GetPosition()
    {
        if (position != null && position.Length >= 3)
            return new Vector3(position[0], position[1], position[2]);
        return Vector3.zero;
    }
}

[Serializable]
public class EnvironmentData
{
    public string scene_id;             // Xác định Unity Scene nào sẽ được nạp
    public List<SmartObject> smart_objects;
}

// ───────────────────────────────────────────
// PERSONA
// ───────────────────────────────────────────

[Serializable]
public class PersonaData
{
    public string name;             // Ví dụ: "Mr. Viktor"
    public string prefab_id;        // Ví dụ: "char_victor_v3"
    public string[] traits;         // ["Nóng tính", "Coi trọng thời gian", ...]
    public string voice_style;      // "Trầm, dứt khoát"
    public int initial_trust;       // 0-100, mức tin tưởng ban đầu
    public string[] goals;          // ["Được check-in ngay", "Nhận lời xin lỗi"]

    // ── Mở rộng cho AI Persona Agent ──
    public string role;             // "customer", "patient"...
    public string unity_asset_key;  // Key để load prefab từ Resources
}

// ───────────────────────────────────────────
// KNOWLEDGE BASE
// ───────────────────────────────────────────

[Serializable]
public class PolicyItem
{
    public string id;       // "pol_vip_01"
    public string title;    // "Quy trình đón khách VIP"
    public string content;  // Nội dung chi tiết policy
}

[Serializable]
public class KnowledgeBase
{
    public string context;              // Bối cảnh tình huống
    public List<PolicyItem> policies;   // Các quy tắc nghiệp vụ
}

// ───────────────────────────────────────────
// REWARD SYSTEM
// ───────────────────────────────────────────

[Serializable]
public class RewardRule
{
    public string trigger;      // "user_read:obj_ticket_001"
    public int reward;          // Điểm cộng (ví dụ: +10)
    public int penalty;         // Điểm trừ (ví dụ: -5)
    public string explanation;  // Giải thích hiển thị cho học viên
}

[Serializable]
public class RewardSystem
{
    public List<RewardRule> rules;
    public int passing_score;   // Ngưỡng đạt (ví dụ: 70)
}

// ───────────────────────────────────────────
// RUNTIME STATE (Thay đổi liên tục khi chạy)
// ───────────────────────────────────────────

[Serializable]
public class ChatMessage
{
    public string role;     // "user" hoặc "assistant"
    public string content;  // Nội dung tin nhắn
    public string timestamp;
}

[Serializable]
public class RuntimeState
{
    public int current_score;
    public string current_event;            // Event node hiện tại trong skeleton
    public List<ChatMessage> history;       // Lịch sử hội thoại
    public List<string> interacted_objects; // Danh sách objectId đã tương tác
    public string active_memory;            // Tóm tắt bối cảnh hiện tại

    // ── Mở rộng cho branching (từ current_expected) ──
    public string customer_emotion;         // "angry", "neutral", "satisfied"...
    public string situation_status;         // "not_understood", "verified", "solved"...

    public RuntimeState()
    {
        current_score = 0;
        history = new List<ChatMessage>();
        interacted_objects = new List<string>();
        customer_emotion = "angry";
        situation_status = "not_understood";
    }

    /// <summary>Kiểm tra xem một object đã được tương tác chưa.</summary>
    public bool HasInteracted(string objectId)
    {
        return interacted_objects != null && interacted_objects.Contains(objectId);
    }

    /// <summary>Ghi nhận một object đã được tương tác.</summary>
    public void RecordInteraction(string objectId)
    {
        if (interacted_objects == null)
            interacted_objects = new List<string>();
        if (!interacted_objects.Contains(objectId))
            interacted_objects.Add(objectId);
    }
}

// ───────────────────────────────────────────
// CASE STUDY STATE (Top-level container)
// ───────────────────────────────────────────

/// <summary>
/// Class tổng thể chứa toàn bộ state của một case study.
/// Map 1:1 với JSON payload gửi/nhận qua API.
/// </summary>
[Serializable]
public class CaseStudyState
{
    public CaseMetadata case_metadata;
    public EnvironmentData environment;
    public PersonaData persona;
    public KnowledgeBase knowledge_base;
    public RewardSystem reward_system;
    public RuntimeState runtime_state;

    public CaseStudyState()
    {
        runtime_state = new RuntimeState();
    }
}

// ───────────────────────────────────────────
// AGENT TURN RESPONSE (Phản hồi từ Agent Server)
// ───────────────────────────────────────────

/// <summary>
/// Dữ liệu trả về từ Agent Server sau mỗi lượt chat.
/// POST /api/agent/sessions/{sessionId}/turn
/// </summary>
[Serializable]
public class AgentTurnResponse
{
    public List<ChatMessage> dialogue_history;
    public string[] active_personas;
    public string event_summary;
    public string current_event;        // null = kết thúc simulation
    public int last_score;

    // ── NPC Audio Output (TTS) ──
    /// <summary>
    /// URL file audio TTS của NPC (MP3/WAV).
    /// Unity sẽ tải về và phát qua NPCOutputController.
    /// </summary>
    public string audio_url;

    /// <summary>
    /// Audio dưới dạng base64 (fallback nếu backend không serve file tĩnh).
    /// Chỉ dùng một trong hai: audio_url hoặc audio_base64.
    /// </summary>
    public string audio_base64;

    // ── NPC Animation / Emotion ──
    /// <summary>Trạng thái cảm xúc NPC: "angry", "calm", "happy", "neutral"...</summary>
    public string persona_emotion;

    /// <summary>Trigger animation cụ thể: "gesture_frustrated", "nod", "point"...</summary>
    public string persona_action;

    /// <summary>
    /// JSON signal cho lip-sync chi tiết (viseme timing).
    /// Format phụ thuộc vào backend — có thể rỗng nếu dùng amplitude-based lip-sync.
    /// </summary>
    public string signal_output;

    /// <summary>Nội dung lời thoại của NPC (dùng để hiển thị subtitle).</summary>
    public string npc_text;

    /// <summary>Kiểm tra xem response có audio không.</summary>
    public bool HasAudio() =>
        !string.IsNullOrEmpty(audio_url) || !string.IsNullOrEmpty(audio_base64);
}

// ───────────────────────────────────────────
// API REQUEST / RESPONSE MODELS
// ───────────────────────────────────────────

[Serializable]
public class AuthRequest
{
    public string email;
    public string password;
}

[Serializable]
public class AuthResponse
{
    public string access_token;
    public string token_type;
}

[Serializable]
public class CreateSessionRequest
{
    public string case_id;
    public bool lazy_init;
    public bool skip_tts;
}

[Serializable]
public class CreateSessionResponse
{
    public string sessionId;
}

[Serializable]
public class TurnRequest
{
    public string user_input;
}

[Serializable]
public class SaveHistoryRequest
{
    public string session_id;
    public string case_id;
    public List<ChatMessage> transcript;
    public int final_score;
}

// ───────────────────────────────────────────
// SPEECH-TO-TEXT MODELS
// ───────────────────────────────────────────

/// <summary>
/// Response từ STT endpoint (FastAPI /api/stt).
/// Backend nhận file audio, trả về text đã transcribe.
/// </summary>
[Serializable]
public class STTResponse
{
    /// <summary>Câu nói đã được chuyển thành văn bản.</summary>
    public string text;

    /// <summary>Ngôn ngữ phát hiện được (ví dụ: "vi", "en").</summary>
    public string language;

    /// <summary>Độ chính xác (0.0 – 1.0), nếu backend có trả về.</summary>
    public float confidence;
}
