using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization; // Chức năng: Gọi thư viện Đa ngôn ngữ

[System.Serializable]
public struct ItemRequirement
{
    public ItemData item;
    public int amount;
}
public enum BlueprintType
{
    Building,       // Xây nhà (Chuồng gà, bò...)
    FarmExpansion,
    SmallProp,
    PenCapacityUpgrade
}

[CreateAssetMenu(fileName = "New Blueprint", menuName = "Farm/Building Blueprint")]
public class BuildingBlueprint : ScriptableObject
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString localizedBuildingName;
    public LocalizedString localizedDescription;

    // Chức năng: Đọc tên bản vẽ từ bảng dịch hoặc trả về tên file gốc nếu rỗng
    public string buildingName
    {
        get
        {
            return localizedBuildingName.IsEmpty ? name : localizedBuildingName.GetLocalizedString();
        }
    }

    public Sprite icon;

    // Chức năng: Đọc mô tả từ bảng dịch hoặc trả về chuỗi rỗng nếu chưa cài
    public string description
    {
        get
        {
            return localizedDescription.IsEmpty ? "" : localizedDescription.GetLocalizedString();
        }
    }

    public GameObject prefabToBuild;

    public BlueprintType blueprintType = BlueprintType.Building;
    public Vector3 expandSize;
    public Vector3 centerOffset;

    public int baseCapacity = 5;
    public int capacityIncreasePerLevel = 2;

    public int currentLevel = 0;
    public int maxLevel = 3;
    public float costMultiplierPerLevel = 2f;

    [Header("Bước 1: Mua Bản Vẽ (Tại Nhà Chính)")]
    public int unlockPrice;
    public List<ItemRequirement> unlockItemCosts;

    [Header("Bước 2: Xây Dựng (Tại Công Trường)")]
    public List<ItemRequirement> buildItemCosts;

    public int GetCurrentCapacity()
    {
        return baseCapacity + (currentLevel * capacityIncreasePerLevel);
    }

    public int GetCurrentUnlockPrice()
    {
        if ((blueprintType != BlueprintType.FarmExpansion && blueprintType != BlueprintType.PenCapacityUpgrade) || currentLevel == 0) return unlockPrice;

        return Mathf.RoundToInt(unlockPrice * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    }

    // --- TÁCH LOGIC: Hàm tính số lượng Nguyên Liệu hiện tại ---
    public int GetCurrentItemAmount(int baseAmount)
    {
        if ((blueprintType != BlueprintType.FarmExpansion && blueprintType != BlueprintType.PenCapacityUpgrade) || currentLevel == 0) return baseAmount;

        return Mathf.RoundToInt(baseAmount * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    }
    private void OnEnable()
    {
        // Tự động dọn dẹp data về 0 để test cho mượt (Sau này làm file Save thì xóa dòng này đi)
        currentLevel = 0;
    }
}