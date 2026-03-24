using UnityEngine;
using UnityEngine.UI; // THÊM CÁI NÀY để code can thiệp được vào UI
using System.Collections.Generic;

public class ChestUI : MonoBehaviour
{
    [Header("Tham chiếu Giao diện")]
    public GameObject chestPanel;      // Tấm nền của UI Rương
    public RectTransform chestContainer; // (ĐÃ ĐỔI THÀNH RectTransform) Chứa các ô
    public GameObject slotPrefab;

    [Header("Cài đặt Tự động co giãn")]
    public int maxColumns = 4; // Một hàng ngang có tối đa bao nhiêu ô? (Thường là 4 hoặc 5)

    private GridLayoutGroup gridLayout;
    private List<InventorySlotUI> uiChestSlots = new List<InventorySlotUI>();

    private void Start()
    {
        // Tự động tìm cái component chia lưới
        gridLayout = chestContainer.GetComponent<GridLayoutGroup>();

        InventoryManager.Instance.OnChestToggled += TogglePanel;
        InventoryManager.Instance.OnInventoryChanged += UpdateUI;

        chestPanel.SetActive(false);
    }

    private void TogglePanel(bool isOpen)
    {
        chestPanel.SetActive(isOpen);

        if (isOpen)
        {
            InitializeUI();
            UpdateUI();
        }
    }

    private void InitializeUI()
    {
        // 1. Dọn dẹp rác cũ
        foreach (Transform child in chestContainer) Destroy(child.gameObject);
        uiChestSlots.Clear();

        Chest currentChest = InventoryManager.Instance.currentOpenChest;
        if (currentChest == null) return;

        // 2. TỰ ĐỘNG CHIA CỘT (Thông minh)
        // Nếu rương có 3 ô -> Xếp 3 cột. Nếu rương 16 ô -> Xếp max 4 cột rồi xuống dòng.
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = Mathf.Min(currentChest.chestSize, maxColumns);
        }

        // 3. Đẻ ra các ô vuông tương ứng với size rương
        for (int i = 0; i < currentChest.chestSize; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, chestContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            slotUI.storageType = StorageType.Chest;
            slotUI.SetSlotIndex(i);

            uiChestSlots.Add(slotUI);
        }

       
        LayoutRebuilder.ForceRebuildLayoutImmediate(chestPanel.GetComponent<RectTransform>());
    }

    private void UpdateUI()
    {
        if (!chestPanel.activeSelf) return;

        Chest currentChest = InventoryManager.Instance.currentOpenChest;
        if (currentChest == null) return;

        List<InventorySlot> chestData = currentChest.chestSlots;

        for (int i = 0; i < uiChestSlots.Count; i++)
        {
            if (i < chestData.Count) uiChestSlots[i].UpdateSlot(chestData[i]);
            else uiChestSlots[i].ClearSlot();
        }
    }
}