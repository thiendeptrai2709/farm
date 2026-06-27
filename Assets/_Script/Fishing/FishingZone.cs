using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Localization;
public class FishingZone : MonoBehaviour, IInteractable
{
    public enum FishingState { NotFishing, WaitingForBite, PlayingMiniGame, WaitingNextRound, Pulling }
    public FishingState currentState = FishingState.NotFishing;
    public enum ArrowKey { Up, Down, Left, Right }

    [Header("Cài đặt Audition (3 Hiệp)")]
    public int maxRounds = 3;
    public float timeBetweenRounds = 1f;

    [Header("Cài đặt thời gian chờ cá")]
    public float minBiteTime = 3f;
    public float maxBiteTime = 6f;

    [Header("Dữ liệu Cá của Hồ này")]
    public List<FishItemData> availableFish = new List<FishItemData>();

    [Header("Hiệu ứng bắt cá (Visuals)")]
    public Transform waterSurfacePoint;    // Điểm mặt nước (Cá nhảy lên từ đây)
    public GameObject genericFishPrefab;   // Model 3D con cá mặc định
    public GameObject splashEffectPrefab;  // Hiệu ứng bọt nước
    public float fishFlyDuration = 1.0f;

    [Header("Cài đặt Độ khó Gốc (DDA)")]
    public int baseSequenceLength = 4;       // Số nút mặc định
    public float baseTimeLimit = 3f;         // Thời gian mặc định

    private int currentSequenceLength;
    private float currentTimeLimit;

    private int currentRound = 0;
    private int successCount = 0;
    private List<ArrowKey> targetSequence = new List<ArrowKey>();
    private List<bool> invertedSequence = new List<bool>();

    private int currentInputIndex = 0;
    private float timer = 0f;
    private float lastInteractTime = 0f;
    private PlayerInputHandler playerInput;
    private bool isFishLoaded = false;
    public FishingLocation thisLocation = FishingLocation.Farm;

    [Header("Cài đặt Phao & Dây Câu Động")]
    public GameObject bobberPrefab;
    public LineRenderer fishingLineRenderer;
    public LayerMask waterLayer;
    private GameObject currentBobber;
    private Transform dynamicRodTip;
    private Vector3 bobberPosition;
    private bool isCastingBobber = false;

    private float currentSagAmount = 0f;
    private float currentSagDirection = -1f;
    public float lineLerpSpeed = 5f;

    [Header("Cài đặt Ngôn ngữ (Localization)")]
    public LocalizedString locInteractNeedRod; // "Khu vực Câu cá (Cần cầm Cần Câu)"
    public LocalizedString locCastRod;         // "[E] Quăng cần"
    public LocalizedString locCancelFishing;   // "[E] Hủy câu"
    public LocalizedString locRound;           // "HIỆP"
    public LocalizedString locPrepareRound;    // "Chuẩn bị hiệp"
    public LocalizedString locInventoryFull;

