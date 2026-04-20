using UnityEngine;

// ╔══════════════════════════════════════════════════════════════╗
// ║  ⚠️  FUTURE IMPLEMENTATION — KHÔNG DÙNG TRONG ĐỒ ÁN NÀY  ║
// ║                                                              ║
// ║  Tính năng bấm chọn vật thể 3D đã được ghi nhận để          ║
// ║  phát triển sau. Trong phiên bản hiện tại, người chơi       ║
// ║  dùng Speech-to-Text (giọng nói) làm input chính.           ║
// ║                                                              ║
// ║  Không xóa file này — giữ để tham khảo khi implement sau.  ║
// ╚══════════════════════════════════════════════════════════════╝

/// <summary>
/// [FUTURE] Gắn vào mỗi vật thể 3D có thể tương tác trong Scene.
/// Mỗi vật thể đại diện cho một hành động nghiệp vụ.
///
/// Ví dụ:
///   - Phiếu đặt phòng → intent: "verify_customer_info"
///   - Điện thoại bàn → intent: "call_manager"
///   - Sách VIP policy → intent: "follow_vip_policy"
///
/// Yêu cầu: GameObject phải có Collider (Box/Mesh/Sphere).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SmartObjectController : MonoBehaviour
{
    [Header("Object Identity")]
    [Tooltip("ID duy nhất, khớp với smart_objects[].id trong STATE_SPEC")]
    public string objectId = "obj_unnamed";

    [Tooltip("Tên hiển thị cho người chơi")]
    public string objectName = "Vật thể";

    [Header("Gameplay")]
    [Tooltip("Intent nghiệp vụ khi tương tác (ví dụ: verify_customer_info)")]
    public string interactionIntent = "examine";

    [Tooltip("Loại tương tác: click, pickup, examine")]
    public string interactionType = "click";

    [Tooltip("Bắt buộc tương tác để hoàn thành scenario?")]
    public bool isRequired = false;

    [Header("Visual Feedback")]
    [Tooltip("Material khi hover (highlight)")]
    public Material highlightMaterial;

    [Tooltip("Hiện tooltip khi hover")]
    public bool showTooltip = true;

    // ── Internal state ──
    private Material _originalMaterial;
    private Renderer _renderer;
    private bool _hasBeenInteracted = false;

    /// <summary>Vật thể đã được tương tác chưa?</summary>
    public bool HasBeenInteracted => _hasBeenInteracted;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }

        // Đảm bảo Collider không phải trigger (cần cho raycast)
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Debug.LogWarning($"[SmartObject] {objectId}: Collider đang là Trigger, " +
                             "có thể không bắt được raycast. Nên tắt isTrigger.");
        }
    }

    /// <summary>
    /// Được gọi bởi InteractionManager khi người chơi click vào vật thể.
    /// </summary>
    public void OnInteract()
    {
        if (_hasBeenInteracted)
        {
            Debug.Log($"[SmartObject] {objectId} đã được tương tác trước đó.");
            // Vẫn cho phép tương tác lại, nhưng không ghi nhận lần thứ 2
        }

        _hasBeenInteracted = true;

        Debug.Log($"[SmartObject] ▶ Tương tác: {objectName} ({objectId}) → {interactionIntent}");

        // Ghi nhận vào RuntimeState
        if (SimulationManager.Instance?.CurrentCaseState?.runtime_state != null)
        {
            SimulationManager.Instance.CurrentCaseState.runtime_state
                .RecordInteraction(objectId);
        }

        // Fire event qua EventManager
        EventManager.TriggerSmartObjectInteracted(objectId, interactionIntent);
    }

    /// <summary>
    /// Bật highlight khi hover.
    /// </summary>
    public void OnHoverEnter()
    {
        if (_renderer != null && highlightMaterial != null)
        {
            _renderer.material = highlightMaterial;
        }

        EventManager.TriggerSmartObjectHoverEnter(objectId);
    }

    /// <summary>
    /// Tắt highlight khi không hover.
    /// </summary>
    public void OnHoverExit()
    {
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.material = _originalMaterial;
        }

        EventManager.TriggerSmartObjectHoverExit(objectId);
    }

    /// <summary>
    /// Khởi tạo thông tin từ SmartObject data model (khi spawn từ JSON).
    /// </summary>
    public void Initialize(SmartObject data)
    {
        objectId = data.id;
        objectName = data.name;
        isRequired = data.is_required;

        if (!string.IsNullOrEmpty(data.intent))
            interactionIntent = data.intent;

        if (!string.IsNullOrEmpty(data.interaction_type))
            interactionType = data.interaction_type;

        // Đặt vị trí theo tọa độ từ JSON
        transform.position = data.GetPosition();
    }

    /// <summary>Reset trạng thái tương tác (khi chơi lại).</summary>
    public void ResetInteraction()
    {
        _hasBeenInteracted = false;
        OnHoverExit(); // Reset visual
    }

#if UNITY_EDITOR
    /// <summary>Hiện gizmo trong Editor để dễ nhận diện smart object.</summary>
    void OnDrawGizmos()
    {
        Gizmos.color = isRequired ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Hiện tên
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"{objectName}\n[{interactionIntent}]");
    }
#endif
}
