using UnityEngine;
using UnityEngine.InputSystem;

public class FoodTroughUIManager : MonoBehaviour
{
    public static FoodTroughUIManager Instance;

    [Header("Giao diện UI")]
    public GameObject troughUIPanel;
    public UITroughDropSlot[] troughUISlots;

    public FoodTrough currentTrough { get; private set; }
    private PlayerInputHandler inputHandler;
    private bool justOpenedThisFrame = false;

    public event System.Action<bool> OnTroughUIToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (troughUIPanel != null) troughUIPanel.SetActive(false);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) inputHandler = player.GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (justOpenedThisFrame)
        {
            justOpenedThisFrame = false;
            return;
        }

        if (troughUIPanel != null && troughUIPanel.activeSelf && inputHandler != null)
        {
            if (inputHandler.InteractTriggered || inputHandler.MenuTriggered)
            {
                CloseTroughUI();
            }
        }
    }

    public void OpenTroughUI(FoodTrough trough)
    {
        currentTrough = trough;
        troughUIPanel.SetActive(true);
        justOpenedThisFrame = true;

        OnTroughUIToggled?.Invoke(true);
        RefreshUI();
    }

    public void CloseTroughUI()
    {
        troughUIPanel.SetActive(false);
        currentTrough = null;
        OnTroughUIToggled?.Invoke(false);
    }

    public void RefreshUI()
    {
        if (currentTrough == null) return;

        for (int i = 0; i < troughUISlots.Length; i++)
        {
            if (i < currentTrough.slots.Length)
            {
                troughUISlots[i].UpdateVisual(currentTrough.slots[i]);
            }
        }
    }

    public bool TryAddFoodFromShiftClick(ItemData item, int amountToMove, out int amountLeft)
    {
        amountLeft = amountToMove;
        if (currentTrough == null) return false;

        // 1. Kiểm tra bộ lọc
        if (!currentTrough.validFoodItem.Contains(item))
        {
            Debug.LogWarning("Món này không phải đồ ăn hợp lệ cho máng!");
            return false;
        }

        // 2. Tìm ô Máng đang chứa cùng loại để nhét thêm vào
        for (int i = 0; i < currentTrough.slots.Length; i++)
        {
            TroughSlot slot = currentTrough.slots[i];
            if (slot.item == item)
            {
                slot.amount += amountLeft;
                amountLeft = 0;

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
                RefreshUI();
                return true; // Bỏ đồ thành công
            }
        }

        // 3. Nếu không có ô cùng loại, tìm ô Trống đầu tiên
        for (int i = 0; i < currentTrough.slots.Length; i++)
        {
            TroughSlot slot = currentTrough.slots[i];
            if (slot.item == null || slot.amount <= 0)
            {
                slot.item = item;
                slot.amount = amountLeft;
                amountLeft = 0;

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");
                RefreshUI();
                return true;
            }
        }

        // 4. Nếu máng đầy
        Debug.LogWarning("Máng đã đầy, không thể ném thêm!");
        return false;
    }

    // ===============================================
    // LOGIC LẤY ĐỒ NHANH BẰNG CHUỘT TRÁI (THÊM MỚI)
    // ===============================================
    public void HandleItemTakenBackWithClick(int troughSlotIndex)
    {
        if (currentTrough == null) return;

        TroughSlot troughSlot = currentTrough.slots[troughSlotIndex];
        if (troughSlot.item == null || troughSlot.amount <= 0) return; // Ô máng rỗng

        int originalAmount = troughSlot.amount;

        // Ép InventoryManager nạp đồ này vào Balo
        bool success = InventoryManager.Instance.AddItem(troughSlot.item, troughSlot.amount, false);

        if (success)
        {
            // Trả đồ thành công -> Xóa đồ ở máng
            troughSlot.item = null;
            troughSlot.amount = 0;

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");

            RefreshUI();
            InventoryManager.Instance.RefreshInventoryUI();
            Debug.Log("Đã lôi thành công đồ ăn từ Máng về Balo bằng Click!");
        }
        else
        {
            Debug.LogWarning("Balo đã đầy, không thể lôi đồ từ máng ra được!");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
        }
    }
    // ===============================================
    // ĐÃ FIX TOÀN BỘ LOGIC TẠI HÀM NÀY
    // ===============================================
    public void HandleItemDropped(InventorySlotUI draggedSlot, int targetTroughSlotIndex)
    {
        if (currentTrough == null) return;

        // 1. Xác định vị trí gốc của ô Balo bị kéo
        InventorySlot sourceSlot = null;
        if (draggedSlot.storageType == StorageType.Hotbar)
            sourceSlot = InventoryManager.Instance.hotbarSlots[draggedSlot.slotIndex];
        else if (draggedSlot.storageType == StorageType.Inventory)
            sourceSlot = InventoryManager.Instance.inventorySlots[draggedSlot.slotIndex];

        if (sourceSlot == null || sourceSlot.item == null || sourceSlot.amount <= 0) return;

        ItemData draggedItem = sourceSlot.item;
        int amountToMove = sourceSlot.amount; // [FIX 3]: Lấy TOÀN BỘ số lượng đang có trong ô

        // 2. Bộ lọc kiểm tra đồ ăn
        if (!currentTrough.validFoodItem.Contains(draggedItem))
        {
            Debug.LogWarning("Máng này chỉ nhận đồ ăn hợp lệ thôi!");
            return;
        }

        TroughSlot targetSlot = currentTrough.slots[targetTroughSlotIndex];

        // 3. Thực hiện chuyển nhượng (Nếu máng trống hoặc cùng loại)
        if (targetSlot.item == null || targetSlot.item == draggedItem)
        {
            // Cộng tát cả đồ vào máng
            targetSlot.item = draggedItem;
            targetSlot.amount += amountToMove;

            // [FIX 1]: Tự tay xóa sạch đồ ở Balo thay vì gọi hàm ConsumeItem
            sourceSlot.item = null;
            sourceSlot.amount = 0;
            sourceSlot.currentDurability = -1f;

            // Refresh để UI cập nhật
            RefreshUI();
            InventoryManager.Instance.RefreshInventoryUI();

            Debug.Log($"Đã ném thành công {amountToMove} {draggedItem.displayName} vào máng ô {targetTroughSlotIndex}!");
        }
        else
        {
            Debug.LogWarning("Ô máng này đang chứa đồ khác, không đè lên được!");
        }
    }
    public void HandleItemTakenBack(int troughSlotIndex, InventorySlotUI targetInvSlot)
    {
        if (currentTrough == null) return;

        TroughSlot troughSlot = currentTrough.slots[troughSlotIndex];
        if (troughSlot.item == null || troughSlot.amount <= 0) return; // Kéo ô rỗng thì bỏ qua

        // Xác định cái ô Balo mà ông vừa thả chuột xuống là ô nào
        InventorySlot destSlot = null;
        if (targetInvSlot.storageType == StorageType.Hotbar)
            destSlot = InventoryManager.Instance.hotbarSlots[targetInvSlot.slotIndex];
        else if (targetInvSlot.storageType == StorageType.Inventory)
            destSlot = InventoryManager.Instance.inventorySlots[targetInvSlot.slotIndex];

        if (destSlot == null) return;

        // Nếu ô Balo đang trống, HOẶC đang chứa cùng loại hạt giống đó
        if (destSlot.item == null || destSlot.item == troughSlot.item)
        {
            int maxStack = troughSlot.item.maxStack;
            int spaceLeft = destSlot.item == null ? maxStack : (maxStack - destSlot.amount);
            int amountToMove = troughSlot.amount;

            if (spaceLeft >= amountToMove)
            {
                // Balo đủ chỗ chứa toàn bộ
                destSlot.item = troughSlot.item;
                destSlot.amount += amountToMove;
                destSlot.currentDurability = -1f;

                // Xóa sạch ở máng
                troughSlot.item = null;
                troughSlot.amount = 0;
            }
            else if (spaceLeft > 0)
            {
                // Balo chỉ chứa được 1 phần, máng giữ lại số còn thừa
                destSlot.item = troughSlot.item;
                destSlot.amount = maxStack;
                destSlot.currentDurability = -1f;
                troughSlot.amount -= spaceLeft;
            }
            else
            {
                Debug.LogWarning("Ô Balo này đã max stack, không nhét thêm được!");
                return;
            }

            RefreshUI();
            InventoryManager.Instance.RefreshInventoryUI();
            Debug.Log("Đã lôi đồ từ Máng cất ngược vào Balo!");
        }
        else
        {
            Debug.LogWarning("Ô Balo này đang chứa món khác, không thả đè lên được!");
        }
    }
}