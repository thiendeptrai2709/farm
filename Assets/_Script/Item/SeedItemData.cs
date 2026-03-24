using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Inventory/Seed Item")]
public class SeedItemData : ItemData
{
    [Header("Cài đặt Hạt Giống")]
    public float growTime = 600f; 
    public GameObject cropPrefab; 
    [Header("Cài đặt Thu Hoạch")]
    public ItemData yieldItem; 
    public int yieldAmount = 1;


    public ItemData woodItem;     
    public int woodAmount = 3;
    public GameObject fruitModelPrefab;


    [Header("Thu Hoạch Nhiều Lần (Cà chua, Dưa chuột...)")]
    public bool isMultiHarvest = false; 
    public int maxHarvestTimes = 3; 
    public float regrowTime = 2880f;

    public bool isBigTree = false;
    private void OnEnable()
    {
        itemType = ItemType.Consumable;
        isStackable = true;
    }
}