    private void Update()
    {
        if (currentState == FishingState.NotFishing)
        {
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.ToggleTierPanel(false);
            if (fishingLineRenderer != null && fishingLineRenderer.enabled) fishingLineRenderer.enabled = false;
            if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }
        }
        else
        {
            if (fishingLineRenderer != null && fishingLineRenderer.enabled)
            {
                // [MỚI]: Liên tục uốn cong dây theo vị trí phao bay
                DrawCurvedFishingLine();
            }
        }
        if (currentState == FishingState.WaitingForBite)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                PlayerInteraction pInteraction = FindAnyObjectByType<PlayerInteraction>();
                if (pInteraction != null && pInteraction.playerAnimator != null)
                {
                    // Chức năng: Reset trigger cũ trước khi gán trigger mới để chống dồn lệnh animation
                    pInteraction.playerAnimator.ResetTrigger("StartFishing");
                    pInteraction.playerAnimator.SetTrigger("StruggleFish");
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Fish_Bite");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLoopSFX("Reel_Struggle");

                StartRound();
            }
        }
        else if (currentState == FishingState.WaitingNextRound)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) StartRound();
        }
        else if (currentState == FishingState.PlayingMiniGame)
        {
            timer -= Time.deltaTime;
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.UpdateTimer(timer);

            if (timer <= 0)
            {
                Debug.Log($"Hiệp {currentRound}: Hết giờ! Tạch!");
                RoundFinished(false);
                return;
            }

            if (playerInput == null) playerInput = FindAnyObjectByType<PlayerInputHandler>();

            if (playerInput != null)
            {
                if (playerInput.ArrowUpTriggered) ProcessInput(ArrowKey.Up);
                else if (playerInput.ArrowDownTriggered) ProcessInput(ArrowKey.Down);
                else if (playerInput.ArrowLeftTriggered) ProcessInput(ArrowKey.Left);
                else if (playerInput.ArrowRightTriggered) ProcessInput(ArrowKey.Right);
            }
        }
    }

    private void StartRound()
    {
        if (FishingUIManager.Instance != null && currentSequenceLength > FishingUIManager.Instance.GetMaxArrowSlots())
        {
            currentSequenceLength = FishingUIManager.Instance.GetMaxArrowSlots();
        }

        currentRound++;
        currentState = FishingState.PlayingMiniGame;
        timer = currentTimeLimit;
        currentInputIndex = 0;
        targetSequence.Clear();
        invertedSequence.Clear(); // Xóa sạch danh sách ngược của hiệp cũ

        for (int i = 0; i < currentSequenceLength; i++)
        {
            targetSequence.Add((ArrowKey)Random.Range(0, 4));
            invertedSequence.Add(false); // Mặc định tất cả đều là nút trắng bình thường
        }

        // 2. CƠ CHẾ SỐ LƯỢNG NÚT ĐỎ (Chỉ kích hoạt ở Hiệp 3)
        if (currentRound == 3)
        {
            // M CÓ THỂ ĐỔI SỐ LƯỢNG NÚT ĐỎ Ở ĐÂY (Ví dụ t đang để cứng là 2 nút)
            int targetRedArrows = 2;

            // Chống lỗi: Đảm bảo số nút đỏ yêu cầu không được vượt quá tổng số nút đang có trên màn hình
            int actualRedArrows = Mathf.Min(targetRedArrows, currentSequenceLength);

            // Tạo một danh sách các "ghế trống" để chọn ngẫu nhiên vị trí đặt nút đỏ
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < currentSequenceLength; i++)
            {
                availableIndices.Add(i);
            }

            // Bốc thăm ngẫu nhiên vị trí để nhét nút đỏ vào
            for (int i = 0; i < actualRedArrows; i++)
            {
                int randomIndex = Random.Range(0, availableIndices.Count);
                int selectedPosition = availableIndices[randomIndex];

                invertedSequence[selectedPosition] = true; // Nhuộm đỏ nút ở vị trí đã trúng tuyển
                availableIndices.RemoveAt(randomIndex); // Rút vị trí này ra khỏi danh sách để các vòng lặp sau không chọn trùng lại
            }
        }

        if (FishingUIManager.Instance != null)
        {
            FishingUIManager.Instance.ToggleAuditionPanel(true);
            FishingUIManager.Instance.ToggleTimer(true, currentTimeLimit, currentTimeLimit);
            // Truyền thêm danh sách ngược sang UI để nó biết đường tô màu
            FishingUIManager.Instance.SetupArrows(currentSequenceLength, targetSequence, invertedSequence);
        }
    }
    private ArrowKey GetOppositeKey(ArrowKey key)
    {
        // Hàm phụ trợ để tìm ra phím ngược chiều
        switch (key)
        {
            case ArrowKey.Up: return ArrowKey.Down;
            case ArrowKey.Down: return ArrowKey.Up;
            case ArrowKey.Left: return ArrowKey.Right;
            case ArrowKey.Right: return ArrowKey.Left;
            default: return key;
        }
    }
    private void ProcessInput(ArrowKey pressedKey)
    {
        ArrowKey displayedKey = targetSequence[currentInputIndex];
        bool isInverted = invertedSequence[currentInputIndex];
        ArrowKey expectedKey = isInverted ? GetOppositeKey(displayedKey) : displayedKey;

        // So sánh phím người chơi bấm với expectedKey thay vì displayedKey
        if (pressedKey == expectedKey)
        {
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.MarkArrowSuccess(currentInputIndex);

            currentInputIndex++;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");
            if (currentInputIndex >= currentSequenceLength)
            {
                RoundFinished(true);
            }
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
            Debug.Log($"Hiệp {currentRound}: Bấm sai nút! Tạch!");
            RoundFinished(false);
        }
    }

    private void RoundFinished(bool wonRound)
    {
        if (wonRound)
        {
            successCount++;
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.UpdateTierUI(successCount);

            if (currentSequenceLength < 5)
            {
                currentSequenceLength++;
            }
            else
            {
                currentTimeLimit = Mathf.Clamp(currentTimeLimit - 0.5f, 1.5f, 5f);
            }
            Debug.Log($"[THẮNG] Cân bằng -> Nút: {currentSequenceLength}, Giờ: {currentTimeLimit}s");
        }
        else
        {
            if (currentTimeLimit < baseTimeLimit)
            {
                currentTimeLimit = Mathf.Clamp(currentTimeLimit + 0.5f, 1.5f, baseTimeLimit);
            }
            else
            {
                currentSequenceLength = Mathf.Clamp(currentSequenceLength - 1, 3, 5);
            }
            Debug.Log($"[THUA] Cân bằng -> Nút: {currentSequenceLength}, Giờ: {currentTimeLimit}s");
        }

        if (currentRound >= maxRounds)
        {
            EndFishing();
        }
        else
        {
            currentState = FishingState.WaitingNextRound;
            timer = timeBetweenRounds;
            if (FishingUIManager.Instance != null)
            {
                FishingUIManager.Instance.ToggleAuditionPanel(false);
                FishingUIManager.Instance.ToggleTimer(false);
            }
        }
    }

    private IEnumerator SpawnAndFlyFishRoutine()
    {
        if (genericFishPrefab == null) yield break;

        // Ép toàn bộ hiệu ứng bọt nước văng lên từ vị trí cái Phao (bobberPosition)
        if (splashEffectPrefab != null)
        {
            GameObject splash = Instantiate(splashEffectPrefab, bobberPosition, Quaternion.identity);
            Destroy(splash, 2f);
        }

        // Sinh con cá bắt đầu bay lên từ đúng cái vị trí Phao
        GameObject fish = Instantiate(genericFishPrefab, bobberPosition, Quaternion.identity);
        Transform playerTransform = FindAnyObjectByType<PlayerMovement>().transform;

        float elapsed = 0f;
        Vector3 startPos = bobberPosition;

        while (elapsed < fishFlyDuration)
        {
            if (fish == null) break;

            elapsed += Time.deltaTime;
            float t = elapsed / fishFlyDuration;

            Vector3 targetPos = playerTransform.position + Vector3.up * 1.5f;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 2.0f;

            fish.transform.position = currentPos;
            fish.transform.Rotate(Vector3.right * 500 * Time.deltaTime);

            yield return null;
        }

        if (fish != null) Destroy(fish);
    }
    private void EndFishing()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX();

        if (FishingUIManager.Instance != null)
        {
            FishingUIManager.Instance.ToggleAuditionPanel(false);
            FishingUIManager.Instance.ToggleTimer(false);
        }

        Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;
        currentState = FishingState.Pulling;

        // [MỚI]: Thu hồi phao và dây ngay lúc nhân vật chuyển animation giật cần lên (dù trượt hay trúng)
        if (fishingLineRenderer != null) fishingLineRenderer.enabled = false;
        if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DeductEquippedToolDurability(1f);
        }

        if (successCount == 0)
        {
            Debug.Log("NẤC 0: Tạch toàn tập. Chuyển sang Anim Trượt!");
            if (playerAnim != null)
            {
                // Chức năng: Xóa lệnh giằng co cá để chuyển mượt mà sang anim trượt
                playerAnim.ResetTrigger("StruggleFish");
                playerAnim.SetTrigger("MissFish");
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Fish_Fail");

            StartCoroutine(ResetFishingStateRoutine(1.5f));
        }
        else
        {
            FishTier caughtTier = GetRandomTierBySuccess(successCount);
            Debug.Log($"NẤC {successCount}: Bắt được cá {caughtTier}! Chuyển sang Anim Lôi cá mới!");

            ProcessCatch(caughtTier);
            StartCoroutine(SpawnAndFlyFishRoutine());
            if (playerAnim != null)
            {
                // Chức năng: Xóa lệnh giằng co cá để chuyển mượt mà sang anim lôi cá
                playerAnim.ResetTrigger("StruggleFish");
                playerAnim.SetTrigger("CatchFish");
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Fish_Catch");

            StartCoroutine(ResetFishingStateRoutine(fishFlyDuration + 0.5f));
        }
    }

    private IEnumerator ResetFishingStateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Chức năng: Đứng treo tại đây, đợi Animator thoát hẳn khỏi anim thu cá / trượt cá mới chốt hạ
        Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;
        if (playerAnim != null)
        {
            while (playerAnim.GetCurrentAnimatorStateInfo(0).IsName("CatchFish") ||
                   playerAnim.GetCurrentAnimatorStateInfo(0).IsName("MissFish"))
            {
                yield return null; // Đợi tiếp frame sau...
            }
        }

        currentState = FishingState.NotFishing;
        if (FishingUIManager.Instance != null) FishingUIManager.Instance.ToggleTierPanel(false);
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = false;
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleFishingCamera(false);
    }

    private void ProcessCatch(FishTier tier)
    {
        if (InventoryManager.Instance == null) return;
        if (availableFish != null && availableFish.Count > 0)
        {
            FishItemData caughtFish = null;
            FishTier currentSearchTier = tier;

            while ((int)currentSearchTier >= 0)
            {
                List<FishItemData> possibleFish = new List<FishItemData>();

                foreach (var fish in availableFish)
                {
                    if (IsFishSpawnable(fish) && fish.tier == currentSearchTier)
                    {
                        possibleFish.Add(fish);
                    }
                }

                if (possibleFish.Count > 0)
                {
                    caughtFish = possibleFish[Random.Range(0, possibleFish.Count)];
                    if (currentSearchTier != tier)
                    {
                        Debug.LogWarning($"Không tìm thấy cá bậc {tier}. Đã hạ xuống đền bù cá bậc {currentSearchTier}");
                    }
                    break;
                }

                currentSearchTier--;
            }

            if (caughtFish != null)
            {
                bool success = InventoryManager.Instance.AddItem(caughtFish, 1);

                if (success)
                {
                    if (NotificationManager.Instance != null)
                    {
                        // Tên cá sẽ được xử lý đa ngôn ngữ lúc sửa file ItemData sau
                        NotificationManager.Instance.ShowNotification($"+1 {caughtFish.displayName}");
                    }
                    if (QuestManager.Instance != null)
                    {
                        // Báo cáo chung là có bắt được 1 con cá (Dành cho Nhiệm vụ 11: Cứ câu lên là tính)
                        QuestManager.Instance.ReportAction("Catch_Fish", 1);

                        // Báo cáo cụ thể tên cá (Dành cho sau này lỡ có Quest bắt đích danh cá chép, cá mập...)
                        QuestManager.Instance.ReportAction("CatchFish_" + caughtFish.name, 1);
                    }
                }

                else
                {
                    Debug.LogWarning("Balo đã đầy! Cá câu được đã bị thả đi.");
                    if (NotificationManager.Instance != null)
                    {
                        string fullMsg = locInventoryFull != null && !locInventoryFull.IsEmpty ? locInventoryFull.GetLocalizedString() : "Balo đầy, không thể chứa cá!";
                        NotificationManager.Instance.ShowNotification(fullMsg);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Hồ câu này không có con cá nào hợp lệ với thời tiết hiện tại để câu!");
            }
        }
    }

    private bool IsFishSpawnable(FishItemData fish)
    {
        bool locationMatch = fish.allowedLocations.Contains(FishingLocation.Any) || fish.allowedLocations.Contains(thisLocation);
        if (!locationMatch) return false;

        if (fish.requiredWeather != SpawnWeather.Any)
        {
            bool isRaining = false;
            if (WeatherManager.Instance != null)
            {
                isRaining = (WeatherManager.Instance.currentWeather == WeatherState.Raining);
            }

            if (fish.requiredWeather == SpawnWeather.RainyOnly && !isRaining) return false;
            if (fish.requiredWeather == SpawnWeather.SunnyOnly && isRaining) return false;
        }

        if (fish.requiredTime != SpawnTime.Any)
        {
            TimeSystem timeSys = FindFirstObjectByType<TimeSystem>();
            if (timeSys != null)
            {
                bool isNight = timeSys.hour < 6f || timeSys.hour >= 18f;

                if (fish.requiredTime == SpawnTime.DayOnly && isNight) return false;
                if (fish.requiredTime == SpawnTime.NightOnly && !isNight) return false;
            }
        }

        return true;
    }

    private FishTier GetRandomTierBySuccess(int nac)
    {
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (nac == 1)
        {
            if (roll <= 70f) return FishTier.Common;
            return FishTier.Uncommon;
        }
        if (nac == 2)
        {
            if (roll <= 40f) return FishTier.Common;
            if (roll <= 75f) return FishTier.Uncommon;
            if (roll <= 95f) return FishTier.Rare;
            return FishTier.Epic;
        }
        if (nac == 3)
        {
            if (roll <= 60f) return FishTier.Rare;
            if (roll <= 90f) return FishTier.Epic;
            return FishTier.Legendary;
        }

        return FishTier.Common;
    }
    public string GetInteractText()
    {
        // Lấy chữ mặc định "Khu vực câu cá..."
        string needRodText = locInteractNeedRod != null && !locInteractNeedRod.IsEmpty ? locInteractNeedRod.GetLocalizedString() : "Khu vực Câu cá (Cần cầm Cần Câu)";

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
            return needRodText;

        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

        if (slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)
        {

            if (currentState == FishingState.NotFishing)
            {
                // Chức năng: Đọc biến isBaloFull siêu nhẹ
                if (InventoryManager.Instance.isBaloFull)
                {
                    return locInventoryFull != null && !locInventoryFull.IsEmpty ? locInventoryFull.GetLocalizedString() : "Balo đầy, không thể chứa cá!";
                }

                return locCastRod != null && !locCastRod.IsEmpty ? locCastRod.GetLocalizedString() : "[E] Quăng cần";
            }

            if (currentState == FishingState.WaitingForBite)
                return locCancelFishing != null && !locCancelFishing.IsEmpty ? locCancelFishing.GetLocalizedString() : "[E] Hủy câu";

            if (currentState == FishingState.PlayingMiniGame)
            {
                string roundTxt = locRound != null && !locRound.IsEmpty ? locRound.GetLocalizedString() : "HIỆP";
                return $"{roundTxt} {currentRound}/{maxRounds}";
            }

            if (currentState == FishingState.WaitingNextRound)
            {
                string prepTxt = locPrepareRound != null && !locPrepareRound.IsEmpty ? locPrepareRound.GetLocalizedString() : "Chuẩn bị hiệp";
                return $"{prepTxt} {currentRound + 1}!!!";
            }

            if (currentState == FishingState.Pulling) return "";
        }
        return needRodText;
    }

    public void Interact()
    {
        if (Time.time - lastInteractTime < 1f) return;
        lastInteractTime = Time.time;

        if (!isFishLoaded)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.itemDatabase != null)
            {
                availableFish.Clear();
                // [!] CHÚ Ý: Chữ "items" ở dưới đây phải đúng với tên list trong ItemDatabase.cs của ông
                foreach (var item in InventoryManager.Instance.itemDatabase.allItems)
                {
                    if (item is FishItemData fish)
                    {
                        if (fish.allowedLocations.Contains(FishingLocation.Any) || fish.allowedLocations.Contains(thisLocation))
                        {
                            availableFish.Add(fish);
                        }
                    }
                }
                isFishLoaded = true;
                Debug.Log($"[Hồ câu {thisLocation}] Đã tải trễ thành công {availableFish.Count} loại cá!");
            }
        }
        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1) return;
        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
        if (!(slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)) return;
        if (slot.currentDurability <= 0) return;

        Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;

        if (currentState == FishingState.NotFishing)
        {
            if (PlayerStamina.Instance != null && PlayerStamina.Instance.currentStamina < PlayerStamina.Instance.fishCost)
            {
                if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ShowNotEnoughWarning();
                return;
            }
            // Chức năng: Đọc biến isBaloFull đã được xử lý sẵn bên InventoryManager
            if (InventoryManager.Instance.isBaloFull)
            {
                if (NotificationManager.Instance != null)
                {
                    string fullMsg = locInventoryFull != null && !locInventoryFull.IsEmpty ? locInventoryFull.GetLocalizedString() : "Balo đầy, không thể chứa cá!";
                    NotificationManager.Instance.ShowNotification(fullMsg);
                }
                return;
            }
            if (PlayerStamina.Instance != null) PlayerStamina.Instance.ConsumeStamina(PlayerStamina.Instance.fishCost);

            if (playerAnim != null) playerAnim.ResetTrigger("CancelFishing");

            currentSequenceLength = baseSequenceLength;
            currentTimeLimit = baseTimeLimit;

            currentRound = 0;
            successCount = 0;
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.UpdateTierUI(successCount);

            Transform playerTransform = FindAnyObjectByType<PlayerMovement>().transform;
            dynamicRodTip = null;
            foreach (Transform t in playerTransform.GetComponentsInChildren<Transform>())
            {
                if (t.name == "RodTip")
                {
                    dynamicRodTip = t;
                    break;
                }
            }
            if (dynamicRodTip == null) dynamicRodTip = playerTransform;

            Vector3 castTarget = playerTransform.position + playerTransform.forward * 5f;
            castTarget.y += 20f;
            RaycastHit hit;
            float finalWaterY = waterSurfacePoint != null ? waterSurfacePoint.position.y : playerTransform.position.y;


            if (Physics.Raycast(castTarget, Vector3.down, out hit, 40f, waterLayer))
            {
                finalWaterY = hit.point.y;
            }
            bobberPosition = new Vector3(castTarget.x, finalWaterY, castTarget.z);

            // Chuyển toàn bộ việc sinh phao và dây vào Coroutine để đợi Animation
            StartCoroutine(FlyBobberRoutine(bobberPosition, 1.0f));

            currentState = FishingState.WaitingForBite;
            // Cộng thêm 0.5s vào thời gian chờ cá cắn để bù cho lúc vung cần
            timer = Random.Range(minBiteTime, maxBiteTime) + 0.5f;

            if (FishingUIManager.Instance != null) FishingUIManager.Instance.ToggleTierPanel(true);

            if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = true;
            if (playerAnim != null) playerAnim.SetTrigger("StartFishing");
            if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleFishingCamera(true);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Fish_Cast");
        }
        else if (currentState == FishingState.WaitingForBite)
        {
            if (playerAnim != null) playerAnim.ResetTrigger("StartFishing");

            StopAllCoroutines();
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.ToggleTierPanel(false);
            if (playerAnim != null) playerAnim.SetTrigger("CancelFishing");

            currentState = FishingState.NotFishing;

            if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = false;
            if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleFishingCamera(false);
        }
    }
    private IEnumerator FlyBobberRoutine(Vector3 endPos, float duration)
    {
        // 1. DELAY KHỚP ANIMATION (Chỉnh 0.4f này cho khớp với Frame vung tay tới đỉnh của Model Player)
        yield return new WaitForSeconds(0.4f);

        // 2. TẠO PHAO & DÂY
        Vector3 startPos = dynamicRodTip != null ? dynamicRodTip.position : transform.position;
        if (bobberPrefab != null)
        {
            currentBobber = Instantiate(bobberPrefab, startPos, Quaternion.identity);
        }
        if (fishingLineRenderer != null)
        {
            fishingLineRenderer.positionCount = 15; // Set sẵn đốt để uốn cong
            fishingLineRenderer.enabled = true;
        }

        // 3. BAY RA BIỂN & BÁO HIỆU CHO DÂY ĐỔI CHIỀU CONG
        isCastingBobber = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (currentBobber == null) yield break;

            // Liên tục cập nhật điểm gốc đề phòng nhân vật đang thở/nhúc nhích
            startPos = dynamicRodTip != null ? dynamicRodTip.position : transform.position;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 2.0f; // Phao bay bổng lên trên

            currentBobber.transform.position = currentPos;
            yield return null;
        }

        if (currentBobber != null) currentBobber.transform.position = endPos;
        isCastingBobber = false; // Phao rớt xuống nước -> Tắt cờ bay
    }

    private void OnDisable()
    {
        // Chức năng: Giải phóng trạng thái nhân vật và Camera nếu khu vực câu bị tắt đột ngột
        if (currentState != FishingState.NotFishing)
        {
            if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = false;
            if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleFishingCamera(false);

            if (FishingUIManager.Instance != null)
            {
                FishingUIManager.Instance.ToggleAuditionPanel(false);
                FishingUIManager.Instance.ToggleTimer(false);
                FishingUIManager.Instance.ToggleTierPanel(false);
            }

            if (fishingLineRenderer != null) fishingLineRenderer.enabled = false;
            if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }

            currentState = FishingState.NotFishing;
        }
    }
    private void DrawCurvedFishingLine()
    {
        if (fishingLineRenderer == null || dynamicRodTip == null) return;

        // Điểm đầu là cần câu, điểm cuối là cái phao (hoặc vị trí phao nếu phao chưa sinh ra)
        Vector3 startPos = dynamicRodTip.position;
        Vector3 endPos = currentBobber != null ? currentBobber.transform.position : bobberPosition;

        // Chia sợi dây thành 15 đốt để bẻ cong
        int lineSegments = 15;
        fishingLineRenderer.positionCount = lineSegments;

        float distance = Vector3.Distance(startPos, endPos);

        // 1. Xác định MỤC TIÊU (Target) của độ cong và hướng cong
        float targetSagAmount = distance * 0.15f;
        float targetSagDirection = -1f; // -1 là cong xuống mặt nước (trọng lực bình thường)

        if (isCastingBobber)
        {
            // [VẬT LÝ 1]: Đang ném phao bay trên trời -> Gió cản đẩy dây vút cong lên trên!
            targetSagAmount = distance * 0.1f;
            targetSagDirection = 1f; // +1 là vút lên trên
        }
        else if (currentState == FishingState.PlayingMiniGame || currentState == FishingState.WaitingNextRound)
        {
            // [VẬT LÝ 2]: Cá cắn câu giằng co -> Dây thẳng đét và chìm xuống
            targetSagAmount = distance * 0.02f;
            targetSagDirection = -1f;
        }

        // 2. LERP: Dịch chuyển mượt mà giá trị HIỆN TẠI tiến dần về MỤC TIÊU
        currentSagAmount = Mathf.Lerp(currentSagAmount, targetSagAmount, Time.deltaTime * lineLerpSpeed);
        currentSagDirection = Mathf.Lerp(currentSagDirection, targetSagDirection, Time.deltaTime * lineLerpSpeed);

        // Vẽ từng đốt của sợi dây
        for (int i = 0; i < lineSegments; i++)
        {
            float t = i / (float)(lineSegments - 1);
            Vector3 currentPoint = Vector3.Lerp(startPos, endPos, t);

            // 3. Dùng giá trị current (đã được làm mượt) thay vì target
            float sag = Mathf.Sin(t * Mathf.PI) * currentSagAmount;
            currentPoint.y += sag * currentSagDirection;

            fishingLineRenderer.SetPosition(i, currentPoint);
        }
    }
}