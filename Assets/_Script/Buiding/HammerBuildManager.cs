using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HammerBuildManager : MonoBehaviour
{
    public static HammerBuildManager Instance;

    [Header("Danh sách đồ có thể chế tạo bằng Búa")]
    public List<BuildingBlueprint> smallPropBlueprints;

    [Header("Cài đặt Bóng mờ (Hologram)")]
    public Material validMaterial;
    public Material invalidMaterial;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    // [THÊM MỚI]: Lưới để khóa vị trí (Đồng bộ với FarmingZone)
    [Header("Grid Settings")]
    public float gridSize = 1.2f;
    public float maxBuildRange = 6f;

    private PlayerInputHandler inputHandler;
    private bool isPlacing = false;
    private BuildingBlueprint blueprintToPlace;
    private GameObject currentHologram;
    private MeshRenderer[] hologramRenderers;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen())
        {
            // Tránh việc đang bật UI Hammer mà nó bị chặn cất búa
            if (HammerUIManager.Instance != null && HammerUIManager.Instance.IsOpen()) { /* Bỏ qua */ }
            else return;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
        {
            if (isPlacing) CancelPlacement();
            return;
        }

        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

        if (slot.item is ToolItemData tool && tool.toolType == ToolType.Hammer)
        {
            if (isPlacing && currentHologram != null)
            {
                HandleHologramPlacement();
            }
            else
            {
                if (inputHandler.BuildMenuTriggered)
                {
                    if (HammerUIManager.Instance != null && !HammerUIManager.Instance.IsOpen())
                    {
                        // Đang cầm bóng mờ mà bấm B -> Cất bóng mờ đi, mở bảng chọn đồ mới!
                        if (isPlacing) CancelPlacement();

                        HammerUIManager.Instance.OpenUI(smallPropBlueprints);
                    }
                }
                else if (isPlacing && currentHologram != null)
                {
                    HandleHologramPlacement();
                }
            }
        }
        else
        {
            if (isPlacing) CancelPlacement();
        }
    }

    public void StartPlacing(BuildingBlueprint blueprint)
    {
        if (blueprint.prefabToBuild == null) return;

        blueprintToPlace = blueprint;
        isPlacing = true;

        currentHologram = Instantiate(blueprint.prefabToBuild);

        Collider[] colliders = currentHologram.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        hologramRenderers = currentHologram.GetComponentsInChildren<MeshRenderer>();
    }

    private void HandleHologramPlacement()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            // [ĐÃ SỬA]: Học cách tạo lưới của FarmingZone
            Vector3 rawPos = hit.point;

            // Ép tọa độ về các mốc chia hết cho gridSize (vd: 1.2, 2.4, 3.6...)
            float snappedX = Mathf.Floor(rawPos.x / gridSize) * gridSize + (gridSize / 2f);
            float snappedZ = Mathf.Floor(rawPos.z / gridSize) * gridSize + (gridSize / 2f);

            // Gán vị trí đã khóa lưới (Snap to grid)
            Vector3 snappedPos = new Vector3(snappedX, rawPos.y, snappedZ);
            currentHologram.transform.position = snappedPos;

            // Xoay 90 độ (Xây nhà thì nên xoay 90 độ thay vì 45 độ như trước cho dễ xếp gọn)
            if (Mouse.current.scroll.ReadValue().y > 0) currentHologram.transform.Rotate(0, 90f, 0);
            if (Mouse.current.scroll.ReadValue().y < 0) currentHologram.transform.Rotate(0, -90f, 0);

            Vector3 playerPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(snappedPos.x, 0, snappedPos.z);
            float distanceToTarget = Vector3.Distance(playerPosXZ, targetPosXZ);
            bool inRange = distanceToTarget <= maxBuildRange;
            bool isClear = CheckPlacementValid(currentHologram.transform.position, currentHologram.transform.rotation);
            bool isValid = inRange && isClear;
            UpdateHologramColor(isValid);

            if (inputHandler.ClickTriggered && isValid)
            {
                ConfirmPlacement(currentHologram.transform.position, currentHologram.transform.rotation);
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    private bool CheckPlacementValid(Vector3 pos, Quaternion rot)
    {
        // Thu nhỏ hộp va chạm lại một chút so với lưới để đặt 2 cái rương sát nhau không bị báo lỗi đỏ
        Vector3 boxSize = new Vector3(gridSize * 0.8f, gridSize * 0.8f, gridSize * 0.8f);
        return !Physics.CheckBox(pos + Vector3.up * (gridSize / 2f), boxSize / 2, rot, obstacleLayer);
    }

    private void UpdateHologramColor(bool isValid)
    {
        Material mat = isValid ? validMaterial : invalidMaterial;
        foreach (var renderer in hologramRenderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            renderer.materials = mats;
        }
    }

    private void ConfirmPlacement(Vector3 pos, Quaternion rot)
    {
        // 1. Trừ Đồ trong túi
        foreach (var req in blueprintToPlace.buildItemCosts)
        {
            InventoryManager.Instance.ConsumeItemsGlobal(req.item, req.amount);
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DeductEquippedToolDurability(1f); // Mỗi nhát đập tụt 1 máu
        }
        Vector3 lookPos = new Vector3(pos.x, transform.position.y, pos.z);
        transform.LookAt(lookPos);

        // Gọi Animator ra để vung búa
        PlayerInteraction playerInteract = GetComponent<PlayerInteraction>();
        if (playerInteract != null && playerInteract.playerAnimator != null)
        {
            // [QUAN TRỌNG]: Đổi "SwingHammer" thành đúng tên Trigger đập búa trong Animator của ông nhé!
            // Ví dụ: "UseTool", "Build", "Hit"...
            playerInteract.playerAnimator.SetTrigger("SwingHammer");
        }
        // =====================================

        // 2. Đẻ ra đồ thật
        Instantiate(blueprintToPlace.prefabToBuild, pos, rot);
        Debug.Log($"[Thành công] Đã xây {blueprintToPlace.buildingName} theo lưới chuẩn!");

        if (HasEnoughMaterials(blueprintToPlace))
        {
            // Vẫn còn đủ đồ -> Không làm gì cả, giữ nguyên trạng thái bóng mờ để người chơi click tiếp!
            Debug.Log($"[Xây Liên Hoàn] Vẫn đủ nguyên liệu, tiếp tục đặt {blueprintToPlace.buildingName}!");
        }
        else
        {
            // Hết đồ rồi -> Hủy bóng mờ, giấu chuột đi
            Debug.Log("[Hết Nguyên Liệu] Tự động cất bản vẽ!");
            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        isPlacing = false;
        blueprintToPlace = null;
        if (currentHologram != null) Destroy(currentHologram);

        // [THÊM MỚI]: Đặt xong đồ hoặc Hủy bỏ thì phải ép chuột biến mất và khóa lại
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private bool HasEnoughMaterials(BuildingBlueprint blueprint)
    {
        foreach (var req in blueprint.buildItemCosts)
        {
            // Nếu đếm trong Balo mà thiếu món nào đó thì báo False ngay
            if (InventoryManager.Instance.GetTotalItemCount(req.item) < req.amount)
            {
                return false;
            }
        }
        return true; // Đủ tất cả các món
    }
    public bool IsCurrentlyPlacing()
    {
        return isPlacing;
    }
}