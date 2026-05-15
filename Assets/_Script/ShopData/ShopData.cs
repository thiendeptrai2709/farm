using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization; // [THÊM MỚI] Thư viện đa ngôn ngữ

[System.Serializable]
public class ShopInventoryItem
{
    public ItemData item;
    public int currentQuantity = 10;
}

[CreateAssetMenu(fileName = "New Shop", menuName = "Market/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString localizedNpcName; // [THÊM MỚI] Biến chứa Key dịch tên chủ sạp

    [Header("Thông tin chủ sạp")]
    // Chức năng: Đọc tên NPC từ bảng dịch, nếu rỗng thì lấy tạm tên file ScriptableObject
    public string npcName
    {
        get
        {
            return localizedNpcName.IsEmpty ? name : localizedNpcName.GetLocalizedString();
        }
    }

    [Header("Ví tiền của NPC (Hiển thị UI)")]
    public int merchantMoney = 1000;
    // Chức năng: Khai báo số tiền tối thiểu NPC có mỗi ngày
    public int minDailyMoney = 500;
    // Chức năng: Khai báo số tiền tối đa NPC có mỗi ngày
    public int maxDailyMoney = 2000;

    // [ĐÃ THÊM]: Giao quyền quyết định số ô cho NPC này (Ví dụ: 12 ô)
    [Header("Cài đặt Sạp hàng")]
    public int maxShopSlots = 12;

    [Header("Danh sách bán ra (Mình mua của họ)")]
    public List<ShopInventoryItem> itemsForSale;

    [Header("Kho Tổng (Tất cả đồ có thể bán)")]
    public List<ItemData> possibleItemsToSell;
    public int minQuantity = 1;    // Số lượng ít nhất mỗi món
    public int maxQuantity = 10;   // Số lượng nhiều nhất mỗi món

    [Header("Danh sách thu mua (Họ mua của mình)")]
    public List<ItemType> acceptedItemTypesToBuy;

    public void GenerateDailyInventory()
    {
        merchantMoney = Random.Range(minDailyMoney, maxDailyMoney + 1);

        itemsForSale.Clear(); // Dọn sạch quầy hàng hôm qua

        if (possibleItemsToSell == null || possibleItemsToSell.Count == 0) return;

        // TRƯỜNG HỢP 1: Số món trong kho ÍT HƠN HOẶC BẰNG số slot (VD: 10 món <= 12 slot)
        // -> Chắc chắn bán đủ 10 món, chỉ random số lượng.
        if (possibleItemsToSell.Count <= maxShopSlots)
        {
            foreach (ItemData item in possibleItemsToSell)
            {
                ShopInventoryItem newItem = new ShopInventoryItem();
                newItem.item = item;
                newItem.currentQuantity = Random.Range(minQuantity, maxQuantity + 1);
                itemsForSale.Add(newItem);
            }
        }
        // TRƯỜNG HỢP 2: Số món trong kho NHIỀU HƠN số slot (VD: 50 món > 12 slot)
        // -> Phải xáo bài và bốc đúng 12 món ra bán.
        else
        {
            List<ItemData> shuffledPool = new List<ItemData>(possibleItemsToSell);
            for (int i = 0; i < shuffledPool.Count; i++)
            {
                ItemData temp = shuffledPool[i];
                int randomIndex = Random.Range(i, shuffledPool.Count);
                shuffledPool[i] = shuffledPool[randomIndex];
                shuffledPool[randomIndex] = temp;
            }

            for (int i = 0; i < maxShopSlots; i++)
            {
                ShopInventoryItem newItem = new ShopInventoryItem();
                newItem.item = shuffledPool[i];
                newItem.currentQuantity = Random.Range(minQuantity, maxQuantity + 1);
                itemsForSale.Add(newItem);
            }
        }
    }
}