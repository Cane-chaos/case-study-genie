using System;
using UnityEngine;

/// <summary>
/// Event bus tĩnh (static) điều phối sự kiện giữa các module.
/// Bất kỳ script nào cũng có thể đăng ký (subscribe) hoặc kích hoạt (trigger) event.
/// Chỉ Lead (Vinh) mới được chỉnh sửa file này.
/// </summary>
public static class EventManager
{
    // ───────────────────────────────────────────
    // APPLICATION STATE
    // ───────────────────────────────────────────

    /// <summary>Khi trạng thái ứng dụng thay đổi (MainMenu → Login → Library...).</summary>
    public static event Action<SimulationState> OnStateChanged;

    public static void TriggerStateChanged(SimulationState newState)
    {
        OnStateChanged?.Invoke(newState);
    }

    // ───────────────────────────────────────────
    // AUTHENTICATION
    // ───────────────────────────────────────────

    /// <summary>Khi login thành công, truyền JWT token.</summary>
    public static event Action<string> OnLoginSuccess;

    /// <summary>Khi login/register thất bại, truyền thông báo lỗi.</summary>
    public static event Action<string> OnAuthError;

    public static void TriggerLoginSuccess(string token)
    {
        OnLoginSuccess?.Invoke(token);
    }

    public static void TriggerAuthError(string errorMessage)
    {
        OnAuthError?.Invoke(errorMessage);
    }

    // ───────────────────────────────────────────
    // CASE DATA
    // ───────────────────────────────────────────

    /// <summary>Khi một case được load thành công từ backend.</summary>
    public static event Action<CaseStudyState> OnCaseLoaded;

    /// <summary>Khi session mô phỏng được tạo thành công.</summary>
    public static event Action<string> OnSessionCreated;

    public static void TriggerCaseLoaded(CaseStudyState caseState)
    {
        OnCaseLoaded?.Invoke(caseState);
    }

    public static void TriggerSessionCreated(string sessionId)
    {
        OnSessionCreated?.Invoke(sessionId);
    }

    // ───────────────────────────────────────────
    // SMART OBJECT INTERACTION
    // ───────────────────────────────────────────

    /// <summary>
    /// Khi người chơi tương tác với vật thể thông minh.
    /// Param 1: objectId (ví dụ: "obj_ticket_001")
    /// Param 2: intent (ví dụ: "verify_customer_info")
    /// </summary>
    public static event Action<string, string> OnSmartObjectInteracted;

    /// <summary>Khi con trỏ hover lên một smart object.</summary>
    public static event Action<string> OnSmartObjectHoverEnter;

    /// <summary>Khi con trỏ rời khỏi smart object.</summary>
    public static event Action<string> OnSmartObjectHoverExit;

    public static void TriggerSmartObjectInteracted(string objectId, string intent)
    {
        Debug.Log($"[EventManager] SmartObject interacted: {objectId} → {intent}");
        OnSmartObjectInteracted?.Invoke(objectId, intent);
    }

    public static void TriggerSmartObjectHoverEnter(string objectId)
    {
        OnSmartObjectHoverEnter?.Invoke(objectId);
    }

    public static void TriggerSmartObjectHoverExit(string objectId)
    {
        OnSmartObjectHoverExit?.Invoke(objectId);
    }

    // ───────────────────────────────────────────
    // CHAT / DIALOGUE
    // ───────────────────────────────────────────

    /// <summary>Khi nhận được phản hồi chat từ AI agent.</summary>
    public static event Action<AgentTurnResponse> OnChatResponseReceived;

    /// <summary>Khi đang chờ phản hồi từ AI (hiện loading).</summary>
    public static event Action<bool> OnChatLoading;

    public static void TriggerChatResponseReceived(AgentTurnResponse response)
    {
        OnChatResponseReceived?.Invoke(response);
    }

    public static void TriggerChatLoading(bool isLoading)
    {
        OnChatLoading?.Invoke(isLoading);
    }

    // ───────────────────────────────────────────
    // SCORING
    // ───────────────────────────────────────────

    /// <summary>Khi điểm số thay đổi. Param: (newScore, scoreDelta, reason).</summary>
    public static event Action<int, int, string> OnScoreUpdated;

    public static void TriggerScoreUpdated(int newScore, int delta, string reason)
    {
        Debug.Log($"[EventManager] Score: {newScore} (delta: {delta:+#;-#;0}) — {reason}");
        OnScoreUpdated?.Invoke(newScore, delta, reason);
    }

    // ───────────────────────────────────────────
    // SIMULATION LIFECYCLE
    // ───────────────────────────────────────────

    /// <summary>Khi phiên mô phỏng kết thúc (chuyển sang Summary).</summary>
    public static event Action<RuntimeState> OnSimulationEnded;

    public static void TriggerSimulationEnded(RuntimeState finalState)
    {
        Debug.Log($"[EventManager] Simulation ended. Final score: {finalState.current_score}");
        OnSimulationEnded?.Invoke(finalState);
    }

    // ───────────────────────────────────────────
    // NETWORKING
    // ───────────────────────────────────────────

    /// <summary>Khi có lỗi mạng / API call thất bại.</summary>
    public static event Action<string> OnNetworkError;

    public static void TriggerNetworkError(string errorMessage)
    {
        Debug.LogWarning($"[EventManager] Network error: {errorMessage}");
        OnNetworkError?.Invoke(errorMessage);
    }
}

