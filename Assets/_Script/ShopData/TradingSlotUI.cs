using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TradingSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI valueText;

    // [ĐÃ THÊM]: Ảnh hiển thị màu cấp độ (Tier)
    public Image tierMarkerImage;

    [HideInInspector] public ItemData currentItem;
    [HideInInspector] public int currentAmount;
    [HideInInspector] public int totalValue;
    [HideInInspector] public float currentDurability = -1f;

    private void Awake()
    {
        if (currentItem == null)
        {
            ClearSlot();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return;

        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) return;

        InventorySlotUI invSlot = draggedObj.GetComponent<InventorySlotUI>();
        if (invSlot != null)
        {
            InventorySlot slotData = GetSlotData(invSlot.storageType, invSlot.slotIndex);
            if (slotData == null || slotData.item == null) return;

            ItemData item = slotData.item;
            ShopData currentShop = ShopUIManager.Instance.currentShop;

            if (item == MarketManager.Instance.coinItem) return;

            if (!currentShop.CanBuyItemFromPlayer(item))
            {
                Debug.LogWarning($"{currentShop.npcName} không mua loại hàng này!");
                return;
            }

            currentItem = item;
            currentAmount = slotData.amount;
            currentDurability = slotData.currentDurability;
            int pricePerUnit = MarketManager.Instance.GetCurrentSellPrice(item);
            totalValue = pricePerUnit * currentAmount;

            slotData.item = null;
            slotData.amount = 0;
            slotData.currentDurability = -1f;
            invSlot.UpdateSlot(slotData);

            UpdateVisuals();
            ShopUIManager.Instance.UpdateTotalSellValue();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Item_Drop");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null && eventData.button == PointerEventData.InputButton.Left)
        {
            ReturnItemToInventory();
        }
    }

    public void ReturnItemToInventory()
    {
        if (currentItem == null) return;

        bool added = InventoryManager.Instance.AddItem(currentItem, currentAmount, false, currentDurability);
        if (added)
        {
            ClearSlot();
            ShopUIManager.Instance.UpdateTotalSellValue();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Item_Drop");
            }
        }
        else
        {
            Debug.LogWarning("Balo đầy, không thể cất lại đồ!");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("UI_Error");
            }
        }
    }

    public void UpdateVisuals()
    {
        icon.sprite = currentItem.icon;
        icon.enabled = true;
        amountText.text = currentAmount > 1 ? currentAmount.ToString() : "";
        valueText.text = $"<color=#FFD700>+{totalValue}G</color>";
        valueText.enabled = true;

        // [ĐÃ THÊM]: Cập nhật màu sắc Tier
        UpdateTierVisuals();
    }

    // [ĐÃ THÊM]: Hàm xử lý màu Tier
    private void UpdateTierVisuals()
    {
        if (tierMarkerImage != null)
        {
            if (currentItem is FishItemData fish)
            {
                tierMarkerImage.gameObject.SetActive(true);
                tierMarkerImage.color = GetColorFromTier(fish.tier);
            }
            else if (currentItem is ToolItemData toolItem)
            {
                tierMarkerImage.gameObject.SetActive(true);
                tierMarkerImage.color = GetColorFromToolTier(toolItem.toolTier);
            }
            else
            {
                tierMarkerImage.gameObject.SetActive(false);
            }
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentAmount = 0;
        totalValue = 0;
        currentDurability = -1f;
        icon.sprite = null;
        icon.enabled = false;
        amountText.text = "";
        valueText.text = "";
        valueText.enabled = false;

        // Nhớ ẩn Tier Marker khi ô trống
        if (tierMarkerImage != null) tierMarkerImage.gameObject.SetActive(false);
    }

    private InventorySlot GetSlotData(StorageType type, int index)
    {
        if (type == StorageType.Hotbar) return InventoryManager.Instance.hotbarSlots[index];
        if (type == StorageType.Inventory) return InventoryManager.Instance.inventorySlots[index];
        return null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.StartHover(currentItem, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.StopHover();
    }

    // ==========================================
    // BỘ HÀM CHUYỂN ĐỔI MÀU TIER
    // ==========================================
    private Color GetColorFromTier(FishTier tier)
    {
        switch (tier)
        {
            case FishTier.Common: return Color.white;
            case FishTier.Uncommon: return new Color(0.2f, 1f, 0.2f);
            case FishTier.Rare: return new Color(0.2f, 0.6f, 1f);
            case FishTier.Epic: return new Color(0.8f, 0.2f, 1f);
            case FishTier.Legendary: return new Color(1f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }

    private Color GetColorFromToolTier(int tier)
    {
        switch (tier)
        {
            case 1: return new Color(0.5f, 0.5f, 0.5f);
            case 2: return new Color(0.8f, 0.4f, 0.15f);
            case 3: return new Color(0.75f, 0.75f, 0.8f);
            case 4: return new Color(1f, 0.85f, 0f);
            default: return Color.white;
        }
    }
}