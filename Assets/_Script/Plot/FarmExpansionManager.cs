using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Cinemachine; // Bắt buộc phải có để điều khiển Cinemachine bản Unity 6

public class FarmExpansionManager : MonoBehaviour
{
    public static FarmExpansionManager Instance;

    [Header("Camera & UI")]
    public CinemachineCamera topDownVCam; // [ĐÃ SỬA] Nhận diện VCam
    public int activePriority = 20;       // [ĐÃ SỬA] Priority khi đang nới vườn (càng cao càng tốt)
    private int defaultPriority;          // [ĐÃ SỬA] Lưu lại Priority gốc lúc tắt

    public GameObject expansionUIPanel;
    public TextMeshProUGUI woodCostText;
    public TextMeshProUGUI coinCostText;
    public Button confirmButton;
    

    [Header("Items (Kéo thả ScriptableObject vào)")]
    public ItemData woodItem;
    public ItemData coinItem;

    [Header("Hiển thị Đất (Visuals)")]
    public GameObject previewHighlight;
    public LineRenderer maxLimitLine;

    [Header("Cài đặt Mở rộng")]
    public float expandAmount = 1.2f;

    private BuildingBlueprint currentBlueprint;
    private bool isExpansionMode = false;

    private enum ExpandDirection { None, North, South, East, West }
    private ExpandDirection currentDir = ExpandDirection.None;
    private int selectedUpgradeCount = 1;

    private PlayerInputHandler inputHandler;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (expansionUIPanel != null) expansionUIPanel.SetActive(false);
        if (previewHighlight != null) previewHighlight.SetActive(false);

        // [MỚI]: Giấu vạch đích đi lúc mới vào game
        if (maxLimitLine != null) maxLimitLine.gameObject.SetActive(false);

