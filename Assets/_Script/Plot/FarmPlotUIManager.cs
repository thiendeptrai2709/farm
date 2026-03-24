using UnityEngine;
using UnityEngine.UI;
using System;


public class FarmPlotUIManager : MonoBehaviour
{
    public static FarmPlotUIManager Instance;

    [Header("UI Panels & Elements")]
    public GameObject plotPanel;
    public Image seedIcon;       // Cục ảnh hạt giống sẽ hiện lên khi thả vào
    public Button plantButton;   // Nút bấm "Plant"
    public Button closeButton;   // Nút bấm "X" để tắt

    private FarmPlot currentOpenPlot;
    private SeedItemData selectedSeed;
    public event Action<bool> OnPlotUIToggled;
    // Lưu lại cái "gốc gác" của hạt giống để tý nữa trừ cho đúng ô
    private StorageType sourceStorage;
    private int sourceIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (plotPanel != null) plotPanel.SetActive(false);

        // Gắn sự kiện cho 2 cái nút bấm
        if (plantButton != null) plantButton.onClick.AddListener(ConfirmPlant);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePlotUI);

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnInventoryUIToggled += (isBaloOpen) =>
            {
                if (!isBaloOpen && IsOpen())
                {
                    ClosePlotUI();
                }
            };
        }
    }

    public void OpenPlotUI(FarmPlot plot)
    {
        currentOpenPlot = plot;
        ClearSelectedSeed();

        // Reset UI cho sạch sẽ trước khi mở
        if (seedIcon != null)
        {
            seedIcon.sprite = null;
            seedIcon.enabled = false;
        }
        if (plantButton != null) plantButton.interactable = false; // Khóa nút Trồng

        if (plotPanel != null) plotPanel.SetActive(true);
        if (InventoryUI.Instance != null) InventoryUI.Instance.TogglePanel(true);

        OnPlotUIToggled?.Invoke(true);
    }
    public void ClearSelectedSeed()
    {
        selectedSeed = null;
        // Reset lại vị trí tạm
        sourceStorage = default;
        sourceIndex = -1;

        // Xóa ảnh
        if (seedIcon != null)
        {
            seedIcon.sprite = null;
            seedIcon.enabled = false;
        }

        // Khóa nút Trồng
        if (plantButton != null) plantButton.interactable = false;
    }
    public void ClosePlotUI()
    {
        if (plotPanel == null || !plotPanel.activeSelf) return;

        currentOpenPlot = null;
        selectedSeed = null;

        if (plotPanel != null) plotPanel.SetActive(false);
        if (InventoryUI.Instance != null) InventoryUI.Instance.TogglePanel(false);

        OnPlotUIToggled?.Invoke(false);
    }

    // --- HÀM NÀY ĐƯỢC GỌI BỞI CÁI SEED_DROP_SLOT Ở BƯỚC 1 ---
    public void OnSeedDropped(SeedItemData seedData, StorageType storage, int index)
    {
        if (seedData.isBigTree)
        {
            Debug.LogWarning("Không thể thả mầm cây to vào luống đất 1x1!");
            return;
        }
        selectedSeed = seedData;
        sourceStorage = storage;
        sourceIndex = index;

        // Bật ảnh hạt giống lên cho người chơi nhìn thấy
        if (seedIcon != null)
        {
            seedIcon.sprite = seedData.icon;
            seedIcon.enabled = true;
        }

        // Mở khóa nút Bấm Trồng
        if (plantButton != null) plantButton.interactable = true;


    }

    private void ConfirmPlant()
    {
        if (currentOpenPlot != null && selectedSeed != null)
        {
            RemoveSeedFromInventory();
            currentOpenPlot.PlantSeedSuccess(selectedSeed);
            ClosePlotUI();
            
        }
    }

    private void RemoveSeedFromInventory()
    {
        // Xác định xem nên móc đồ từ Hotbar hay Inventory để trừ
        var list = sourceStorage == StorageType.Hotbar ? InventoryManager.Instance.hotbarSlots : InventoryManager.Instance.inventorySlots;

        list[sourceIndex].amount--;
        if (list[sourceIndex].amount <= 0)
        {
            list[sourceIndex].item = null; // Xóa sổ nếu dùng hết hạt
        }

        // Cập nhật lại hình ảnh Túi đồ
        InventoryManager.Instance.RefreshInventoryUI();
    }
    public bool IsOpen()
    {
        return plotPanel != null && plotPanel.activeSelf;
    }

    // Lấy tọa độ của cục đất đang mở để đo khoảng cách
    public Transform GetCurrentPlotTransform()
    {
        return currentOpenPlot != null ? currentOpenPlot.transform : null;
    }

    public Collider GetCurrentPlotCollider()
    {
        return currentOpenPlot != null ? currentOpenPlot.GetComponent<Collider>() : null;
    }

    // Nhận hạt giống bay thẳng vào khi người chơi bấm Shift + Click
    public void ReceiveSeedFromShiftClick(SeedItemData seedData, StorageType storage, int index)
    {
        if (seedData.isBigTree)
        {
            Debug.LogWarning("Không thể chuyển mầm cây to vào luống đất 1x1!");
            return;
        }

        selectedSeed = seedData;
        sourceStorage = storage;
        sourceIndex = index;

        if (seedIcon != null)
        {
            seedIcon.sprite = seedData.icon;
            seedIcon.enabled = true;
        }

        if (plantButton != null) plantButton.interactable = true;
    }
}