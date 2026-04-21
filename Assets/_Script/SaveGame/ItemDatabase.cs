using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems = new List<ItemData>();

    public ItemData GetItemByName(string itemName)
    {
        foreach (var item in allItems)
        {
            if (item != null && item.name == itemName)
            {
                return item;
            }
        }
        return null;
    }
}