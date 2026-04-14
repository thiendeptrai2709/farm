using System.Collections.Generic;
using UnityEngine;

public class FarmingZone : MonoBehaviour, IInteractable
{
    [Header("Ranh giới Nông Trại")]
    public BoxCollider farmBoundary;

    [SerializeField] private TerrainPainter terrainPainter;

    [Header("Grid Settings")]
    public float gridSize = 1.2f;
    public Vector3 gridOffset;

    [Header("Prefabs (Kéo 2 loại vào đây)")]
    public GameObject tilledDirtPrefab; // Ô đất 1x1
    public GameObject treePitPrefab;    // Hố cây 2x2 (Prefab chứa script TreePit)
    public GameObject highlightModel;

    [Header("Player (Tự động tìm, không cần kéo thả)")]
    [HideInInspector] public Transform player;
    [HideInInspector] public PlayerInteraction playerRadar;

    private Dictionary<Vector3Int, GameObject> activePlots = new Dictionary<Vector3Int, GameObject>();
    private BoxCollider cursorCollider;

    // Biến lưu trạng thái hiện tại
    private int currentGridSize = 1; // 1 = Cuốc đất, 2 = Trồng cây bự
    private bool isHoldingHoe = false;
    private SeedItemData holdingBigTreeSeed = null;

    [Header("Cài đặt Chống Spam Viền")]
    public float showDuration = 3f;     // Thời gian viền hiện lên (VD: 3 giây)
    public float cooldownTime = 5f;

    private float currentShowTimer = 0f;
    private float currentCooldownTimer = 0f;
    private bool wasHoldingToolLastFrame = false;

    public static FarmingZone Instance;


    [Header("Hiệu ứng viền ranh giới (LineRenderer)")]
    public LineRenderer boundaryLine;   // Đổi từ MeshRenderer sang LineRenderer
    public float pulseSpeed = 4f;
    public float maxAlpha = 0.8f;       // Viền thì có thể để đậm hơn thảm (0.8)

