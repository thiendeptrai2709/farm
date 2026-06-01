using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TrashUIManager : MonoBehaviour
{
    public static TrashUIManager Instance;

    [Header("UI Components")]
    public GameObject confirmPanel; // Kéo khối Panel (cái khung nền xám) vào đây
    public TextMeshProUGUI warningText; // Chữ "Bạn có chắc muốn vứt cái X không?"
    public Image itemIcon; // Ảnh của món đồ chuẩn bị vứt

    private StorageType targetStorage;
    private int targetIndex;
    private ItemData itemToTrash;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    // Hàm này được gọi từ InventorySlotUI khi người chơi trỏ chuột vào đồ và bấm R
    public void ShowConfirmPanel(StorageType type, int index, ItemData item)
    {
        targetStorage = type;
        targetIndex = index;
        itemToTrash = item;

        // Cập nhật thông tin lên bảng
        if (itemIcon != null) itemIcon.sprite = item.icon;
        if (warningText != null) warningText.text = $"Do you want to destroy:\n<color=yellow>{item.displayName}</color>?";

        confirmPanel.SetActive(true);
    }

    // Gắn hàm này vào Nút "YES" (Đồng ý vứt) trên Panel
    public void ConfirmTrash()
    {
        if (itemToTrash == null) return;

        // Xóa hoàn toàn món đồ khỏi kho
        if (targetStorage == StorageType.Inventory)
        {
            InventoryManager.Instance.inventorySlots[targetIndex].item = null;
            InventoryManager.Instance.inventorySlots[targetIndex].amount = 0;
        }
        else if (targetStorage == StorageType.Hotbar)
        {
            InventoryManager.Instance.hotbarSlots[targetIndex].item = null;
            InventoryManager.Instance.hotbarSlots[targetIndex].amount = 0;
        }
        else if (targetStorage == StorageType.Chest && InventoryManager.Instance.currentOpenChest != null)
        {
            InventoryManager.Instance.currentOpenChest.chestSlots[targetIndex].item = null;
            InventoryManager.Instance.currentOpenChest.chestSlots[targetIndex].amount = 0;
        }

        // Cập nhật lại hình ảnh Balo
        InventoryManager.Instance.RefreshInventoryUI();

        // Kêu cái xoảng cho sinh động (Tùy chọn)
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Drop");

        ClosePanel();
    }

    // Gắn hàm này vào Nút "NO" (Hủy bỏ) trên Panel
    public void CancelTrash()
    {
        ClosePanel();
    }

    private void ClosePanel()
    {
        itemToTrash = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }
}