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
    private float customYRotation = 0f;

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
        customYRotation = 0f;

        currentHologram = Instantiate(blueprint.prefabToBuild);

        Collider[] colliders = currentHologram.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        hologramRenderers = currentHologram.GetComponentsInChildren<MeshRenderer>();
    }

    private void HandleHologramPlacement()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0) customYRotation += 15f;
        if (scroll < 0) customYRotation -= 15f;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 rawPos = hit.point;

            bool isSnapped;
            Vector3 snappedPos = ApplyMagneticSnap(rawPos, out isSnapped);

            currentHologram.transform.position = snappedPos;

            if (!isSnapped)
            {
                currentHologram.transform.rotation = Quaternion.Euler(0, customYRotation, 0);
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
                // [CHẶN THỂ LỰC TRƯỚC KHI XÂY]
                if (PlayerStamina.Instance != null && PlayerStamina.Instance.currentStamina < PlayerStamina.Instance.axeCost)
                {
                    // Chức năng: Đẩy việc thông báo sang cho thằng UI Manager lo
                    if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ShowNotEnoughWarning();
                }
                else
                {
                    // Nếu đủ sức, trừ thể lực và bắt đầu đặt đồ
                    if (PlayerStamina.Instance != null)
                        PlayerStamina.Instance.ConsumeStamina(PlayerStamina.Instance.axeCost);

                    ConfirmPlacement(currentHologram.transform.position, currentHologram.transform.rotation);
                }
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    private Vector3 ApplyMagneticSnap(Vector3 rawPos, out bool isSnapped)
    {
        isSnapped = false;

        // Chức năng: Kiểm tra ưu tiên hít bằng hệ thống Socket_A và Socket_B
        Transform holoSocketA = currentHologram.transform.Find("Socket_A");
        Transform holoSocketB = currentHologram.transform.Find("Socket_B");

        if (holoSocketA != null && holoSocketB != null)
        {
            Collider[] nearbyCols = Physics.OverlapSphere(rawPos, magneticSnapRange, obstacleLayer);
            Transform bestTargetSocket = null;
            float minSocketDist = float.MaxValue;

            foreach (var col in nearbyCols)
            {
                if (col.gameObject == currentHologram) continue;

                Transform sA = col.transform.Find("Socket_A");
                Transform sB = col.transform.Find("Socket_B");

                if (sA == null && col.transform.parent != null)
                {
                    sA = col.transform.parent.Find("Socket_A");
                    sB = col.transform.parent.Find("Socket_B");
                }

                if (sA != null)
                {
                    float dA = Vector3.Distance(rawPos, sA.position);
                    if (dA < minSocketDist) { minSocketDist = dA; bestTargetSocket = sA; }
                }
                if (sB != null)
                {
                    float dB = Vector3.Distance(rawPos, sB.position);
                    if (dB < minSocketDist) { minSocketDist = dB; bestTargetSocket = sB; }
                }
            }

            if (bestTargetSocket != null)
            {
                // Chức năng: Ghép nối ngược đầu Socket
                Transform holoMatchSocket = (bestTargetSocket.name == "Socket_A") ? holoSocketB : holoSocketA;

                currentHologram.transform.rotation = bestTargetSocket.parent.rotation * Quaternion.Euler(0, customYRotation, 0);

                Vector3 worldOffset = currentHologram.transform.rotation * holoMatchSocket.localPosition;
                Vector3 finalPos = bestTargetSocket.position - worldOffset;

                finalPos.y = rawPos.y;
                isSnapped = true;
                return finalPos;
            }
        }

        // Chức năng: Dự phòng hít bằng BoxCollider cũ nếu vật thể không có Socket
        BoxCollider holoBox = currentHologram.GetComponentInChildren<BoxCollider>();
        if (holoBox == null) return rawPos;

        Collider[] nearbyObstacles = Physics.OverlapSphere(rawPos, magneticSnapRange, obstacleLayer);
        BoxCollider closestObj = null;
        float minDistance = float.MaxValue;

        foreach (var col in nearbyObstacles)
        {
            if (currentHologram != null && col.gameObject == currentHologram) continue;

            BoxCollider box = col as BoxCollider;
            if (box == null) continue;

            float dist = Vector3.Distance(rawPos, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestObj = box;
            }
        }

        if (closestObj != null)
        {
            Vector3 anchorSize = Vector3.Scale(closestObj.size, closestObj.transform.lossyScale);
            Vector3 holoSize = Vector3.Scale(holoBox.size, currentHologram.transform.lossyScale);

            Vector3 anchorPos = closestObj.transform.position;
            Vector3 dir = rawPos - anchorPos;

            float dotX = Vector3.Dot(dir, closestObj.transform.right);
            float dotZ = Vector3.Dot(dir, closestObj.transform.forward);

            Vector3 snappedPos = anchorPos;

            if (Mathf.Abs(dotX) > Mathf.Abs(dotZ))
            {
                float distanceToKhit = (anchorSize.x / 2f) + (holoSize.x / 2f);
                snappedPos += closestObj.transform.right * Mathf.Sign(dotX) * distanceToKhit;
            }
            else
            {
                float distanceToKhit = (anchorSize.z / 2f) + (holoSize.z / 2f);
                snappedPos += closestObj.transform.forward * Mathf.Sign(dotZ) * distanceToKhit;
            }

            currentHologram.transform.rotation = closestObj.transform.rotation * Quaternion.Euler(0, customYRotation, 0);

            snappedPos.y = rawPos.y;
            isSnapped = true;
            return snappedPos;
        }

        return rawPos;
    }
    private bool CheckPlacementValid(GameObject hologram)
    {
        BoxCollider box = hologram.GetComponentInChildren<BoxCollider>();
        if (box == null) return true;

        Vector3 actualSize = Vector3.Scale(box.size, hologram.transform.lossyScale);
        Vector3 collisionBoxSize = actualSize * 0.9f;
        Vector3 center = hologram.transform.TransformPoint(box.center);

        if (Physics.CheckBox(center, collisionBoxSize / 2f, hologram.transform.rotation, obstacleLayer))
            return false;

        // Chức năng: Ngăn cấm đặt công trình đè lên các cành cây, tảng đá nhặt được
        Collider[] hits = Physics.OverlapBox(center, collisionBoxSize / 2f, hologram.transform.rotation);
        foreach (var hit in hits)
        {
            if (hit.gameObject == hologram) continue;
            if (hit.GetComponentInParent<PickupItem>() != null) return false;
        }

        // Chức năng: Cấm tuyệt đối xây dựng lọt vào vùng Quy Hoạch Kịch Kim của vườn (kể cả phần đất chưa nâng cấp)
        if (FarmingZone.Instance != null && FarmingZone.Instance.maxFarmBoundary != null)
        {
            Bounds maxGardenBounds = FarmingZone.Instance.maxFarmBoundary.bounds;

            Vector3 safeGardenSize = new Vector3(
                Mathf.Max(0f, maxGardenBounds.size.x - 0.1f),
                1000f,
                Mathf.Max(0f, maxGardenBounds.size.z - 0.1f)
            );

            Bounds forbiddenZoningBox = new Bounds(
                new Vector3(maxGardenBounds.center.x, 0f, maxGardenBounds.center.z),
                safeGardenSize
            );

            if (forbiddenZoningBox.Intersects(box.bounds))
            {
                return false;
            }
        }

        return true;
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
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportAction("Build_" + blueprintToPlace.prefabToBuild.name, 1);
        }
        customYRotation = 0f;
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