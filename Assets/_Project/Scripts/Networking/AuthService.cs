using UnityEngine;

/// <summary>
/// Service xử lý Authentication (Đăng nhập / Đăng ký).
/// Giao tiếp với FastAPI backend (port 8001).
/// Endpoints: POST /api/register, POST /api/login
/// </summary>
public class AuthService : MonoBehaviour
{
    public static AuthService Instance { get; private set; }

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

    /// <summary>
    /// Đăng ký tài khoản mới.
    /// POST http://localhost:8001/api/register
    /// Body: { "email": "...", "password": "..." }
    /// </summary>
    public void Register(string email, string password)
    {
        string url = $"{SimulationManager.Instance.BackendBaseUrl}/api/register";
        var body = new AuthRequest { email = email, password = password };

        Debug.Log($"[AuthService] Đăng ký: {email}");

        ApiClient.Instance.Post(url, body,
            onSuccess: (response) =>
            {
                Debug.Log($"[AuthService] Đăng ký thành công! Tự động đăng nhập...");
                // Sau khi đăng ký thành công, tự động login
                Login(email, password);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[AuthService] Đăng ký thất bại: {error}");
                EventManager.TriggerAuthError($"Đăng ký thất bại: {error}");
            },
            requireAuth: false  // Không cần token để đăng ký
        );
    }

    /// <summary>
    /// Đăng nhập và nhận JWT token.
    /// POST http://localhost:8001/api/login
    /// Body: { "email": "...", "password": "..." }
    /// Response: { "access_token": "eyJhbG...", "token_type": "bearer" }
    /// </summary>
    public void Login(string email, string password)
    {
        string url = $"{SimulationManager.Instance.BackendBaseUrl}/api/login";
        var body = new AuthRequest { email = email, password = password };

        Debug.Log($"[AuthService] Đăng nhập: {email}");

        ApiClient.Instance.Post(url, body,
            onSuccess: (response) =>
            {
                // Parse response JSON → lấy access_token
                AuthResponse authResp = JsonUtility.FromJson<AuthResponse>(response);

                if (!string.IsNullOrEmpty(authResp.access_token))
                {
                    // Lưu token vào SimulationManager
                    SimulationManager.Instance.AuthToken = authResp.access_token;

                    Debug.Log("[AuthService] Đăng nhập thành công!");

                    // Thông báo cho hệ thống
                    EventManager.TriggerLoginSuccess(authResp.access_token);

                    // Chuyển sang màn Library
                    SimulationManager.Instance.ChangeState(SimulationState.Library);
                }
                else
                {
                    EventManager.TriggerAuthError("Token rỗng từ server.");
                }
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[AuthService] Đăng nhập thất bại: {error}");
                EventManager.TriggerAuthError($"Đăng nhập thất bại: {error}");
            },
            requireAuth: false  // Không cần token để đăng nhập
        );
    }
}
