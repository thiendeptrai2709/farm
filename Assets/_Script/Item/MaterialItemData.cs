using UnityEngine;

public enum MaterialType
{
    Wood,
    Stone,
    ore,
    money

}
[CreateAssetMenu(fileName = "New Material", menuName = "Inventory/Material Item")]
public class MaterialItemData : ItemData
{
    public MaterialType materialCategory;

    private void OnEnable()
    {
        itemType = ItemType.Material;
    }
}