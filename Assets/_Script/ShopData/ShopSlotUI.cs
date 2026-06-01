using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI priceText;

    // [ĐÃ THÊM]: Ảnh hiển thị màu cấp độ (Tier)
    public Image tierMarkerImage;

    [HideInInspector] public ItemData currentItem;
    [HideInInspector] public bool isBuyMode = true;

    private int itemPrice;
    private ShopInventoryItem currentShopItemRef;
    private StorageType itemStorageType;
    private int itemSlotIndex;

    public void SetupBuySlot(ShopInventoryItem shopItem, int price)
    {
        if (shopItem == null || shopItem.item == null)
        {
            ClearSlot();
            return;
        }

        currentShopItemRef = shopItem;
        currentItem = shopItem.item;
        isBuyMode = true;
        itemPrice = price;

        UpdateVisuals();
    }

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

            if (isBuyMode && currentShopItemRef != null && currentShopItemRef.currentQuantity <= 0)
            {
                icon.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                priceText.text = "<color=#FF5555>Sold Out</color>";
            }
            else
            {
                icon.color = Color.white;
                priceText.text = isBuyMode ? $"<color=#55FF55>{itemPrice}G</color>" : $"<color=#FFD700>+{itemPrice}G</color>";
            }

            priceText.enabled = true;

            // [ĐÃ THÊM]: Cập nhật màu sắc Tier
            UpdateTierVisuals();
        }
        else
        {
            ClearSlot();
        }
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
        currentShopItemRef = null;
        icon.sprite = null;
        icon.enabled = false;
        priceText.text = "";
        priceText.enabled = false;

        // Nhớ ẩn Tier Marker khi ô trống
        if (tierMarkerImage != null) tierMarkerImage.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isBuyMode)
            {
                if (currentShopItemRef.currentQuantity > 0)
                {
                    ShopUIManager.Instance.ShowBuyPopup(currentShopItemRef, itemPrice);
                }
                else
                {
                    Debug.Log("Món này đã hết hàng!");
                }
            }
            else
            {
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