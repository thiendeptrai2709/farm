using System.Collections.Generic;
using UnityEngine;
using static GameData;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;

    [Header("Cài đặt biến động giá")]
    public float minPriceMultiplier = 0.8f;
    public float maxPriceMultiplier = 1.5f;
    public List<ItemData> allItemsDatabase;

    [Header("Cài đặt Tiền Tệ Vật Lý")]
    public ItemData coinItem;
    // Kéo file "Đồng Vàng" vào đây
    [Header("Danh sách các Sạp hàng (Để restock mỗi ngày)")]
    public List<ShopData> allShopsInGame;
    private Dictionary<ItemData, float> dailyPriceMultipliers = new Dictionary<ItemData, float>();

    private bool hasLoadedFromSave = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay += GenerateNewDailyPrices;

        if (hasLoadedFromSave) return;

        // KIỂM TRA SỔ CÁI: Hôm nay đã khởi tạo thị trường chưa?
        if (GameDataManager.Instance != null)
        {
            if (!GameDataManager.Instance.isMarketInitialized)
            {
                // Nếu chưa (Mới bật game) -> Tạo giá mới và nạp hàng cho NPC
                GenerateNewDailyPrices();
                GameDataManager.Instance.isMarketInitialized = true;
            }
            else
            {
                // Nếu rồi (Do đi xe Bus qua lại) -> Phục hồi lại giá từ Sổ Cái, TUYỆT ĐỐI KHÔNG nạp lại hàng NPC
                dailyPriceMultipliers = new Dictionary<ItemData, float>(GameDataManager.Instance.savedDailyPrices);
                Debug.Log("Đã tải lại giá cả từ Sổ Cái, giữ nguyên tình trạng quầy hàng của NPC!");
            }
        }
        else
        {
            GenerateNewDailyPrices();
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay -= GenerateNewDailyPrices;
    }

    private void GenerateNewDailyPrices()
    {
        dailyPriceMultipliers.Clear();
        foreach (ItemData item in allItemsDatabase)
        {
            float randomMultiplier = Random.Range(minPriceMultiplier, maxPriceMultiplier);
            dailyPriceMultipliers.Add(item, randomMultiplier);
        }

        // [QUAN TRỌNG]: Lưu lại một bản sao của bảng giá vào Sổ Cái để mang đi Scene khác
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.savedDailyPrices = new Dictionary<ItemData, float>(dailyPriceMultipliers);
        }

        foreach (ShopData shop in allShopsInGame)
        {
            if (shop != null) shop.GenerateDailyInventory();
        }

        Debug.Log("Thị trường đã mở cửa! Giá cả và Hàng hóa hôm nay đã được cập nhật.");
    }

    public int GetCurrentSellPrice(ItemData item)
    {
        if (dailyPriceMultipliers.ContainsKey(item))
            return Mathf.RoundToInt(item.sellPrice * dailyPriceMultipliers[item]);
        return item.sellPrice;
    }

    public int GetCurrentBuyPrice(ItemData item)
    {
        if (dailyPriceMultipliers.ContainsKey(item))
            return Mathf.RoundToInt(item.buyPrice * dailyPriceMultipliers[item]);
        return item.buyPrice;
    }

    // ==========================================
    // LOGIC THU NGÂN (MUA / BÁN)
    // ==========================================
    public bool TryBuyItem(ShopInventoryItem shopItem, int pricePerUnit, int amountToBuy, ShopData currentShop)
    {
        int totalPrice = pricePerUnit * amountToBuy;
        int playerPhysicalMoney = CountPlayerCoins();

        if (playerPhysicalMoney < totalPrice)
        {
            Debug.LogWarning("Không đủ tiền trong Balo để mua chừng này đồ!");
            return false;
        }

        // Thử nhét đồ vào Balo (với số lượng tương ứng)
        bool addedSuccessfully = InventoryManager.Instance.AddItem(shopItem.item, amountToBuy, false);
        if (!addedSuccessfully)
        {
            Debug.LogWarning("Balo không đủ chỗ trống!");
            return false;
        }

        // Trừ tiền người chơi, cộng tiền NPC, TRỪ SỐ LƯỢNG TỒN KHO CỦA NPC
        DeductPlayerCoins(totalPrice);
        currentShop.merchantMoney += totalPrice;
        shopItem.currentQuantity -= amountToBuy; // Trừ kho NPC

        InventoryManager.Instance.RefreshInventoryUI();

        return true;
    }

    public bool TrySellItem(ItemData itemToSell, int price, ShopData currentShop, StorageType storageType, int slotIndex)
    {
        if (currentShop.merchantMoney < price)
        {
            Debug.LogWarning($"{currentShop.npcName} đã cạn tiền, không thể mua thêm!");
            return false;
        }

        bool coinsAdded = InventoryManager.Instance.AddItem(coinItem, price, false);
        if (!coinsAdded)
        {
            Debug.LogWarning("Balo không còn chỗ chứa Tiền vàng!");
            return false;
        }

        InventoryManager.Instance.ConsumeItem(storageType, slotIndex);
        currentShop.merchantMoney -= price;


        return true;
    }

    // ==========================================
    // LỤC BALO ĐẾM TIỀN & TRỪ TIỀN
    // ==========================================
    private int CountPlayerCoins()
    {
        int total = 0;
        foreach (var slot in InventoryManager.Instance.hotbarSlots) if (slot.item == coinItem) total += slot.amount;
        foreach (var slot in InventoryManager.Instance.inventorySlots) if (slot.item == coinItem) total += slot.amount;
        return total;
    }

    private void DeductPlayerCoins(int amountToDeduct)
    {
        int remaining = amountToDeduct;
        remaining = DeductFromList(InventoryManager.Instance.inventorySlots, remaining);
        if (remaining > 0) DeductFromList(InventoryManager.Instance.hotbarSlots, remaining);
    }

    private int DeductFromList(System.Collections.Generic.List<InventorySlot> slots, int amount)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].item == coinItem)
            {
                if (slots[i].amount >= amount)
                {
                    slots[i].amount -= amount;

                    // [ĐÃ FIX]: Nếu trừ xong mà bằng 0 thì gán thủ công về rỗng
                    if (slots[i].amount == 0)
                    {
                        slots[i].item = null;
                        slots[i].amount = 0;
                    }
                    return 0;
                }
                else
                {
                    amount -= slots[i].amount;

                    // [ĐÃ FIX]: Vét sạch tiền ở ô này nên cho nó về rỗng
                    slots[i].item = null;
                    slots[i].amount = 0;
                }
            }
        }
        return amount;
    }
    public void SaveShopData(GameData data)
    {
        data.savedShops.Clear();
        foreach (ShopData shop in allShopsInGame)
        {
            if (shop != null)
            {
                SavedShopData savedShop = new SavedShopData();
                savedShop.shopName = shop.npcName; // Dùng tên NPC làm ID (Lưu ý đừng đặt trùng tên 2 ông NPC nhé)
                savedShop.currentMoney = shop.merchantMoney;

                savedShop.itemsForSale = new List<SavedSlotData>();
                foreach (var item in shop.itemsForSale)
                {
                    string id = item.item != null ? item.item.name : "";
                    savedShop.itemsForSale.Add(new SavedSlotData { itemID = id, amount = item.currentQuantity, currentDurability = -1f });
                }

                data.savedShops.Add(savedShop);
            }
        }
        data.savedPrices.Clear();
        foreach (var kvp in dailyPriceMultipliers)
        {
            if (kvp.Key != null)
            {
                data.savedPrices.Add(new SavedPriceMultiplier { itemID = kvp.Key.name, multiplier = kvp.Value });
            }
        }
        Debug.Log("[MarketManager] Đã lưu ví tiền và kho hàng của tất cả NPC.");
    }

    public void LoadShopData(GameData data)
    {
        if (data == null || data.savedShops == null || data.savedShops.Count == 0) return;

        hasLoadedFromSave = true;

        foreach (SavedShopData savedShop in data.savedShops)
        {
            ShopData shop = allShopsInGame.Find(x => x != null && x.npcName == savedShop.shopName);
            if (shop != null)
            {
                // Phục hồi tiền
                shop.merchantMoney = savedShop.currentMoney;

                // Phục hồi kho hàng
                shop.itemsForSale.Clear();
                foreach (var savedSlot in savedShop.itemsForSale)
                {
                    ItemData loadedItem = string.IsNullOrEmpty(savedSlot.itemID) ? null : allItemsDatabase.Find(x => x.name == savedSlot.itemID);
                    if (loadedItem != null)
                    {
                        ShopInventoryItem sItem = new ShopInventoryItem();
                        sItem.item = loadedItem;
                        sItem.currentQuantity = savedSlot.amount;
                        shop.itemsForSale.Add(sItem);
                    }
                }
            }
        }
        if (data.savedPrices != null && data.savedPrices.Count > 0)
        {
            dailyPriceMultipliers.Clear();
            foreach (var savedPrice in data.savedPrices)
            {
                ItemData item = allItemsDatabase.Find(x => x.name == savedPrice.itemID);
                if (item != null)
                {
                    dailyPriceMultipliers.Add(item, savedPrice.multiplier);
                }
            }
        }
        Debug.Log("[MarketManager] Đã tải ví tiền và kho hàng của NPC từ File Save.");
    }
}