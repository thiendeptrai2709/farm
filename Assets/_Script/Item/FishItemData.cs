using UnityEngine;
using System.Collections.Generic;

public enum FishTier
{
    Common,   // Phổ thông
    Uncommon, // Ít gặp
    Rare,     // Hiếm
    Epic,     // Cực hiếm
    Legendary // Huyền thoại
}

// KHAI BÁO CÁC ĐIỀU KIỆN XUẤT HIỆN
public enum SpawnTime { Any, DayOnly, NightOnly }
public enum SpawnWeather { Any, SunnyOnly, RainyOnly }
public enum FishingLocation { Any, Farm, Forest, Town, DeepLake } // Ông có thể thêm tên map tùy ý vào đây

[CreateAssetMenu(fileName = "New Fish", menuName = "Inventory/Fish Item")]
public class FishItemData : ItemData
{
    [Header("Thông tin riêng của Cá")]
    public FishTier tier;

    [Header("Điều Kiện Xuất Hiện (Bộ Lọc)")]
    [Tooltip("Cá này có thể câu được ở những khu vực nào?")]
    public List<FishingLocation> allowedLocations = new List<FishingLocation> { FishingLocation.Any };

    [Tooltip("Chỉ xuất hiện ban ngày hay ban đêm?")]
    public SpawnTime requiredTime = SpawnTime.Any;

    [Tooltip("Chỉ xuất hiện lúc trời mưa hay trời nắng?")]
    public SpawnWeather requiredWeather = SpawnWeather.Any;

    private void OnValidate()
    {
        itemType = ItemType.Fish;
        isStackable = false;
        maxStack = 1;
    }
}