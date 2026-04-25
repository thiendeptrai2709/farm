using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SavedConstructionSite
{
    public string siteID;
    public int state; // 0 = Hidden, 1 = Pending, 2 = Completed
}

[System.Serializable]
public class SavedQuestProgress
{
    public string questID;
    public int progress;
}

[System.Serializable]
public class SavedSlotData
{
    public string itemID;
    public int amount;
    public float currentDurability;
}

[System.Serializable]
public class SavedChestData
{
    public string chestID;
    public string prefabID;
    public string sceneName; // ĐÂY LÀ CHÌA KHÓA PHÂN BIỆT MAP!
    public Vector3 position;
    public Quaternion rotation;
    public bool isBuiltByPlayer;
    public List<SavedSlotData> slots;
}
[System.Serializable]
public class SavedShopData
{
    public string shopName; // Dùng tên Shop làm ID
    public int currentMoney;
    public List<SavedSlotData> itemsForSale; // Dùng lại SavedSlotData cho tiện
}
[System.Serializable]
public class SavedAnimalData
{
    public string animalName; // Tên con vật (để đối chiếu lúc load)
    public Vector3 position;
    public Quaternion rotation;
    public float currentHunger;
    public AnimalState currentState;
}
[System.Serializable]
public class SavedAnimalPenData
{
    public string penID;
    public List<SavedAnimalData> savedAnimals = new List<SavedAnimalData>();
}
[System.Serializable]
public class SavedDroppedItem
{
    public string sceneName; // Để biết đồ này rớt ở map Farm hay map Town
    public string prefabName; // Tên của Prefab (Vd: "Egg_Pickup")
    public Vector3 position;
    public Quaternion rotation;
    public int amount;
}
[System.Serializable]
public class SavedPropData
{
    public string sceneName;
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public string instanceID;
}
[System.Serializable]
public class SavedTroughData
{
    public string troughID;
    public List<SavedSlotData> slots = new List<SavedSlotData>();
}

[System.Serializable]
public class GameData
{
    public string lastSceneName;
    public Vector3 playerPosition;

    public int daysInGame;
    public int savedHour;
    public int savedMinute;

    public List<SavedSlotData> hotbarData;
    public List<SavedSlotData> inventoryData;
    public List<SavedChestData> savedChests;
    public List<string> activeQuestIDs;
    public List<string> completedQuestIDs;
    public List<SavedQuestProgress> actionProgressList;
    public List<SavedShopData> savedShops;
    public List<FarmPlotData> savedFarmPlots;
    public List<SavedTreePitData> savedTreePits;
    public long lastFarmExitTimeTicks;

    public List<string> pickedUpItemIDs;
    public List<string> unlockedBlueprintIDs;
    public List<SavedConstructionSite> savedConstructionSites;
    public int farmExpansionLevel;
    public Vector3 farmBoundarySize;
    public Vector3 farmBoundaryCenter;
    public int penCapacityLevel;
    public List<SavedAnimalPenData> savedAnimalPens;
    public List<SavedDroppedItem> savedDroppedItems;
    public List<SavedPropData> savedProps;
    public List<SavedTroughData> savedTroughs;

    public GameData()
    {
        lastSceneName = "Farm";
        playerPosition = Vector3.zero;
        hotbarData = new List<SavedSlotData>();
        inventoryData = new List<SavedSlotData>();
        savedChests = new List<SavedChestData>();
        activeQuestIDs = new List<string>();
        completedQuestIDs = new List<string>();
        actionProgressList = new List<SavedQuestProgress>();
        savedShops = new List<SavedShopData>();
        savedFarmPlots = new List<FarmPlotData>();
        savedTreePits = new List<SavedTreePitData>();
        lastFarmExitTimeTicks = 0;
        pickedUpItemIDs = new List<string>();
        unlockedBlueprintIDs = new List<string>();
        savedConstructionSites = new List<SavedConstructionSite>();
        farmExpansionLevel = 0;
        farmBoundarySize = Vector3.zero;
        farmBoundaryCenter = Vector3.zero;
        penCapacityLevel = 0;
        savedAnimalPens = new List<SavedAnimalPenData>();
        savedDroppedItems = new List<SavedDroppedItem>();
        savedProps = new List<SavedPropData>();
        savedTroughs = new List<SavedTroughData>();
    }
}