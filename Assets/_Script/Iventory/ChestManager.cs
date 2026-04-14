using UnityEngine;
using System.Collections.Generic;

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
        // 1. QUÉT SỔ CÁI VÀ ĐẺ LẠI RƯƠNG
        if (GameDataManager.Instance != null)
        {
            foreach (var kvp in GameDataManager.Instance.chestDataDict)
            {
                ChestData data = kvp.Value;
                if (data.isBuiltByPlayer)
                {
                    // Lục tìm trong thư viện xem cục Prefab nào có mã trùng với mã trong Sổ Cái
                    GameObject prefabToSpawn = GetPrefabByID(data.prefabID);

                    if (prefabToSpawn != null)
                    {
                        GameObject restoredChest = Instantiate(prefabToSpawn, data.position, data.rotation);
                        Chest chestScript = restoredChest.GetComponent<Chest>();

                        chestScript.chestID = data.id;
                        chestScript.prefabID = data.prefabID; // Trả lại mã sản phẩm
                        chestScript.isBuiltByPlayer = true;
                    }
                    else
                    {
                        Debug.LogError("Cảnh báo: Không tìm thấy Prefab Rương nào có mã: " + data.prefabID);
                    }
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

        chestScript.ForceSaveToManager();

        Debug.Log($"Người chơi đã đóng 1 {prefabID} với ID: {newID}");
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