using UnityEngine;

public class TreePit : MonoBehaviour, IInteractable
{
    public enum PitState { Planted, Grown_Empty, Grown_Fruited }
    public PitState currentState = PitState.Planted;

    [Header("Khối 3D Hiển thị")]
    public GameObject saplingModel;



    private SeedItemData plantedTree;
    private float growTimer = 0f;

    private GameObject matureTreeObject;
    private Transform fruitVisuals;

    public ItemData wateringCanItem;
    public ItemData fertilizerItem;

    private int treeHealth = 3;
    private bool isDead = false;

    public GameObject needWaterIcon;
    // --- BIẾN MỚI: BỘ NHỚ TƯỚI NƯỚC & BÓN PHÂN ---
    private bool isWatered = false;
    private bool isFertilized = false;
    private bool isTargeted = false;
    [Header("Cài đặt tầm nhìn Icon")]
    public float iconVisibleDistance = 20f; // Cách 20 mét là tắt
    private Transform playerTransform;

    public ParticleSystem waterSplashEffect;
    private float GetBaseTime()
    {
        if (plantedTree == null) return 0f;
        return (currentState == PitState.Planted) ? plantedTree.growTime : plantedTree.regrowTime;
    }
    private void Start()
    {
        // Tự động mò tìm thằng Player trong map bằng Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        UpdateVisuals();
    }
    public void SetTargeted(bool targeted)
    {
        isTargeted = targeted;
        UpdateVisuals(); // Cập nhật lại hình ảnh ngay lập tức
    }
    private bool HasPersonalItem(ItemData itemToCheck)
    {
        if (itemToCheck == null || InventoryManager.Instance == null) return false;

        foreach (var slot in InventoryManager.Instance.hotbarSlots)
        {
            if (slot.item == itemToCheck && slot.amount > 0)
            {
                // ĐÃ SỬA: Nếu món đồ là Công cụ, bắt buộc phải CÒN MÁU (>0) mới xài được
                if (slot.item is ToolItemData tool)
                {
                    if (slot.currentDurability > 0) return true;
                }
                else return true;
            }
        }
        foreach (var slot in InventoryManager.Instance.inventorySlots)
        {
            if (slot.item == itemToCheck && slot.amount > 0)
            {
                if (slot.item is ToolItemData tool)
                {
                    if (slot.currentDurability > 0) return true;
                }
                else return true;
            }
        }
        return false;
    }

    private void ConsumePersonalItem(ItemData itemToConsume)
    {
        if (itemToConsume == null || InventoryManager.Instance == null) return;

        foreach (var slot in InventoryManager.Instance.hotbarSlots)
        {
            if (slot.item == itemToConsume && slot.amount > 0)
            {
                slot.amount--;
                if (slot.amount <= 0) slot.item = null;
                InventoryManager.Instance.RefreshInventoryUI();
                return;
            }
        }
        foreach (var slot in InventoryManager.Instance.inventorySlots)
        {
            if (slot.item == itemToConsume && slot.amount > 0)
            {
                slot.amount--;
                if (slot.amount <= 0) slot.item = null;
                InventoryManager.Instance.RefreshInventoryUI();
                return;
            }
        }
    }
    public bool CanBeWatered()
    {
        return !isWatered && HasPersonalItem(wateringCanItem);
    }

    public bool CanBeFertilized()
    {
        return isWatered && !isFertilized && HasPersonalItem(fertilizerItem);
    }
    public void InitializePlanted(SeedItemData seed)
    {
        plantedTree = seed;
        currentState = PitState.Planted;
        growTimer = 0f;
        treeHealth = 1;

        isWatered = false;
        isFertilized = false;

        UpdateVisuals();
    }

    private void Update()
    {
        if ((currentState == PitState.Planted || currentState == PitState.Grown_Empty) && plantedTree != null)
        {
            growTimer += Time.deltaTime;
            if (growTimer >= GetBaseTime())
            {
                if (currentState == PitState.Planted) GrowUpTree();
                else RegrowFruits();
            }
        }
        UpdateIconVisibility();
    }

    public void FastForwardTime(float realSecondsToAdd)
    {
        if ((currentState == PitState.Planted || currentState == PitState.Grown_Empty) && plantedTree != null)
        {
            growTimer += realSecondsToAdd;
            if (growTimer >= GetBaseTime())
            {
                if (currentState == PitState.Planted) GrowUpTree();
                else RegrowFruits();
            }
        }
    }

    public float GetGrowProgress()
    {
        if (plantedTree == null || GetBaseTime() <= 0) return 0f;
        return Mathf.Clamp01(growTimer / GetBaseTime());
    }

