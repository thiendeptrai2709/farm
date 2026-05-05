using UnityEngine;
using UnityEngine.EventSystems;

public class SeedDropSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public SeedItemData currentSeed;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            InventorySlotUI draggedSlot = droppedObject.GetComponentInParent<InventorySlotUI>();

            if (draggedSlot != null)
            {
                ItemData draggedItem = GetItemData(draggedSlot.storageType, draggedSlot.slotIndex);

                if (draggedItem is SeedItemData seedData)
                {
                    currentSeed = seedData;
                    FarmPlotUIManager.Instance.OnSeedDropped(seedData, draggedSlot.storageType, draggedSlot.slotIndex);
                }
                else
                {
                    Debug.LogWarning("You can only drop Seeds here!");
                }
            }
        }
    }

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
            currentSeed = null;
            FarmPlotUIManager.Instance.ClearSelectedSeed();

            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.StopHover();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSeed != null && ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StartHover(currentSeed, GetComponent<RectTransform>());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }
}