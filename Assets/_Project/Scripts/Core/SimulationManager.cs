using UnityEngine;

/// <summary>
/// Các trạng thái chính của ứng dụng, điều khiển flow màn hình.
/// MainMenu → Login → Library → Designer → Simulator → Summary
/// </summary>
public enum SimulationState
{
    MainMenu,
    Login,
    Library,
    Designer,
    Simulator,
    Summary
}

/// <summary>
/// Singleton quản lý trạng thái toàn cục của ứng dụng.
/// Giữ JWT token, case data hiện tại, và session ID.
/// Chỉ Lead (Vinh) mới được chỉnh sửa file này.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Application State")]
    public SimulationState CurrentState = SimulationState.MainMenu;

    [Header("Auth")]
    private string _authToken;
    /// <summary>JWT token nhận từ backend sau khi login thành công.</summary>
    public string AuthToken
    {
        get => _authToken;
        set
        {
            _authToken = value;
            // Lưu token vào PlayerPrefs để không phải login lại khi restart
            if (!string.IsNullOrEmpty(value))
                PlayerPrefs.SetString("auth_token", value);
            else
                PlayerPrefs.DeleteKey("auth_token");
            PlayerPrefs.Save();
        }
    }

    [Header("Session")]
    /// <summary>Case state đang được load/chạy hiện tại.</summary>
    public CaseStudyState CurrentCaseState { get; set; }

    /// <summary>Session ID hiện tại từ Agent Server (port 9000).</summary>
    public string CurrentSessionId { get; set; }

    [Header("Config")]
    /// <summary>Base URL của FastAPI backend.</summary>
    public string BackendBaseUrl = "http://localhost:8001";

    /// <summary>Base URL của Agent Server.</summary>
    public string AgentServerUrl = "http://localhost:9000";

    /// <summary>Base URL của Express BFF.</summary>
    public string BffBaseUrl = "http://localhost:8000";

    void Awake()
    {
        // Singleton pattern — đảm bảo chỉ có 1 Instance duy nhất
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedToken();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Chuyển trạng thái ứng dụng và thông báo cho tất cả listener qua EventManager.
    /// </summary>
    public void ChangeState(SimulationState newState)
    {
        SimulationState oldState = CurrentState;
        CurrentState = newState;
        Debug.Log($"[SimulationManager] State: {oldState} → {newState}");

        // Gọi EventManager để báo cho UI biết mà cập nhật
        EventManager.TriggerStateChanged(newState);
    }

    /// <summary>
    /// Đăng xuất: xóa token, reset session, quay về MainMenu.
    /// </summary>
    public void Logout()
    {
        AuthToken = null;
        CurrentCaseState = null;
        CurrentSessionId = null;
        ChangeState(SimulationState.MainMenu);
        Debug.Log("[SimulationManager] Đã đăng xuất.");
    }

    /// <summary>
    /// Kiểm tra user đã đăng nhập chưa (có token hay không).
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    /// <summary>Load token đã lưu từ PlayerPrefs (nếu có).</summary>
    private void LoadSavedToken()
    {
        string savedToken = PlayerPrefs.GetString("auth_token", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            _authToken = savedToken;
            Debug.Log("[SimulationManager] Đã khôi phục token từ phiên trước.");
        }
    }
}