    private void GrowUpTree()
    {
        currentState = PitState.Grown_Fruited;
        treeHealth = 3;
        if (saplingModel) saplingModel.SetActive(false);

        if (plantedTree.cropPrefab != null)
        {
            matureTreeObject = Instantiate(plantedTree.cropPrefab, transform.position, Quaternion.identity, transform);
            TreeVisuals visuals = matureTreeObject.GetComponent<TreeVisuals>();
            if (visuals != null && visuals.fruitVisuals != null)
            {
                fruitVisuals = visuals.fruitVisuals;
                fruitVisuals.gameObject.SetActive(true);
            }
        }
    }

    private void RegrowFruits()
    {
        currentState = PitState.Grown_Fruited;
        if (fruitVisuals != null) fruitVisuals.gameObject.SetActive(true);
    }

    private void HarvestFruit()
    {
        if (plantedTree != null && plantedTree.yieldItem != null)
        {
            bool hasSpace = InventoryManager.Instance.AddItem(plantedTree.yieldItem, plantedTree.yieldAmount);
            if (!hasSpace) return;

            if (fruitVisuals != null) fruitVisuals.gameObject.SetActive(false);

            currentState = PitState.Grown_Empty;
            growTimer = 0f;

            // Cây ra quả xong cũng cần được tưới/bón lại để lứa sau ra nhanh hơn
            isWatered = false;
            isFertilized = false;
            UpdateVisuals();
        }
    }

    private void ChopTree()
    {
        if (isDead) return;
        treeHealth--;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DeductEquippedToolDurability(1f); // Mỗi nhát chém tụt 1 máu
        }

        if (treeHealth <= 0)
        {
            if (currentState != PitState.Planted && plantedTree.woodItem != null)
                InventoryManager.Instance.AddItem(plantedTree.woodItem, plantedTree.woodAmount);

            if (currentState == PitState.Grown_Fruited && plantedTree.yieldItem != null)
                InventoryManager.Instance.AddItem(plantedTree.yieldItem, plantedTree.yieldAmount);

            Destroy(gameObject);
        }
    }

    public string GetInteractText()
    {
        bool isHoldingAxe = false;
        if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe) isHoldingAxe = true;
        }

        if (isHoldingAxe) return currentState == PitState.Planted ? "[E] Chop Sapling" : "[E] Chop Tree";
        if (currentState == PitState.Grown_Fruited) return $"[E] Harvest {plantedTree.displayName}";

        // QUÉT ĐỒ TƯỚI/BÓN KHI ĐANG MỌC
        if (currentState == PitState.Planted || currentState == PitState.Grown_Empty)
        {
            if (CanBeWatered()) return "[E] Water Plant";
            if (CanBeFertilized()) return "[E] Fertilize";

            int percent = Mathf.Clamp(Mathf.RoundToInt((growTimer / GetBaseTime()) * 100), 0, 100);
            return currentState == PitState.Planted ? $"Growing... {percent}%" : $"Regrowing Fruits... {percent}%";
        }

        return "";
    }

    public void Interact()
    {
        bool isHoldingAxe = false;
        if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe) isHoldingAxe = true;
        }

        if (isHoldingAxe) ChopTree();
        else if (currentState == PitState.Grown_Fruited) HarvestFruit();
        else if (currentState == PitState.Planted || currentState == PitState.Grown_Empty)
        {
            if (CanBeWatered()) WaterTree();
            else if (CanBeFertilized()) FertilizeTree();
        }
    }
    private void WaterTree()
    {
        isWatered = true;
        UpdateVisuals();
        growTimer += GetBaseTime() * 0.25f;

        if (waterSplashEffect != null)
        {
            waterSplashEffect.Play();
        }

        if (InventoryManager.Instance != null && wateringCanItem != null)
        {
            InventoryManager.Instance.DeductPersonalToolDurability(wateringCanItem, 1f);
        }
    }

    private void FertilizeTree()
    {
        if (fertilizerItem != null)
        {
            ConsumePersonalItem(fertilizerItem);
            isFertilized = true;

            growTimer += GetBaseTime() * 0.25f;
        }
    }
    private void UpdateIconVisibility()
    {
        if (needWaterIcon == null) return;

        // 1. Check điều kiện gốc: Đang mọc + Chưa tưới + Không bị nhìn trúng
        bool needsWater = (currentState == PitState.Planted) && !isWatered && !isTargeted;

        // 2. Check thêm điều kiện khoảng cách: Nếu ở quá xa thì ÉP TẮT
        if (needsWater && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > iconVisibleDistance)
            {
                needsWater = false;
            }
        }

        // Tối ưu hóa: Chỉ bật/tắt nếu trạng thái thực sự thay đổi (đỡ giật lag)
        if (needWaterIcon.activeSelf != needsWater)
        {
            needWaterIcon.SetActive(needsWater);
        }
    }
    private void UpdateVisuals()
    {
        if (saplingModel) saplingModel.SetActive(currentState == PitState.Planted);
        if (needWaterIcon != null)
        {
            bool needsWater = (currentState == PitState.Planted || currentState == PitState.Grown_Empty) && !isWatered && !isTargeted;
            needWaterIcon.SetActive(needsWater);
        }
    }
}