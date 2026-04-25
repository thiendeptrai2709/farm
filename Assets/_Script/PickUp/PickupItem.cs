using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Dữ liệu món đồ")]
    public ItemData itemData;
    public int amount = 1;

    public bool isStaticMapItem = false;
    public string uniqueID;
    public string GetInteractText()
    {
        // 1. Phải check xem cục đồ đã được gắn ItemData thật chưa đã
        if (itemData != null)
        {
            // 2. Nếu balo kịch kim hết chỗ -> Hiện chữ báo đầy (Bác sẽ không cúi xuống nhặt được)
            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasSpaceFor(itemData, amount))
            {
                return ""; // Nếu bác muốn im bặt thì đổi thành return "";
            }

            // 3. Bình thường, balo còn chỗ -> Trả về rỗng để sạch màn hình (Nhân vật sẽ cúi nhặt bình thường)
            return "";
        }

        // 4. Nếu bác thật sự quên kéo ItemData ở Inspector thì nó mới báo lỗi này
        return "Lỗi: Cục này chưa có ItemData!";
    }
    private void Start()
    {
        // CHỈ KIỂM TRA ID nếu đây là đồ tĩnh đặt sẵn trên map
        if (isStaticMapItem)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
            {
                if (SaveManager.Instance.GetCurrentData().pickedUpItemIDs.Contains(uniqueID))
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // [MỚI]: Nếu là đồ đẻ ra (động), tự động báo danh với Trưởng Bản Đồ
            if (DroppedItemManager.Instance != null)
            {
                DroppedItemManager.Instance.RegisterItem(this);
            }
        }
    }
    private void OnDestroy()
    {
        if (!isStaticMapItem && DroppedItemManager.Instance != null)
        {
            DroppedItemManager.Instance.UnregisterItem(this);
        }
    }
    public void Interact()
    {
        if (itemData != null)
        {
            // Gọi Balo tàng hình và xin phép nhét đồ vào
            bool wasPickedUp = InventoryManager.Instance.AddItem(itemData, amount);

            if (wasPickedUp)
            {
                if (isStaticMapItem && SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
                {
                    SaveManager.Instance.GetCurrentData().pickedUpItemIDs.Add(uniqueID);
                }

                Debug.Log("Vừa nhặt: " + itemData.displayName);
                Destroy(gameObject);
            }
            else
            {
                // Nếu balo đầy, hàm AddItem trả về false, cục đồ vẫn nằm im dưới đất
                Debug.Log("Không thể nhặt " + itemData.displayName + ", balo đã đầy!");
            }
        }
    }
    [ContextMenu("Tự động tạo ID cho đồ lượm")]
    private void AutoGenerateID()
    {
        // Tạo một chuỗi ngẫu nhiên không bao giờ trùng giống hệt rương
        uniqueID = "Pickup_" + System.Guid.NewGuid().ToString().Substring(0, 8);

        // Dòng này báo cho Unity biết file đã bị sửa để nó cho phép bấm Save (Ctrl + S)
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}