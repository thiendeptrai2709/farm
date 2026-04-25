using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DroppedItemManager : MonoBehaviour
{
    public static DroppedItemManager Instance;

    [Header("Kho Prefab Đồ Rơi")]
    [Tooltip("Kéo thả tất cả các Prefab như Trứng, Sữa, Củ cải rớt... vào đây để hệ thống biết mà đẻ ra khi Load game")]
    public List<PickupItem> dropPrefabs;

    // Bộ nhớ nội bộ đếm những cục đồ đang rớt thực tế trên Map này
    private List<PickupItem> currentDroppedItems = new List<PickupItem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void ClearAllDroppedItemsInScene()
    {
        foreach (var item in currentDroppedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        currentDroppedItems.Clear();
    }

    // Các quả trứng vừa đẻ ra sẽ tự gọi hàm này để xin vào danh sách
    public void RegisterItem(PickupItem item)
    {
        if (!currentDroppedItems.Contains(item))
        {
            currentDroppedItems.Add(item);
        }
    }

    // Khi bạn nhặt lên, quả trứng tự gọi hàm này để xóa tên
    public void UnregisterItem(PickupItem item)
    {
        if (currentDroppedItems.Contains(item))
        {
            currentDroppedItems.Remove(item);
        }
    }

    public void SaveDroppedItemsToData(GameData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 1. Dọn sạch rác của Map này trong File Save cũ
        data.savedDroppedItems.RemoveAll(item => item.sceneName == currentScene);

        // 2. Chép toàn bộ đồ đang rớt vào File Save
        foreach (var item in currentDroppedItems)
        {
            if (item != null)
            {
                SavedDroppedItem sItem = new SavedDroppedItem
                {
                    sceneName = currentScene,
                    prefabName = item.gameObject.name.Replace("(Clone)", "").Trim(),
                    position = item.transform.position,
                    rotation = item.transform.rotation,
                    amount = item.amount
                };
                data.savedDroppedItems.Add(sItem);
            }
        }
        Debug.Log($"[DroppedItem] Đã lưu {currentDroppedItems.Count} món đồ rớt tại {currentScene}.");
    }

    public void LoadDroppedItemsFromData(GameData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (data == null || data.savedDroppedItems == null) return;

        
        ClearAllDroppedItemsInScene();

        // Lọc ra đúng những cục đồ rớt ở Map hiện tại
        List<SavedDroppedItem> itemsInThisScene = data.savedDroppedItems.FindAll(item => item.sceneName == currentScene);

        foreach (var sItem in itemsInThisScene)
        {
            // Đi tìm Prefab dựa theo cái tên đã lưu
            PickupItem prefabToSpawn = dropPrefabs.Find(p => p.gameObject.name == sItem.prefabName);

            if (prefabToSpawn != null)
            {
                PickupItem newObj = Instantiate(prefabToSpawn, sItem.position, sItem.rotation);
                newObj.amount = sItem.amount;
                // Không cần Add vào currentDroppedItems vì hàm Start() của cục đồ sẽ tự làm chuyện đó.
            }
            else
            {
                Debug.LogWarning($"[DroppedItem] CẢNH BÁO: Không tìm thấy Prefab '{sItem.prefabName}' trong danh sách Drop Prefabs của Manager!");
            }
        }
    }
}