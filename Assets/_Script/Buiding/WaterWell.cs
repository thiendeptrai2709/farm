using UnityEngine;

public class WaterWell : MonoBehaviour, IInteractable
{
    [Header("Hiệu ứng múc nước")]
    public ParticleSystem refillSplashEffect; // Kéo thả hiệu ứng nước vào đây (nếu có)

    public string GetInteractText()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
        {
            // Soi xem ông đang cầm cái gì trên tay
            InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

            if (slot.item is ToolItemData tool && tool.toolType == ToolType.WateringCan)
            {
                // Nếu bình đã đầy thì báo đầy
                if (slot.currentDurability >= tool.durability)
                {
                    return "Bình đã đầy nước!";
                }

                // Nếu bình vơi thì hiện nút múc
                return "[E] Múc Nước";
            }
        }

        // Nếu cất tay không hoặc cầm Rìu/Cuốc thì hiện cái này
        return "Giếng Nước (Cần cầm Bình Tưới)";
    }

    public void Interact()
    {
        // Chặn cổng 1: Tay không thì cút
        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1) return;

        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

        // Chặn cổng 2: Chỉ chạy lệnh nếu đúng là Bình Tưới
        if (slot.item is ToolItemData tool && tool.toolType == ToolType.WateringCan)
        {
            // Chặn cổng 3: Phải vơi nước mới cho múc
            if (slot.currentDurability < tool.durability)
            {
                InventoryManager.Instance.RefillEquippedWateringCan();

                if (refillSplashEffect != null)
                {
                    refillSplashEffect.Play();
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("Water_Pour");
                }
            }
        }
    }
}