    private Material lineMaterial;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRadar = playerObj.GetComponent<PlayerInteraction>();
        }
        else
        {
            Debug.LogError("FarmingZone không tìm thấy Player! Hãy chắc chắn Player có tag là 'Player'.");
        }

        cursorCollider = GetComponent<BoxCollider>();
        if (boundaryLine != null)
        {
            lineMaterial = boundaryLine.material;
            boundaryLine.positionCount = 4;
            boundaryLine.loop = true;
            boundaryLine.gameObject.SetActive(false);

            UpdateBoundaryLine();
        }
        RefreshFarmTerrain();

        // -----------------------------------------------------
        // KẾT NỐI SỔ CÁI: Đọc dữ liệu và đẻ lại toàn bộ luống đất cũ
        // -----------------------------------------------------
        if (GameDataManager.Instance != null && tilledDirtPrefab != null)
        {
            // 1. Tính toán xem bạn đã đi vắng bao nhiêu giây thực tế (Kể cả tắt game)
            float offlineSeconds = 0f;
            if (GameDataManager.Instance.lastFarmExitTimeTicks != 0)
            {
                System.TimeSpan timeSpan = System.DateTime.Now - new System.DateTime(GameDataManager.Instance.lastFarmExitTimeTicks);
                offlineSeconds = (float)timeSpan.TotalSeconds;
                Debug.Log($"Bạn đã rời Farm {offlineSeconds} giây. Đang tua nhanh sự phát triển của cây...");
            }

            // 2. Đẻ lại luống đất và nhồi thời gian đi vắng vào cây
            foreach (var kvp in GameDataManager.Instance.farmPlotDataDict)
            {
                FarmPlotData data = kvp.Value;
                Vector3Int coords = GetGridCoords(data.position);

                if (!activePlots.ContainsKey(coords))
                {
                    // QUAN TRỌNG NHẤT: Cộng dồn thời gian đi vắng vào tuổi của cái cây
                    // (Nếu cây là Planted thì nó sẽ tự động tính xem với số giây này nó đã biến thành Grown chưa)
                    if (data.state == PlotState.Planted)
                    {
                        data.growTimer += offlineSeconds;
                    }

                    GameObject restoredPlot = Instantiate(tilledDirtPrefab, data.position, Quaternion.identity);
                    restoredPlot.GetComponent<FarmPlot>().plotID = data.id;
                    activePlots.Add(coords, restoredPlot);
                }
            }
        }
    }

    private void Update()
    {
        UpdateTargetGrid();
        PulseBoundaryVisual();
    }
    private void PulseBoundaryVisual()
    {
        if (boundaryLine == null || lineMaterial == null) return;

        bool isCurrentlyHoldingTool = isHoldingHoe || holdingBigTreeSeed != null;

        // 1. Trừ Timer mỗi khung hình (để nó đếm ngược)
        if (currentCooldownTimer > 0) currentCooldownTimer -= Time.deltaTime;
        if (currentShowTimer > 0) currentShowTimer -= Time.deltaTime;

        // 2. Bắt khoảnh khắc rút cuốc ra để nạp Timer
        if (isCurrentlyHoldingTool && !wasHoldingToolLastFrame)
        {
            if (currentCooldownTimer <= 0)
            {
                currentShowTimer = showDuration;
                currentCooldownTimer = cooldownTime;
            }
        }
        wasHoldingToolLastFrame = isCurrentlyHoldingTool;

        // 3. Xử lý hiển thị (CHỈ CẦN TIMER > 0 LÀ CHẠY, ĐẾCH CẦN BIẾT CẦM GÌ)
        bool shouldShow = currentShowTimer > 0;
        boundaryLine.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            UpdateBoundaryLine(); // Ép viền bám chặt ranh giới

            Color c = lineMaterial.color;
            // Hiệu ứng thở (Sóng Sin)
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float currentAlpha = Mathf.Lerp(0.05f, maxAlpha, wave);

            // Hiệu ứng Fade out mượt mà trong 1 giây cuối
            if (currentShowTimer < 1f)
            {
                currentAlpha *= currentShowTimer;
            }

            c.a = currentAlpha;
            lineMaterial.color = c;
        }
    }
    private void UpdateBoundaryLine()
    {
        if (boundaryLine == null || farmBoundary == null) return;

        // Lấy tâm và kích thước thực tế (World Space) của khung đất
        Vector3 center = farmBoundary.transform.TransformPoint(farmBoundary.center);
        Vector3 extents = new Vector3(
            (farmBoundary.size.x * Mathf.Abs(farmBoundary.transform.lossyScale.x)) / 2f,
            0f,
            (farmBoundary.size.z * Mathf.Abs(farmBoundary.transform.lossyScale.z)) / 2f
        );

        float lineY = center.y + 0.05f; // Nổi nhẹ lên 0.05f để không ghim xuống lòng đất

        // Tính tọa độ 4 góc
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(center.x - extents.x, lineY, center.z - extents.z); // Góc Trái Dưới
        corners[1] = new Vector3(center.x - extents.x, lineY, center.z + extents.z); // Góc Trái Trên
        corners[2] = new Vector3(center.x + extents.x, lineY, center.z + extents.z); // Góc Phải Trên
        corners[3] = new Vector3(center.x + extents.x, lineY, center.z - extents.z); // Góc Phải Dưới

        // Ép LineRenderer vẽ theo 4 điểm này
        boundaryLine.SetPositions(corners);
    }
    private Vector3Int GetGridCoords(Vector3 position)
    {
        if (farmBoundary == null) return Vector3Int.zero;

        // Lấy mép dưới cùng bên trái của Lưới Trắng (BoxCollider) làm gốc
        Vector3 origin = farmBoundary.bounds.min;

        // Tính khoảng cách từ điểm đang xét đến cái mép đó
        float localX = position.x - origin.x;
        float localZ = position.z - origin.z;

        // Dùng FloorToInt để chia ô, nó sẽ khóa chặt cục xanh vào lưới trắng
        int x = Mathf.FloorToInt(localX / gridSize);
        int z = Mathf.FloorToInt(localZ / gridSize);

        return new Vector3Int(x, 0, z);
    }

    private void UpdateTargetGrid()
    {
        if (player == null || farmBoundary == null) return;

        IInteractable self = this as IInteractable;
        if (playerRadar != null && playerRadar.currentTarget != null && playerRadar.currentTarget != self)
        {
            SetVisuals(false);
            return;
        }

        // 1. KIỂM TRA ĐỒ TRÊN TAY ĐỂ XÁC ĐỊNH SIZE LƯỚI (1x1 HAY 2x2)
        isHoldingHoe = false;
        holdingBigTreeSeed = null;
        currentGridSize = 1;

        int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;
        if (selectedIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[selectedIndex].item;
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hoe)
            {
                isHoldingHoe = true;
                currentGridSize = 1;
            }
            else if (holdingItem is SeedItemData seed && seed.isBigTree)
            {
                holdingBigTreeSeed = seed;
                currentGridSize = 2; // Cây to chiếm 4 ô!
            }
        }

        Vector3 targetPos = player.position + player.forward * 1.0f;

        if ((!isHoldingHoe && holdingBigTreeSeed == null) || !farmBoundary.bounds.Contains(targetPos))
        {
            SetVisuals(false);
            return;
        }

        // 2. TÍNH TOÁN TỌA ĐỘ VÀ SCALE CÁI Ô XANH
        Vector3Int baseCoords = GetGridCoords(targetPos);
        Vector3 origin = farmBoundary.bounds.min;

        // Tính tâm của ô 1x1 dựa vào mép của Lưới Trắng
        float centerX = origin.x + (baseCoords.x * gridSize) + (gridSize / 2f);
        float centerZ = origin.z + (baseCoords.z * gridSize) + (gridSize / 2f);

        // Nếu là cây to 2x2, dịch tâm sang phải và lên trên nửa ô
        float offset = (currentGridSize == 2) ? (gridSize / 2f) : 0f;
        Vector3 worldPos = new Vector3(centerX + offset, transform.position.y, centerZ + offset);

        transform.position = worldPos;

        // Phóng to thu nhỏ cái ô highlight
        if (highlightModel != null)
        {
            float visualScale = (currentGridSize == 2) ? 1.9f : 1f;
            float currentY = highlightModel.transform.localScale.y;
            highlightModel.transform.localScale = new Vector3(visualScale, currentY, visualScale);
        }
        if (cursorCollider != null)
        {
            cursorCollider.size = new Vector3(gridSize * currentGridSize, 0.1f, gridSize * currentGridSize);
        }
        // 3. QUÉT TOÀN BỘ KHU VỰC (1 Ô HOẶC 4 Ô) XEM CÓ BỊ VƯỚNG GÌ KHÔNG
        bool isOutOfBounds = false;

        // --- SỬA Ở ĐÂY: Tính chính xác MÉP NGOÀI của vùng chọn ---
        // Tổng chiều dài/rộng của ô đang xét (1.2 hoặc 2.4)
        float totalSize = gridSize * currentGridSize;
        float halfSize = totalSize / 2f;

        // Từ tâm worldPos, trừ/cộng đi một nửa kích thước để ra chính xác 4 mép
        float minX = worldPos.x - halfSize;
        float maxX = worldPos.x + halfSize;
        float minZ = worldPos.z - halfSize;
        float maxZ = worldPos.z + halfSize;

        Vector3[] AreaCheckPoints = new Vector3[] {
            new Vector3(minX, transform.position.y, minZ),
            new Vector3(maxX, transform.position.y, minZ),
            new Vector3(minX, transform.position.y, maxZ),
            new Vector3(maxX, transform.position.y, maxZ)
        };

        // Kiểm tra 4 cái góc này đều phải nằm trong Bounds của nông trại
        foreach (var corner in AreaCheckPoints)
        {
            // Thêm một dung sai cực nhỏ (0.01f) để tránh lỗi làm tròn số của dấu phẩy động (float) trong Unity
            Bounds bounds = farmBoundary.bounds;
            bounds.Expand(0.01f);

            if (!bounds.Contains(corner))
            {
                isOutOfBounds = true;
                break;
            }
        }

        // --- 4. CHECK DỮ LIỆU CUỐC ĐẤT (Active plots) ---
        bool isOccupied = false;
        for (int x = 0; x < currentGridSize; x++)
        {
            for (int z = 0; z < currentGridSize; z++)
            {
                Vector3Int checkCoords = new Vector3Int(baseCoords.x + x, 0, baseCoords.z + z);

                if (activePlots.ContainsKey(checkCoords) && activePlots[checkCoords] == null)
                    activePlots.Remove(checkCoords);

                if (activePlots.ContainsKey(checkCoords))
                {
                    isOccupied = true;
                    break;
                }
            }
            if (isOccupied) break;
        }

        SetVisuals(!isOutOfBounds && !isOccupied);
    }

    private void SetVisuals(bool isActive)
    {
        if (cursorCollider != null) cursorCollider.enabled = isActive;
        if (highlightModel != null) highlightModel.SetActive(isActive);
    }

    public string GetInteractText()
    {
        if (isHoldingHoe) return "[E] Till Soil";
        if (holdingBigTreeSeed != null) return $"[E] Plant {holdingBigTreeSeed.displayName}";
        return "";
    }

    public void Interact()
    {
        if (isHoldingHoe) DigPlot();
        else if (holdingBigTreeSeed != null) PlantBigTree();
    }

    public void DigPlot()
    {
        Vector3Int coords = GetGridCoords(transform.position);
        if (!activePlots.ContainsKey(coords) && tilledDirtPrefab != null)
        {
            // Tạo ID ngẫu nhiên không trùng lặp cho luống đất mới
            string newID = "Plot_" + System.Guid.NewGuid().ToString();

            GameObject newPlot = Instantiate(tilledDirtPrefab, transform.position, Quaternion.identity);
            newPlot.GetComponent<FarmPlot>().plotID = newID; // Cấp Căn cước công dân

            activePlots.Add(coords, newPlot);
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.DeductEquippedToolDurability(1f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Hoe_Hit");
            }

            UpdateTargetGrid();
        }
    }

    // ==========================================
    // LOGIC TRỒNG CÂY TO 2x2 TRỰC TIẾP
    // ==========================================
    private void PlantBigTree()
    {
        Vector3Int baseCoords = GetGridCoords(transform.position - new Vector3(gridSize / 2f, 0, gridSize / 2f));

        if (treePitPrefab != null && holdingBigTreeSeed != null)
        {
            // Trừ đồ trong balo
            int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;
            InventoryManager.Instance.hotbarSlots[selectedIndex].amount--;
            if (InventoryManager.Instance.hotbarSlots[selectedIndex].amount <= 0)
                InventoryManager.Instance.hotbarSlots[selectedIndex].item = null;
            InventoryManager.Instance.RefreshInventoryUI();

            // Sinh ra hố cây
            GameObject newTree = Instantiate(treePitPrefab, transform.position, Quaternion.identity);

            // Ép hố cây nhận diện hạt giống và tự mọc (Bỏ qua state Empty)
            TreePit pitScript = newTree.GetComponent<TreePit>();
            if (pitScript != null) pitScript.InitializePlanted(holdingBigTreeSeed);

            // Ghi đè vào não bộ: KHÓA CHẶT CẢ 4 Ô LƯỚI LẠI!
            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3Int lockCoord = new Vector3Int(baseCoords.x + x, 0, baseCoords.z + z);
                    if (!activePlots.ContainsKey(lockCoord)) activePlots.Add(lockCoord, newTree);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Plant_Seed"); // Tiếng sột soạt lấp đất non
            }

            UpdateTargetGrid();
            Debug.Log("Trồng cây to chiếm 4 ô thành công!");
        }
    }
    public void ExpandFarmBoundary(Vector3 extraSize, Vector3 extraCenterOffset)
    {
        if (farmBoundary != null)
        {
            // 1. Nới rộng kích thước và dịch tâm của BoxCollider như bình thường
            farmBoundary.size += extraSize;
            farmBoundary.center += extraCenterOffset;
            UpdateBoundaryLine();
            RefreshFarmTerrain();

            currentShowTimer = showDuration; // Nạp lại 3 giây hiển thị
            currentCooldownTimer = cooldownTime;
        }
    }
    public void RefreshFarmTerrain()
    {
        if (farmBoundary != null && terrainPainter != null)
        {
            // Lấy tâm thực tế
            Vector3 worldCenter = farmBoundary.transform.TransformPoint(farmBoundary.center);

            // Lấy kích thước thực tế
            Vector3 worldSize = new Vector3(
                farmBoundary.size.x * Mathf.Abs(farmBoundary.transform.lossyScale.x),
                0f,
                farmBoundary.size.z * Mathf.Abs(farmBoundary.transform.lossyScale.z)
            );

            // Bắt cọ vẽ làm việc
            terrainPainter.PaintDirtArea(worldCenter, worldSize);
        }
    }
    private void OnDrawGizmos()
    {
        if (farmBoundary == null || gridSize <= 0) return;

        // Vẽ lưới màu trắng mờ để dễ căn chỉnh viền
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Bounds bounds = farmBoundary.bounds;

        // Vẽ các đường dọc trục Z
        for (float x = bounds.min.x; x <= bounds.max.x; x += gridSize)
        {
            Gizmos.DrawLine(new Vector3(x, transform.position.y, bounds.min.z), new Vector3(x, transform.position.y, bounds.max.z));
        }

        // Vẽ các đường ngang trục X
        for (float z = bounds.min.z; z <= bounds.max.z; z += gridSize)
        {
            Gizmos.DrawLine(new Vector3(bounds.min.x, transform.position.y, z), new Vector3(bounds.max.x, transform.position.y, z));
        }
    }
    private void OnDestroy()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.lastFarmExitTimeTicks = System.DateTime.Now.Ticks;
        }
    }
}