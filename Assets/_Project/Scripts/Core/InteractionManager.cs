using UnityEngine;

/// <summary>
/// Quản lý tương tác giữa người chơi và các SmartObject trong scene.
/// Sử dụng Raycast từ camera chính khi click/hover chuột.
/// Tự động phát hiện SmartObjectController trên vật thể bị hit.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Camera dùng để bắn Raycast (để trống = Camera.main)")]
    public Camera interactionCamera;

    [Tooltip("Khoảng cách Raycast tối đa")]
    public float maxRayDistance = 100f;

    [Tooltip("Layer mask cho các vật thể tương tác được")]
    public LayerMask interactableLayerMask = ~0; // Mặc định: tất cả layer

    [Tooltip("Chỉ cho phép tương tác khi đang ở trạng thái Simulator")]
    public bool requireSimulatorState = true;

    // ── Internal ──
    private SmartObjectController _currentHovered;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
    }

    void Update()
    {
        // Kiểm tra trạng thái — chỉ cho tương tác khi đang Simulator
        if (requireSimulatorState &&
            SimulationManager.Instance?.CurrentState != SimulationState.Simulator)
        {
            ClearHover();
            return;
        }

        HandleHover();
        HandleClick();
    }

    // ───────────────────────────────────────────
    // HOVER — Highlight vật thể khi di chuột
    // ───────────────────────────────────────────

    private void HandleHover()
    {
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayerMask))
        {
            SmartObjectController smartObj = hit.collider.GetComponent<SmartObjectController>();

            if (smartObj != null)
            {
                // Đang hover vật thể mới
                if (_currentHovered != smartObj)
                {
                    ClearHover();
                    _currentHovered = smartObj;
                    _currentHovered.OnHoverEnter();
                }
            }
            else
            {
                ClearHover();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        if (_currentHovered != null)
        {
            _currentHovered.OnHoverExit();
            _currentHovered = null;
        }
    }

    // ───────────────────────────────────────────
    // CLICK — Kích hoạt tương tác
    // ───────────────────────────────────────────

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return; // Chỉ xử lý click trái

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayerMask))
        {
            SmartObjectController smartObj = hit.collider.GetComponent<SmartObjectController>();

            if (smartObj != null)
            {
                smartObj.OnInteract();

                // Gửi interaction như input cho AI Agent (Mode A — Guided)
                string interactionInput = $"[OBJECT_INTERACTION] {smartObj.objectId}: {smartObj.interactionIntent}";
                Debug.Log($"[InteractionManager] Sending object interaction as turn input: {interactionInput}");

                // Tự động gửi lên Agent Server nếu đang có session
                if (!string.IsNullOrEmpty(SimulationManager.Instance?.CurrentSessionId))
                {
                    CaseService.Instance?.SendTurn(interactionInput);
                }
            }
        }
    }

    // ───────────────────────────────────────────
    // PUBLIC API
    // ───────────────────────────────────────────

    /// <summary>
    /// Tắt tạm thời hệ thống interaction (ví dụ khi đang mở UI panel).
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled) ClearHover();
    }

    /// <summary>
    /// Lấy smart object đang được hover (nếu có).
    /// </summary>
    public SmartObjectController GetCurrentHovered()
    {
        return _currentHovered;
    }
}
