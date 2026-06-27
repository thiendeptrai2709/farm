using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    [Header("Đa Ngôn Ngữ")]
    public LocalizedString locMerchantWallet;
    public LocalizedString locInStock;
    public LocalizedString locTotal;
    public LocalizedString locTotalEarnings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (shopPanel != null) shopPanel.SetActive(false);
        if (buyPopupPanel != null) buyPopupPanel.SetActive(false);
    }
    private void OnDestroy()
    {
        // Khi scene Tower bị unload, xóa tham chiếu để lần sau quay lại tạo cái mới không bị lỗi
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (IsOpen())
        {
            RefreshUI();
            UpdateTotalSellValue();
            if (buyPopupPanel != null && buyPopupPanel.activeSelf)
            {
                UpdatePopupUI();
                if (selectedBuyItem != null)
                {
                    string inStockTxt = locInStock.IsEmpty ? "In Stock:" : locInStock.GetLocalizedString();
                    popupStockText.text = $"{inStockTxt} {selectedBuyItem.currentQuantity}";
                }
            }
        }
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
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ForceOpen(false);
            // [THÊM MỚI ĐỂ GIỮ HOTBAR]: Ép bật lại giao diện In-Game ngay lập tức
            InventoryUI.Instance.ToggleInGameUI(true);
        }
        // [THÊM MỚI]: Giấu thanh thể lực đi khi bảng Shop đang mở
        if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ToggleVisibility(false);

        _isOpeningShop = false;

        OnShopUIToggled?.Invoke(true);
        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetShopOpenState(true);
        }
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

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ForceClose(false);
            InventoryUI.Instance.ToggleInGameUI(true); // [ĐÃ SỬA]: Thêm dòng này để gọi Hotbar hiện về!
        }

        // [THÊM MỚI]: Bật lại thanh thể lực khi thoát Shop ra ngoài cày cuốc tiếp
        if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ToggleVisibility(true);

        OnShopUIToggled?.Invoke(false);
        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetShopOpenState(false);
        }
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
        {
            string walletTxt = locMerchantWallet.IsEmpty ? "Merchant Wallet:" : locMerchantWallet.GetLocalizedString();
            merchantMoneyText.text = $"{walletTxt} <color=#FFD700>{currentShop.merchantMoney}G</color>";
        }

        if (buyTabContent.gameObject.activeSelf) RefreshBuySlots();
    }

    private void RefreshBuySlots()
    {
        if (currentShop == null) return;

        buySlotsList.RemoveAll(slot => slot == null);
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

        string inStockTxt = locInStock.IsEmpty ? "In Stock:" : locInStock.GetLocalizedString();
        popupStockText.text = $"{inStockTxt} {sItem.currentQuantity}";

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

        string totalTxt = locTotal.IsEmpty ? "Total:" : locTotal.GetLocalizedString();
        popupTotalPriceText.text = $"{totalTxt} <color=#FFD700>{amount * selectedBuyPrice}G</color>";
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

        string earningsTxt = locTotalEarnings.IsEmpty ? "Total Earnings:" : locTotalEarnings.GetLocalizedString();
        totalSellValueText.text = $"{earningsTxt} <color=#FFD700>{total}G</color>";

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
            if (slot.currentItem != null)
            {
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ReportAction("Sell_" + slot.currentItem.name, slot.currentAmount);
                }
            }

            slot.ClearSlot(); // Sau khi báo cáo xong thì mới dọn bàn
        }
        UpdateTotalSellValue();
        RefreshUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Coin_Trade");

        Debug.Log($"Sold successfully! Earned {totalProfit}G");
    }
    public bool TryAddTradeItemFromShiftClick(ItemData item, int amount, out int originalAmountLeft, float passedDurability = -1f)
    {
        originalAmountLeft = amount; // Khởi tạo lượng đồ còn lại

        if (item == MarketManager.Instance.coinItem) return false;

        // Dùng hàm kiểm tra chuẩn từ ShopData
        if (currentShop != null && !currentShop.CanBuyItemFromPlayer(item))
        {
            Debug.LogWarning($"{currentShop.npcName} không mua loại hàng này!");
            return false;
        }

        if (!sellTabContent.gameObject.activeSelf)
        {
            SwitchToSellTab();
        }

        int pricePerUnit = MarketManager.Instance.GetCurrentSellPrice(item);
        bool itemAdded = false; // Đánh dấu xem có nhét thành công món nào lên bàn không

        // ==============================================================
        // 1. NẾU ĐỒ ĐƯỢC XẾP CHỒNG: Ưu tiên nhét vào ô đang có sẵn đồ cùng loại
        // ==============================================================
        if (item.isStackable)
        {
            foreach (TradingSlotUI slot in tradeSlots)
            {
                // Tìm thấy ô có cùng món đồ VÀ ô đó chưa chạm ngưỡng maxStack
                if (slot.currentItem == item && slot.currentAmount < item.maxStack)
                {
                    // Tính xem ô này còn "nuốt" thêm được bao nhiêu cục nữa
                    int spaceLeft = item.maxStack - slot.currentAmount;
                    int amountToAdd = Mathf.Min(originalAmountLeft, spaceLeft);

                    slot.currentAmount += amountToAdd;
                    slot.totalValue = pricePerUnit * slot.currentAmount;
                    slot.UpdateVisuals();

                    originalAmountLeft -= amountToAdd; // Trừ đi số lượng vừa nhét
                    itemAdded = true;

                    // Nếu đã nhét hết sạch đồ trên tay -> Hoàn thành!
                    if (originalAmountLeft <= 0)
                    {
                        UpdateTotalSellValue();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
                        return true;
                    }
                }
            }
        }

        // ==============================================================
        // 2. TÌM Ô TRỐNG ĐỂ NHÉT TIẾP (Nếu đồ chưa hết, hoặc đồ không cho xếp chồng)
        // ==============================================================
        foreach (TradingSlotUI slot in tradeSlots)
        {
            if (slot.currentItem == null)
            {
                // Nếu cho xếp chồng -> Nhét tối đa maxStack. Nếu KHÔNG cho xếp -> Chỉ nhét 1 cái.
                int amountToAdd = item.isStackable ? Mathf.Min(originalAmountLeft, item.maxStack) : 1;

                slot.currentItem = item;
                slot.currentAmount = amountToAdd;
                slot.currentDurability = passedDurability;
                slot.totalValue = pricePerUnit * slot.currentAmount;
                slot.UpdateVisuals();

                originalAmountLeft -= amountToAdd;
                itemAdded = true;

                if (originalAmountLeft <= 0)
                {
                    UpdateTotalSellValue();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
                    return true;
                }
            }
        }

        // ==============================================================
        // 3. NẾU CHẠY ĐẾN ĐÂY MÀ VẪN CÒN ĐỒ (Bàn giao dịch không còn chỗ trống)
        // ==============================================================
        if (itemAdded)
        {
            // Đã nhét được một phần, nhưng bàn bị đầy giữa chừng
            UpdateTotalSellValue();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
            Debug.LogWarning("Bàn giao dịch đã đầy, một số đồ bị giữ lại trong Balo!");

            return true;
        }

        // Không nhét được bất cứ thứ gì lên bàn
        Debug.LogWarning("Bàn giao dịch đã đầy!");
        return false;
    }
}