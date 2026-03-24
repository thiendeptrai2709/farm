using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

// [FILE MỚI]: Bảng UI 2 (Nộp Nguyên Liệu) trồi lên ngay tại Bãi đất
public class SiteConstructionUIManager : MonoBehaviour
{
    public static SiteConstructionUIManager Instance;

    [Header("Main UI Panel (UI 2)")]
    public GameObject depositPanel; // Một cái bảng UI nhỏ nhỏ ở góc màn hình

    [Header("Details")]
    public TextMeshProUGUI buildingNameText;

    [Header("Build Action")]
    public Button buildButton;

    // Bộ nhớ nội bộ
    private ConstructionSite currentSite;
    // Danh sách các món đồ ĐÃ KÉO vào trong cái bảng UI 2 này
    
    public List<DepositSlotUI> depositSlots;

    public event Action<bool> OnSiteConstructionUIToggled; // Báo hiệu Camera mở chuột

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (depositPanel != null) depositPanel.SetActive(false);
    }
    public bool IsOpen()
    {
        return depositPanel != null && depositPanel.activeSelf;
    }

    public Transform GetCurrentSiteTransform()
    {
        return currentSite != null ? currentSite.transform : null;
    }

    public Collider GetCurrentSiteCollider()
    {
        return currentSite != null ? currentSite.GetComponent<Collider>() : null;
    }
    public void OpenUI(ConstructionSite site)
    {
        currentSite = site;
        depositPanel.SetActive(true);
        OnSiteConstructionUIToggled?.Invoke(true); // Nhả chuột (Camera Manager lắng nghe cái này)

        buildingNameText.text = $"Thi Công: {site.myBlueprint.buildingName}";

        foreach (var slot in depositSlots) slot.ClearSlot();

        // 2. Quét qua bản vẽ, cần món gì thì lôi từng đĩa ra giao nhiệm vụ
        for (int i = 0; i < site.myBlueprint.buildItemCosts.Count; i++)
        {
            // Chắc chắn là có đủ đĩa trống để giao việc
            if (i < depositSlots.Count)
            {
                var req = site.myBlueprint.buildItemCosts[i];
                // Ép đĩa phải hiển thị "0 / req.amount" và hình của req.item
                depositSlots[i].SetupRequirement(req.item, req.amount);
            }
        }

        if (InventoryUI.Instance != null) InventoryUI.Instance.ForceOpen();

        RefreshUI();
    }

    public void CloseUI()
    {
        depositPanel.SetActive(false);
        OnSiteConstructionUIToggled?.Invoke(false); // Khóa chuột

        // Trả lại đồ đã nộp cho Balo nếu người chơi tắt bảng mà chưa xây! (UX Siêu VIP)
        ReturnDepositedItemsToInventory();

        currentSite = null;
        if (InventoryUI.Instance != null) InventoryUI.Instance.ForceClose();
    }

    public void RefreshUI()
    {
        if (currentSite == null) return;

        // CHECK XEM ĐỦ ĐỒ ĐỂ XÂY CHƯA
        bool canBuild = true;
        foreach (var req in currentSite.myBlueprint.buildItemCosts)
        {
            int countInDeposit = CountItemInDepositList(req.item);
            if (countInDeposit < req.amount)
            {
                canBuild = false;
            }
        }

        // Sáng nút Xây Dựng nếu đủ đồ!
        if (buildButton != null) buildButton.interactable = canBuild;
    }

    // Gắn hàm này vào cái nút [MỞ KHÓA]
    public void OnClick_ConfirmBuild()
    {
        if (currentSite == null) return;

        foreach (var slot in depositSlots)
        {
            slot.ClearSlot();
        }

        Debug.Log($"[Xây Dựng] Đã trừ đủ vật liệu! Khánh thành {currentSite.myBlueprint.buildingName}!");
        PlayerInteraction player = FindAnyObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.PlayBuildAnimation(currentSite);
        }
        else
        {
            currentSite.FinishBuilding(); // Sơ cua lỡ không tìm thấy player
        }
        CloseUI();
    }

    private int CountItemInDepositList(ItemData targetItem)
    {
        int total = 0;
        foreach (var slot in depositSlots)
        {
            if (slot.currentItem == targetItem) total += slot.currentAmount;
        }
        return total;
    }

    // [ĐÃ HOÀN THIỆN]: Khi tắt bảng mà chưa xây, phải trả đồ lại cho người chơi!
    public void ReturnDepositedItemsToInventory()
    {
        foreach (var slot in depositSlots)
        {
            if (slot.currentItem != null)
            {
                slot.ReturnToInventory();
            }
        }
    }
    public bool TryReceiveItemFromShiftClick(ItemData inputItem, int inputAmount, out int leftover)
    {
        leftover = inputAmount;
        bool hasAddedAny = false;

        if (currentSite == null) return false;

        // Quét xem công trình này có yêu cầu món đồ người chơi vừa Shift-Click không
        for (int i = 0; i < currentSite.myBlueprint.buildItemCosts.Count; i++)
        {
            var req = currentSite.myBlueprint.buildItemCosts[i];

            // Nếu đúng món đang cần xây
            if (req.item == inputItem)
            {
                // Vì lúc OpenUI ông map index của Blueprint khớp với depositSlots
                DepositSlotUI targetSlot = depositSlots[i];

                // Tính toán xem còn thiếu bao nhiêu cái nữa
                int neededAmount = req.amount - targetSlot.currentAmount;

                if (neededAmount > 0)
                {
                    // Lấy số lượng vừa đủ để nhét vào
                    int amountToDeposit = Mathf.Min(leftover, neededAmount);

                    // Nhét vào đĩa
                    targetSlot.currentItem = inputItem;
                    targetSlot.currentAmount += amountToDeposit;

                    // [QUAN TRỌNG]: Update lại cái Text "Đã nộp / Yêu cầu" trên cái ô UI đó
                    // Ông gọi hàm cập nhật giao diện của file DepositSlotUI ở đây
                    targetSlot.UpdateVisuals();

                    // Trừ số lượng trên tay người chơi
                    leftover -= amountToDeposit;
                    hasAddedAny = true;

                    if (leftover <= 0) break; // Nhét hết đồ trên tay rồi thì dừng
                }
            }
        }

        if (hasAddedAny)
        {
            RefreshUI(); // Check lại xem đủ đồ chưa để còn thắp sáng nút [Xây Dựng]
            return true;
        }

        return false; // Trả về false nếu đồ ném sang không phải vật liệu cần xây
    }
}