using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Thẻ căn cước gắn lên món đồ để khi bị đập vỡ, nó tự biết xóa tên khỏi sổ
public class PlacedProp : MonoBehaviour
{
    public string prefabID;
    public string instanceID;

    private void OnDestroy()
    {
        if (PlacedPropManager.Instance != null)
        {
            PlacedPropManager.Instance.RemoveProp(this);
        }
    }
}

public class PlacedPropManager : MonoBehaviour
{
    public static PlacedPropManager Instance;

    [Header("Kho Prefab Đồ Tự Xây")]
    [Tooltip("Kéo thả tất cả các Prefab như Hàng Rào, Lò Rèn, Đèn... (TRỪ RƯƠNG) vào đây để hệ thống Load lại được")]
    public List<GameObject> buildablePrefabs;

    private List<PlacedProp> currentPropsInScene = new List<PlacedProp>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Búa đập xong gọi hàm này để thêm vào sổ
    public void RegisterProp(GameObject propObj, string prefabName, string forcedID = "")
    {
        PlacedProp propComp = propObj.AddComponent<PlacedProp>();
        propComp.prefabID = prefabName;

        // 1. Cấp ID: Nếu là load game thì dùng ID cũ (forcedID), nếu mới cầm búa xây thì tạo ID mới tinh
        if (string.IsNullOrEmpty(forcedID))
        {
            propComp.instanceID = "Prop_" + System.Guid.NewGuid().ToString();
        }
        else
        {
            propComp.instanceID = forcedID;
        }

        // 2. [QUAN TRỌNG]: NẾU MÓN NÀY LÀ MÁNG ĂN -> ÉP NÓ DÙNG CHUNG CÁI ID NÀY LUÔN!
        FoodTrough trough = propObj.GetComponent<FoodTrough>();
        if (trough != null)
        {
            trough.troughID = propComp.instanceID;
        }

        currentPropsInScene.Add(propComp);
    }
    // Đồ bị đập vỡ tự gọi hàm này để xóa sổ
    public void RemoveProp(PlacedProp propComp)
    {
        if (currentPropsInScene.Contains(propComp))
        {
            currentPropsInScene.Remove(propComp);
        }
    }

    public void SavePropsToData(GameData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Dọn dữ liệu rác của map này trong Save cũ
        data.savedProps.RemoveAll(p => p.sceneName == currentScene);

        // Chép list mới vào
        foreach (var prop in currentPropsInScene)
        {
            if (prop != null)
            {
                data.savedProps.Add(new SavedPropData
                {
                    sceneName = currentScene,
                    prefabName = prop.prefabID,
                    instanceID = prop.instanceID,
                    position = prop.transform.position,
                    rotation = prop.transform.rotation
                });
            }
        }
        Debug.Log($"[PlacedProp] Đã lưu {currentPropsInScene.Count} đồ tự xây tại {currentScene}.");
    }

    public void ClearAllPropsInScene()
    {
        foreach (var prop in currentPropsInScene)
        {
            if (prop != null) Destroy(prop.gameObject);
        }
        currentPropsInScene.Clear();
    }

    public void LoadPropsFromData(GameData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (data == null || data.savedProps == null) return;

        ClearAllPropsInScene(); // Dọn dẹp trước khi đổ đồ

        List<SavedPropData> propsInThisScene = data.savedProps.FindAll(p => p.sceneName == currentScene);

        foreach (var sProp in propsInThisScene)
        {
            GameObject prefabToSpawn = buildablePrefabs.Find(p => p.name == sProp.prefabName);

            if (prefabToSpawn != null)
            {
                GameObject newObj = Instantiate(prefabToSpawn, sProp.position, sProp.rotation);
                RegisterProp(newObj, sProp.prefabName, sProp.instanceID); // Gắn thẻ lại cho nó
            }
            else
            {
                Debug.LogWarning($"[PlacedProp] Không tìm thấy Prefab '{sProp.prefabName}' trong danh sách Manager!");
            }
        }
    }
}