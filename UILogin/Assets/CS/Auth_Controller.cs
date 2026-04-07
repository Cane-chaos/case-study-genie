using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

/// <summary>
/// Auth_Controller.cs
/// Điều khiển Auth_UI.uxml
/// Gắn vào GameObject có UIDocument component.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class Auth_Controller : MonoBehaviour
{
    // ─── Config ──────────────────────────────────────────────────
    [Header("FastAPI Backend URL")]
    [SerializeField] private string _baseUrl = "http://localhost:8000";

    // ─── UI References — tên khớp 100% với name trong UXML ──────
    private TextField _UsernameField;    // name="UsernameField"
    private TextField _passwordField;   // name="PasswordField"
    private Button _Loginbtn;         // name="Loginbtn"
    private Button _RegisterLoginbtn; // name="RegisterLoginbtn"
    private Button _Forgetpassbtn;    // name="Forgetpassbtn"

    // ─── Unity Lifecycle ─────────────────────────────────────────
    private void OnEnable()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return null; // đợi UI load xong

        var root = GetComponent<UIDocument>().rootVisualElement;

        BindElements(root);
        RegisterCallbacks();

        Debug.Log("UI READY");
    }

    // ─── Bind — dùng đúng name trong UXML ───────────────────────
    private void BindElements(VisualElement root)
    {
        _UsernameField = root.Q<TextField>("UsernameField");
        _passwordField = root.Q<TextField>("passwordField");  // giữ nguyên typo
        _Loginbtn = root.Q<Button>("Loginbtn");
        _RegisterLoginbtn = root.Q<Button>("RegisterLoginbtn");
        _Forgetpassbtn = root.Q<Button>("Forgetpassbtn");

        // Kiểm tra null — log cảnh báo nếu UXML thiếu element
        if (_UsernameField == null) Debug.LogWarning("[Auth] Không tìm thấy UsernameField");
        if (_passwordField == null) Debug.LogWarning("[Auth] Không tìm thấy PasswordField");
        if (_Loginbtn == null) Debug.LogWarning("[Auth] Không tìm thấy Loginbtn");
        if (_RegisterLoginbtn == null) Debug.LogWarning("[Auth] Không tìm thấy RegisterLoginbtn");
        if (_Forgetpassbtn == null) Debug.LogWarning("[Auth] Không tìm thấy Forgetpassbtn");
    }

    // ─── Register Callbacks ──────────────────────────────────────
    private void RegisterCallbacks()
    {
        _Loginbtn?.RegisterCallback<ClickEvent>(OnLoginClicked);
        _RegisterLoginbtn?.RegisterCallback<ClickEvent>(OnRegisterClicked);
        _Forgetpassbtn?.RegisterCallback<ClickEvent>(OnForgetPasswordClicked);
    }

    private void UnregisterCallbacks()
    {
        _Loginbtn?.UnregisterCallback<ClickEvent>(OnLoginClicked);
        _RegisterLoginbtn?.UnregisterCallback<ClickEvent>(OnRegisterClicked);
        _Forgetpassbtn?.UnregisterCallback<ClickEvent>(OnForgetPasswordClicked);
    }

    // =========================================================
    //  BUTTON HANDLERS
    // =========================================================

    private void OnLoginClicked(ClickEvent evt)
    {
        if (!ValidateInputs()) return;
        SetButtonsEnabled(false);
        StartCoroutine(LoginRoutine(
            _UsernameField.value.Trim(),
            _passwordField.value
        ));
    }

    private void OnRegisterClicked(ClickEvent evt)
    {
        if (!ValidateInputs()) return;
        SetButtonsEnabled(false);
        StartCoroutine(RegisterRoutine(
            _UsernameField.value.Trim(),
            _passwordField.value
        ));
    }

    private void OnForgetPasswordClicked(ClickEvent evt)
    {
        string username = _UsernameField.value.Trim();
        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Vui lòng nhập username hoặc email.", MessageType.Error);
            return;
        }
        SetButtonsEnabled(false);
        StartCoroutine(ForgetPasswordRoutine(username));
    }

    // =========================================================
    //  COROUTINES — Gọi FastAPI
    // =========================================================

    /// POST /auth/login
    private IEnumerator LoginRoutine(string username, string password)
    {
        ShowMessage("Đang đăng nhập...", MessageType.Info);

        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        using var req = PostJson($"{_baseUrl}/auth/login", json);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            if (res.success)
            {
                ShowMessage("Đăng nhập thành công!", MessageType.Success);
                PlayerPrefs.SetString("auth_token", res.token ?? "");
                PlayerPrefs.SetString("username", username);
                PlayerPrefs.Save();

                // TODO: Thay SimulationState.Home bằng đúng enum trong project
                // SimulationManager.Instance.ChangeState(SimulationState.Home);
                Debug.Log("[Auth] Login OK → chuyển sang Home");
            }
            else
            {
                ShowMessage(res.message ?? "Sai tài khoản hoặc mật khẩu.", MessageType.Error);
                SetButtonsEnabled(true);
            }
        }
        else
        {
            // Chưa có backend → dùng stub local
            Debug.LogWarning($"[Auth] Không kết nối được server: {req.error}. Dùng stub.");
            yield return StubLoginRoutine(username, password);
        }
    }

    /// POST /auth/register
    private IEnumerator RegisterRoutine(string username, string password)
    {
        ShowMessage("Đang đăng ký...", MessageType.Info);

        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        using var req = PostJson($"{_baseUrl}/auth/register", json);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            if (res.success)
            {
                ShowMessage("Đăng ký thành công! Đang đăng nhập...", MessageType.Success);
                PlayerPrefs.SetString("auth_token", res.token ?? "");
                PlayerPrefs.SetString("username", username);
                PlayerPrefs.Save();

                // SimulationManager.Instance.ChangeState(SimulationState.Home);
                Debug.Log("[Auth] Register OK → chuyển sang Home");
            }
            else
            {
                ShowMessage(res.message ?? "Tài khoản đã tồn tại.", MessageType.Error);
                SetButtonsEnabled(true);
            }
        }
        else
        {
            Debug.LogWarning($"[Auth] Không kết nối được server: {req.error}. Dùng stub.");
            yield return StubRegisterRoutine(username, password);
        }
    }

    /// POST /auth/forget-password
    private IEnumerator ForgetPasswordRoutine(string username)
    {
        ShowMessage("Đang gửi yêu cầu...", MessageType.Info);

        string json = $"{{\"username\":\"{username}\"}}";
        using var req = PostJson($"{_baseUrl}/auth/forget-password", json);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            ShowMessage(res.message ?? $"Đã gửi yêu cầu reset tới {username}", MessageType.Success);
        }
        else
        {
            ShowMessage($"Không kết nối được server.", MessageType.Error);
        }

        SetButtonsEnabled(true);
    }

    // =========================================================
    //  STUB — Chạy local khi chưa có FastAPI
    // =========================================================

    private IEnumerator StubLoginRoutine(string username, string password)
    {
        yield return null; // giả lập 1 frame delay

        string savedUser = PlayerPrefs.GetString("username", "");
        string savedPass = PlayerPrefs.GetString("password", "");

        if (savedUser == username && savedPass == password)
        {
            ShowMessage("[Stub] Đăng nhập thành công!", MessageType.Success);
            // SimulationManager.Instance.ChangeState(SimulationState.Home);
            Debug.Log("[Auth Stub] Login OK");
        }
        else
        {
            ShowMessage("[Stub] Sai tài khoản hoặc mật khẩu.", MessageType.Error);
            SetButtonsEnabled(true);
        }
    }

    private IEnumerator StubRegisterRoutine(string username, string password)
    {
        yield return null;

        if (PlayerPrefs.GetString("username", "") == username)
        {
            ShowMessage("[Stub] Tài khoản đã tồn tại.", MessageType.Error);
            SetButtonsEnabled(true);
            yield break;
        }

        PlayerPrefs.SetString("username", username);
        PlayerPrefs.SetString("password", password);
        PlayerPrefs.Save();

        ShowMessage("[Stub] Đăng ký thành công!", MessageType.Success);
        // SimulationManager.Instance.ChangeState(SimulationState.Home);
        Debug.Log("[Auth Stub] Register OK");
    }

    // =========================================================
    //  HELPERS
    // =========================================================

    private static UnityWebRequest PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (_Loginbtn != null) _Loginbtn.SetEnabled(enabled);
        if (_RegisterLoginbtn != null) _RegisterLoginbtn.SetEnabled(enabled);
        if (_Forgetpassbtn != null) _Forgetpassbtn.SetEnabled(enabled);
    }

    // ─── Message ─────────────────────────────────────────────────
    private enum MessageType { Info, Success, Error }

    private void ShowMessage(string text, MessageType type)
    {
        // In ra Console — sau này thay bằng Label nếu bạn thêm message-label vào UXML
        switch (type)
        {
            case MessageType.Success: Debug.Log($"[Auth] ✅ {text}"); break;
            case MessageType.Error: Debug.LogError($"[Auth] ❌ {text}"); break;
            case MessageType.Info: Debug.Log($"[Auth] ℹ️ {text}"); break;
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(_UsernameField?.value.Trim()))
        {
            ShowMessage("Username không được để trống.", MessageType.Error);
            return false;
        }
        if (string.IsNullOrEmpty(_passwordField?.value))
        {
            ShowMessage("Mật khẩu không được để trống.", MessageType.Error);
            return false;
        }
        if (_passwordField.value.Length < 6)
        {
            ShowMessage("Mật khẩu phải có ít nhất 6 ký tự.", MessageType.Error);
            return false;
        }
        return true;
    }

    // ─── Data Model ──────────────────────────────────────────────
    [System.Serializable]
    private class AuthResponse
    {
        public bool success;
        public string message;
        public string token;
    }
}