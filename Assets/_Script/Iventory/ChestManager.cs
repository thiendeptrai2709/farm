using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class ChestManager : MonoBehaviour
{
    public static ChestManager Instance;

    [Header("Thư viện Rương (Kéo TẤT CẢ các loại Prefab rương vào đây)")]
    public List<GameObject> chestPrefabs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // [ĐÃ SỬA]: Không gọi LoadChests ở đây nữa vì Player chưa kịp đẻ ra
    }

    // [ĐÃ THÊM]: Bắt đầu nghe đài báo hiệu từ LoadingManager
    private void OnEnable()
    {
        LoadingManager.OnPlayerReady += LoadChestsFromSave;
    }

    // [ĐÃ THÊM]: Hủy nghe đài khi chuyển map
    private void OnDisable()
    {
        LoadingManager.OnPlayerReady -= LoadChestsFromSave;
    }
    private void LoadChestsFromSave()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.GetCurrentData() == null) return;
        GameData data = SaveManager.Instance.GetCurrentData();
        string currentScene = SceneManager.GetActiveScene().name;

        // BƯỚC 1: Tìm rương có sẵn trên Map (Map Chests) để chờ nạp đồ
        Chest[] mapChests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        Dictionary<string, Chest> mapChestDict = new Dictionary<string, Chest>();
        foreach (var c in mapChests)
        {
            if (!c.isBuiltByPlayer) mapChestDict[c.chestID] = c;
        }

        // BƯỚC 2: Quét toàn bộ rương trong File JSON
        foreach (var savedChest in data.savedChests)
        {
            // BỘ LỌC SCENE: Nếu rương thuộc map khác -> NGÓ LƠ NGAY LẬP TỨC!
            if (savedChest.sceneName != currentScene) continue;

            if (savedChest.isBuiltByPlayer)
            {
                // Nhóm 1: Rương người chơi tự đóng (Cần đẻ lại cái thùng)
                GameObject prefab = GetPrefabByID(savedChest.prefabID);
                if (prefab != null)
                {
                    GameObject go = Instantiate(prefab, savedChest.position, savedChest.rotation);
                    Chest chestScript = go.GetComponent<Chest>();
                    chestScript.chestID = savedChest.chestID;
                    chestScript.prefabID = savedChest.prefabID;
                    chestScript.isBuiltByPlayer = true;
                    chestScript.LoadSlotsFromSave(savedChest.slots); // Nhét đồ vào rương
                }
            }
            else
            {
                // Nhóm 2: Rương dính cứng trên Map (Chỉ cần nạp đồ vào)
                if (mapChestDict.ContainsKey(savedChest.chestID))
                {
                    mapChestDict[savedChest.chestID].LoadSlotsFromSave(savedChest.slots);
                }
            }
        }
    }

    // 2. NGƯỜI CHƠI XÂY RƯƠNG MỚI
    public void BuildNewChest(Vector3 position, Quaternion rotation, GameObject originalPrefab)
    {
        // Phải lấy mã sản phẩm từ cái bản vẽ gốc
        string prefabID = originalPrefab.GetComponent<Chest>().prefabID;

        // Cấp ID duy nhất
        string newID = "Chest_Built_" + System.Guid.NewGuid().ToString();

        GameObject newChest = Instantiate(originalPrefab, position, rotation);
        Chest chestScript = newChest.GetComponent<Chest>();

        chestScript.chestID = newID;
        chestScript.prefabID = prefabID; // Gắn mác sản phẩm
        chestScript.isBuiltByPlayer = true; // Đóng dấu Hàng tự chế

        Debug.Log($"Người chơi đã đóng 1 {prefabID} với ID: {newID}");
    }
    public void SaveAllChestsToData(GameData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Dọn sạch data rương cũ CỦA SCENE NÀY trong file (Giữ lại rương của scene khác)
        data.savedChests.RemoveAll(c => c.sceneName == currentScene);

        // Gom tất cả rương đang tồn tại đắp lên file JSON mới
        Chest[] allChestsInScene = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (var c in allChestsInScene)
        {
            SavedChestData scd = new SavedChestData();
            scd.chestID = c.chestID;
            scd.prefabID = c.prefabID;
            scd.sceneName = currentScene; // Gắn mác map!
            scd.position = c.transform.position;
            scd.rotation = c.transform.rotation;
            scd.isBuiltByPlayer = c.isBuiltByPlayer;
            scd.slots = c.GetSavedSlots();

            data.savedChests.Add(scd);
        }
    }
    // Hàm phụ trợ lục thư viện
    private GameObject GetPrefabByID(string searchID)
    {
        foreach (GameObject prefab in chestPrefabs)
        {
            Chest script = prefab.GetComponent<Chest>();
            if (script != null && script.prefabID == searchID)
            {
                return prefab;
            }
        }
        return null;
    }
}