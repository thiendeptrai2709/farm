using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;

public class QuestUIManager : MonoBehaviour
{
    public static QuestUIManager Instance;

    [Header("UI Panels")]
    public GameObject questPanel;

    [Header("Văn bản Hiển thị")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI requirementText;
    public TextMeshProUGUI rewardText;

    [Header("Đa Ngôn Ngữ cho Chữ tĩnh")]
    [Tooltip("Tạo Key trong bảng từ vựng cho các chữ cố định")]
    public LocalizedString reqLabelText;       // "Yêu cầu: " / "Requirement: "
    public LocalizedString rewardLabelText;    // "Phần thưởng:\n" / "Rewards:\n"
    public LocalizedString goldText;           // " Vàng" / " Gold"
    public LocalizedString justTalkText;       // "Chỉ cần đến gặp là xong!" / "Just talk to them!"

    [Header("Nút bấm")]
    public Button acceptButton;
    public Button completeButton;
    public Button closeButton;

    private QuestData currentDisplayingQuest;
    private Transform currentNPCTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (questPanel != null) questPanel.SetActive(false);

        acceptButton.onClick.AddListener(OnAcceptClicked);
        completeButton.onClick.AddListener(OnCompleteClicked);
        closeButton.onClick.AddListener(CloseEverythingAndUnlockCamera);
    }

    public void OpenQuestUI(QuestData quest, Transform npcTransform)
    {
        currentDisplayingQuest = quest;
        currentNPCTransform = npcTransform;

        questNameText.text = quest.GetQuestName();
        descriptionText.text = quest.GetDescription();

        // ==========================================
        // 1. HIỂN THỊ TIẾN ĐỘ THU THẬP
        // ==========================================
        if (quest.questType == QuestType.FetchItem && quest.requiredItems != null && quest.requiredItems.Count > 0)
        {
            requirementText.text = $"{reqLabelText.GetLocalizedString()}\n";

            foreach (var req in quest.requiredItems)
            {
                if (req.item != null)
                {
                    int currentAmount = InventoryManager.Instance.GetPersonalItemCount(req.item);
                    string colorHex = currentAmount >= req.amount ? "#00FF00" : "#FF0000";

                    requirementText.text += $"- {req.item.displayName} (<color={colorHex}>{currentAmount}/{req.amount}</color>)\n";
                }
            }
        }
        else if (quest.questType == QuestType.Action && quest.requiredActions != null)
        {
            requirementText.text = $"{reqLabelText.GetLocalizedString()}\n";
            foreach (var act in quest.requiredActions)
            {
                int currentAmount = 0;
                string key = quest.questID + "_" + act.actionName;
                if (QuestManager.Instance.actionProgress.ContainsKey(key))
                    currentAmount = QuestManager.Instance.actionProgress[key];

                string colorHex = currentAmount >= act.amount ? "#00FF00" : "#FF0000";

                string actDesc = act.actionDescription.GetLocalizedString();
                string displayName = string.IsNullOrEmpty(actDesc) ? act.actionName : actDesc;

                requirementText.text += $"- {displayName} (<color={colorHex}>{currentAmount}/{act.amount}</color>)\n";
            }
        }
        else
        {
            requirementText.text = justTalkText.GetLocalizedString();
        }

        // ==========================================
        // 2. HIỂN THỊ PHẦN THƯỞNG
        // ==========================================
        rewardText.text = $"{rewardLabelText.GetLocalizedString()}";
        if (quest.coinReward > 0) rewardText.text += $"- {quest.coinReward}{goldText.GetLocalizedString()}\n";

        // [ĐÃ SỬA]: Quét qua danh sách các phần thưởng
        if (quest.itemRewards != null)
        {
            foreach (var reward in quest.itemRewards)
            {
                if (reward.item != null && reward.amount > 0)
                {
                    rewardText.text += $"- {reward.amount}x {reward.item.displayName}\n";
                }
            }
        }

        // Cập nhật trạng thái nút bấm
        QuestStatus status = QuestManager.Instance.GetQuestStatus(quest);

        acceptButton.gameObject.SetActive(status == QuestStatus.Available);
        completeButton.gameObject.SetActive(status == QuestStatus.ReadyToTurnIn);

        if (status == QuestStatus.InProgress)
        {
            acceptButton.gameObject.SetActive(false);
            completeButton.gameObject.SetActive(false);
        }

        questPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
    }

    private void OnAcceptClicked()
    {
        if (currentDisplayingQuest != null)
        {
            QuestManager.Instance.AcceptQuest(currentDisplayingQuest);
            CloseEverythingAndUnlockCamera();
        }
    }

    private void OnCompleteClicked()
    {
        if (currentDisplayingQuest != null)
        {
            QuestManager.Instance.TurnInQuest(currentDisplayingQuest);
            CloseEverythingAndUnlockCamera();
        }
    }

    public void CloseEverythingAndUnlockCamera()
    {
        currentDisplayingQuest = null;
        currentNPCTransform = null;
        questPanel.SetActive(false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.CloseDialogue();
        }
    }

    public bool IsOpen() => questPanel != null && questPanel.activeSelf;
    public Transform GetNPCTransform() => currentNPCTransform;
}