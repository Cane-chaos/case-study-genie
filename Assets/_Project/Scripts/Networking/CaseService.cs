using System;
using UnityEngine;

/// <summary>
/// Service quản lý Case data, Session, và Turn trong mô phỏng.
/// Giao tiếp với:
///   - FastAPI (port 8001): lấy/lưu case data
///   - Agent Server (port 9000): tạo session, gửi turn
///   - Express BFF (port 8000): lưu lịch sử session
/// </summary>
public class CaseService : MonoBehaviour
{
    public static CaseService Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ───────────────────────────────────────────
    // CASE DATA (FastAPI :8001)
    // ───────────────────────────────────────────

    /// <summary>
    /// Lấy dữ liệu case từ backend.
    /// GET http://localhost:8001/api/cases/{caseId}
    /// Response: { skeleton, personas, context }
    /// </summary>
    public void GetCase(string caseId, Action<CaseStudyState> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{SimulationManager.Instance.BackendBaseUrl}/api/cases/{caseId}";

        Debug.Log($"[CaseService] Loading case: {caseId}");

        ApiClient.Instance.Get(url,
            onSuccess: (response) =>
            {
                CaseStudyState caseState = JsonUtility.FromJson<CaseStudyState>(response);

                // Lưu vào SimulationManager
                SimulationManager.Instance.CurrentCaseState = caseState;

                Debug.Log($"[CaseService] Case loaded: {caseState.case_metadata?.title}");
                EventManager.TriggerCaseLoaded(caseState);
                onSuccess?.Invoke(caseState);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[CaseService] Load case thất bại: {error}");
                onError?.Invoke(error);
            }
        );
    }

    /// <summary>
    /// Lưu/tạo case mới lên backend.
    /// POST http://localhost:8001/api/cases
    /// </summary>
    public void SaveCase(CaseStudyState caseState, Action<string> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{SimulationManager.Instance.BackendBaseUrl}/api/cases";

        Debug.Log($"[CaseService] Saving case: {caseState.case_metadata?.case_id}");

        ApiClient.Instance.Post(url, caseState,
            onSuccess: (response) =>
            {
                Debug.Log("[CaseService] Case saved successfully!");
                onSuccess?.Invoke(response);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[CaseService] Save case thất bại: {error}");
                onError?.Invoke(error);
            }
        );
    }

    // ───────────────────────────────────────────
    // SESSION (Agent Server :9000)
    // ───────────────────────────────────────────

    /// <summary>
    /// Tạo session mô phỏng mới.
    /// POST http://localhost:9000/api/agent/sessions
    /// Body: { "case_id": "...", "lazy_init": true, "skip_tts": true }
    /// Response: { "sessionId": "..." }
    /// </summary>
    public void CreateSession(string caseId, Action<string> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{SimulationManager.Instance.AgentServerUrl}/api/agent/sessions";
        var body = new CreateSessionRequest
        {
            case_id = caseId,
            lazy_init = true,
            skip_tts = true
        };

        Debug.Log($"[CaseService] Creating session for case: {caseId}");

        ApiClient.Instance.Post(url, body,
            onSuccess: (response) =>
            {
                CreateSessionResponse sessionResp = JsonUtility.FromJson<CreateSessionResponse>(response);
                string sessionId = sessionResp.sessionId;

                // Lưu session ID
                SimulationManager.Instance.CurrentSessionId = sessionId;

                Debug.Log($"[CaseService] Session created: {sessionId}");
                EventManager.TriggerSessionCreated(sessionId);
                onSuccess?.Invoke(sessionId);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[CaseService] Create session thất bại: {error}");
                onError?.Invoke(error);
            }
        );
    }

    /// <summary>
    /// Gửi một lượt tương tác (chat hoặc object interaction) tới Agent.
    /// POST http://localhost:9000/api/agent/sessions/{sessionId}/turn
    /// Body: { "user_input": "..." }
    /// Response: AgentTurnResponse (dialogue_history, score, event...)
    /// </summary>
    public void SendTurn(string userInput, Action<AgentTurnResponse> onSuccess = null, Action<string> onError = null)
    {
        string sessionId = SimulationManager.Instance.CurrentSessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            string error = "Chưa có session! Gọi CreateSession() trước.";
            Debug.LogWarning($"[CaseService] {error}");
            onError?.Invoke(error);
            return;
        }

        string url = $"{SimulationManager.Instance.AgentServerUrl}/api/agent/sessions/{sessionId}/turn";
        var body = new TurnRequest { user_input = userInput };

        Debug.Log($"[CaseService] Sending turn: \"{userInput}\"");
        EventManager.TriggerChatLoading(true);

        ApiClient.Instance.Post(url, body,
            onSuccess: (response) =>
            {
                EventManager.TriggerChatLoading(false);

                AgentTurnResponse turnResponse = JsonUtility.FromJson<AgentTurnResponse>(response);

                Debug.Log($"[CaseService] Turn response received. Event: {turnResponse.current_event}");
                EventManager.TriggerChatResponseReceived(turnResponse);

                // Cập nhật score nếu có thay đổi
                if (SimulationManager.Instance.CurrentCaseState?.runtime_state != null)
                {
                    int oldScore = SimulationManager.Instance.CurrentCaseState.runtime_state.current_score;
                    int newScore = turnResponse.last_score;
                    if (newScore != oldScore)
                    {
                        SimulationManager.Instance.CurrentCaseState.runtime_state.current_score = newScore;
                        EventManager.TriggerScoreUpdated(newScore, newScore - oldScore, "AI evaluation");
                    }
                }

                // Kiểm tra kết thúc simulation
                if (string.IsNullOrEmpty(turnResponse.current_event))
                {
                    Debug.Log("[CaseService] Simulation ended (current_event = null).");
                    if (SimulationManager.Instance.CurrentCaseState?.runtime_state != null)
                    {
                        EventManager.TriggerSimulationEnded(
                            SimulationManager.Instance.CurrentCaseState.runtime_state);
                    }
                }

                onSuccess?.Invoke(turnResponse);
            },
            onError: (error) =>
            {
                EventManager.TriggerChatLoading(false);
                Debug.LogWarning($"[CaseService] Send turn thất bại: {error}");
                onError?.Invoke(error);
            }
        );
    }

    // ───────────────────────────────────────────
    // HISTORY (Express BFF :8000)
    // ───────────────────────────────────────────

    /// <summary>
    /// Lưu lịch sử phiên mô phỏng sau khi kết thúc.
    /// POST http://localhost:8000/api/sessions/history
    /// </summary>
    public void SaveHistory(Action<string> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{SimulationManager.Instance.BffBaseUrl}/api/sessions/history";

        var caseState = SimulationManager.Instance.CurrentCaseState;
        if (caseState == null || caseState.runtime_state == null)
        {
            onError?.Invoke("Không có dữ liệu case để lưu.");
            return;
        }

        var body = new SaveHistoryRequest
        {
            session_id = SimulationManager.Instance.CurrentSessionId,
            case_id = caseState.case_metadata?.case_id,
            transcript = caseState.runtime_state.history,
            final_score = caseState.runtime_state.current_score
        };

        Debug.Log("[CaseService] Saving session history...");

        ApiClient.Instance.Post(url, body,
            onSuccess: (response) =>
            {
                Debug.Log("[CaseService] History saved successfully!");
                onSuccess?.Invoke(response);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[CaseService] Save history thất bại: {error}");
                onError?.Invoke(error);
            }
        );
    }
}
