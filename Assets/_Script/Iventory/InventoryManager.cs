using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum StorageType { Hotbar, Inventory, Chest }

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;
    public float currentDurability;
    public InventorySlot(ItemData item, int amount, float durability = -1f)
    {
        this.item = item;
        this.amount = amount;
        this.currentDurability = durability;
    }
    public void AddAmount(int value) { amount += value; }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Cài đặt Hotbar (Dưới đáy)")]
    public int maxHotbarSlots = 8;
    public List<InventorySlot> hotbarSlots = new List<InventorySlot>();
    public int selectedHotbarIndex = -1; // -1 nghĩa là tay không

    [Header("Cài đặt Balo chính (Bấm TAB)")]
    public int maxInventorySlots = 16;
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

    public Chest currentOpenChest;

    public event Action OnInventoryChanged;
    public event Action<bool> OnChestToggled;
    public event Action OnConsumeAnimationStart;

    private StorageType pendingConsumeType;
    private int pendingConsumeIndex = -1;
    public bool isEating = false;
    private float lastConsumeTime = 0f;

    [Header("Hệ thống Rương toàn bản đồ")]
    public List<Chest> allChestsInWorld = new List<Chest>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < maxHotbarSlots; i++) hotbarSlots.Add(new InventorySlot(null, 0, -1f));
        for (int i = 0; i < maxInventorySlots; i++) inventorySlots.Add(new InventorySlot(null, 0, -1f));
    }

    private void Start()
    {

    }

    public void OpenChest(Chest chest)
    {
        currentOpenChest = chest;
        OnChestToggled?.Invoke(true);
        OnInventoryChanged?.Invoke();
    }

    public void CloseChest()
    {
        if (currentOpenChest != null)
        {
            currentOpenChest.CloseChestVisuals();
        }

        currentOpenChest = null;
        OnChestToggled?.Invoke(false);
    }

    // ==========================================
    // LOGIC NHẶT ĐỒ (THÔNG MINH)
    // ==========================================
    public bool AddItem(ItemData itemToAdd, int amountToAdd)
    {
        // 1. Ưu tiên cộng dồn vào Hotbar (nếu Hotbar ĐÃ CÓ sẵn món đó)
        if (itemToAdd.isStackable)
        {
            amountToAdd = AddToExistingStacksOnly(hotbarSlots, itemToAdd, amountToAdd);
            if (amountToAdd <= 0) return true;
        }

        // 2. Chỗ thừa còn lại (hoặc đồ nhặt lần đầu) ném tất cả vào Balo
        amountToAdd = AddToList(inventorySlots, itemToAdd, amountToAdd, -1);

        if (amountToAdd > 0)
        {
            Debug.LogWarning("Balo đã đầy! Không thể nhặt thêm.");
            return false;
        }
        return true;
    }

    // Hàm phụ: Chỉ dò tìm ô CÓ SẴN ĐỒ để gộp, bỏ qua ô rỗng
    private int AddToExistingStacksOnly(List<InventorySlot> targetList, ItemData itemToAdd, int amountLeft)
    {
        foreach (InventorySlot slot in targetList)
        {
            if (slot.item == itemToAdd && slot.amount < itemToAdd.maxStack)
            {
                int spaceLeft = itemToAdd.maxStack - slot.amount;
                if (amountLeft <= spaceLeft)
                {
                    slot.AddAmount(amountLeft);
                    OnInventoryChanged?.Invoke();
                    return 0;
                }
                else
                {
                    slot.AddAmount(spaceLeft);
                    amountLeft -= spaceLeft;
                }
            }
        }
        return amountLeft;
    }

    private int AddToList(List<InventorySlot> targetList, ItemData itemToAdd, int amountLeft, float passedDurability = -1f)
    {
        if (itemToAdd.isStackable)
        {
            foreach (InventorySlot slot in targetList)
            {
                if (slot.item == itemToAdd && slot.amount < itemToAdd.maxStack)
                {
                    int spaceLeft = itemToAdd.maxStack - slot.amount;
                    if (amountLeft <= spaceLeft)
                    {
                        slot.AddAmount(amountLeft);
                        OnInventoryChanged?.Invoke();
                        return 0;
                    }
                    else
                    {
                        slot.AddAmount(spaceLeft);
                        amountLeft -= spaceLeft;
                    }
                }
            }
        }

        foreach (InventorySlot slot in targetList)
        {
            if (slot.item == null && amountLeft > 0)
            {
                slot.item = itemToAdd;
                int amountForNewSlot = Mathf.Min(amountLeft, itemToAdd.maxStack);
                slot.amount = amountForNewSlot;
                amountLeft -= amountForNewSlot;

                if (passedDurability != -1f)
                {
                    slot.currentDurability = passedDurability; // Đồ cũ cất rương/chuyển kho
                }
                else if (itemToAdd is ToolItemData tool)
                {
                    slot.currentDurability = tool.durability; // Đồ mới tinh (full máu)
                }
                else
                {
                    slot.currentDurability = -1f; // Không phải tool
                }
            }
        }

        if (amountLeft < 0) amountLeft = 0;
        OnInventoryChanged?.Invoke();
        return amountLeft;
    }

    // ==========================================
    // LOGIC KÉO THẢ VÀ CHUYỂN ĐỒ
    // ==========================================
    public void SwapItems(StorageType typeA, int indexA, StorageType typeB, int indexB)
    {
        List<InventorySlot> listA = GetListByType(typeA);
        List<InventorySlot> listB = GetListByType(typeB);

        if (listA == null || listB == null) return;

        InventorySlot slotA = listA[indexA];
        InventorySlot slotB = listB[indexB];

        if (slotA.item != null && slotB.item != null && slotA.item == slotB.item && slotA.item.isStackable)
        {
            int total = slotA.amount + slotB.amount;
            if (total <= slotA.item.maxStack)
            {
                slotB.amount = total;
                slotA.item = null; slotA.amount = 0; slotA.currentDurability = -1f;
            }
            else
            {
                slotB.amount = slotA.item.maxStack;
                slotA.amount = total - slotA.item.maxStack;
            }
        }
        else
        {
            ItemData tempItem = slotA.item;
            int tempAmount = slotA.amount;
            float tempDur = slotA.currentDurability;

            slotA.item = slotB.item; slotA.amount = slotB.amount; slotA.currentDurability = slotB.currentDurability;
            slotB.item = tempItem; slotB.amount = tempAmount; slotB.currentDurability = tempDur;
        }

        OnInventoryChanged?.Invoke();
    }

    private List<InventorySlot> GetListByType(StorageType type)
    {
        if (type == StorageType.Hotbar) return hotbarSlots;
        if (type == StorageType.Inventory) return inventorySlots;
        if (type == StorageType.Chest && currentOpenChest != null) return currentOpenChest.chestSlots;
        return null;
    }

    public void SortInventoryButton() { CompactAndSort(StorageType.Inventory); }
    public void SortChestButton() { CompactAndSort(StorageType.Chest); }

    public void CompactAndSort(StorageType type)
    {
        List<InventorySlot> list = GetListByType(type);
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].item != null && list[i].item.isStackable && list[i].amount < list[i].item.maxStack)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (list[j].item == list[i].item && list[j].amount > 0)
                    {
                        int space = list[i].item.maxStack - list[i].amount;
                        int transfer = Mathf.Min(space, list[j].amount);

                        list[i].amount += transfer;
                        list[j].amount -= transfer;

                        if (list[j].amount <= 0)
                        {
                            list[j].item = null;
                            list[j].currentDurability = -1f;
                        }
                        if (list[i].amount == list[i].item.maxStack) break;
                    }
                }
            }
        }

        var tempSortedData = list.Select(slot => new { ItemDataCopy = slot.item, AmountCopy = slot.amount, DurCopy = slot.currentDurability })
                                 .OrderByDescending(data => data.ItemDataCopy != null)
                                 .ThenBy(data => data.ItemDataCopy != null ? data.ItemDataCopy.displayName : "")
                                 .ToList();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].item = tempSortedData[i].ItemDataCopy;
            list[i].amount = tempSortedData[i].AmountCopy;
            list[i].currentDurability = tempSortedData[i].DurCopy;
        }

        OnInventoryChanged?.Invoke();
    }

    public void ShiftClickItem(StorageType fromType, int fromIndex)
    {
        List<InventorySlot> fromList = GetListByType(fromType);
        if (fromList == null || fromList[fromIndex].item == null) return;

        InventorySlot slotToMove = fromList[fromIndex];
        int leftover = slotToMove.amount;
        float durToMove = slotToMove.currentDurability;

        if (FarmPlotUIManager.Instance != null && FarmPlotUIManager.Instance.IsOpen())
        {

            // Nếu đồ ông bấm Shift CÓ PHẢI là Hạt Giống không?
            if (slotToMove.item is SeedItemData seedData)
            {
                if (seedData.isBigTree)
                {
                    Debug.LogWarning("Mầm cây to không thể gieo vào luống đất nhỏ!");
                    return;
                }
                FarmPlotUIManager.Instance.ReceiveSeedFromShiftClick(seedData, fromType, fromIndex);
            }
            else
            {
                Debug.Log("Chỉ có thể Shift-Click Hạt Giống vào ô này!");
            }
            return; // Đã xử lý xong, THOÁT KHỎI HÀM! Không cho chạy code chuyển đồ xuống rương nữa.
        }
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsOpen())
        {
            // Chỉ cho phép chuyển đồ từ Balo lên Bàn Giao Dịch, không cho chuyển ngược lại bằng Shift-Click từ đây
            // (Việc rút lại đồ từ bàn giao dịch đã được xử lý bằng Click trái bên TradingSlotUI)
            if (fromType == StorageType.Inventory || fromType == StorageType.Hotbar)
            {
                bool success = ShopUIManager.Instance.TryAddTradeItemFromShiftClick(slotToMove.item, leftover, out leftover);

                if (success)
                {
                    if (leftover == 0)
                    {
                        slotToMove.item = null;
                        slotToMove.amount = 0;
                        slotToMove.currentDurability = -1f;
                    }
                    else
                    {
                        slotToMove.amount = leftover;
                    }
                    OnInventoryChanged?.Invoke();
                }
                return; // THOÁT KHỎI HÀM, không cho rớt xuống logic rương/hotbar bên dưới
            }
        }
        if (SiteConstructionUIManager.Instance != null && SiteConstructionUIManager.Instance.IsOpen())
        {
            // Chỉ cho phép ném đồ từ Balo hoặc Hotbar vào Hàng Rào
            if (fromType == StorageType.Inventory || fromType == StorageType.Hotbar)
            {
                bool success = SiteConstructionUIManager.Instance.TryReceiveItemFromShiftClick(slotToMove.item, leftover, out leftover);

                if (success)
                {
                    // Cập nhật lại số lượng trong Balo sau khi ném
                    if (leftover <= 0)
                    {
                        slotToMove.item = null;
                        slotToMove.amount = 0;
                        slotToMove.currentDurability = -1f;
                    }
                    else
                    {
                        slotToMove.amount = leftover;
                    }
                    OnInventoryChanged?.Invoke();
                }
                return; // ĐÃ XỬ LÝ XONG! Thoát hàm, không cho rớt xuống Rương hay Shop nữa.
            }
        }
        if (currentOpenChest != null)
        {
            if (fromType == StorageType.Inventory || fromType == StorageType.Hotbar)
                leftover = AddToList(currentOpenChest.chestSlots, slotToMove.item, leftover, durToMove);
            else if (fromType == StorageType.Chest)
                leftover = AddToList(inventorySlots, slotToMove.item, leftover, durToMove);
        }
        else
        {
            if (fromType == StorageType.Inventory)
            {
                // CHẶN CHUYỂN NGUYÊN LIỆU LÊN HOTBAR BẰNG SHIFT
                if (slotToMove.item.itemType == ItemType.Material || slotToMove.item.itemType == ItemType.Misc)
                {
                    Debug.Log("Không thể đưa Nguyên liệu lên Hotbar!");
                    return;
                }
                leftover = AddToList(hotbarSlots, slotToMove.item, leftover, durToMove);
            }
            else if (fromType == StorageType.Hotbar)
            {
                leftover = AddToList(inventorySlots, slotToMove.item, leftover, durToMove);
            }
        }

        if (leftover == 0)
        {
            slotToMove.item = null;
            slotToMove.amount = 0;
            slotToMove.currentDurability = -1f;
        }
        else
        {
            slotToMove.amount = leftover;
        }

        OnInventoryChanged?.Invoke();
    }

    public void ScrollHotbar(float scrollDelta)
    {
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked) return;
        if (Mathf.Abs(scrollDelta) < 0.1f) return;
        if (selectedHotbarIndex == -1)
        {
            selectedHotbarIndex = scrollDelta > 0 ? maxHotbarSlots - 1 : 0;
        }
        else
        {
            if (scrollDelta > 0)
            {
                selectedHotbarIndex--;
                if (selectedHotbarIndex < 0) selectedHotbarIndex = maxHotbarSlots - 1;
            }
            else if (scrollDelta < 0)
            {
                selectedHotbarIndex++;
                if (selectedHotbarIndex >= maxHotbarSlots) selectedHotbarIndex = 0;
            }
        }

        OnInventoryChanged?.Invoke();
    }
    public void CancelPendingConsume()
    {
        if (isEating)
        {
            Debug.Log("Đã hủy ăn uống do làm việc khác!");
            isEating = false;
            pendingConsumeIndex = -1; // Xóa trí nhớ
        }
    }
    public void ConsumeItem(StorageType type, int index)
    {
        if (isEating) return;

        if (Time.time - lastConsumeTime < 0.1f) return;
        lastConsumeTime = Time.time;

        List<InventorySlot> list = GetListByType(type);
        if (list == null || list[index].item == null) return;

        if (list[index].item is ConsumableItemData consumable)
        {
            if (selectedHotbarIndex != -1)
            {
                selectedHotbarIndex = -1;
                Debug.Log("Đã cất vũ khí vào túi để chuẩn bị ăn!");
                OnInventoryChanged?.Invoke();
            }

            isEating = true;
            // Lưu lại vị trí món đồ đang chuẩn bị ăn
            pendingConsumeType = type;
            pendingConsumeIndex = index;

            // Bóp còi cho Animator bắt đầu diễn!
            OnConsumeAnimationStart?.Invoke();
        }
        else
        {
            Debug.Log("Vật phẩm này không thể ăn/uống được!");
        }
    }

    public void FinishConsumingItem()
    {
        if (pendingConsumeIndex == -1) return;

        List<InventorySlot> list = GetListByType(pendingConsumeType);
        if (list != null && list[pendingConsumeIndex].item is ConsumableItemData consumable)
        {
            // Tương lai trừ máu ở đây
            Debug.Log($"[HỆ THỐNG] ĐÃ NUỐT: {consumable.displayName}. Hồi {consumable.healthRestore} máu!");

            list[pendingConsumeIndex].amount--;

            if (list[pendingConsumeIndex].amount <= 0)
            {
                list[pendingConsumeIndex].item = null;
                list[pendingConsumeIndex].currentDurability = -1f;
            }

            OnInventoryChanged?.Invoke();
        }

        pendingConsumeIndex = -1;
    }
    public void ResetConsumeState()
    {
        isEating = false;
    }
    public void UseHotbarSlot(int index)
    {
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked) return;
        if (index < 0 || index >= maxHotbarSlots) return;

        InventorySlot slot = hotbarSlots[index];

        // ==========================================
        // 1. ƯU TIÊN KIỂM TRA ĐỒ ĂN TRƯỚC 
        // (Dù đang sáng viền hay không, cứ là đồ ăn thì bấm vào là nhai luôn)
        // ==========================================
        if (slot.item is ConsumableItemData)
        {
            ConsumeItem(StorageType.Hotbar, index);
            return; // Đã gọi lệnh ăn thì thoát hàm, không chạy xuống dưới nữa
        }

        // ==========================================
        // 2. NẾU KHÔNG PHẢI ĐỒ ĂN (Vũ khí, Công cụ, Rỗng...)
        // ==========================================
        if (selectedHotbarIndex == index)
        {
            // Bấm lại đúng ô đang cầm -> Cất tay không
            CancelPendingConsume();
            selectedHotbarIndex = -1;
            Debug.Log("Đã cất đồ. Đang rảnh tay!");
        }
        else
        {
            // Bấm sang ô mới -> Cầm đồ lên
            CancelPendingConsume();
            selectedHotbarIndex = index;
            string itemName = slot.item != null ? slot.item.displayName : "Tay không";
            Debug.Log($"Đang cầm: {itemName} ở ô số {index + 1}");
        }

        OnInventoryChanged?.Invoke();
    }
    public void RefreshInventoryUI()
    {
        OnInventoryChanged?.Invoke();
    }
    public void SplitItem(StorageType type, int index)
    {
        List<InventorySlot> list = GetListByType(type);

        if (list == null || list[index].item == null)
        {
            Debug.Log("[Chia Đồ] Lỗi: Ô này trống không có gì để chia!");
            return;
        }

        if (list[index].amount <= 1)
        {
            Debug.Log("[Chia Đồ] Lỗi: Số lượng chỉ có 1 món, không thể chia đôi!");
            return;
        }

        // Ưu tiên tìm 1 ô trống ở cùng loại kho chứa
        InventorySlot emptySlot = list.FirstOrDefault(s => s.item == null);

        // Phương án 2: Nếu đang ở Hotbar/Chest mà nó bị đầy, thì thử ném đồ thừa xuống Balo chính
        if (emptySlot == null && type != StorageType.Inventory)
        {
            emptySlot = inventorySlots.FirstOrDefault(s => s.item == null);
        }

        if (emptySlot == null)
        {
            Debug.LogWarning("[Chia Đồ] Thất bại: Balo đã đầy, không còn ô trống để chứa đồ chia ra!");
            return;
        }

        // Thực hiện phép chia
        int amountToMove = list[index].amount / 2;
        list[index].amount -= amountToMove;

        emptySlot.item = list[index].item;
        emptySlot.amount = amountToMove;
        emptySlot.currentDurability = list[index].currentDurability; // Cứ gán đại, dù vũ khí ko stack được
        Debug.Log($"[Chia Đồ] Thành công! Đã tách {amountToMove} món sang ô rỗng.");
        OnInventoryChanged?.Invoke();
    }
    public void RegisterChest(Chest chest)
    {
        if (!allChestsInWorld.Contains(chest)) allChestsInWorld.Add(chest);
    }

    // Rương tự xóa tên khi bị đập đi
    public void UnregisterChest(Chest chest)
    {
        if (allChestsInWorld.Contains(chest)) allChestsInWorld.Remove(chest);
    }
    public int GetTotalItemCount(ItemData targetItem)
    {
        if (targetItem == null) return 0;
        int total = 0;

        // Quét Hotbar
        foreach (var slot in hotbarSlots) { if (slot.item == targetItem) total += slot.amount; }

        // Quét Balo
        foreach (var slot in inventorySlots) { if (slot.item == targetItem) total += slot.amount; }

        // Quét toàn bộ Rương trong game
        foreach (var chest in allChestsInWorld)
        {
            if (chest != null && chest.chestSlots != null)
            {
                foreach (var slot in chest.chestSlots)
                {
                    if (slot.item == targetItem) total += slot.amount;
                }
            }
        }
        return total;
    }
    public void ConsumeItemsGlobal(ItemData targetItem, int amountNeeded)
    {
        if (targetItem == null || amountNeeded <= 0) return;

        // Rút từ Hotbar
        amountNeeded = DeductFromList(hotbarSlots, targetItem, amountNeeded);
        if (amountNeeded <= 0) { OnInventoryChanged?.Invoke(); return; }

        // Rút từ Balo
        amountNeeded = DeductFromList(inventorySlots, targetItem, amountNeeded);
        if (amountNeeded <= 0) { OnInventoryChanged?.Invoke(); return; }

        // Rút máu từ Rương
        foreach (var chest in allChestsInWorld)
        {
            if (chest != null && chest.chestSlots != null)
            {
                amountNeeded = DeductFromList(chest.chestSlots, targetItem, amountNeeded);
                if (amountNeeded <= 0) break; // Đủ đồ rồi thì ngừng hút
            }
        }

        OnInventoryChanged?.Invoke();
    }
    private int DeductFromList(List<InventorySlot> list, ItemData targetItem, int amountNeeded)
    {
        foreach (var slot in list)
        {
            if (slot.item == targetItem && slot.amount > 0)
            {
                if (slot.amount >= amountNeeded)
                {
                    // Ô này đủ bao trọn gói
                    slot.amount -= amountNeeded;
                    if (slot.amount == 0) { slot.item = null; slot.currentDurability = -1f; }
                    return 0; // Trả về 0 nghĩa là đã trừ xong
                }
                else
                {
                    // Ô này không đủ, vét sạch ô này rồi trừ tiếp ở ô khác
                    amountNeeded -= slot.amount;
                    slot.amount = 0;
                    slot.item = null;
                    slot.currentDurability = -1f;
                }
            }
        }
        return amountNeeded; // Trả về số lượng CÒN THIẾU
    }
    public void DeductEquippedToolDurability(float amountToDeduct)
    {
        if (selectedHotbarIndex == -1) return; // Đang cất tay không

        InventorySlot slot = hotbarSlots[selectedHotbarIndex];

        // Chỉ trừ nếu món đang cầm là Công Cụ
        if (slot.item is ToolItemData tool)
        {
            ProcessDurabilityDeduction(slot, tool, amountToDeduct);
        }
    }

    // Trừ đồ trong Balo (Dùng cho Bình Tưới - không cần cầm trên tay)
    public void DeductPersonalToolDurability(ItemData toolItem, float amountToDeduct)
    {
        if (toolItem == null) return;

        foreach (var slot in hotbarSlots)
        {
            if (slot.item == toolItem)
            {
                ProcessDurabilityDeduction(slot, toolItem, amountToDeduct);
                return;
            }
        }

        foreach (var slot in inventorySlots)
        {
            if (slot.item == toolItem)
            {
                ProcessDurabilityDeduction(slot, toolItem, amountToDeduct);
                return;
            }
        }
    }

    // Hàm lõi xử lý trừ máu và vỡ đồ chung cho cả 2 hàm trên
    private void ProcessDurabilityDeduction(InventorySlot slot, ItemData toolItem, float amountToDeduct)
    {
        slot.currentDurability -= amountToDeduct;

        // Hỏng vũ khí / Cạn bình nước
        if (slot.currentDurability <= 0)
        {
            if (toolItem is ToolItemData tool && tool.toolType == ToolType.WateringCan)
            {
                // [ĐÃ SỬA] Nếu là Bình tưới -> Ép máu về 0, KHÔNG xóa đồ
                slot.currentDurability = 0;
                Debug.Log($"[HỆ THỐNG] {toolItem.displayName} đã cạn sạch nước. Hãy đi múc thêm!");
            }
            else
            {
                // Các công cụ khác (Rìu, Cuốc) thì vỡ vụn biến mất
                Debug.Log($"[HỆ THỐNG] Rắc! {toolItem.displayName} của bạn đã vỡ vụn!");

                slot.item = null;
                slot.amount = 0;
                slot.currentDurability = -1f;

                // Nếu đang cầm cái đồ hỏng trên tay thì ép cất đi luôn
                if (hotbarSlots.Contains(slot))
                {
                    int index = hotbarSlots.IndexOf(slot);
                    if (selectedHotbarIndex == index) selectedHotbarIndex = -1;
                }
            }
        }

        OnInventoryChanged?.Invoke(); // Load lại UI để thanh Slider nó tụt
    }
    public void RefillEquippedWateringCan()
    {
        if (selectedHotbarIndex == -1) return; // Đang cất tay không

        InventorySlot slot = hotbarSlots[selectedHotbarIndex];

        if (slot.item is ToolItemData tool && tool.toolType == ToolType.WateringCan)
        {
            slot.currentDurability = tool.durability; // Bơm đầy thanh máu (nước)
            Debug.Log($"[HỆ THỐNG] Đã múc đầy nước vào {tool.displayName}!");

            OnInventoryChanged?.Invoke(); // Cập nhật lại UI Slider
        }
        else
        {
            Debug.Log("Bạn phải cầm Bình Tưới trên tay mới múc được nước!");
        }
    }
    public int GetPersonalItemCount(ItemData targetItem)
    {
        if (targetItem == null) return 0;
        int total = 0;

        // Chỉ quét Hotbar
        foreach (var slot in hotbarSlots) { if (slot.item == targetItem) total += slot.amount; }

        // Chỉ quét Balo
        foreach (var slot in inventorySlots) { if (slot.item == targetItem) total += slot.amount; }

        return total;
    }

    public void ConsumePersonalItems(ItemData targetItem, int amountNeeded)
    {
        if (targetItem == null || amountNeeded <= 0) return;

        // Rút từ Hotbar trước
        amountNeeded = DeductFromList(hotbarSlots, targetItem, amountNeeded);
        if (amountNeeded <= 0) { OnInventoryChanged?.Invoke(); return; }

        // Thiếu thì vét tiếp trong Balo
        amountNeeded = DeductFromList(inventorySlots, targetItem, amountNeeded);

        OnInventoryChanged?.Invoke();
    }
}