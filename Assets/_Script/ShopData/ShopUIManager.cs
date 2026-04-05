using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Main UI Panel")]
    public GameObject shopPanel;
    public Transform buyTabContent;
    public Transform sellTabContent;
    public GameObject shopSlotPrefab;
    public TextMeshProUGUI merchantMoneyText;

    [Header("Buy Confirmation Popup")]
    public GameObject buyPopupPanel;
    public TextMeshProUGUI popupNameText;
    public TextMeshProUGUI popupStockText;
    public TextMeshProUGUI popupTotalPriceText;
    public TextMeshProUGUI popupAmountText;
    public Slider amountSlider;

    private List<ShopSlotUI> buySlotsList = new List<ShopSlotUI>();
    private List<ShopSlotUI> sellSlotsList = new List<ShopSlotUI>();

    [HideInInspector] public ShopData currentShop;

    private ShopInventoryItem selectedBuyItem;
    private int selectedBuyPrice;

    [Header("Trading Desk (Sell Tab)")]
    public TradingSlotUI[] tradeSlots;
    public TextMeshProUGUI totalSellValueText;
    public Button confirmSellButton;
    public event Action<bool> OnShopUIToggled;

    private Transform playerTransform;
    private Transform currentMerchantTransform;
    private Collider currentMerchantCollider;
    private bool _isOpeningShop = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (shopPanel != null) shopPanel.SetActive(false);
        if (buyPopupPanel != null) buyPopupPanel.SetActive(false);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        // HANDSHAKE WITH INVENTORY: If TAB is pressed, close Shop alongside Inventory
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnInventoryUIToggled += (isBaloOpen) =>
            {
                if (!isBaloOpen && IsOpen() && !_isOpeningShop)
                {
                    CloseShop();
                }
            };
        }
    }

    public Transform GetCurrentMerchantTransform()
    {
        return currentMerchantTransform;
    }

    public Collider GetCurrentMerchantCollider()
    {
        return currentMerchantCollider;
    }

    public Vector3 GetCurrentMerchantPosition()
    {
        return currentMerchantTransform != null ? currentMerchantTransform.position : Vector3.zero;
    }

    public void OpenShop(ShopData shopData, Transform merchantTransform)
    {
        currentShop = shopData;
        currentMerchantTransform = merchantTransform;
        currentMerchantCollider = merchantTransform.GetComponent<Collider>();

        RefreshUI();
        SwitchToBuyTab();

        // Enable Shop Panel
        if (shopPanel != null) shopPanel.SetActive(true);

        _isOpeningShop = true;
        if (InventoryUI.Instance != null) InventoryUI.Instance.ForceOpen(false);
        
        _isOpeningShop = false;

        OnShopUIToggled?.Invoke(true);
    }

    public void CloseShop()
    {
        // GUARD: Prevent infinite loops if already closed
        if (!IsOpen()) return;

        // Return items from trading desk to inventory before closing
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem != null) slot.ReturnItemToInventory();
        }

        currentShop = null;
        currentMerchantTransform = null;
        currentMerchantCollider = null;

        if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.StopHover();

        // Disable Shop Panel
        if (shopPanel != null) shopPanel.SetActive(false);
        if (buyPopupPanel != null) buyPopupPanel.SetActive(false);

        // Tell Inventory to close (This will trigger Camera/Cursor to lock again)
        if (InventoryUI.Instance != null) InventoryUI.Instance.ForceClose(false);

        OnShopUIToggled?.Invoke(false);
    }

    public bool IsOpen()
    {
        return shopPanel != null && shopPanel.activeSelf;
    }

    // ==========================================
    // INTERNAL UI LOGIC
    // ==========================================

    public void SwitchToBuyTab()
    {
        if (!buyTabContent.gameObject.activeSelf && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Item_Pickup");

        buyTabContent.gameObject.SetActive(true);
        sellTabContent.gameObject.SetActive(false);
        RefreshBuySlots();
    }

    public void SwitchToSellTab()
    {
        if (!sellTabContent.gameObject.activeSelf && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Item_Pickup");

        buyTabContent.gameObject.SetActive(false);
        sellTabContent.gameObject.SetActive(true);
    }

    public void RefreshUI()
    {
        if (currentShop == null) return;

        if (merchantMoneyText != null)
            merchantMoneyText.text = $"Merchant Wallet: <color=#FFD700>{currentShop.merchantMoney}G</color>";

        if (buyTabContent.gameObject.activeSelf) RefreshBuySlots();
    }

    private void RefreshBuySlots()
    {
        if (currentShop == null) return;

        // Lấy số slot từ NPC thay vì fix cứng 16
        int targetSlotsCount = currentShop.maxShopSlots;
        int itemsCount = Mathf.Min(currentShop.itemsForSale.Count, targetSlotsCount);

        // Đẻ cho đủ số ô mà NPC yêu cầu (Ví dụ 12)
        while (buySlotsList.Count < targetSlotsCount)
        {
            GameObject newSlotObj = Instantiate(shopSlotPrefab, buyTabContent);
            buySlotsList.Add(newSlotObj.GetComponent<ShopSlotUI>());
        }

        for (int i = 0; i < buySlotsList.Count; i++)
        {
            // Nếu vượt quá số slot của NPC -> Tắt đi (Trường hợp trước đó NPC A có 24 ô, nay NPC B chỉ có 12 ô)
            if (i >= targetSlotsCount)
            {
                buySlotsList[i].gameObject.SetActive(false);
                continue;
            }

            // Nếu nằm trong phạm vi hiển thị
            buySlotsList[i].gameObject.SetActive(true);

            // Có đồ thì setup, không có thì ẩn hình đi (thành ô trống)
            if (i < itemsCount)
            {
                ShopInventoryItem sItem = currentShop.itemsForSale[i];
                int dynamicPrice = MarketManager.Instance.GetCurrentBuyPrice(sItem.item);
                buySlotsList[i].SetupBuySlot(sItem, dynamicPrice);
            }
            else
            {
                // Gọi hàm làm trống ô (Đảm bảo ShopSlotUI của ông có hàm này, hoặc ông tự ẩn ảnh đi)
                buySlotsList[i].SetupBuySlot(null, 0);
            }
        }
    }

    public void ShowBuyPopup(ShopInventoryItem sItem, int price)
    {
        selectedBuyItem = sItem;
        selectedBuyPrice = price;
        buyPopupPanel.SetActive(true);

        popupNameText.text = sItem.item.displayName;
        popupStockText.text = $"In Stock: {sItem.currentQuantity}";

        amountSlider.minValue = 1;
        amountSlider.maxValue = sItem.currentQuantity;
        amountSlider.value = 1;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");

        UpdatePopupUI();
    }

    public void OnSliderValueChanged()
    {
        UpdatePopupUI();
    }

    private void UpdatePopupUI()
    {
        int amount = Mathf.RoundToInt(amountSlider.value);
        popupAmountText.text = amount.ToString();
        popupTotalPriceText.text = $"Total: <color=#FFD700>{amount * selectedBuyPrice}G</color>";
    }

    public void ConfirmBuy()
    {
        int amount = Mathf.RoundToInt(amountSlider.value);
        bool success = MarketManager.Instance.TryBuyItem(selectedBuyItem, selectedBuyPrice, amount, currentShop);

        if (success)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Coin_Trade");

            buyPopupPanel.SetActive(false);
            RefreshUI();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error"); // Không đủ tiền / Balo đầy
        }
    }

    public void CancelBuy()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");

        buyPopupPanel.SetActive(false);
    }

    public void UpdateTotalSellValue()
    {
        int total = 0;
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem != null) total += slot.totalValue;
        }

        totalSellValueText.text = $"Total Earnings: <color=#FFD700>{total}G</color>";
        confirmSellButton.interactable = (total > 0);
    }

    public void ConfirmSellTransaction()
    {
        int totalProfit = 0;
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem != null) totalProfit += slot.totalValue;
        }

        if (totalProfit > currentShop.merchantMoney)
        {
            Debug.LogWarning("The Merchant doesn't have enough money to buy these items!");
            return;
        }

        bool coinsAdded = InventoryManager.Instance.AddItem(MarketManager.Instance.coinItem, totalProfit, false);
        if (!coinsAdded)
        {
            Debug.LogWarning("Inventory is full, cannot receive Coins!");
            return;
        }

        currentShop.merchantMoney -= totalProfit;

        foreach (TradingSlotUI slot in tradeSlots)
        {
            slot.ClearSlot();
        }

        UpdateTotalSellValue();
        RefreshUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Coin_Trade");

        Debug.Log($"Sold successfully! Earned {totalProfit}G");
    }
    public bool TryAddTradeItemFromShiftClick(ItemData item, int amount, out int originalAmountLeft)
    {
        originalAmountLeft = amount; // Khởi tạo lượng đồ còn lại (ban đầu là tất cả)

        // Kiểm tra xem NPC có mua đồ này không
        if (item == MarketManager.Instance.coinItem) return false;
        if (currentShop != null && !currentShop.acceptedItemTypesToBuy.Contains(item.itemType))
        {
            Debug.LogWarning($"{currentShop.npcName} không mua loại hàng này!");
            return false;
        }
        if (!sellTabContent.gameObject.activeSelf)
        {
            SwitchToSellTab();
        }
        int pricePerUnit = MarketManager.Instance.GetCurrentBuyPrice(item); // Note: Should probably be GetCurrentSellPrice(item) based on MarketManager structure, but assuming you use BuyPrice for now.

        // 1. Ưu tiên cộng dồn vào ô TradingSlot đang có sẵn đồ cùng loại
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem == item)
            {
                // Trading slots usually don't have maxStack limits in games unless specified. 
                // We'll assume they can hold whatever you shift-click, or we just add it.
                slot.currentAmount += originalAmountLeft;
                slot.totalValue = pricePerUnit * slot.currentAmount;
                slot.UpdateVisuals(); // Make sure UpdateVisuals() in TradingSlotUI is PUBLIC
                UpdateTotalSellValue();

                originalAmountLeft = 0;

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
                
                return true;
            }
        }

        // 2. Nếu không có ô nào trùng loại, tìm ô TradingSlot trống đầu tiên
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem == null)
            {
                slot.currentItem = item;
                slot.currentAmount = originalAmountLeft;
                slot.totalValue = pricePerUnit * slot.currentAmount;
                slot.UpdateVisuals(); // Make sure UpdateVisuals() in TradingSlotUI is PUBLIC
                UpdateTotalSellValue();

                originalAmountLeft = 0;

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");

                return true;
            }
        }

        // Không còn ô trống trên bàn giao dịch
        Debug.LogWarning("Bàn giao dịch đã đầy!");
        return false;
    }
}