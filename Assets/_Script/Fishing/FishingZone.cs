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
    private int currentInputIndex = 0;
    private float timer = 0f;
    private float lastInteractTime = 0f;
    private PlayerInputHandler playerInput;
    private bool isFishLoaded = false;
    public FishingLocation thisLocation = FishingLocation.Farm;

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

        for (int i = 0; i < currentSequenceLength; i++)
        {
            targetSequence.Add((ArrowKey)Random.Range(0, 4));
        }

        if (FishingUIManager.Instance != null)
        {
            FishingUIManager.Instance.ToggleAuditionPanel(true);
            FishingUIManager.Instance.ToggleTimer(true, currentTimeLimit, currentTimeLimit);
            FishingUIManager.Instance.SetupArrows(currentSequenceLength, targetSequence);
        }
    }

    private void ProcessInput(ArrowKey pressedKey)
    {
        if (pressedKey == targetSequence[currentInputIndex])
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
        if (waterSurfacePoint == null || genericFishPrefab == null) yield break;

        if (splashEffectPrefab != null)
        {
            GameObject splash = Instantiate(splashEffectPrefab, waterSurfacePoint.position, Quaternion.identity);
            Destroy(splash, 2f);
        }

        GameObject fish = Instantiate(genericFishPrefab, waterSurfacePoint.position, Quaternion.identity);
        Transform playerTransform = FindAnyObjectByType<PlayerMovement>().transform;

        float elapsed = 0f;
        Vector3 startPos = waterSurfacePoint.position;

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
                isRaining = (WeatherManager.Instance.currentWeather.ToString() == "Raining");
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
        if (nac == 1) return (FishTier)Random.Range(0, 2);
        if (nac == 2) return (FishTier)Random.Range(0, 4);
        if (nac == 3) return (FishTier)Random.Range(2, 5);
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

            if (playerAnim != null) playerAnim.ResetTrigger("CancelFishing");

            currentSequenceLength = baseSequenceLength;
            currentTimeLimit = baseTimeLimit;

            currentRound = 0;
            successCount = 0;
            if (FishingUIManager.Instance != null) FishingUIManager.Instance.UpdateTierUI(successCount);

            currentState = FishingState.WaitingForBite;
            timer = Random.Range(minBiteTime, maxBiteTime);

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

            currentState = FishingState.NotFishing;
        }
    }
}