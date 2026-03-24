using UnityEngine;

[CreateAssetMenu(fileName = "New Material", menuName = "Inventory/Material Item")]
public class MaterialItemData : ItemData
{
    private void OnEnable()
    {
        itemType = ItemType.Material;
    }
}