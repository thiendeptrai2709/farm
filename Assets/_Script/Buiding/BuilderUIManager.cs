using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuilderUIManager : MonoBehaviour
{
    public static BuilderUIManager Instance;

    [Header("Main UI Panels")]
    public GameObject builderPanel;

    [Header("Left Panel: Blueprint List")]
    public Transform blueprintListContainer;
    public GameObject blueprintSlotPrefab;

    [Header("Right Panel: Details")]
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;

    [Header("Right Panel: Requirements")]
    public Transform requirementContainer;
    public GameObject requirementSlotPrefab;
    public Button unlockButton;

    [Header("Quản lý Tiền Bạc (MỚI)")]
    public TextMeshProUGUI playerMoneyText; // Kéo chữ hiển thị số tiền vào đây
    public ItemData coinItem;

    // Bộ nhớ nội bộ
    private BuildingBlueprint currentSelectedBlueprint;
    private List<GameObject> spawnedRequirementSlots = new List<GameObject>();
    private BlueprintSlotUI currentSelectedSlotUI;
    private bool canAffordCurrentBlueprint = false;
    private List<BuildingBlueprint> currentAvailableBlueprints;
    public event Action<bool> OnBuilderUIToggled;
    public static event Action<BuildingBlueprint> OnBlueprintUnlocked;

    private Transform currentTableTransform;
    private Collider currentTableCollider;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (builderPanel != null) builderPanel.SetActive(false);
    }

    // Gọi hàm này khi tương tác với cái Bàn/Nhà Chính, truyền vào list các bản vẽ có thể mua
    public void OpenUI(List<BuildingBlueprint> availableBlueprints, Transform tableTransform)
    {
        currentAvailableBlueprints = availableBlueprints;
        currentTableTransform = tableTransform;
        currentTableCollider = tableTransform.GetComponent<Collider>();
        builderPanel.SetActive(true);

        OnBuilderUIToggled?.Invoke(true);

        RefreshMoneyUI();

        PopulateBlueprintList(availableBlueprints);

    }
    public Collider GetCurrentTableCollider()
    {
        return currentTableCollider;
    }
    public void CloseUI()
    {
        builderPanel.SetActive(false);
        OnBuilderUIToggled?.Invoke(false);
    }
    public bool IsOpen()
    {
        return builderPanel != null && builderPanel.activeSelf;
    }
    public Transform GetCurrentTableTransform()
    {
        return currentTableTransform;
    }
    public void RefreshMoneyUI()
    {
        if (playerMoneyText != null && coinItem != null)
        {
            // Lục tung cả Balo lẫn Rương để đếm tổng Đồng Vàng
            int currentMoney = InventoryManager.Instance.GetTotalItemCount(coinItem);
            playerMoneyText.text = currentMoney.ToString();
        }
    }
    private void PopulateBlueprintList(List<BuildingBlueprint> blueprints)
    {
        // Dọn sạch danh sách cũ
        foreach (Transform child in blueprintListContainer)
        {
            Destroy(child.gameObject);
        }

        // Đẻ ra danh sách mới
        foreach (var bp in blueprints)
        {
            GameObject newSlot = Instantiate(blueprintSlotPrefab, blueprintListContainer);
            newSlot.GetComponent<BlueprintSlotUI>().Setup(bp);
        }
        if (blueprints.Count > 0 && blueprintListContainer.childCount > 0)
        {
            BlueprintSlotUI firstSlotUI = blueprintListContainer.GetChild(0).GetComponent<BlueprintSlotUI>();
            SelectBlueprint(blueprints[0], firstSlotUI);
        }
    }

    public void SelectBlueprint(BuildingBlueprint blueprint, BlueprintSlotUI slotUI = null)
    {
        currentSelectedBlueprint = blueprint;

        if (currentSelectedSlotUI != null)
        {
            currentSelectedSlotUI.SetHighlight(false);
        }

        // Bật viền cái mới
        currentSelectedSlotUI = slotUI;
        if (currentSelectedSlotUI != null)
        {
            currentSelectedSlotUI.SetHighlight(true);
        }

        detailNameText.text = blueprint.buildingName;
        detailDescText.text = blueprint.description;

        UpdatePriceDisplay();
        RefreshRequirements();
    }
    private void UpdatePriceDisplay()
    {
        if (currentSelectedBlueprint == null || detailDescText == null) return;

        int price = currentSelectedBlueprint.GetCurrentUnlockPrice();
        if (coinItem != null)
        {
            int currentMoney = InventoryManager.Instance.GetTotalItemCount(coinItem);

            // Hiện số tiền: Xanh nếu đủ, Đỏ nếu thiếu
            if (currentMoney >= price)
                detailDescText.text = $"Price: <color=#55FF55>{price}</color>";
            else
                detailDescText.text = $"Price: <color=#FF5555>{price}</color>";
        }
    }
    private void RefreshRequirements()
    {
        // Xóa các ô nguyên liệu cũ
        foreach (var slot in spawnedRequirementSlots) Destroy(slot);
        spawnedRequirementSlots.Clear();

        canAffordCurrentBlueprint = true;
        if (coinItem != null)
        {
            int currentMoney = InventoryManager.Instance.GetTotalItemCount(coinItem);
            if (currentMoney < currentSelectedBlueprint.GetCurrentUnlockPrice()) canAffordCurrentBlueprint = false;
        }
        // Quét từng món nguyên liệu yêu cầu
        foreach (var req in currentSelectedBlueprint.unlockItemCosts)
        {
            GameObject reqObj = Instantiate(requirementSlotPrefab, requirementContainer);
            spawnedRequirementSlots.Add(reqObj);

            // GỌI TỔNG CỤC HẬU CẦN QUÉT XUYÊN RƯƠNG!
            int currentAmountHas = InventoryManager.Instance.GetTotalItemCount(req.item);
            int requiredAmount = currentSelectedBlueprint.GetCurrentItemAmount(req.amount);

            reqObj.GetComponent<RequirementSlotUI>().Setup(req.item, currentAmountHas, requiredAmount);
            // Nếu chỉ cần 1 món bị thiếu -> Khóa nút bấm lại
            if (currentAmountHas < requiredAmount)
            {
                canAffordCurrentBlueprint = false;
            }
        }

        // Bật/tắt nút bấm dựa trên việc có đủ tiền/đồ không
        unlockButton.interactable = canAffordCurrentBlueprint;
    }

    // Gắn hàm này vào cái nút [MỞ KHÓA]
    public void OnClick_ConfirmUnlock()
    {
        if (!canAffordCurrentBlueprint || currentSelectedBlueprint == null) return;

        int currentGoldCost = currentSelectedBlueprint.GetCurrentUnlockPrice();

        if (coinItem != null && currentGoldCost > 0)
        {
            InventoryManager.Instance.ConsumeItemsGlobal(coinItem, currentGoldCost);
        }

        // [ÔNG ĐANG THIẾU ĐOẠN NÀY]: 2. TRỪ NGUYÊN LIỆU (Gỗ, Đá...)
        foreach (var req in currentSelectedBlueprint.unlockItemCosts)
        {
            int currentRequiredAmount = currentSelectedBlueprint.GetCurrentItemAmount(req.amount);
            InventoryManager.Instance.ConsumeItemsGlobal(req.item, currentRequiredAmount);
        }
        Debug.Log($"[Builder System] Trừ đồ thành công! Đã mở khóa: {currentSelectedBlueprint.buildingName}");

        if (currentSelectedBlueprint.blueprintType == BlueprintType.FarmExpansion)
        {
            // 1. Nếu là nâng cấp vườn -> Nới rộng vườn ngay lập tức!
            if (FarmingZone.Instance != null)
            {
                FarmingZone.Instance.ExpandFarmBoundary(currentSelectedBlueprint.expandSize, currentSelectedBlueprint.centerOffset);
            }

            // 2. Tăng cấp độ mở rộng
            currentSelectedBlueprint.currentLevel++;

            // 3. Kiểm tra xem đã đạt max cấp chưa
            if (currentSelectedBlueprint.currentLevel >= currentSelectedBlueprint.maxLevel)
            {
                // Nếu max rồi mới xóa khỏi UI
                RemoveBlueprintFromUI();
            }
            else
            {
                // Nếu chưa max -> Làm mới lại giao diện để hiển thị cấp tiếp theo (nút vẫn giữ nguyên)
                detailDescText.text = currentSelectedBlueprint.description + $" (Cấp {currentSelectedBlueprint.currentLevel}/{currentSelectedBlueprint.maxLevel})";
                SelectBlueprint(currentSelectedBlueprint, currentSelectedSlotUI);
            }
        }
        else
        {
            // Nếu là xây nhà (Building) -> Phát sự kiện và XÓA LUÔN KHỎI UI NHƯ CŨ
            OnBlueprintUnlocked?.Invoke(currentSelectedBlueprint);
            RemoveBlueprintFromUI();
        }

        RefreshMoneyUI();

        CloseUI();
    }
    private void RemoveBlueprintFromUI()
    {
        if (currentAvailableBlueprints != null)
        {
            currentAvailableBlueprints.Remove(currentSelectedBlueprint);
        }

        if (currentSelectedSlotUI != null)
        {
            Destroy(currentSelectedSlotUI.gameObject);
            currentSelectedSlotUI = null;
        }
    }
}