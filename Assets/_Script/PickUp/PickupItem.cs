using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Dữ liệu món đồ")]
    public ItemData itemData;
    public int amount = 1;

    public string GetInteractText()
    {
        if (itemData != null)
            return "";
        return "Lỗi: Cục này chưa có ItemData!";
    }

    public void Interact()
    {
        if (itemData != null)
        {
            // Gọi Balo tàng hình và xin phép nhét đồ vào
            bool wasPickedUp = InventoryManager.Instance.AddItem(itemData, amount);

            if (wasPickedUp)
            {
                Debug.Log("Vừa cho " + amount + " " + itemData.displayName + " vào balo.");
                Destroy(gameObject); // Nhét thành công thì xóa cục đồ dưới đất
            }
            else
            {
                // Nếu balo đầy, hàm AddItem trả về false, cục đồ vẫn nằm im dưới đất
                Debug.Log("Không thể nhặt " + itemData.displayName + ", balo đã đầy!");
            }
        }
    }
}