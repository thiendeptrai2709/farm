using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FishingZone : MonoBehaviour, IInteractable
{
    public enum FishingState { NotFishing, WaitingForBite, PlayingMiniGame, WaitingNextRound, Pulling }
    public FishingState currentState = FishingState.NotFishing;
    public enum ArrowKey { Up, Down, Left, Right }

    [Header("Cài đặt Audition (3 Hiệp)")]
    public int maxRounds = 3;
    public int sequenceLength = 4;
    public float miniGameTimeLimit = 3f;
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
    }

    private void Update()
    {
        // 1. CHỜ CÁ CẮN LẦN ĐẦU TIÊN
        if (currentState == FishingState.WaitingForBite)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                // [THÊM MỚI]: Cá cắn! Chuyển ngay sang Animation Giằng Co (Loop)
                Animator playerAnim = FindAnyObjectByType<PlayerInteraction>().playerAnimator;
                if (playerAnim != null) playerAnim.SetTrigger("StruggleFish");

                StartRound();
            }
        }
        // 2. CHỜ GIỮA CÁC HIỆP 2 VÀ 3
        else if (currentState == FishingState.WaitingNextRound)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) StartRound();
        }
        // 3. ĐANG BẤM AUDITION
        else if (currentState == FishingState.PlayingMiniGame)
        {
            timer -= Time.deltaTime;

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
        currentRound++;
        currentState = FishingState.PlayingMiniGame;
        timer = miniGameTimeLimit;
        currentInputIndex = 0;
        targetSequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            targetSequence.Add((ArrowKey)Random.Range(0, 4));
        }

        if (auditionPanel != null) auditionPanel.SetActive(true);

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (i < sequenceLength)
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

            if (currentInputIndex >= sequenceLength)
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
        if (wonRound)
        {
            successCount++;
            UpdateTierUI();
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

    private void EndFishing()
    {
        if (auditionPanel != null) auditionPanel.SetActive(false);
        if (tierPanel != null) tierPanel.SetActive(false);

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
            if (playerAnim != null) playerAnim.SetTrigger("CatchFish");
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

            if (currentState == FishingState.PlayingMiniGame) return $"HIỆP {currentRound}/3 - CÒN {timer:F1}s";
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
        else if (currentState == FishingState.WaitingForBite)
        {
            currentState = FishingState.Pulling;
            if (auditionPanel != null) auditionPanel.SetActive(false);
            if (tierPanel != null) tierPanel.SetActive(false);
            if (playerAnim != null) playerAnim.SetTrigger("CancelFishing");
        }
    }
}