using UnityEngine;
using UnityEngine.EventSystems;

public class SeedDropSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 1. Lấy cái Object đang bị chuột kéo lê
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            // 2. Kiểm tra xem nó có phải là 1 ô đồ trong Inventory của ông không
            InventorySlotUI draggedSlot = droppedObject.GetComponentInParent<InventorySlotUI>();

            if (draggedSlot != null)
            {
                ItemData draggedItem = GetItemData(draggedSlot.storageType, draggedSlot.slotIndex);

                // 3. Kiểm tra xem đồ thả vào CÓ ĐÚNG LÀ HẠT GIỐNG KHÔNG?
                if (draggedItem is SeedItemData seedData)
                {
                    // Nếu đúng -> Báo cáo lên Manager để hiển thị ảnh và mở khóa nút Trồng
                    FarmPlotUIManager.Instance.OnSeedDropped(seedData, draggedSlot.storageType, draggedSlot.slotIndex);
                }
                else
                {
                    Debug.LogWarning("You can only drop Seeds here!");
                }
            }
        }
    }

    // Hàm phụ: Dò tìm Data gốc của món đồ (Tương tự code trong Inventory của ông)
    private ItemData GetItemData(StorageType type, int index)
    {
        if (type == StorageType.Hotbar) return InventoryManager.Instance.hotbarSlots[index].item;
        if (type == StorageType.Inventory) return InventoryManager.Instance.inventorySlots[index].item;
        return null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (FarmPlotUIManager.Instance != null)
        {
            FarmPlotUIManager.Instance.ClearSelectedSeed();
        }
    }
}