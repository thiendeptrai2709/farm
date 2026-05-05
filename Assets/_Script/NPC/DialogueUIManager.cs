using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject optionsPanel;

    [Header("Option Buttons")]
    public Button shopButton;
    public Button questButton;
    public Button byeButton;

    public LocalizedString btnAcceptText;  // Text "Accept" / "Chấp nhận"
    public LocalizedString btnQuestText;

    private PlayerInputHandler playerInput;

    [Header("Cài đặt Chữ chạy")]
    public float typingSpeed = 0.03f;

    public NPCVillager currentVillager { get; private set; }
    public NPCMerchant currentMerchant { get; private set; }
    public Transform currentNPCTransform { get; private set; }

    private string[] currentLines;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private QuestData currentAvailableQuest;

    private bool isPlayingQuestDialogue = false;
    private float inputDelayTimer = 0f;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        byeButton.onClick.AddListener(CloseDialogue);
    }

    private void Update()
    {
        if (inputDelayTimer > 0)
        {
            inputDelayTimer -= Time.deltaTime;
            return;
        }

        if (playerInput == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerInput = player.GetComponent<PlayerInputHandler>();
        }

        bool advanceDialogue = playerInput != null && (playerInput.ClickTriggered || playerInput.InteractTriggered);

        if (dialoguePanel.activeSelf && !optionsPanel.activeSelf && advanceDialogue)
        {
            OnNextLineClicked();
        }
    }

    // ==========================================
    // CHO DÂN LÀNG
    // ==========================================
    public void OpenDialogueForVillager(NPCVillager villager, string[] lines, QuestData quest = null, bool isStoryDialogue = false)
    {
        isPlayingQuestDialogue = isStoryDialogue;
        currentVillager = villager;
        currentMerchant = null;
        currentNPCTransform = villager.transform;
        currentAvailableQuest = quest;

        npcNameText.text = villager.npcName;
        shopButton.gameObject.SetActive(false);

        SetupQuestButton(quest, isStoryDialogue);
        StartDialogue(lines);
    }

    // ==========================================
    // CHO THƯƠNG NHÂN
    // ==========================================
    public void OpenDialogueForMerchant(NPCMerchant merchant, string[] lines, QuestData quest = null, bool isStoryDialogue = false)
    {
        isPlayingQuestDialogue = isStoryDialogue;
        currentMerchant = merchant;
        currentVillager = null;
        npcNameText.text = merchant.myShopData.npcName;
        currentNPCTransform = merchant.transform;
        currentAvailableQuest = quest;

        shopButton.gameObject.SetActive(true);
        shopButton.onClick.RemoveAllListeners();
        shopButton.onClick.AddListener(OpenShopFromDialogue);

        SetupQuestButton(quest, isStoryDialogue);
        StartDialogue(lines);
    }

    // ==========================================
    // TỰ ĐỘNG SETUP NÚT NHIỆM VỤ ("Quest" hay "Accept")
    // ==========================================
    private void SetupQuestButton(QuestData quest, bool isStoryDialogue)
    {
        bool hasActiveQuest = quest != null && QuestManager.Instance.GetQuestStatus(quest) != QuestStatus.Completed;
        questButton.gameObject.SetActive(hasActiveQuest);

        TextMeshProUGUI btnText = questButton.GetComponentInChildren<TextMeshProUGUI>();
        questButton.onClick.RemoveAllListeners();

        if (isStoryDialogue)
        {
            // [ĐÃ SỬA]: Lấy chữ "Accept" từ bảng từ vựng
            if (btnText != null) btnText.text = btnAcceptText.GetLocalizedString();

            questButton.onClick.AddListener(() => {
                if (quest != null && QuestManager.Instance.GetQuestStatus(quest) == QuestStatus.Available)
                {
                    QuestManager.Instance.AcceptQuest(quest);
                }
                TransitionToQuestUI();
            });
        }
        else
        {
            // [ĐÃ SỬA]: Lấy chữ "Quest" từ bảng từ vựng
            if (btnText != null) btnText.text = btnQuestText.GetLocalizedString();

            questButton.onClick.AddListener(StartQuestDialogueSequence);
        }
    }

    private void StartDialogue(string[] lines)
    {
        dialoguePanel.SetActive(true);
        optionsPanel.SetActive(false);

        LockCameraAndCursor(true);
        ToggleGameplayUI(false);

        currentLines = (lines == null || lines.Length == 0) ? new string[] { "..." } : lines;
        currentLineIndex = 0;

        inputDelayTimer = 0.2f;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        dialogueText.text = "";
        foreach (char c in currentLines[currentLineIndex].ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        CheckIfDialogueEnded();
    }

    private void OnNextLineClicked()
    {
        if (dialogueText.text != currentLines[currentLineIndex])
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentLines[currentLineIndex];
            CheckIfDialogueEnded();
        }
        else if (currentLineIndex < currentLines.Length - 1)
        {
            currentLineIndex++;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeLine());
        }
        else if (isPlayingQuestDialogue)
        {
            TransitionToQuestUI();
        }
    }

    private void CheckIfDialogueEnded()
    {
        if (currentLineIndex == currentLines.Length - 1 && !isPlayingQuestDialogue)
        {
            optionsPanel.SetActive(true);
        }
    }

    private void StartQuestDialogueSequence()
    {
        if (currentAvailableQuest == null) return;

        isPlayingQuestDialogue = true;
        QuestStatus status = QuestManager.Instance.GetQuestStatus(currentAvailableQuest);

        string[] questLines = currentAvailableQuest.GetOfferLines();

        if (status == QuestStatus.InProgress) questLines = currentAvailableQuest.GetInProgressLines();
        else if (status == QuestStatus.ReadyToTurnIn) questLines = currentAvailableQuest.GetCompleteLines();

        // [MỚI]: Đổi nút [Quest] thành nút [Accept] khi mạch truyện bắt đầu
        SetupQuestButton(currentAvailableQuest, true);

        StartDialogue(questLines);
    }

    private void TransitionToQuestUI()
    {
        dialoguePanel.SetActive(false);
        if (QuestUIManager.Instance != null && currentAvailableQuest != null)
        {
            QuestUIManager.Instance.OpenQuestUI(currentAvailableQuest, currentNPCTransform);
        }
    }

    private void OpenShopFromDialogue()
    {
        dialoguePanel.SetActive(false);
        if (ShopUIManager.Instance != null && currentMerchant != null)
        {
            ShopUIManager.Instance.OpenShop(currentMerchant.myShopData, currentMerchant.transform);
        }
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentVillager = null;
        currentMerchant = null;
        currentNPCTransform = null;
        isPlayingQuestDialogue = false;

        LockCameraAndCursor(false);
        ToggleGameplayUI(true);
    }

    public bool IsOpen()
    {
        bool isDialoguePanelOpen = dialoguePanel != null && dialoguePanel.activeSelf;
        bool isQuestPanelOpen = QuestUIManager.Instance != null && QuestUIManager.Instance.IsOpen();

        return isDialoguePanelOpen || isQuestPanelOpen;
    }
    private void LockCameraAndCursor(bool isLocked)
    {
        if (PlayerCameraManager.Instance != null)
            PlayerCameraManager.Instance.SetShopOpenState(isLocked);
    }

    private void ToggleGameplayUI(bool isVisible)
    {
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ToggleInGameUI(isVisible);
    }
}