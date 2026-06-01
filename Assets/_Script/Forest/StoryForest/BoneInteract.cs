using UnityEngine;

public class BoneInteract : MonoBehaviour, IInteractable
{
    [Tooltip("Tên sẽ hiển thị trên bảng UI (VD: Đầu lâu, Xương sườn)")]
    public string displayName = "Khúc xương";

    private bool isCollected = false;

    public void SyncCollectedState()
    {
        isCollected = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Default");
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = Color.gray;
    }

    public void Interact()
    {
        if (isCollected || SkeletonQuestManager.Instance == null) return;

        // Báo cho UI Ngôi mộ biết đã nhặt được BẰNG CÁCH GỬI CHÍNH NÓ (this)
        SkeletonQuestManager.Instance.CollectBone(this);
        isCollected = true;

        // Vô hiệu hóa Collider để radar (PlayerScanner) quét trượt qua nó
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Cẩn thận hơn: Đổi luôn Layer về Default để tuột khỏi LayerMask của Scanner
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Đổi màu thành xám xịt để báo hiệu đồ đã chết
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.gray;
        }
    }

    public string GetInteractText()
    {
        // Nếu đã nhặt thì không hiện chữ gì hết
        if (isCollected) return string.Empty;

        // Hiện lên màn hình: "[E] Nhặt Đầu lâu"
        return $"[E] Thu thập {displayName}";
    }
}