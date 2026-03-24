using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ItemRequirement
{
    public ItemData item;
    public int amount;
}
public enum BlueprintType
{
    Building,       // Xây nhà (Chuồng gà, bò...)
    FarmExpansion   // Mở rộng diện tích vườn
}
[CreateAssetMenu(fileName = "New Blueprint", menuName = "Farm/Building Blueprint")]
public class BuildingBlueprint : ScriptableObject
{
    public string buildingName;
    public Sprite icon;
    public string description;

    public BlueprintType blueprintType = BlueprintType.Building;
    public Vector3 expandSize;
    public Vector3 centerOffset;

    public int currentLevel = 0;
    public int maxLevel = 3;
    public float costMultiplierPerLevel = 2f;

    [Header("Bước 1: Mua Bản Vẽ (Tại Nhà Chính)")]
    public int unlockPrice;
    public List<ItemRequirement> unlockItemCosts;

    [Header("Bước 2: Xây Dựng (Tại Công Trường)")]
    public List<ItemRequirement> buildItemCosts;

    public int GetCurrentUnlockPrice()
    {
        if (blueprintType != BlueprintType.FarmExpansion || currentLevel == 0) return unlockPrice;

        // Công thức: Giá gốc * (Hệ số nhân ^ Cấp độ hiện tại)
        return Mathf.RoundToInt(unlockPrice * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    }

    // --- TÁCH LOGIC: Hàm tính số lượng Nguyên Liệu hiện tại ---
    public int GetCurrentItemAmount(int baseAmount)
    {
        if (blueprintType != BlueprintType.FarmExpansion || currentLevel == 0) return baseAmount;

        return Mathf.RoundToInt(baseAmount * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    }
    private void OnEnable()
    {
        // Tự động dọn dẹp data về 0 để test cho mượt (Sau này làm file Save thì xóa dòng này đi)
        currentLevel = 0;
    }
}