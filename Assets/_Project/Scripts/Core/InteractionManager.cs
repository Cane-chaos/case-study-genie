using UnityEngine;

// ╔══════════════════════════════════════════════════════════════╗
// ║  ⚠️  FUTURE IMPLEMENTATION — KHÔNG DÙNG TRONG ĐỒ ÁN NÀY  ║
// ║                                                              ║
// ║  Tính năng bấm chọn vật thể 3D đã được ghi nhận để          ║
// ║  phát triển sau. Trong phiên bản hiện tại, input của        ║
// ║  người chơi là Speech-to-Text (giọng nói).                  ║
// ║                                                              ║
// ║  Không xóa file này — giữ để tham khảo khi implement sau.  ║
// ╚══════════════════════════════════════════════════════════════╝

/// <summary>
/// [FUTURE] Quản lý tương tác Raycast click vật thể 3D.
/// Không active trong demo hiện tại.
/// Input chính của người chơi là Speech-to-Text — xem SpeechInputController.cs
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Config — FUTURE IMPLEMENTATION")]
    [Tooltip("Camera dùng để bắn Raycast (để trống = Camera.main)")]
    public Camera interactionCamera;

    [Tooltip("Khoảng cách Raycast tối đa")]
    public float maxRayDistance = 100f;

    [Tooltip("Layer mask cho các vật thể tương tác được")]
    public LayerMask interactableLayerMask = ~0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Toàn bộ logic interaction bị vô hiệu hóa trong scope hiện tại.
        // Bật lại khi implement Smart Object feature trong tương lai.
        this.enabled = false;
        Debug.Log("[InteractionManager] Disabled — Smart Object interaction is a Future Implementation. " +
                  "Current input: Speech-to-Text (SpeechInputController.cs)");
    }

    // ── FUTURE: Các method dưới đây sẽ được bật lại khi implement ──

    public void SetEnabled(bool active)
    {
        // Future: bật/tắt interaction system
    }

    public SmartObjectController GetCurrentHovered()
    {
        return null; // Future: trả về smart object đang hover
    }
}
