using UnityEngine;

// Kế thừa IInteractable để dùng chung hệ thống tương tác bằng phím E
public class ConstructionSite : MonoBehaviour, IInteractable
{
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
        UpdateVisuals();
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

    public void FinishBuilding()
    {
        // UI Nộp đồ báo là đã đủ đồ, khánh thành đi!
        currentState = SiteState.Completed;
        UpdateVisuals();
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

    // ==========================================
    // HỆ THỐNG TƯƠNG TÁC PHÍM E (CỦA ÔNG)
    // ==========================================

    public string GetInteractText()
    {
        // Chỉ hiện chữ [E] Thi Công khi nó đang là Hàng Rào
        return (currentState == SiteState.Pending) ? $"[E] Thi Công {myBlueprint.buildingName}" : "";
    }

    public void Interact()
    {
        // 1. Lấy vị trí ô Hotbar đang chọn
        int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;

        // 2. Kiểm tra xem tay có đang cầm đồ không
        if (selectedIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[selectedIndex].item;

            // 3. Kiểm tra xem món đó có phải là Tool và có phải là Búa (Hammer) không
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hammer)
            {
                // [ĐÚNG LÀ BÚA] -> Cho phép mở bảng UI nộp vật liệu!
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

}