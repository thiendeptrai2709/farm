using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private readonly byte[] encryptionKey = Encoding.UTF8.GetBytes("G7k9P2mX5vA1qL4zD8wE3bY6hN0cR1fT");
    private readonly byte[] encryptionIV = Encoding.UTF8.GetBytes("J4mB7vC2zX9qR1wF");

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
    public void SaveAllNPCsToData(GameData data)
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        NPCVillager[] villagers = UnityEngine.Object.FindObjectsByType<NPCVillager>(FindObjectsSortMode.None);
        foreach (var v in villagers)
        {
            // Ép thêm tên Scene vào trước tên NPC
            string uniqueID = currentScene + "_" + v.gameObject.name;
            SavedNPCData existing = data.savedNPCs.Find(n => n.npcName == uniqueID);
            if (existing != null) existing.position = v.transform.position;
            else data.savedNPCs.Add(new SavedNPCData { npcName = uniqueID, position = v.transform.position });
        }

        NPCMerchant[] merchants = UnityEngine.Object.FindObjectsByType<NPCMerchant>(FindObjectsSortMode.None);
        foreach (var m in merchants)
        {
            string uniqueID = currentScene + "_" + m.gameObject.name;
            SavedNPCData existing = data.savedNPCs.Find(n => n.npcName == uniqueID);
            if (existing != null) existing.position = m.transform.position;
            else data.savedNPCs.Add(new SavedNPCData { npcName = uniqueID, position = m.transform.position });
        }
    }
    private string Encrypt(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = encryptionKey;
            aesAlg.IV = encryptionIV;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    private string Decrypt(string cipherText)
    {
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = encryptionKey;
                aesAlg.IV = encryptionIV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch
        {
            return string.Empty;
        }
    }
    public void SaveGame()
    {
        if (currentData == null) currentData = new GameData();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentData.playerPosition = player.transform.position;
            currentData.playerRotation = player.transform.rotation;
            currentData.lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        if (PlayerStamina.Instance != null)
        {
            currentData.currentStamina = PlayerStamina.Instance.currentStamina;
            currentData.maxStamina = PlayerStamina.Instance.maxStamina;
        }
        if (Camera.main != null)
        {
            currentData.cameraAngles = Camera.main.transform.eulerAngles;
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
            currentData.selectedHotbarIndex = InventoryManager.Instance.selectedHotbarIndex;
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
        if (SkeletonQuestManager.Instance != null)
        {
            SkeletonQuestManager.Instance.SaveQuestData(currentData);
        }
        if (FarmingZone.Instance != null)
        {
            FarmingZone.Instance.SaveAllPlots(currentData);

            currentData.lastFarmExitTimeTicks = System.DateTime.Now.Ticks;
        }
        else
        {
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

        SaveAllNPCsToData(currentData);

        if (BusUI.Instance != null)
        {
            currentData.unlockedBusStops = new System.Collections.Generic.List<string>(BusUI.Instance.discoveredStops);
        }
        string path = GetSaveFilePath(currentSlot);
        string json = JsonUtility.ToJson(currentData, true);
        string encryptedData = Encrypt(json);

        string capturedPath = path;
        string capturedData = encryptedData;
        System.Threading.Tasks.Task.Run(() =>
        {
            File.WriteAllText(capturedPath, capturedData);
        });
        Debug.Log("<color=green>Đã lưu game thành công tại Slot " + currentSlot + ": </color>" + path);
    }

    public void LoadGame()
    {
        string path = GetSaveFilePath(currentSlot);
        if (File.Exists(path))
        {
            string encryptedData = File.ReadAllText(path);
            string json = Decrypt(encryptedData);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Dữ liệu save bị hỏng hoặc sai mã hóa!");
                currentData = new GameData();
            }
            else
            {
                currentData = JsonUtility.FromJson<GameData>(json);
                Debug.Log("<color=yellow>Đã tải dữ liệu Save Game từ Slot " + currentSlot + "!</color>");
            }
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
        if (BusUI.Instance != null && currentData.unlockedBusStops != null)
        {
            // Nếu là mảng trống thì BusUI sẽ tự động gọi UnlockDefaultRoutes (nếu ông viết logic đó ở Awake)
            // Cứ cắm thẳng dữ liệu từ file Save đè vào list hiện tại
            BusUI.Instance.discoveredStops = new System.Collections.Generic.List<string>(currentData.unlockedBusStops);
        }
        if (SkeletonQuestManager.Instance != null)
        {
            SkeletonQuestManager.Instance.LoadQuestData(currentData);
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
            string encryptedData = System.IO.File.ReadAllText(path);
            string json = Decrypt(encryptedData);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonUtility.FromJson<GameData>(json);
            }
        }
        return null;
    }
}