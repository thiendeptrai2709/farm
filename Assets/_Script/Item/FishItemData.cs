using UnityEngine;

// Khai báo các cấp bậc của cá để dễ phân loại và làm màu UI sau này
public enum FishTier
{
    Common,   // Phổ thông (Cá chép, cá rô...)
    Uncommon, // Ít gặp
    Rare,     // Hiếm
    Epic,     // Cực hiếm
    Legendary // Huyền thoại (Cá mập, thuỷ quái...)
}

// Bật CreateAssetMenu để ông có thể chuột phải tạo Data Cá trực tiếp trong Unity Hub
[CreateAssetMenu(fileName = "New Fish", menuName = "Inventory/Fish Item")]
public class FishItemData : ItemData // <--- KẾ THỪA TỪ ItemData
{
    [Header("Thông tin riêng của Cá")]
    public FishTier tier;
    private void OnValidate()
    {
        itemType = ItemType.Fish;

        isStackable = false;
        maxStack = 1;
    }
}