using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

public class UI_Register : MonoBehaviour
{
    private TextField _emailField;
    private TextField _usernameField;
    private TextField _passwordField;
    private TextField _confirmField;
    private Button _registerBtn;
    private Button _gotoLoginBtn;
    private Label _messageLb;

    private TabView _tabView;
    private Tab _loginTab;
    private Tab _registerTab;

    private bool _isLoading = false;
    private bool _isRegistering = false; // ✅ Flag phân biệt flow

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var registerTab = root.Q<Tab>("registertabsample");
        _registerTab = registerTab;

        _emailField = registerTab.Q<TextField>("emailfieldregister");
        _usernameField = registerTab.Q<TextField>("usernamefieldregister");
        _passwordField = registerTab.Q<TextField>("passwordfieldregister");
        _confirmField = registerTab.Q<TextField>("confirmpasswordfieldregister");

        _registerBtn = registerTab.Q<Button>("registerbtn");
        _gotoLoginBtn = registerTab.Q<Button>("loginbtnregister");
        _messageLb = registerTab.Q<Label>("messagelbregister");

        _tabView = root.Q<TabView>();
        _loginTab = root.Q<Tab>("logintab");

        ValidateUI();
        SetMessage("", Color.white);
        SetLoading(false);

        _registerBtn.clicked += OnRegisterClicked;
        _gotoLoginBtn.clicked += GoToLogin;

        EventManager.OnLoginSuccess += HandleRegisterSuccess;
        EventManager.OnAuthError += HandleAuthError;
    }

    void OnDisable()
    {
        _registerBtn.clicked -= OnRegisterClicked;
        _gotoLoginBtn.clicked -= GoToLogin;

        EventManager.OnLoginSuccess -= HandleRegisterSuccess;
        EventManager.OnAuthError -= HandleAuthError;
    }

    // ─────────────────────────────
    private void OnRegisterClicked()
    {
        if (_isLoading) return;

        string email = _emailField.value?.Trim();
        string username = _usernameField.value?.Trim();
        string password = _passwordField.value;
        string confirm = _confirmField.value;

        if (string.IsNullOrEmpty(username))
        {
            SetMessage("Không được để trống tên người dùng !", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            SetMessage("Không được để trống Email !", Color.red);
            return;
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            SetMessage("Đây chưa phải là một địa chỉ email !", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            SetMessage("Mật khẩu không được để trống!", Color.red);
            return;
        }

        if (password.Length < 8)
        {
            SetMessage("Mật khẩu có ít nhất 8 ký tự !.", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(confirm))
        {
            SetMessage("Mật khẩu không được để trống!", Color.red);
            return;
        }

        if (password != confirm)
        {
            SetMessage("Mật khẩu không khớp !.", Color.red);
            return;
        }

        _isRegistering = true; // ✅ Bật flag trước khi gọi API
        SetLoading(true);
        SetMessage("Đang đăng ký...", Color.yellow);

        AuthService.Instance.Register(email, password);
    }

    private void GoToLogin()
    {
        _tabView.activeTab = _loginTab;
    }

    // ─────────────────────────────
    private void HandleRegisterSuccess(string token)
    {
        // ✅ Chỉ xử lý nếu đang trong flow Register
        if (!_isRegistering) return;

        _isRegistering = false;
        SetLoading(false);
        SetMessage("Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.", Color.green);
        _tabView.activeTab = _loginTab;
    }

    private void HandleAuthError(string error)
    {
        // ✅ Chỉ xử lý nếu tab Register đang active VÀ đang trong flow Register
        if (_tabView.activeTab != _registerTab) return;
        if (!_isRegistering) return;

        _isRegistering = false;
        SetLoading(false);
        SetMessage(error, Color.red);
    }

    // ─────────────────────────────
    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        _registerBtn?.SetEnabled(!isLoading);
        _emailField?.SetEnabled(!isLoading);
        _usernameField?.SetEnabled(!isLoading);
        _passwordField?.SetEnabled(!isLoading);
        _confirmField?.SetEnabled(!isLoading);

        if (_registerBtn != null)
            _registerBtn.text = isLoading ? "Đang xử lý..." : "Register";
    }

    private void SetMessage(string msg, Color color)
    {
        if (_messageLb == null)
        {
            Debug.LogError("messagelbregister NOT FOUND");
            return;
        }
        _messageLb.text = msg;
        _messageLb.style.color = new StyleColor(color);
    }

    private void ValidateUI()
    {
        if (_emailField == null) Debug.LogError("emailfieldregister missing");
        if (_usernameField == null) Debug.LogError("usernamefieldregister missing");
        if (_passwordField == null) Debug.LogError("passwordfieldregister missing");
        if (_confirmField == null) Debug.LogError("confirmpasswordfieldregister missing");
        if (_registerBtn == null) Debug.LogError("registerbtn missing");
        if (_gotoLoginBtn == null) Debug.LogError("loginbtnregister missing");
        if (_messageLb == null) Debug.LogError("messagelbregister missing");
    }
}