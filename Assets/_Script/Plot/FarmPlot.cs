using UnityEngine;

// 1. Thêm trạng thái Grown (Đã lớn) vào danh sách
public enum PlotState { Tilled, Planted, Grown }

public class FarmPlot : MonoBehaviour, IInteractable
{
    [Header("Current State")]
    public PlotState currentState = PlotState.Tilled;

    [Header("Visuals (Các khối hiển thị)")]
    public GameObject dirtTilled;
    public GameObject seedSprout;

    public ItemData wateringCanItem;
    public ItemData fertilizerItem;
    // BỘ NHỚ CỦA Ô ĐẤT
    private SeedItemData plantedSeed;
    private float growTimer = 0f;
    private GameObject matureCropObject;
    private int currentHarvestCount = 0;

    public GameObject needWaterIcon;

    private bool isWatered = false;
    private bool isFertilized = false;

    private bool isTargeted = false;
    public float iconVisibleDistance = 20f; // Cách 20 mét là tắt
    private Transform playerTransform;

    public ParticleSystem waterSplashEffect;
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.OnWeatherChanged += HandleWeatherChange;
            // Ép xử lý ngay lúc vừa spawn ra lỡ đang mưa sẵn
            HandleWeatherChange(WeatherManager.Instance.currentWeather);
        }

        UpdateVisuals();
    }
    private void HandleWeatherChange(WeatherState newWeather)
    {
        if (newWeather == WeatherState.Raining)
        {
            // Nếu trời mưa, mà đang trồng cây và chưa ướt -> Tự cho ướt luôn
            if (currentState == PlotState.Planted && !isWatered)
            {
                isWatered = true;
                UpdateVisuals();
                growTimer += GetBaseTime() * 0.25f; // Mưa tự động buff 25% thời gian như tưới tay
                Debug.Log("Trời mưa! Luống đất đã tự động ướt mèm!");
            }
        }
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

        // Trừ trong Hotbar trước
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
        // Nếu không có thì trừ trong Balo chính
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
        if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == WeatherState.Raining)
            return false;

        return !isWatered && HasPersonalItem(wateringCanItem);
    }

    public bool CanBeFertilized()
    {
        return isWatered && !isFertilized && HasPersonalItem(fertilizerItem);
    }
    private float GetBaseTime()
    {
        if (plantedSeed == null) return 0f;
        // Lấy đúng thời gian mọc gốc của cái cây đó
        return (currentHarvestCount == 0) ? plantedSeed.growTime : plantedSeed.regrowTime;
    }
    // ==========================================
    // BỘ ĐẾM GIỜ LỚN LÊN (Chạy bằng thời gian thực)
    // ==========================================
    private void Update()
    {
        if (currentState == PlotState.Planted && plantedSeed != null)
        {
            growTimer += Time.deltaTime;

            // Xác định mốc thời gian lớn (Lứa đầu hay Lứa sau)
            float targetTime = (currentHarvestCount == 0) ? plantedSeed.growTime : plantedSeed.regrowTime;

            // FIX LỖI: Chỗ này phải check với targetTime chứ không phải growTime
            if (growTimer >= GetBaseTime())
            {
                GrowUp();
            }
        }
        UpdateIconVisibility();
    }

    public string GetInteractText()
    {
        if (currentState == PlotState.Tilled) return "[E] Plant Seed";
        if (currentState == PlotState.Planted)
        {
            if (CanBeWatered()) return "[E] Water Plant";
            if (CanBeFertilized()) return "[E] Fertilize";

            int percent = Mathf.Clamp(Mathf.RoundToInt((growTimer / GetBaseTime()) * 100), 0, 100);
            return $"Growing... {percent}%";
        }
        if (currentState == PlotState.Grown) return $"[E] Harvest {plantedSeed.displayName} (100%)";
        return "";
    }
    public float GetGrowProgress()
    {
        if (plantedSeed == null || plantedSeed.growTime <= 0) return 0f;
        float baseTime = (currentHarvestCount == 0) ? plantedSeed.growTime : plantedSeed.regrowTime;

        // Tiến độ hiện tại = Thời gian đã trôi qua / Thời gian thực tế cần để lớn (Đã Buff)
        return Mathf.Clamp01(growTimer / GetBaseTime());
    }
    public void Interact()
    {
        if (currentState == PlotState.Tilled)
        {
            if (FarmPlotUIManager.Instance != null) FarmPlotUIManager.Instance.OpenPlotUI(this);
        }
        else if (currentState == PlotState.Planted)
        {
            if (CanBeWatered()) WaterPlant();
            else if (CanBeFertilized()) FertilizePlant();
        }
        else if (currentState == PlotState.Grown) Harvest();
    }

    public void PlantSeedSuccess(SeedItemData seedPlanted)
    {
        currentState = PlotState.Planted;
        plantedSeed = seedPlanted;
        growTimer = 0f; // Reset đồng hồ về 0 để bắt đầu đếm
        currentHarvestCount = 0;

        isWatered = false;
        isFertilized = false;

        if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == WeatherState.Raining)
        {
            isWatered = true;
            growTimer += GetBaseTime() * 0.25f;
        }

        UpdateVisuals();
        Debug.Log($"Đã trồng: {seedPlanted.displayName}. Cần {seedPlanted.growTime} giây để lớn.");
    }

    private void WaterPlant()
    {
        isWatered = true;
        UpdateVisuals(); // Gọi để đổi màu đất
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

    private void FertilizePlant()
    {
        // Trừ 1 Phân Bón trong kho (Giả sử tên item là "Fertilizer")
        if (fertilizerItem != null)
        {
            ConsumePersonalItem(fertilizerItem);
            isFertilized = true;
            growTimer += GetBaseTime() * 0.25f;
            Debug.Log("Đã bón phân! Giảm thêm 25% thời gian mọc.");
        }
    }

    private void GrowUp()
    {
        currentState = PlotState.Grown;

        // 1. Tắt cái mầm giả (dùng chung) đi
        if (seedSprout) seedSprout.SetActive(false);

        // 2. Lôi cái model 3D Cây Thật ra khỏi dữ liệu hạt giống và cắm xuống đất
        if (plantedSeed.cropPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 0.2f, 0);
            matureCropObject = Instantiate(plantedSeed.cropPrefab, spawnPos, Quaternion.identity, transform);
        }

        // Không cần gọi UpdateVisuals nữa vì ta vừa xử lý bằng tay ở trên rồi
    }

    // ==========================================
    // HÀM XỬ LÝ KHI THU HOẠCH
    // ==========================================
    private void Harvest()
    {
        // 1. Nhét vật phẩm vào Balo (Chống tràn balo)
        if (plantedSeed.yieldItem != null)
        {
            bool hasSpace = InventoryManager.Instance.AddItem(plantedSeed.yieldItem, plantedSeed.yieldAmount);
            if (!hasSpace)
            {
                Debug.LogWarning("Balo đầy! Không thể thu hoạch!");
                return;
            }
            Debug.Log($"Đã thu hoạch: {plantedSeed.yieldAmount}x {plantedSeed.yieldItem.displayName}");

            if (plantedSeed != null && plantedSeed.isMultiHarvest)
            {
                currentHarvestCount++; // Tăng bộ đếm lên 1

                // Nếu vẫn CHƯA đạt giới hạn (ví dụ mới hái lần 1, lần 2, max là 3)
                if (currentHarvestCount < plantedSeed.maxHarvestTimes)
                {
                    Debug.Log($"Cây sẽ mọc lại! Đã hái {currentHarvestCount}/{plantedSeed.maxHarvestTimes} lần.");

                    // Tua ngược trạng thái về lại Planted để lớn tiếp
                    currentState = PlotState.Planted;
                    growTimer = 0f; // Reset đồng hồ cho vòng lặp mới

                    // Xóa cái quả/cây đã lớn hiện tại đi (để lộ ra cái mầm nhú)
                    if (matureCropObject != null) Destroy(matureCropObject);

                    UpdateVisuals();
                    return; // LỆNH THOÁT HÀM SỚM (CHẶN KHÔNG CHO XÓA LUỐNG ĐẤT BÊN DƯỚI)
                }
                else
                {
                    Debug.Log("Cây đã cạn kiệt dinh dưỡng, tự động phá hủy luống đất!");
                }
            }
            Destroy(gameObject);
        }
    }

    public void UpdateVisuals()
    {
        if (dirtTilled) dirtTilled.SetActive(true);
        if (seedSprout) seedSprout.SetActive(currentState == PlotState.Planted);
        if (needWaterIcon != null)
        {
            bool needsWater = (currentState == PlotState.Planted) && !isWatered && !isTargeted;
            needWaterIcon.SetActive(needsWater);
        }
    }
    private void UpdateIconVisibility()
    {
        if (needWaterIcon == null) return;

        // 1. Check điều kiện gốc: Đang mọc + Chưa tưới + Không bị nhìn trúng
        bool needsWater = (currentState == PlotState.Planted) && !isWatered && !isTargeted;

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
    public void FastForwardTime(float realSecondsToAdd)
    {
        if (currentState == PlotState.Planted && plantedSeed != null)
        {
            growTimer += realSecondsToAdd;

            if (growTimer >= GetBaseTime())
            {
                GrowUp();
            }
        }
    }
    public bool BeEatenByAnimal()
    {
        // Gà sẽ ăn cả hạt giống đang mọc (Planted) và cây đã lớn (Grown)
        if (currentState == PlotState.Planted || currentState == PlotState.Grown)
        {
            // 1. Phá hủy hình ảnh cái cây/mầm
            if (matureCropObject != null) Destroy(matureCropObject);
            if (seedSprout) seedSprout.SetActive(false);

            // 2. Reset luống đất về trạng thái vừa cuốc (Tilled) 
            // Hoặc ông có thể dùng Destroy(gameObject) nếu muốn nó cày nát luôn cả luống đất
            currentState = PlotState.Tilled;
            plantedSeed = null;
            growTimer = 0f;
            currentHarvestCount = 0;
            isWatered = false;
            isFertilized = false;

            UpdateVisuals();

            Debug.LogWarning("BÁO ĐỘNG: Động vật đã ăn trộm cây trồng của bạn!");
            return true; // Trả về true để báo cho con Gà biết là nó đã ăn no
        }

        return false; // Nếu đất trống thì không ăn được
    }
}