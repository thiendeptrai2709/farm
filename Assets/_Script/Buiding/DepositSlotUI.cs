using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DepositSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Tham chiếu UI")]
    public Image itemIcon;
    public TextMeshProUGUI amountText;

    [Header("Dữ liệu đang chứa")]
    public ItemData currentItem;
    public int currentAmount;
    public int requiredAmount; // [MỚI]: Biến lưu số lượng yêu cầu (VD: 10)

    // [MỚI]: Thằng Quản Lý sẽ gọi hàm này để gán nhiệm vụ cho đĩa lúc mới mở bảng
    public void SetupRequirement(ItemData reqItem, int reqAmount)
    {
        currentItem = reqItem;
        requiredAmount = reqAmount;
        currentAmount = 0; // Mới mở thì chưa nộp gì cả
        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        InventorySlotUI draggedSlot = droppedObj.GetComponentInParent<InventorySlotUI>();
        if (draggedSlot != null)
        {
            ItemData draggedItem = GetItemData(draggedSlot.storageType, draggedSlot.slotIndex);
            int draggedAmount = GetItemAmount(draggedSlot.storageType, draggedSlot.slotIndex);

            // [ĐÃ SỬA]: CHỈ NHẬN ĐỒ NẾU THẢ ĐÚNG MÓN MÀ CÁI ĐĨA NÀY YÊU CẦU!
            if (draggedItem != null && draggedItem == currentItem)
            {
                // Tính xem còn thiếu bao nhiêu cục để không bị nạp lố
                int needed = requiredAmount - currentAmount;
                if (needed <= 0) return; // Đã nạp đủ rồi thì không nhận thêm nữa

                // Lấy số lượng thực tế cần nạp (tránh việc ném 50 cục vào lỗ chỉ cần 10)
                int amountToTake = Mathf.Min(draggedAmount, needed);

                currentAmount += amountToTake;
                InventoryManager.Instance.ConsumeItemsGlobal(draggedItem, amountToTake);

                UpdateVisuals();
                if (SiteConstructionUIManager.Instance != null) SiteConstructionUIManager.Instance.RefreshUI();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentAmount > 0)
        {
            ReturnToInventory();
        }
    }

    public void ReturnToInventory()
    {
        if (currentAmount <= 0) return; // Không có đồ thì thôi

        bool success = InventoryManager.Instance.AddItem(currentItem, currentAmount);
        if (success)
        {
            currentAmount = 0; // Trả xong thì về 0 (nhưng vẫn giữ hình yêu cầu)
            UpdateVisuals();
            if (SiteConstructionUIManager.Instance != null) SiteConstructionUIManager.Instance.RefreshUI();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentAmount = 0;
        requiredAmount = 0;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (currentItem != null)
        {
            itemIcon.sprite = currentItem.icon;
            itemIcon.gameObject.SetActive(true);

            // Nếu chưa nộp cục nào -> Icon mờ mờ
            if (currentAmount == 0)
                itemIcon.color = new Color(1, 1, 1, 0.3f);
            else // Đã nộp rồi -> Icon sáng rõ
                itemIcon.color = Color.white;

            // LUÔN LUÔN in dòng chữ 0/10, 5/10
            amountText.text = $"{currentAmount}/{requiredAmount}";
        }
        else
        {
            // Đĩa dư (không dùng đến) thì tàng hình
            itemIcon.sprite = null;
            itemIcon.color = new Color(1, 1, 1, 0);
            itemIcon.gameObject.SetActive(false);
            amountText.text = "";
        }
    }

    // ==========================================
    // HÀM HỖ TRỢ ĐỌC DATA (Giữ nguyên)
    // ==========================================
    private ItemData GetItemData(StorageType type, int index)
    {
        if (type == StorageType.Hotbar) return InventoryManager.Instance.hotbarSlots[index].item;
        if (type == StorageType.Inventory) return InventoryManager.Instance.inventorySlots[index].item;
        if (type == StorageType.Chest && InventoryManager.Instance.currentOpenChest != null)
        {
            if (index < InventoryManager.Instance.currentOpenChest.chestSlots.Count)
                return InventoryManager.Instance.currentOpenChest.chestSlots[index].item;
        }
        return null;
    }

    private int GetItemAmount(StorageType type, int index)
    {
        if (type == StorageType.Hotbar) return InventoryManager.Instance.hotbarSlots[index].amount;
        if (type == StorageType.Inventory) return InventoryManager.Instance.inventorySlots[index].amount;
        if (type == StorageType.Chest && InventoryManager.Instance.currentOpenChest != null)
        {
            if (index < InventoryManager.Instance.currentOpenChest.chestSlots.Count)
                return InventoryManager.Instance.currentOpenChest.chestSlots[index].amount;
        }
        return 0;
    }
}