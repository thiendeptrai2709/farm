using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
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

    [Header("Giao diện UI Audition")]
    public GameObject auditionPanel;
    public Image[] arrowSlots;
    public Sprite upSprite, downSprite, leftSprite, rightSprite;
    public Color normalColor = Color.white;
    public Color successColor = Color.green;

    [Header("Giao diện UI Nấc (Tier Panel)")]
    public GameObject tierPanel;
    public Image[] tierHighlights;
    public Color tierLockedColor = Color.gray;
    public Color tierUnlockedColor = Color.yellow;

    public FishItemData[] availableFish;
    public Slider timerSlider;

    [Header("Hiệu ứng bắt cá (Visuals)")]
    public Transform waterSurfacePoint;    // Điểm mặt nước (Cá sẽ nhảy lên từ đây)
    public GameObject genericFishPrefab;   // Model 3D con cá mặc định
    public GameObject splashEffectPrefab;  // (Tùy chọn) Hiệu ứng hạt nước bắn lên
    public float fishFlyDuration = 1.0f;

    [Header("Cài đặt Độ khó Gốc (DDA)")]
    public int baseSequenceLength = 4;       // Nút mặc định (Mới quăng cần)
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

    private void Start()
    {
        if (auditionPanel != null) auditionPanel.SetActive(false);
        if (tierPanel != null) tierPanel.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentState == FishingState.NotFishing)
        {
            if (tierPanel != null && tierPanel.activeSelf) tierPanel.SetActive(false);
        }

        if (currentState == FishingState.WaitingForBite)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;
                if (playerAnim != null) playerAnim.SetTrigger("StruggleFish");

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
            if (timerSlider != null) timerSlider.value = timer;

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
        // [ĐÃ SỬA]: Chốt chặn an toàn cho UI. Nếu panel chỉ có 6 ô mà code đòi 7 nút thì khóa về 6
        if (arrowSlots != null && currentSequenceLength > arrowSlots.Length)
        {
            currentSequenceLength = arrowSlots.Length;
        }

        currentRound++;
        currentState = FishingState.PlayingMiniGame;
        timer = currentTimeLimit; // Dùng thời gian HIỆN TẠI
        currentInputIndex = 0;
        targetSequence.Clear();

        for (int i = 0; i < currentSequenceLength; i++) // Đẻ nút theo độ khó HIỆN TẠI
        {
            targetSequence.Add((ArrowKey)Random.Range(0, 4));
        }

        if (auditionPanel != null) auditionPanel.SetActive(true);
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(true);
            timerSlider.maxValue = currentTimeLimit;
            timerSlider.value = currentTimeLimit;
        }

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (i < currentSequenceLength)
            {
                arrowSlots[i].gameObject.SetActive(true);
                arrowSlots[i].color = normalColor;
                arrowSlots[i].sprite = GetSpriteForArrow(targetSequence[i]);
            }
            else arrowSlots[i].gameObject.SetActive(false);
        }
    }

    private void ProcessInput(ArrowKey pressedKey)
    {
        if (pressedKey == targetSequence[currentInputIndex])
        {
            if (arrowSlots.Length > currentInputIndex && arrowSlots[currentInputIndex] != null)
                arrowSlots[currentInputIndex].color = successColor;

            currentInputIndex++;

            if (currentInputIndex >= currentSequenceLength)
            {
                RoundFinished(true);
            }
        }
        else
        {
            Debug.Log($"Hiệp {currentRound}: Bấm sai nút! Tạch!");
            RoundFinished(false);
        }
    }

    private void RoundFinished(bool wonRound)
    {
        // [ĐÃ SỬA]: LOGIC TIẾN/LÙI ĐỘ KHÓ SIÊU MƯỢT
        if (wonRound)
        {
            successCount++;
            UpdateTierUI();

            // [LOGIC MỚI - KHI THẮNG]: 
            // Ưu tiên tăng nút lên tối đa 5 trước. Đạt 5 nút rồi thì mới bắt đầu rút thời gian.
            if (currentSequenceLength < 5)
            {
                currentSequenceLength++; // Tăng thêm 1 nút
                // Thời gian giữ nguyên
            }
            else
            {
                // Đã đạt max 5 nút -> Bắt đầu ép thời gian (tối thiểu 1.5s)
                currentTimeLimit = Mathf.Clamp(currentTimeLimit - 0.5f, 1.5f, 5f);
            }

            Debug.Log($"[THẮNG] Cân bằng -> Nút: {currentSequenceLength}, Giờ: {currentTimeLimit}s");
        }
        else
        {
            // [LOGIC MỚI - KHI THUA]: 
            // Nhả dần độ khó ra. Ưu tiên trả lại thời gian trước, nếu thời gian đã ở mức gốc rồi thì mới trừ bớt nút.
            if (currentTimeLimit < baseTimeLimit)
            {
                // Trả lại 0.5s thời gian (tối đa bằng mức gốc)
                currentTimeLimit = Mathf.Clamp(currentTimeLimit + 0.5f, 1.5f, baseTimeLimit);
            }
            else
            {
                // Trừ đi 1 nút (tối thiểu là 3 nút cho dễ)
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
            if (auditionPanel != null) auditionPanel.SetActive(false);
            if (timerSlider != null) timerSlider.gameObject.SetActive(false);
        }
    }

    private void UpdateTierUI()
    {
        if (tierHighlights == null || tierHighlights.Length < 4) return;

        for (int i = 0; i < tierHighlights.Length; i++)
        {
            if (i <= successCount)
                tierHighlights[i].color = tierUnlockedColor;
            else
                tierHighlights[i].color = tierLockedColor;
        }
    }
    private IEnumerator SpawnAndFlyFishRoutine()
    {
        if (waterSurfacePoint == null || genericFishPrefab == null) yield break;

        // 1. Tạo hiệu ứng bọt nước (Nếu có)
        if (splashEffectPrefab != null)
        {
            GameObject splash = Instantiate(splashEffectPrefab, waterSurfacePoint.position, Quaternion.identity);
            Destroy(splash, 2f); // Bọt nước tự tan sau 2s
        }

        // 2. Spawn Model con cá ra
        GameObject fish = Instantiate(genericFishPrefab, waterSurfacePoint.position, Quaternion.identity);
        Transform playerTransform = FindAnyObjectByType<PlayerMovement>().transform;

        float elapsed = 0f;
        Vector3 startPos = waterSurfacePoint.position;

        // 3. Cho cá bay vòng cung (Parabola) vào mặt player
        while (elapsed < fishFlyDuration)
        {
            if (fish == null) break;

            elapsed += Time.deltaTime;
            float t = elapsed / fishFlyDuration;

            // Tính điểm đến (Vào ngực nhân vật)
            Vector3 targetPos = playerTransform.position + Vector3.up * 1.5f;

            // Tính toán đường cong Parabola (Nhô cao lên 2 mét rồi rớt xuống)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 2.0f;

            fish.transform.position = currentPos;

            // Xoay vòng vòng lộn nhào cho sinh động
            fish.transform.Rotate(Vector3.right * 500 * Time.deltaTime);

            yield return null; // Đợi 1 frame
        }

        // 4. Bay tới tay người chơi thì tự biến mất
        if (fish != null) Destroy(fish);
    }
    private void EndFishing()
    {
        if (auditionPanel != null) auditionPanel.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false);

        Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;
        currentState = FishingState.Pulling;

        // TỔNG KẾT SAU 3 HIỆP
        if (successCount == 0)
        {
            Debug.Log("NẤC 0: Tạch toàn tập. Chuyển sang Anim Trượt!");
            if (playerAnim != null) playerAnim.SetTrigger("MissFish");
        }
        else
        {
            FishTier caughtTier = GetRandomTierBySuccess(successCount);
            Debug.Log($"NẤC {successCount}: Bắt được cá {caughtTier}! Chuyển sang Anim Lôi cá mới!");

            ProcessCatch(caughtTier);
            StartCoroutine(SpawnAndFlyFishRoutine());
            if (playerAnim != null) playerAnim.SetTrigger("CatchFish");
        }
    }

    private void ProcessCatch(FishTier tier)
    {
        if (InventoryManager.Instance == null) return;

        // 1. TRỪ ĐỘ BỀN CẦN CÂU
        int selectedIndex = InventoryManager.Instance.selectedHotbarIndex;
        if (selectedIndex != -1)
        {
            InventorySlot slot = InventoryManager.Instance.hotbarSlots[selectedIndex];
            if (slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)
            {
                slot.currentDurability -= 1;
                Debug.Log($"Cần câu trừ 1 độ bền. Còn lại: {slot.currentDurability}");
            }
        }

        // 2. CỘNG CÁ VÀO BALO
        if (availableFish != null && availableFish.Length > 0)
        {
            List<FishItemData> possibleFish = new List<FishItemData>();
            foreach (var fish in availableFish)
            {
                if (fish.tier == tier) possibleFish.Add(fish);
            }

            if (possibleFish.Count > 0)
            {
                FishItemData caughtFish = possibleFish[Random.Range(0, possibleFish.Count)];
                InventoryManager.Instance.AddItem(caughtFish, 1);
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowNotification($"+1 {caughtFish.displayName}");
                }
            }
            else
            {
                Debug.LogWarning($"Lỗi: Không tìm thấy Data cá nào thuộc Tier {tier} trong mảng availableFish!");
            }
        }
    }

    private FishTier GetRandomTierBySuccess(int nac)
    {
        if (nac == 1) return (FishTier)Random.Range(0, 2);
        if (nac == 2) return (FishTier)Random.Range(0, 4);
        if (nac == 3) return (FishTier)Random.Range(2, 5);
        return FishTier.Common;
    }

    private Sprite GetSpriteForArrow(ArrowKey key)
    {
        switch (key)
        {
            case ArrowKey.Up: return upSprite;
            case ArrowKey.Down: return downSprite;
            case ArrowKey.Left: return leftSprite;
            case ArrowKey.Right: return rightSprite;
            default: return null;
        }
    }

    public string GetInteractText()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1)
            return "Khu vực Câu cá (Cần cầm Cần Câu)";

        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];

        if (slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)
        {
            if (slot.currentDurability <= 0) return "Cần câu đã hỏng!";

            if (currentState == FishingState.NotFishing) return "[E] Quăng cần";
            if (currentState == FishingState.WaitingForBite) return "[E] Hủy câu";

            if (currentState == FishingState.PlayingMiniGame) return $"HIỆP {currentRound}/3";
            if (currentState == FishingState.WaitingNextRound) return $"Chuẩn bị hiệp {currentRound + 1}!!!";

            if (currentState == FishingState.Pulling) return "";
        }
        return "Khu vực Câu cá (Cần cầm Cần Câu)";
    }

    public void Interact()
    {
        if (Time.time - lastInteractTime < 0.5f) return;
        lastInteractTime = Time.time;

        if (InventoryManager.Instance == null || InventoryManager.Instance.selectedHotbarIndex == -1) return;
        InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
        if (!(slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)) return;
        if (slot.currentDurability <= 0) return;

        Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;

        if (currentState == FishingState.NotFishing)
        {
            // [ĐÃ SỬA]: RESET MỌI CHỈ SỐ VỀ BASE MỖI KHI QUĂNG CẦN MỚI
            currentSequenceLength = baseSequenceLength;
            currentTimeLimit = baseTimeLimit;

            currentRound = 0;
            successCount = 0;
            UpdateTierUI();

            currentState = FishingState.WaitingForBite;
            timer = Random.Range(minBiteTime, maxBiteTime);

            if (tierPanel != null) tierPanel.SetActive(true);

            if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = true;
            if (playerAnim != null) playerAnim.SetTrigger("StartFishing");
            if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleFishingCamera(true);
        }
        else if (currentState == FishingState.WaitingForBite || currentState == FishingState.WaitingNextRound || currentState == FishingState.PlayingMiniGame)
        {
            currentState = FishingState.Pulling;
            if (auditionPanel != null) auditionPanel.SetActive(false);
            if (tierPanel != null) tierPanel.SetActive(false);
            if (timerSlider != null) timerSlider.gameObject.SetActive(false);
            if (playerAnim != null) playerAnim.SetTrigger("CancelFishing");
        }
    }
}