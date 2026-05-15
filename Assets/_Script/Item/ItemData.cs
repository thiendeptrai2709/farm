using UnityEngine;
using UnityEngine.Localization;

public class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public LocalizedString localizedDisplayName;
    public Sprite icon;
    public ItemType itemType;

    [Header("Cài đặt Túi đồ")]
    public bool isStackable = true;
    public int maxStack = 99;

    [Header("Giá trị Mua Bán")]
    public int sellPrice;
    public int buyPrice;

    public string displayName
    {
        get
        {
            // Nếu chưa gán Key đa ngôn ngữ thì lấy tạm tên của file ScriptableObject, nếu gán rồi thì bốc chữ từ từ điển ra
            return localizedDisplayName.IsEmpty ? name : localizedDisplayName.GetLocalizedString();
        }
    }
}