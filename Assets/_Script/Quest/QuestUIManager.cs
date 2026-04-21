using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Nút bấm")]
    public Button acceptButton;
    public Button completeButton;
    public Button closeButton;

    private QuestData currentDisplayingQuest;
    private Transform currentNPCTransform; // Để đo khoảng cách tự đóng UI

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (questPanel != null) questPanel.SetActive(false);

        // Gán sự kiện cho các nút (Đều trỏ về hàm đóng an toàn)
        acceptButton.onClick.AddListener(OnAcceptClicked);
        completeButton.onClick.AddListener(OnCompleteClicked);
        closeButton.onClick.AddListener(CloseEverythingAndUnlockCamera);
    }

    // Hàm do DialogueUIManager gọi để mở bảng Nhiệm vụ lên
    public void OpenQuestUI(QuestData quest, Transform npcTransform)
    {
        currentDisplayingQuest = quest;
        currentNPCTransform = npcTransform;

        // Điền chữ vào giao diện
        questNameText.text = quest.questName;
        descriptionText.text = quest.description;

        // Hiển thị tiến độ thu thập
        if (quest.questType == QuestType.FetchItem && quest.requiredItem != null)
        {
            int currentAmount = InventoryManager.Instance.GetPersonalItemCount(quest.requiredItem);
            string colorHex = currentAmount >= quest.requiredAmount ? "#00FF00" : "#FF0000";
            requirementText.text = $"Yêu cầu: {quest.requiredItem.displayName} (<color={colorHex}>{currentAmount}/{quest.requiredAmount}</color>)";
        }
        else if (quest.questType == QuestType.Action)
        {
            int currentAmount = 0;
            if (QuestManager.Instance.actionProgress.ContainsKey(quest.questID))
                currentAmount = QuestManager.Instance.actionProgress[quest.questID];

            string colorHex = currentAmount >= quest.requiredAmount ? "#00FF00" : "#FF0000";

            // [ĐÃ SỬA]: Lấy actionDescription ra in thay vì requiredAction
            string displayName = string.IsNullOrEmpty(quest.actionDescription) ? quest.requiredAction : quest.actionDescription;
            requirementText.text = $"Nhiệm vụ: {displayName} (<color={colorHex}>{currentAmount}/{quest.requiredAmount}</color>)";
        }
        else
        {
            requirementText.text = "Chỉ cần đến gặp là xong!";
        }

        // Hiển thị phần thưởng
        rewardText.text = "Phần thưởng:\n";
        if (quest.coinReward > 0) rewardText.text += $"- {quest.coinReward} Vàng\n";
        if (quest.itemReward != null) rewardText.text += $"- {quest.itemRewardAmount}x {quest.itemReward.displayName}";

        // Bật/Tắt nút tùy theo trạng thái
        QuestStatus status = QuestManager.Instance.GetQuestStatus(quest);

        acceptButton.gameObject.SetActive(status == QuestStatus.Available);
        completeButton.gameObject.SetActive(status == QuestStatus.ReadyToTurnIn);

        // Nếu đang làm dở (InProgress), tắt cả 2 nút
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
        questPanel.SetActive(false); // Tắt bảng Quest

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        // Gọi thằng DialogueManager để nó gỡ lệnh Khóa Camera và bật lại thanh Balo/Hotbar
        if (DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.CloseDialogue();
        }
    }

    public bool IsOpen() => questPanel != null && questPanel.activeSelf;
    public Transform GetNPCTransform() => currentNPCTransform;
}