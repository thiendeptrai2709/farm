using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI priceText;

    [HideInInspector] public ItemData currentItem;
    [HideInInspector] public bool isBuyMode = true;

    private int itemPrice;

    // [ĐÃ THÊM]: Tham chiếu đến "Gói hàng" chứa số lượng của NPC
    private ShopInventoryItem currentShopItemRef;

    private StorageType itemStorageType;
    private int itemSlotIndex;

    // Hàm Setup cho Tab MUA (Dùng ShopInventoryItem)
    public void SetupBuySlot(ShopInventoryItem shopItem, int price)
    {
        // [CHỐT CHẶN CHỐNG LỖI NULL]: Nếu NPC có 12 ô nhưng chỉ có 10 món, 2 ô thừa sẽ bị truyền vào giá trị null.
        if (shopItem == null || shopItem.item == null)
        {
            ClearSlot(); // Dọn dẹp sạch sẽ ô này thành ô rỗng tàng hình
            return;      // THOÁT HÀM NGAY LẬP TỨC để chống lỗi Null dòng bên dưới!
        }

        currentShopItemRef = shopItem;
        currentItem = shopItem.item; // Lấy dữ liệu Item gốc để hiện ảnh/tooltip
        isBuyMode = true;
        itemPrice = price;

        UpdateVisuals();
    }

    // Hàm Setup cho Tab BÁN (Dùng ItemData như cũ)
    public void SetupSellSlot(ItemData item, int price, StorageType sType, int sIndex)
    {
        currentShopItemRef = null;
        currentItem = item;
        isBuyMode = false;
        itemPrice = price;
        itemStorageType = sType;
        itemSlotIndex = sIndex;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (currentItem != null)
        {
            icon.sprite = currentItem.icon;
            icon.enabled = true;

            // [ĐÃ SỬA]: Kiểm tra xem phải đang ở Tab Mua và có bị hết hàng không?
            if (isBuyMode && currentShopItemRef != null && currentShopItemRef.currentQuantity <= 0)
            {
                // HẾT HÀNG: Ảnh tối đi, chữ báo đỏ
                icon.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Màu xám đen, hơi mờ
                priceText.text = "<color=#FF5555>Sold Out</color>"; // Hoặc ghi "Hết hàng"
            }
            else
            {
                // CÒN HÀNG (Hoặc đang ở Tab Bán): Ảnh sáng bình thường
                icon.color = Color.white;
                priceText.text = isBuyMode ? $"<color=#55FF55>{itemPrice}G</color>" : $"<color=#FFD700>+{itemPrice}G</color>";
            }

            priceText.enabled = true;
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentShopItemRef = null;
        icon.sprite = null;
        icon.enabled = false;
        priceText.text = "";
        priceText.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isBuyMode)
            {
                // [ĐÃ SỬA]: Hết lỗi! Giờ không mua thẳng nữa mà gọi Bảng Popup lên
                if (currentShopItemRef.currentQuantity > 0)
                {
                    ShopUIManager.Instance.ShowBuyPopup(currentShopItemRef, itemPrice);
                }
                else
                {
                    Debug.Log("Món này đã hết hàng!"); // (Tương lai có thể làm chữ Sold Out mờ đi)
                }
            }
            else
            {
                // BÁN (Tạm giữ nguyên code click bán thẳng, bài sau mình sẽ làm bàn giao dịch 5 ô)
                bool success = MarketManager.Instance.TrySellItem(currentItem, itemPrice, ShopUIManager.Instance.currentShop, itemStorageType, itemSlotIndex);
                if (success) ShopUIManager.Instance.RefreshUI();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && ItemTooltipUI.Instance != null)
        {
            RectTransform myRect = GetComponent<RectTransform>();
            ItemTooltipUI.Instance.StartHover(currentItem, myRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.StopHover();
    }
}