using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

public class UI_Login : MonoBehaviour
{
    private TextField _emailField;
    private TextField _passwordField;
    private Button _loginBtn;
    private Button _gotoRegisterBtn;
    private Label _messageLb;
    private TabView _tabView;
    private Tab _registerTab;
    private Tab _loginTab;

    private bool _isLoading = false;
    private bool _isLoggingIn = false;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var loginTab = root.Q<Tab>("logintab");
        _loginTab = loginTab;

        _emailField = loginTab.Q<TextField>("usernamefieldlogin");
        _passwordField = loginTab.Q<TextField>("passwordfieldlogin");
        _loginBtn = loginTab.Q<Button>("loginbtn");
        _gotoRegisterBtn = loginTab.Q<Button>("gotoregisterlogin");
        _messageLb = loginTab.Q<Label>("messagelblogin");

        _tabView = root.Q<TabView>();
        _registerTab = root.Q<Tab>("registertabsample");

        ValidateUI();
        SetMessage("", Color.white);
        SetLoading(false);

        _loginBtn.clicked += OnLoginClicked;
        _gotoRegisterBtn.clicked += GoToRegister;

        EventManager.OnLoginSuccess += HandleLoginSuccess;
        EventManager.OnAuthError += HandleAuthError;
    }

    void OnDisable()
    {
        _loginBtn.clicked -= OnLoginClicked;
        _gotoRegisterBtn.clicked -= GoToRegister;

        EventManager.OnLoginSuccess -= HandleLoginSuccess;
        EventManager.OnAuthError -= HandleAuthError;
    }

    // ─────────────────────────────
    private void OnLoginClicked()
    {
        if (_isLoading) return;

        string emailOrUsername = _emailField.value?.Trim();
        string password = _passwordField.value;

        // FUNC-DN02: Bắt buộc nhập email/username
        if (string.IsNullOrEmpty(emailOrUsername))
        {
            SetMessage("The email field is required.", Color.red);
            return;
        }

        // FUNC-DN06: Nếu có @ thì validate format email
        // Nếu không có @ thì coi là username → bỏ qua
        if (emailOrUsername.Contains("@") &&
            !Regex.IsMatch(emailOrUsername, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            SetMessage("The email field must be a valid email address.", Color.red);
            return;
        }

        // FUNC-DN03: Bắt buộc nhập password
        if (string.IsNullOrEmpty(password))
        {
            SetMessage("The password field is required.", Color.red);
            return;
        }

        _isLoggingIn = true;
        SetLoading(true);
        SetMessage("Đang đăng nhập...", Color.yellow);

        AuthService.Instance.Login(emailOrUsername, password);
    }

    private void GoToRegister()
    {
        _tabView.activeTab = _registerTab;
    }

    // ─────────────────────────────
    private void HandleLoginSuccess(string token)
    {
        if (!_isLoggingIn) return;

        _isLoggingIn = false;
        SetLoading(false);
        SetMessage("Đăng nhập thành công!", Color.green);
    }

    private void HandleAuthError(string error)
    {
        if (_tabView.activeTab != _loginTab) return;
        if (!_isLoggingIn) return;

        _isLoggingIn = false;
        SetLoading(false);
        SetMessage(error, Color.red);
    }

    // ─────────────────────────────
    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        _loginBtn?.SetEnabled(!isLoading);
        _emailField?.SetEnabled(!isLoading);
        _passwordField?.SetEnabled(!isLoading);

        if (_loginBtn != null)
            _loginBtn.text = isLoading ? "Đang xử lý..." : "Đăng nhập";
    }

    private void SetMessage(string msg, Color color)
    {
        if (_messageLb == null)
        {
            Debug.LogError("messagelblogin NOT FOUND");
            return;
        }
        _messageLb.text = msg;
        _messageLb.style.color = new StyleColor(color);
    }

    private void ValidateUI()
    {
        if (_emailField == null) Debug.LogError("usernamefieldlogin missing");
        if (_passwordField == null) Debug.LogError("passwordfieldlogin missing");
        if (_loginBtn == null) Debug.LogError("loginbtn missing");
        if (_gotoRegisterBtn == null) Debug.LogError("gotoregisterlogin missing");
        if (_messageLb == null) Debug.LogError("messagelblogin missing");
    }
}