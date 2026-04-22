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
    // SPEECH-TO-TEXT (INPUT CHỦ YẾU)
    // ───────────────────────────────────────────

    /// <summary>Khi người chơi bắt đầu nói (mic đang thu âm).</summary>
    public static event Action OnSTTRecordingStarted;

    /// <summary>Khi người chơi dừng nói (đang gửi lên API xử lý).</summary>
    public static event Action OnSTTRecordingStopped;

    /// <summary>
    /// Khi STT trả về kết quả text.
    /// Param: transcribedText — câu nói đã được chuyển thành chữ.
    /// </summary>
    public static event Action<string> OnSTTResult;

    /// <summary>Khi STT gặp lỗi (micro không hoạt động, API lỗi...).</summary>
    public static event Action<string> OnSTTError;

    public static void TriggerSTTRecordingStarted()
    {
        Debug.Log("[EventManager] STT: Bắt đầu thu âm...");
        OnSTTRecordingStarted?.Invoke();
    }

    public static void TriggerSTTRecordingStopped()
    {
        Debug.Log("[EventManager] STT: Dừng thu âm, đang xử lý...");
        OnSTTRecordingStopped?.Invoke();
    }

    public static void TriggerSTTResult(string transcribedText)
    {
        Debug.Log($"[EventManager] STT Result: \"{transcribedText}\"");
        OnSTTResult?.Invoke(transcribedText);
    }

    public static void TriggerSTTError(string error)
    {
        Debug.LogWarning($"[EventManager] STT Error: {error}");
        OnSTTError?.Invoke(error);
    }

    // ───────────────────────────────────────────
    // SMART OBJECT INTERACTION (⚠️ FUTURE IMPLEMENTATION)
    // ───────────────────────────────────────────

    /// <summary>[FUTURE] Khi người chơi click vào vật thể thông minh.</summary>
    public static event Action<string, string> OnSmartObjectInteracted;

    /// <summary>[FUTURE] Khi con trỏ hover lên một smart object.</summary>
    public static event Action<string> OnSmartObjectHoverEnter;

    /// <summary>[FUTURE] Khi con trỏ rời khỏi smart object.</summary>
    public static event Action<string> OnSmartObjectHoverExit;

    public static void TriggerSmartObjectInteracted(string objectId, string intent)
    {
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
    // NPC OUTPUT — LIP-SYNC & AUDIO
    // ───────────────────────────────────────────

    /// <summary>
    /// Khi NPC bắt đầu phát âm thanh + lip-sync.
    /// Param: audioUrl — URL file audio để tải về và phát.
    /// </summary>
    public static event Action<string> OnNPCSpeakStart;

    /// <summary>Khi NPC đã phát xong audio + lip-sync kết thúc.</summary>
    public static event Action OnNPCSpeakEnd;

    /// <summary>
    /// Khi NPC thay đổi cảm xúc/animation.
    /// Param: emotionState — "angry", "neutral", "satisfied"...
    /// </summary>
    public static event Action<string> OnNPCEmotionChanged;

    public static void TriggerNPCSpeakStart(string audioUrl)
    {
        Debug.Log($"[EventManager] NPC bắt đầu nói. Audio: {audioUrl}");
        OnNPCSpeakStart?.Invoke(audioUrl);
    }

    public static void TriggerNPCSpeakEnd()
    {
        Debug.Log("[EventManager] NPC kết thúc nói.");
        OnNPCSpeakEnd?.Invoke();
    }

    public static void TriggerNPCEmotionChanged(string emotionState)
    {
        Debug.Log($"[EventManager] NPC emotion: {emotionState}");
        OnNPCEmotionChanged?.Invoke(emotionState);
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

