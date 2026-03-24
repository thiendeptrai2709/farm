using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Vị trí cầm đồ")]
    public Transform rightHandSocket;

    private GameObject currentEquippedModel;
    public ToolItemData currentToolData { get; private set; }

    private Animator anim;

    private void Start()
    {
        // ĐÃ SỬA: Dùng GetComponentInChildren để quét cả trong file 3D con
        anim = GetComponentInChildren<Animator>();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += SyncHotbarEquipment;
            SyncHotbarEquipment();
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= SyncHotbarEquipment;
        }
    }

    private void SyncHotbarEquipment()
    {
        int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;

        if (selectedIndex == -1)
        {
            EquipTool(null);
            return;
        }

        InventorySlot selectedSlot = InventoryManager.Instance.hotbarSlots[selectedIndex];

        if (selectedSlot.item != null && selectedSlot.item is ToolItemData toolData)
        {
            EquipTool(toolData);
        }
        else
        {
            EquipTool(null);
        }
    }

    public void EquipTool(ToolItemData newToolData)
    {
        if (currentToolData == newToolData && currentEquippedModel != null) return;

        if (currentEquippedModel != null)
        {
            Destroy(currentEquippedModel);
        }

        currentToolData = newToolData;

        // Dò tìm chính xác ID của Layer dựa theo tên ông đặt trong Animator
        // Thay chữ "TayCachLayer" bằng ĐÚNG CÁI TÊN LAYER ông đã tạo nhé
        int handLayerIndex = anim != null ? anim.GetLayerIndex("HandLayer") : -1;

        if (newToolData == null || newToolData.toolPrefab == null)
        {
            // TAY KHÔNG: Tắt Layer khóa tay
            if (handLayerIndex != -1) anim.SetLayerWeight(handLayerIndex, 0f);
            return;
        }

        currentEquippedModel = Instantiate(newToolData.toolPrefab, rightHandSocket.position, rightHandSocket.rotation);
        currentEquippedModel.transform.SetParent(rightHandSocket);
        currentEquippedModel.transform.localPosition = Vector3.zero;
        currentEquippedModel.transform.localRotation = Quaternion.identity;

        // CÓ CẦM ĐỒ: Bật Layer khóa tay
        if (handLayerIndex != -1) anim.SetLayerWeight(handLayerIndex, 1f);
    }
}