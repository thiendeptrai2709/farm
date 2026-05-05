using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization; // [THÊM MỚI] Khai báo thư viện ngôn ngữ

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

        // [ĐÃ SỬA]: Gọi hàm hỗ trợ từ QuestData để lấy chữ đã dịch
        questNameText.text = quest.GetQuestName();
        descriptionText.text = quest.GetDescription();

        // Hiển thị tiến độ thu thập
        if (quest.questType == QuestType.FetchItem && quest.requiredItem != null)
        {
            int currentAmount = InventoryManager.Instance.GetPersonalItemCount(quest.requiredItem);
            string colorHex = currentAmount >= quest.requiredAmount ? "#00FF00" : "#FF0000";

            // Lấy chữ "Yêu cầu:" từ từ điển
            requirementText.text = $"{reqLabelText.GetLocalizedString()} {quest.requiredItem.displayName} (<color={colorHex}>{currentAmount}/{quest.requiredAmount}</color>)";
        }
        else if (quest.questType == QuestType.Action)
        {
            int currentAmount = 0;
            if (QuestManager.Instance.actionProgress.ContainsKey(quest.questID))
                currentAmount = QuestManager.Instance.actionProgress[quest.questID];

            string colorHex = currentAmount >= quest.requiredAmount ? "#00FF00" : "#FF0000";

            // [ĐÃ SỬA]: Lấy actionDescription đã dịch ra
            string actDesc = quest.GetActionDescription();
            string displayName = string.IsNullOrEmpty(actDesc) ? quest.requiredAction : actDesc;

            requirementText.text = $"{reqLabelText.GetLocalizedString()} {displayName} (<color={colorHex}>{currentAmount}/{quest.requiredAmount}</color>)";
        }
        else
        {
            requirementText.text = justTalkText.GetLocalizedString();
        }

        // Hiển thị phần thưởng
        rewardText.text = $"{rewardLabelText.GetLocalizedString()}";
        if (quest.coinReward > 0) rewardText.text += $"- {quest.coinReward}{goldText.GetLocalizedString()}\n";

        // (ItemData sẽ cần được gắn Localization sau nếu m muốn dịch cả tên đồ vật)
        if (quest.itemReward != null) rewardText.text += $"- {quest.itemRewardAmount}x {quest.itemReward.displayName}";

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