        if (topDownVCam != null) defaultPriority = topDownVCam.Priority;
    }

    private void Start()
    {
    }

    public void StartExpansionMode(BuildingBlueprint blueprint)
    {
        if (inputHandler == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) inputHandler = playerObj.GetComponent<PlayerInputHandler>();
            else Debug.LogError("[FarmExpansion] Lỗi nặng: Vẫn không tìm thấy Player, kiểm tra lại xem Nhân vật có Tag là 'Player' chưa!");
        }

        currentBlueprint = blueprint;
        isExpansionMode = true;
        currentDir = ExpandDirection.None;
        selectedUpgradeCount = 1; 

        // [ĐÃ SỬA] Đẩy Priority của VCam này lên thật cao. Não (Brain) sẽ tự động quay sang nó!
        if (topDownVCam != null) topDownVCam.Priority = activePriority;

        expansionUIPanel.SetActive(true);
        previewHighlight.SetActive(false);
        confirmButton.interactable = true;
        confirmButton.gameObject.SetActive(false);


        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetFarmExpansionOpenState(true);
        }
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ToggleInGameUI(false);
        }
        if (StaminaUIManager.Instance != null)
        {
            StaminaUIManager.Instance.ToggleVisibility(false);
        }
        UpdateCostUI();
        DrawMaxLimitLine();
    }

    public bool IsExpansionModeActive()
    {
        return isExpansionMode;
    }

    private void Update()
    {
        if (!isExpansionMode || inputHandler == null) return;

        // Bấm ESC để thoát nhanh
        if (inputHandler.EscTriggered)
        {
            OnClick_Cancel();
            return;
        }

        if (inputHandler.ArrowUpTriggered) TrySelectDirection(ExpandDirection.South);
        else if (inputHandler.ArrowDownTriggered) TrySelectDirection(ExpandDirection.North);
        else if (inputHandler.ArrowRightTriggered) TrySelectDirection(ExpandDirection.West);
        else if (inputHandler.ArrowLeftTriggered) TrySelectDirection(ExpandDirection.East);
    }
    private ExpandDirection GetOppositeDirection(ExpandDirection dir)
    {
        if (dir == ExpandDirection.North) return ExpandDirection.South;
        if (dir == ExpandDirection.South) return ExpandDirection.North;
        if (dir == ExpandDirection.East) return ExpandDirection.West;
        if (dir == ExpandDirection.West) return ExpandDirection.East;
        return ExpandDirection.None;
    }
    private void TrySelectDirection(ExpandDirection dir)
    {
        int nextCount = selectedUpgradeCount;
        ExpandDirection nextDir = currentDir;

        // Xử lý logic bấm Phím mũi tên
        if (currentDir != ExpandDirection.None && dir == GetOppositeDirection(currentDir))
        {
            if (selectedUpgradeCount > 1)
            {
                nextCount--; // Bấm ngược hướng -> Lùi đất lại
            }
            else
            {
                // Đang nới 1 ô mà bấm lùi -> Hủy chọn hướng, dọn dẹp UI
                currentDir = ExpandDirection.None;
                previewHighlight.SetActive(false);
                confirmButton.gameObject.SetActive(false);
                UpdateCostUI();
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");
                return;
            }
        }
        else if (currentDir == dir)
        {
            nextCount++; // Bấm cùng hướng -> Tăng đất
        }
        else
        {
            nextDir = dir; // Bấm hướng mới toanh -> Đổi hướng, reset về 1
            nextCount = 1;
        }

        // Chặn nếu vượt Max Level
        if (currentBlueprint != null && currentBlueprint.currentLevel + nextCount > currentBlueprint.maxLevel)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
            return;
        }

        // Kiểm tra Ranh giới max
        if (CanExpand(nextDir, nextCount))
        {
            currentDir = nextDir;
            selectedUpgradeCount = nextCount;

            UpdatePreview();
            UpdateCostUI();

            confirmButton.gameObject.SetActive(HasEnoughResources());

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
            Debug.LogWarning("Không thể nới thêm hướng này vì chạm vạch đích đỏ!");
        }
    }

    private int GetCumulativeUnlockPrice()
    {
        if (currentBlueprint == null) return 0;
        int total = 0;
        for (int i = 0; i < selectedUpgradeCount; i++)
        {
            total += Mathf.RoundToInt(currentBlueprint.unlockPrice + (currentBlueprint.unlockPrice * currentBlueprint.costIncreasePerLevel * (currentBlueprint.currentLevel + i)));
        }
        return total;
    }

    private int GetCumulativeItemAmount(int baseAmount)
    {
        if (currentBlueprint == null) return 0;
        int total = 0;
        for (int i = 0; i < selectedUpgradeCount; i++)
        {
            total += Mathf.RoundToInt(baseAmount + (baseAmount * currentBlueprint.costIncreasePerLevel * (currentBlueprint.currentLevel + i)));
        }
        return total;
    }
    // [THÊM MỚI]: Hàm chuyên dùng để check xem có đủ tiền/đồ trong kho không
    private bool HasEnoughResources()
    {
        if (currentBlueprint == null) return false;

        int requiredWood = 0;
        int requiredCoin = GetCumulativeUnlockPrice();

        foreach (var req in currentBlueprint.unlockItemCosts)
        {
            if (req.item == woodItem) requiredWood = GetCumulativeItemAmount(req.amount);
        }

        return InventoryManager.Instance.GetTotalItemCount(woodItem) >= requiredWood &&
               InventoryManager.Instance.GetTotalItemCount(coinItem) >= requiredCoin;
    }

    private bool CanExpand(ExpandDirection dir, int count)
    {
        if (FarmingZone.Instance == null || FarmingZone.Instance.maxFarmBoundary == null) return true;

        BoxCollider current = FarmingZone.Instance.farmBoundary;
        BoxCollider max = FarmingZone.Instance.maxFarmBoundary;

        Vector3 newMin = current.bounds.min;
        Vector3 newMax = current.bounds.max;

        float totalExpandAmount = expandAmount * count; // Cộng dồn diện tích

        if (dir == ExpandDirection.North) newMax.z += totalExpandAmount;
        if (dir == ExpandDirection.South) newMin.z -= totalExpandAmount;
        if (dir == ExpandDirection.East) newMax.x += totalExpandAmount;
        if (dir == ExpandDirection.West) newMin.x -= totalExpandAmount;

        if (newMin.x < max.bounds.min.x - 0.1f || newMin.z < max.bounds.min.z - 0.1f ||
            newMax.x > max.bounds.max.x + 0.1f || newMax.z > max.bounds.max.z + 0.1f)
        {
            return false;
        }
        return true;
    }

    private void UpdatePreview()
    {
        BoxCollider current = FarmingZone.Instance.farmBoundary;
        Bounds bounds = current.bounds;
        Vector3 center = bounds.center;

        float widthX = bounds.size.x;
        float widthZ = bounds.size.z;
        float totalExpandAmount = expandAmount * selectedUpgradeCount; // Lấy tổng diện tích dồn

        previewHighlight.SetActive(true);

        if (currentDir == ExpandDirection.North)
        {
            previewHighlight.transform.localScale = new Vector3(widthX, 0.2f, totalExpandAmount);
            previewHighlight.transform.position = center + new Vector3(0, 0.1f, bounds.extents.z + totalExpandAmount / 2f);
        }
        else if (currentDir == ExpandDirection.South)
        {
            previewHighlight.transform.localScale = new Vector3(widthX, 0.2f, totalExpandAmount);
            previewHighlight.transform.position = center + new Vector3(0, 0.1f, -bounds.extents.z - totalExpandAmount / 2f);
        }
        else if (currentDir == ExpandDirection.East)
        {
            previewHighlight.transform.localScale = new Vector3(totalExpandAmount, 0.2f, widthZ);
            previewHighlight.transform.position = center + new Vector3(bounds.extents.x + totalExpandAmount / 2f, 0.1f, 0);
        }
        else if (currentDir == ExpandDirection.West)
        {
            previewHighlight.transform.localScale = new Vector3(totalExpandAmount, 0.2f, widthZ);
            previewHighlight.transform.position = center + new Vector3(-bounds.extents.x - totalExpandAmount / 2f, 0.1f, 0);
        }
    }

    private void UpdateCostUI()
    {
        if (currentBlueprint == null) return;

        int requiredWood = 0;
        int requiredCoin = 0;

        // Chỉ tính tiền nếu đang chọn nới, nếu hủy chọn thì chữ đỏ/xanh biến mất
        if (currentDir != ExpandDirection.None)
        {
            requiredCoin = GetCumulativeUnlockPrice();
            foreach (var req in currentBlueprint.unlockItemCosts)
            {
                if (req.item == woodItem) requiredWood = GetCumulativeItemAmount(req.amount);
            }
        }

        int currentWood = InventoryManager.Instance.GetTotalItemCount(woodItem);
        int currentCoin = InventoryManager.Instance.GetTotalItemCount(coinItem);

        // [SỬA LỖI UI]: Ẩn hẳn dòng Text đi nếu như nguyên liệu đó không bị yêu cầu (Giá = 0)
        if (woodCostText != null)
        {
            woodCostText.text = $"Gỗ: {currentWood}/{requiredWood}";
            woodCostText.color = currentWood >= requiredWood ? Color.green : Color.red;
            woodCostText.gameObject.SetActive(requiredWood > 0);
        }

        if (coinCostText != null)
        {
            coinCostText.text = $"Tiền: {currentCoin}/{requiredCoin}";
            coinCostText.color = currentCoin >= requiredCoin ? Color.green : Color.red;
            coinCostText.gameObject.SetActive(requiredCoin > 0);
        }
    }

    private void DrawMaxLimitLine()
    {
        // 1. Máy chửi nếu quên gán LineRenderer
        if (maxLimitLine == null)
        {
            Debug.LogError("[FarmExpansion] LỖI: Chưa kéo Object chứa LineRenderer vào ô 'Max Limit Line' trong FarmExpansionManager!");
            return;
        }

        if (FarmingZone.Instance == null) return;

        // 2. Máy chửi nếu quên gán BoxCollider
        if (FarmingZone.Instance.maxFarmBoundary == null)
        {
            Debug.LogError("[FarmExpansion] LỖI: Chưa kéo Object chứa BoxCollider to nhất vào ô 'Max Farm Boundary' trong FarmingZone!");
            return;
        }

        // Vượt qua 2 vòng kiểm tra thì mới bật viền lên
        maxLimitLine.gameObject.SetActive(true);
        Debug.Log("<color=green>[FarmExpansion] Đã bật Line thành công, bắt đầu tính toán vẽ 4 góc!</color>");

        // ==========================================
        // [THÊM MỚI]: ÉP BUỘC THÔNG SỐ ĐỂ CHỐNG TÀNG HÌNH
        // ==========================================
        maxLimitLine.useWorldSpace = true; // Bắt buộc dùng tọa độ World (vì m để nó làm Object con, nếu false nó sẽ bay tít ra ngoài vũ trụ)
        maxLimitLine.startWidth = 0.3f;    // Bề dày nét vẽ (m có thể chỉnh to hơn nếu khó nhìn)
        maxLimitLine.endWidth = 0.3f;

        BoxCollider max = FarmingZone.Instance.maxFarmBoundary;
        Vector3 center = max.transform.TransformPoint(max.center);
        Vector3 extents = new Vector3(
            (max.size.x * Mathf.Abs(max.transform.lossyScale.x)) / 2f,
            0f,
            (max.size.z * Mathf.Abs(max.transform.lossyScale.z)) / 2f
        );

        // Nâng line lên 0.5 mét so với mặt đất để không bị cỏ che khuất
        float lineY = center.y + 0.5f;

        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(center.x - extents.x, lineY, center.z - extents.z);
        corners[1] = new Vector3(center.x - extents.x, lineY, center.z + extents.z);
        corners[2] = new Vector3(center.x + extents.x, lineY, center.z + extents.z);
        corners[3] = new Vector3(center.x + extents.x, lineY, center.z - extents.z);

        maxLimitLine.positionCount = 4;
        maxLimitLine.loop = true;
        maxLimitLine.SetPositions(corners);
    }

    public void OnClick_Confirm()
    {
        if (currentDir == ExpandDirection.None) return;

        int requiredWood = 0;
        int requiredCoin = GetCumulativeUnlockPrice();

        foreach (var req in currentBlueprint.unlockItemCosts)
        {
            if (req.item == woodItem) requiredWood = GetCumulativeItemAmount(req.amount);
        }

        if (InventoryManager.Instance.GetTotalItemCount(woodItem) < requiredWood ||
            InventoryManager.Instance.GetTotalItemCount(coinItem) < requiredCoin)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
            return;
        }

        if (requiredCoin > 0) InventoryManager.Instance.ConsumeItemsGlobal(coinItem, requiredCoin);
        if (requiredWood > 0) InventoryManager.Instance.ConsumeItemsGlobal(woodItem, requiredWood);

        Vector3 worldDir = Vector3.zero;
        if (currentDir == ExpandDirection.North) worldDir = Vector3.forward;
        else if (currentDir == ExpandDirection.South) worldDir = Vector3.back;
        else if (currentDir == ExpandDirection.East) worldDir = Vector3.right;
        else if (currentDir == ExpandDirection.West) worldDir = Vector3.left;

        Transform farmTransform = FarmingZone.Instance.farmBoundary.transform;
        Vector3 localDir = farmTransform.InverseTransformDirection(worldDir);

        localDir.x = Mathf.Round(localDir.x);
        localDir.y = Mathf.Round(localDir.y);
        localDir.z = Mathf.Round(localDir.z);

        float totalExpandAmount = expandAmount * selectedUpgradeCount;

        Vector3 extraSize = new Vector3(Mathf.Abs(localDir.x), Mathf.Abs(localDir.y), Mathf.Abs(localDir.z)) * totalExpandAmount;
        Vector3 centerOffset = localDir * (totalExpandAmount / 2f);

        FarmingZone.Instance.ExpandFarmBoundary(extraSize, centerOffset);

        currentBlueprint.currentLevel += selectedUpgradeCount;


        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
            SaveManager.Instance.GetCurrentData().farmExpansionLevel = currentBlueprint.currentLevel;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportAction("Upgrade_Workshop", 1);
            QuestManager.Instance.ReportAction($"Upgrade_Farm_{currentBlueprint.currentLevel}", 1);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Build_Success");

        OnClick_Cancel();
    }

    public void OnClick_Cancel()
    {
        isExpansionMode = false;
        expansionUIPanel.SetActive(false);
        previewHighlight.SetActive(false);

        // [MỚI]: Tắt vạch đích đi khi cất bản vẽ
        if (maxLimitLine != null) maxLimitLine.gameObject.SetActive(false);

        if (topDownVCam != null) topDownVCam.Priority = defaultPriority;

        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetFarmExpansionOpenState(false);
        }
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ToggleInGameUI(true);
        }
        if (StaminaUIManager.Instance != null)
        {
            StaminaUIManager.Instance.ToggleVisibility(true);
        }
    }
}