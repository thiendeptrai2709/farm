using UnityEngine;
public enum ToolType
{
    None,       // Tay không
    Axe,        // Rìu (Chặt cây gỗ)
    Pickaxe,    // Cuốc chim (Đập đá khoáng)
    Hoe,        // Cuốc thường (Cuốc đất làm nông)
    Hammer,     // Lưỡi hái (Gặt cỏ, lúa)
    WateringCan,// Bình tưới nước
    Sword,
    FishingRod
}
[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Tool Item")]
public class ToolItemData : ItemData
{
    [Header("Phân loại & Cấp độ (Quan Trọng)")]
    public ToolType toolType;

    [Tooltip("Cấp độ vũ khí: 1 (Đá), 2 (Đồng), 3 (Sắt), 4 (Vàng)...")]
    [Min(1)]
    public int toolTier = 1;

    [Header("Chỉ số Chiến đấu / Khai thác")]
    public float baseDamage = 10f;  // Sát thương gốc chém vào quái/cây
    public float durability = 100f; // Độ bền (Tương lai làm mẻ rìu)
    public float staminaCost = 5f;  // Thể lực tiêu hao mỗi nhát chém

    [Header("Mô hình hiển thị trên tay")]
    public GameObject toolPrefab;   // Cái VỎ BỌC (Wrapper) hôm trước mình làm

    private void OnEnable()
    {
        itemType = ItemType.Tool;
        isStackable = false; // Vũ khí 100% không cho xếp chồng
    }
}