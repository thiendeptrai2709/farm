using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private GameData currentData;
    private int currentSlot = 1;

    [Header("Auto Save Settings")]
    public float autoSaveIntervalMinutes = 5f; // Chu kỳ lưu ngầm (Phút)
    private Coroutine autoSaveCoroutine;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void StartAutoSave()
    {
        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
        autoSaveCoroutine = StartCoroutine(AutoSaveLoop());
    }

    public void StopAutoSave()
    {
        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
    }
    private System.Collections.IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveIntervalMinutes * 60f);

            // Tránh lưu nhầm khi đang ở ngoài Main Menu
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            {
                SaveGame();
                Debug.Log($"<color=cyan>[HỆ THỐNG] Đã tự động Auto-Save ngầm sau {autoSaveIntervalMinutes} phút.</color>");
            }
        }
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Application.persistentDataPath + "/MyGameSave_Slot" + slotIndex + ".json";
    }
    public bool HasSaveFile(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }
    public void SetCurrentSlotAndLoad(int slotIndex)
    {
        currentSlot = slotIndex;
        LoadGame();
    }
    public void SaveGame()
    {
        if (currentData == null) currentData = new GameData();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentData.playerPosition = player.transform.position;
            currentData.lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        if (TimeManager.Instance != null)
        {
            currentData.daysInGame = TimeManager.Instance.daysInGame;
        }

        TimeSystem timeSys = UnityEngine.Object.FindFirstObjectByType<TimeSystem>();
        if (timeSys != null)
        {
            currentData.savedHour = timeSys.CurrentHour;
            currentData.savedMinute = timeSys.CurrentMinute;
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveInventoryData(currentData);
        }
        ChestManager currentMapChestManager = UnityEngine.Object.FindFirstObjectByType<ChestManager>();
        if (currentMapChestManager != null)
        {
            currentMapChestManager.SaveAllChestsToData(currentData);
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SaveQuestData(currentData);
        }
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.SaveShopData(currentData);
        }
        if (FarmingZone.Instance != null)
        {
            FarmingZone.Instance.SaveAllPlots(currentData);
        }
        else
        {
            
            if (currentData.lastSceneName != "Farm")
                currentData.lastFarmExitTimeTicks = System.DateTime.Now.Ticks;
        }
        ConstructionSite[] allSites = UnityEngine.Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None);
        foreach (var site in allSites)
        {
            SavedConstructionSite existingSave = currentData.savedConstructionSites.Find(s => s.siteID == site.siteID);
            if (existingSave != null)
            {
                existingSave.state = (int)site.currentState;
            }
            else
            {
                currentData.savedConstructionSites.Add(new SavedConstructionSite { siteID = site.siteID, state = (int)site.currentState });
            }
        }
        AnimalPen[] allPens = UnityEngine.Object.FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
        foreach (var pen in allPens)
        {
            pen.SaveAnimalData(currentData);
        }
        DroppedItemManager currentMapItemManager = UnityEngine.Object.FindFirstObjectByType<DroppedItemManager>();
        if (currentMapItemManager != null)
        {
            currentMapItemManager.SaveDroppedItemsToData(currentData);
        }
        PlacedPropManager currentPropManager = UnityEngine.Object.FindFirstObjectByType<PlacedPropManager>();
        if (currentPropManager != null)
        {
            currentPropManager.SavePropsToData(currentData);
        }
        FoodTrough[] allTroughs = UnityEngine.Object.FindObjectsByType<FoodTrough>(FindObjectsSortMode.None);
        foreach (var trough in allTroughs)
        {
            trough.SaveTroughData(currentData);
        }
        string path = GetSaveFilePath(currentSlot);
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(path, json);
        Debug.Log("<color=green>Đã lưu game thành công tại Slot " + currentSlot + ": </color>" + path);
    }

    public void LoadGame()
    {
        string path = GetSaveFilePath(currentSlot);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            currentData = JsonUtility.FromJson<GameData>(json);
            Debug.Log("<color=yellow>Đã tải dữ liệu Save Game từ Slot " + currentSlot + "!</color>");
        }
        else
        {
            Debug.Log("Không tìm thấy file save ở Slot " + currentSlot + ", tạo dữ liệu mới.");
            currentData = new GameData();
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadQuestData(currentData);
        }
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.LoadShopData(currentData);
        }
        StartAutoSave();
    }
    // Hàm public để các script khác (như LoadingManager) lấy data ra dùng
    public GameData GetCurrentData()
    {
        return currentData;
    }
    public GameData PeekSlotData(int slotIndex)
    {
        string path = GetSaveFilePath(slotIndex);
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(json);
        }
        return null; // Trả về null nếu slot này chưa có ai chơi
    }
}