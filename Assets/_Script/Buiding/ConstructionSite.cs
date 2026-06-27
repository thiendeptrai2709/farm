using UnityEngine;
using UnityEngine.Localization;
// Kế thừa IInteractable để dùng chung hệ thống tương tác bằng phím E
public class ConstructionSite : MonoBehaviour, IInteractable
{
    public string siteID;

    
    public LocalizedString locInteractText;
    public LocalizedString locNeedHammer;

    [Header("Bản vẽ của khu đất này")]
    public BuildingBlueprint myBlueprint;

    [Header("Các Trạng Thái Hình Ảnh (GameObject)")]
    public GameObject hiddenStateGraphic;    // (Tùy chọn) Bãi cỏ lúc chưa unlock
    public GameObject pendingStateGraphic;   // Giàn giáo/Hàng rào đang thi công
    public GameObject completedStateGraphic; // Mô hình Chuồng gà hoàn tất

    public enum SiteState { Hidden, Pending, Completed }
    [HideInInspector] public SiteState currentState = SiteState.Hidden;

    private void Start()
    {
        // Lắng nghe xem Nhà chính đã unlock chưa
        BuilderUIManager.OnBlueprintUnlocked += HandleBlueprintUnlocked;
        LoadSiteData();
    }

    private void OnDestroy()
    {
        BuilderUIManager.OnBlueprintUnlocked -= HandleBlueprintUnlocked;
    }

    private void HandleBlueprintUnlocked(BuildingBlueprint unlockedBlueprint)
    {
        // Nếu Nhà chính vừa unlock đúng cái nhà của mình -> Chuyển sang Hàng Rào!
        if (unlockedBlueprint == myBlueprint && currentState == SiteState.Hidden)
        {
            currentState = SiteState.Pending;
            UpdateVisuals();
        }
    }
    private void LoadSiteData()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            GameData data = SaveManager.Instance.GetCurrentData();

            // Nếu nhà này đã từng được Unlock trong sổ cái thì chuyển ngay sang Pending
            if (myBlueprint != null && data.unlockedBlueprintIDs.Contains(myBlueprint.name))
            {
                if (currentState == SiteState.Hidden) currentState = SiteState.Pending;
            }

            // Ghi đè trạng thái nếu bãi đất này đã từng được xử lý
            SavedConstructionSite savedData = data.savedConstructionSites.Find(s => s.siteID == siteID);
            if (savedData != null)
            {
                currentState = (SiteState)savedData.state;
            }
        }
        UpdateVisuals();
    }
    public void FinishBuilding()
    {
        // UI Nộp đồ báo là đã đủ đồ, khánh thành đi!
        currentState = SiteState.Completed;
        UpdateVisuals();
        if (QuestManager.Instance != null && myBlueprint != null)
        {
            // Báo cáo chung chung là vừa xây xong một cái gì đó
            QuestManager.Instance.ReportAction("Build_Any", 1);

            // Báo cáo đích danh tên của cái nhà vừa xây (Dùng tên file Blueprint làm mã)
            // VD: Xây giếng nước -> Báo cáo "Build_Blueprint_Well"
            QuestManager.Instance.ReportAction("Build_" + myBlueprint.name, 1);
        }
    }

    private void UpdateVisuals()
    {
        // Bật/tắt các mô hình 3D tương ứng
        if (hiddenStateGraphic != null) hiddenStateGraphic.SetActive(currentState == SiteState.Hidden);
        if (pendingStateGraphic != null) pendingStateGraphic.SetActive(currentState == SiteState.Pending);
        if (completedStateGraphic != null) completedStateGraphic.SetActive(currentState == SiteState.Completed);

        // Nếu xây xong, tắt Collider và Script đi cho nhẹ
        if (currentState == SiteState.Completed)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            this.enabled = false;
        }
    }
    public string GetInteractText()
    {
        if (currentState == SiteState.Pending)
        {
            if (PlayerStamina.Instance != null && PlayerStamina.Instance.currentStamina < PlayerStamina.Instance.axeCost)
            {
                return ""; // Đuối sức thì giấu chữ luôn
            }

            // Kiểm tra xem trên tay có đang cầm Búa không
            bool isHoldingHammer = false;
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hammer)
                {
                    isHoldingHammer = true;
                }
            }

            // Xử lý hiển thị chữ dựa vào việc có búa hay không
            if (isHoldingHammer)
            {
                string prefix = locInteractText.IsEmpty ? "[E] Thi Công" : locInteractText.GetLocalizedString();
                return $"{prefix} {myBlueprint.buildingName}";
            }
            else
            {
                return locNeedHammer.IsEmpty ? "Cần cầm Búa (Hammer)" : locNeedHammer.GetLocalizedString();
            }
        }
        return "";
    }

    public void Interact()
    {
        int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;

        if (selectedIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[selectedIndex].item;

            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hammer)
            {
                // [CHẶN THỂ LỰC]: Kiểm tra máu TRƯỚC KHI bung bảng UI
                if (PlayerStamina.Instance != null && PlayerStamina.Instance.currentStamina < PlayerStamina.Instance.axeCost)
                {
                    if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ShowNotEnoughWarning();
                    return; // Đuối sức -> cấm mở bảng
                }

                // Đủ sức, đúng búa -> Mở bảng UI
                SiteConstructionUIManager.Instance.OpenUI(this);
            }
            else
            {
                Debug.Log("Bạn cần cầm Búa (Hammer) trên tay mới có thể thi công!");
            }
        }
        else
        {
            Debug.Log("Bạn đang tay không! Hãy cầm Búa lên!");
        }
    }
    [ContextMenu("Tự động tạo ID cho Bãi Đất")]
    private void AutoGenerateID()
    {
        siteID = "Site_" + System.Guid.NewGuid().ToString().Substring(0, 8);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}