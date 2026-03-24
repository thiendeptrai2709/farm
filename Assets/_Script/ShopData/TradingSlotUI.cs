using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TradingSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI valueText; // Hiện số tiền bán được của riêng ô này

    [HideInInspector] public ItemData currentItem;
    [HideInInspector] public int currentAmount;
    [HideInInspector] public int totalValue;

    private void Awake()
    {
        if (currentItem == null)
        {
            ClearSlot();
        }
    }

    // ==========================================
    // 1. NHẬN ĐỒ KHI KÉO THẢ VÀO (IDropHandler)
    // ==========================================
    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return; // Ô này đang để đồ rồi thì không cho đè lên

        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) return;

        // Bắt lấy cái Ô Balo đang được kéo tới
        InventorySlotUI invSlot = draggedObj.GetComponent<InventorySlotUI>();
        if (invSlot != null)
        {
            // Chọc vào kho dữ liệu Balo để lấy thông tin đồ
            InventorySlot slotData = GetSlotData(invSlot.storageType, invSlot.slotIndex);
            if (slotData == null || slotData.item == null) return;

            ItemData item = slotData.item;
            ShopData currentShop = ShopUIManager.Instance.currentShop;

            // KIỂM TRA LUẬT LỆ: Cấm bán Tiền và NPC phải chịu mua món này
            if (item == MarketManager.Instance.coinItem) return;
            if (!currentShop.acceptedItemTypesToBuy.Contains(item.itemType))
            {
                Debug.LogWarning($"{currentShop.npcName} không mua loại hàng này!");
                return;
            }

            // CHUYỂN ĐỒ: Copy dữ liệu sang Bàn Giao Dịch
            currentItem = item;
            currentAmount = slotData.amount;
            int pricePerUnit = MarketManager.Instance.GetCurrentSellPrice(item);
            totalValue = pricePerUnit * currentAmount;

            // XÓA ĐỒ: Rút sạch đồ ở ô Balo đó
            slotData.item = null;
            slotData.amount = 0;
            invSlot.UpdateSlot(slotData); // Cập nhật lại hình ảnh Balo thành rỗng

            UpdateVisuals();
            ShopUIManager.Instance.UpdateTotalSellValue(); // Báo Manager tính tổng tiền
        }
    }

    // ==========================================
    // 2. BẤM VÀO ĐỂ LẤY LẠI ĐỒ (HỦY BÁN MÓN ĐÓ)
    // ==========================================
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

        // Trả lại Balo
        bool added = InventoryManager.Instance.AddItem(currentItem, currentAmount);
        if (added)
        {
            ClearSlot();
            ShopUIManager.Instance.UpdateTotalSellValue();
        }
        else
        {
            Debug.LogWarning("Balo đầy, không thể cất lại đồ!"); // Thường ít xảy ra vì lúc nãy rút đồ ra đã tạo ô trống rồi
        }
    }

    // ==========================================
    // GIAO DIỆN & TIỆN ÍCH
    // ==========================================
    public void UpdateVisuals()
    {
        icon.sprite = currentItem.icon;
        icon.enabled = true;
        amountText.text = currentAmount > 1 ? currentAmount.ToString() : "";
        valueText.text = $"<color=#FFD700>+{totalValue}G</color>";
        valueText.enabled = true;
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentAmount = 0;
        totalValue = 0;
        icon.sprite = null;
        icon.enabled = false;
        amountText.text = "";
        valueText.text = "";
        valueText.enabled = false;
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
}