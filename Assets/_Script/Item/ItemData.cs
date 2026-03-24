using UnityEngine;

// Lưu ý: Tui bỏ dòng CreateAssetMenu ở đây vì mình sẽ không tạo "Item chung chung" nữa
public class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string displayName;
    public Sprite icon;
    public ItemType itemType;

    [Header("Cài đặt Túi đồ")]
    public bool isStackable = true;
    public int maxStack = 99;

    [Header("Giá trị Mua Bán")]
    public int sellPrice;
    public int buyPrice;
}