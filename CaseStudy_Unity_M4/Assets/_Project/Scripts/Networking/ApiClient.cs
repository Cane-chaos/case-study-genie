using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTP client cơ sở dùng UnityWebRequest.
/// Hỗ trợ GET, POST JSON với auto-attach JWT token.
/// Các service khác (AuthService, CaseService) sẽ dùng class này.
/// </summary>
public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Timeout cho mỗi request (giây)")]
    public int timeoutSeconds = 30;

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
    // PUBLIC API
    // ───────────────────────────────────────────

    /// <summary>
    /// Gửi GET request.
    /// </summary>
    /// <param name="url">Full URL (ví dụ: http://localhost:8001/api/cases/123)</param>
    /// <param name="onSuccess">Callback khi thành công, truyền response body (JSON string)</param>
    /// <param name="onError">Callback khi thất bại, truyền error message</param>
    /// <param name="requireAuth">Tự động gắn JWT token vào header nếu true</param>
    public void Get(string url, Action<string> onSuccess, Action<string> onError = null, bool requireAuth = true)
    {
        StartCoroutine(SendRequest("GET", url, null, onSuccess, onError, requireAuth));
    }

    /// <summary>
    /// Gửi POST request với JSON body.
    /// </summary>
    /// <param name="url">Full URL</param>
    /// <param name="jsonBody">JSON string làm request body</param>
    /// <param name="onSuccess">Callback khi thành công</param>
    /// <param name="onError">Callback khi thất bại</param>
    /// <param name="requireAuth">Tự động gắn JWT token vào header nếu true</param>
    public void Post(string url, string jsonBody, Action<string> onSuccess, Action<string> onError = null, bool requireAuth = true)
    {
        StartCoroutine(SendRequest("POST", url, jsonBody, onSuccess, onError, requireAuth));
    }

    /// <summary>
    /// Gửi POST request với object sẽ được serialize thành JSON.
    /// </summary>
    public void Post<T>(string url, T bodyObject, Action<string> onSuccess, Action<string> onError = null, bool requireAuth = true)
    {
        string jsonBody = JsonUtility.ToJson(bodyObject);
        Post(url, jsonBody, onSuccess, onError, requireAuth);
    }

    // ───────────────────────────────────────────
    // INTERNAL
    // ───────────────────────────────────────────

    private IEnumerator SendRequest(string method, string url, string jsonBody,
        Action<string> onSuccess, Action<string> onError, bool requireAuth)
    {
        UnityWebRequest request;

        if (method == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else // POST
        {
            request = new UnityWebRequest(url, "POST");
            if (!string.IsNullOrEmpty(jsonBody))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }

        // Auto-attach JWT token
        if (requireAuth && SimulationManager.Instance != null
            && !string.IsNullOrEmpty(SimulationManager.Instance.AuthToken))
        {
            request.SetRequestHeader("Authorization",
                $"Bearer {SimulationManager.Instance.AuthToken}");
        }

        request.timeout = timeoutSeconds;

        Debug.Log($"[ApiClient] {method} {url}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseBody = request.downloadHandler.text;
            Debug.Log($"[ApiClient] ✓ {method} {url} → {request.responseCode}");
            onSuccess?.Invoke(responseBody);
        }
        else
        {
            string errorMsg = $"{request.responseCode}: {request.error}";
            Debug.LogWarning($"[ApiClient] ✗ {method} {url} → {errorMsg}");

            // Thử đọc error body từ server
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMsg += $" | Body: {request.downloadHandler.text}";
            }

            onError?.Invoke(errorMsg);
            EventManager.TriggerNetworkError(errorMsg);
        }

        request.Dispose();
    }
}
