using UnityEngine;

public class WeakPointTarget : MonoBehaviour
{
    [Header("Tham chiếu (Kéo thả vào)")]
    public Transform highRockTarget;
    public Transform myMainCamera;
    public GameObject aimDotUI;

    [Header("Cấu hình vật phẩm")]
    public ItemData stoneItemData; // Kéo file data viên đá vào đây

    [Header("Cài đặt Tầm Nhìn")]
    public float maxViewAngle = 30f;

    private bool isPlayerInZone = false;
    private PlayerInputHandler playerInput;

    private void Start()
    {
        if (aimDotUI != null) aimDotUI.SetActive(false);
    }

    // Player bước vào thảm -> Bắt đầu quét
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            // Lấy sẵn bộ đọc phím của Player để tí nữa bắt nút E
            playerInput = other.GetComponent<PlayerInputHandler>();
        }
    }

    // Player bước ra -> Dọn dẹp sạch sẽ
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (aimDotUI != null) aimDotUI.SetActive(false);

            // Tắt chữ UI của ông
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
        }
    }

    private void Update()
    {
        // Phải đứng trong vùng mới chạy code
        if (!isPlayerInZone || myMainCamera == null || highRockTarget == null) return;
        bool isPlaying = ThrowMinigameUI.Instance != null && ThrowMinigameUI.Instance.IsMinigameActive();

        if (isPlaying)
        {
            if (aimDotUI != null && aimDotUI.activeSelf) aimDotUI.SetActive(false);
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
            return;
        }

        Vector3 directionToTarget = (highRockTarget.position - myMainCamera.position).normalized;
        float angle = Vector3.Angle(myMainCamera.forward, directionToTarget);

        if (angle <= maxViewAngle)
        {
            // BẬT CHẤM TRÒN
            if (aimDotUI != null)
            {
                if (!aimDotUI.activeSelf) aimDotUI.SetActive(true);
                aimDotUI.transform.position = highRockTarget.position - (directionToTarget * 1f);
                aimDotUI.transform.LookAt(myMainCamera.position);
                aimDotUI.transform.Rotate(0, 180, 0);
            }

            // 2. CHECK TÚI ĐỒ VÀ HIỆN TEXT CHỮ
            int stoneCount = 0;
            if (InventoryManager.Instance != null && stoneItemData != null)
            {
                // [ĐÃ SỬA LẠI KHÔNG NGU NỮA]: Lấy đúng số lượng trên người thằng Player
                stoneCount = InventoryManager.Instance.GetPersonalItemCount(stoneItemData);
            }

            if (stoneCount > 0)
            {
                // Ép Interaction UI của ông hiện chữ ngay trên đỉnh cục đá (Kèm thanh Slider ẩn)
                if (InteractionUI.Instance != null)
                {
                    InteractionUI.Instance.ShowPrompt(highRockTarget, $"[E] Chuẩn bị ném ({stoneCount})", false);
                }

                // 3. BẤM E ĐỂ LÊN NÒNG
                if (playerInput != null && playerInput.InteractTriggered && StoneThrower.Instance != null && !StoneThrower.Instance.IsAiming)
                {
                    // Dọn UI cho đỡ rối mắt
                    if (aimDotUI != null) aimDotUI.SetActive(false);
                    if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();

                    InteractionUI.Instance.gameObject.SetActive(false);

                    // --- CHUYỂN CAMERA & KHÓA CHÂN ---
                    if (PlayerCameraManager.Instance != null)
                    {
                        PlayerCameraManager.Instance.ToggleThrowCamera(true);
                    }
                    if (PlayerMovement.Instance != null)
                    {
                        PlayerMovement.Instance.isActionLocked = true;
                    }
                    StoneThrower.Instance.StartAiming(highRockTarget);
                    Debug.Log("[HỆ THỐNG] Đã vác đá lên vai, Camera đổi góc ngắm!");
                }
            }
            else
            {
                // Nhìn trúng nhưng hết đá -> Ẩn chữ đi (chỉ để lại chấm tròn)
                if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
            }
        }
        else
        {
            // Ngoảnh mặt đi chỗ khác -> Tắt ráo
            if (aimDotUI != null) aimDotUI.SetActive(false);
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
        }
    }
}