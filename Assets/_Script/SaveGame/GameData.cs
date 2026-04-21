using UnityEngine;
using System.Collections.Generic;

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
public class GameData
{
    public string lastSceneName;
    public Vector3 playerPosition;

    public int daysInGame;
    public int savedHour;
    public int savedMinute;

    // [ĐÃ THÊM]: Hai danh sách chứa dữ liệu đồ đạc
    public List<SavedSlotData> hotbarData;
    public List<SavedSlotData> inventoryData;
    public List<SavedChestData> savedChests;

    public List<string> activeQuestIDs;
    public List<string> completedQuestIDs;
    public List<SavedQuestProgress> actionProgressList;
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
    }
}