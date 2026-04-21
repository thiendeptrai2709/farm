using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        byeButton.onClick.AddListener(CloseDialogue);
    }

    private void Update()
    {
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
            // LUỒNG 1: Cốt truyện -> Hiện chữ "Accept", bấm là nhận luôn
            if (btnText != null) btnText.text = "Accept";

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
            // LUỒNG 2: Bình thường -> Hiện chữ "Quest", bấm thì bắt đầu chạy chữ nhờ vặt
            if (btnText != null) btnText.text = "Quest";

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
        string[] questLines = currentAvailableQuest.offerLines;

        if (status == QuestStatus.InProgress) questLines = currentAvailableQuest.inProgressLines;
        else if (status == QuestStatus.ReadyToTurnIn) questLines = currentAvailableQuest.completeLines;

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

    public bool IsOpen() => dialoguePanel != null && dialoguePanel.activeSelf;

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