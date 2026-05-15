using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

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

    public float magneticSnapRange = 1.2f;

    // [THÊM MỚI]: Lưới để khóa vị trí (Đồng bộ với FarmingZone)
    [Header("Grid Settings")]
    public float gridSize = 1.2f;
    public float maxBuildRange = 6f;

    private PlayerInputHandler inputHandler;
    private bool isPlacing = false;
    private BuildingBlueprint blueprintToPlace;
    private GameObject currentHologram;
    private MeshRenderer[] hologramRenderers;

    public TextMeshProUGUI buildHintText; 
    public string farmSceneName = "Farm";

    [Header("Đa Ngôn Ngữ - Gợi ý xây dựng")]
    public LocalizedString textPlacingHint;
    public LocalizedString textOpenMenuHint;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();

        if (buildHintText != null)
        {
            buildHintText.text = "";
        }
    }

    private void Update()
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen())
        {
            if (HammerUIManager.Instance != null && HammerUIManager.Instance.IsOpen()) { /* Bỏ qua */ }
            else return;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
        {
            if (isPlacing) CancelPlacement();

            // Cất tay không -> Tắt chữ
            if (buildHintText != null) buildHintText.text = "";
            return;
        }

        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

        if (slot.item is ToolItemData tool && tool.toolType == ToolType.Hammer)
        {
            // ==========================================
            // [HỆ THỐNG GỢI Ý THÔNG MINH]
            // ==========================================
            if (buildHintText != null && SceneManager.GetActiveScene().name == farmSceneName)
            {
                if (buildHintText != null && SceneManager.GetActiveScene().name == farmSceneName)
                {
                    if (HammerUIManager.Instance != null && HammerUIManager.Instance.IsOpen())
                    {
                        buildHintText.text = ""; // Đang bật bảng chọn đồ thì giấu chữ đi cho đỡ vướng
                    }
                    else if (isPlacing)
                    {
                        // Đang cầm bóng mờ
                        buildHintText.text = textPlacingHint.IsEmpty ? "[B] Đổi mẫu   |   [Chuột phải/ESC] Hủy" : textPlacingHint.GetLocalizedString();
                    }
                    else
                    {
                        // Vừa rút búa ra chưa làm gì
                        buildHintText.text = textOpenMenuHint.IsEmpty ? "[B] Mở Menu xây dựng" : textOpenMenuHint.GetLocalizedString();
                    }
                }
            }
                // ==========================================

                if (inputHandler.BuildMenuTriggered)
            {
                if (HammerUIManager.Instance != null && !HammerUIManager.Instance.IsOpen())
                {
                    if (isPlacing) CancelPlacement();
                    HammerUIManager.Instance.OpenUI(smallPropBlueprints);
                }
                else if (HammerUIManager.Instance != null)
                {
                    HammerUIManager.Instance.CloseUI();
                }
            }
            else if (isPlacing && currentHologram != null)
            {
                HandleHologramPlacement();
            }
        }
        else
        {
            if (isPlacing) CancelPlacement();

            // Cất búa cầm Cuốc/Rìu -> Tắt chữ
            if (buildHintText != null) buildHintText.text = "";
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
            Vector3 rawPos = hit.point;

            // Đưa vị trí chuột qua bộ lọc Nam Châm. Biến isSnapped sẽ báo true nếu bị hút vào đồ khác.
            bool isSnapped;
            Vector3 snappedPos = ApplyMagneticSnap(rawPos, out isSnapped);

            currentHologram.transform.position = snappedPos;

            // Nếu KHÔNG bị hút nam châm thì mới cho phép lăn chuột xoay góc tự do 15 độ
            // Nếu BỊ HÚT thì nó phải nằm thẳng hàng với món đồ cũ, cấm xoay!
            if (!isSnapped)
            {
                if (Mouse.current.scroll.ReadValue().y > 0) currentHologram.transform.Rotate(0, 15f, 0);
                if (Mouse.current.scroll.ReadValue().y < 0) currentHologram.transform.Rotate(0, -15f, 0);
            }

            Vector3 playerPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(snappedPos.x, 0, snappedPos.z);

            float distanceToTarget = Vector3.Distance(playerPosXZ, targetPosXZ);
            bool inRange = distanceToTarget <= maxBuildRange;

            // Gọi hàm check va chạm bản Động (Dynamic)
            bool isClear = CheckPlacementValid(currentHologram);
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

    // ========================================================
    // CƠ CHẾ NAM CHÂM THÔNG MINH (TỰ ĐO KÍCH THƯỚC)
    // ========================================================
    private Vector3 ApplyMagneticSnap(Vector3 rawPos, out bool isSnapped)
    {
        isSnapped = false;

        // Bắt buộc đồ vật của bạn phải gắn BoxCollider để tính năng này đo được kích thước!
        BoxCollider holoBox = currentHologram.GetComponentInChildren<BoxCollider>();
        if (holoBox == null) return rawPos;

        // Quét tìm đồ vật xung quanh trong bán kính 3 mét
        Collider[] nearbyObstacles = Physics.OverlapSphere(rawPos, magneticSnapRange, obstacleLayer);
        BoxCollider closestObj = null;
        float minDistance = float.MaxValue;

        foreach (var col in nearbyObstacles)
        {
            if (currentHologram != null && col.gameObject == currentHologram) continue;

            BoxCollider box = col as BoxCollider;
            if (box == null) continue; // Chỉ hút những đồ có BoxCollider

            float dist = Vector3.Distance(rawPos, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestObj = box;
            }
        }

        // Đã tìm thấy một món đồ cũ ở gần
        if (closestObj != null)
        {
            // Lấy KÍCH THƯỚC THẬT (chiều dài, rộng) của cả 2 món đồ
            Vector3 anchorSize = Vector3.Scale(closestObj.size, closestObj.transform.lossyScale);
            Vector3 holoSize = Vector3.Scale(holoBox.size, currentHologram.transform.lossyScale);

            Vector3 anchorPos = closestObj.transform.position;
            Vector3 dir = rawPos - anchorPos;

            // Xác định xem chuột đang nằm ở mặt Cạnh Hông (Trục X) hay mặt Trước Sau (Trục Z) của món đồ cũ
            float dotX = Vector3.Dot(dir, closestObj.transform.right);
            float dotZ = Vector3.Dot(dir, closestObj.transform.forward);

            Vector3 snappedPos = anchorPos;

            if (Mathf.Abs(dotX) > Mathf.Abs(dotZ))
            {
                // Nối vào Cạnh Hông -> Khoảng cách Khít = Nửa thân cục cũ + Nửa thân cục đang cầm
                float distanceToKhit = (anchorSize.x / 2f) + (holoSize.x / 2f);
                snappedPos += closestObj.transform.right * Mathf.Sign(dotX) * distanceToKhit;
            }
            else
            {
                // Nối vào Trước/Sau
                float distanceToKhit = (anchorSize.z / 2f) + (holoSize.z / 2f);
                snappedPos += closestObj.transform.forward * Mathf.Sign(dotZ) * distanceToKhit;
            }

            // Ép món đồ đang cầm xoay theo y hệt góc của đồ cũ để nối với nhau tạo thành đường thẳng
            currentHologram.transform.rotation = closestObj.transform.rotation;

            snappedPos.y = rawPos.y; // Chốt lại độ cao mặt đất
            isSnapped = true;
            return snappedPos;
        }

        return rawPos;
    }

    // ========================================================
    // KIỂM TRA VA CHẠM THÔNG MINH (TỰ ĐO THEO SIZE MÓN ĐỒ)
    // ========================================================
    private bool CheckPlacementValid(GameObject hologram)
    {
        BoxCollider box = hologram.GetComponentInChildren<BoxCollider>();
        if (box == null) return true; // Nếu lỡ quên gắn Collider thì cho đặt tự do

        // Tính kích thước thật
        Vector3 actualSize = Vector3.Scale(box.size, hologram.transform.lossyScale);

        // Thu nhỏ cái Hộp va chạm ảo xuống còn 90%. 
        // Bắt buộc phải có dòng này để khi 2 món đồ ghép khít vào nhau 100%, cái hộp va chạm không vô tình liếm sang đồ bên cạnh và báo lỗi Đỏ!
        Vector3 collisionBoxSize = actualSize * 0.9f;

        // Lấy đúng tâm của BoxCollider (phòng trường hợp cái tâm lệch so với gốc tọa độ)
        Vector3 center = hologram.transform.TransformPoint(box.center);

        // Trả về True nếu xung quanh không vướng thứ gì
        return !Physics.CheckBox(center, collisionBoxSize / 2f, hologram.transform.rotation, obstacleLayer);
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Build_Success");
        }
        // =====================================

        // 2. Đẻ ra đồ thật
        Chest isBuildingChest = blueprintToPlace.prefabToBuild.GetComponent<Chest>();

        if (isBuildingChest != null && ChestManager.Instance != null)
        {
            // Nếu là Rương -> Gọi Manager chuyên trách để đẻ rương và làm thẻ Căn cước
            ChestManager.Instance.BuildNewChest(pos, rot, blueprintToPlace.prefabToBuild);
            Debug.Log($"[Thành công] Đã đóng một cái Rương tự chế!");
        }
        else
        {
            GameObject newProp = Instantiate(blueprintToPlace.prefabToBuild, pos, rot);

            // [MỚI]: Báo cho Quản lý biết là tao vừa xây thêm 1 món, ghi sổ đi!
            if (PlacedPropManager.Instance != null)
            {
                // Lấy tên Prefab gốc làm ID nhận diện
                PlacedPropManager.Instance.RegisterProp(newProp, blueprintToPlace.prefabToBuild.name);
            }
        }

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

    public void CancelPlacement()
    {
        isPlacing = false;
        blueprintToPlace = null;
        if (currentHologram != null) Destroy(currentHologram);

        // [THÊM MỚI]: Đặt xong đồ hoặc Hủy bỏ thì phải ép chuột biến mất và khóa lại
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (buildHintText != null)
        {
            buildHintText.text = "";
        }
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