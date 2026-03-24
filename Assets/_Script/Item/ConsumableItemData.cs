using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Item")]
public class ConsumableItemData : ItemData // Kế thừa toàn bộ thông tin gốc
{
    [Header("Chỉ số Sinh tồn")]
    public float healthRestore;
    public float hungerRestore;
    public float thirstRestore;

    private void OnEnable()
    {
        itemType = ItemType.Consumable; // Tự động gán loại
    }
}