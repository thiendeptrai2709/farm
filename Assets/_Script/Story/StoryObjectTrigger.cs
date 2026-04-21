using UnityEngine;

public class StoryObjectTrigger : MonoBehaviour
{
    [Header("Theo dõi Cốt truyện")]
    public QuestData targetQuest;

    private void Start()
    {
        CheckCondition(); // Vừa vô game phải check ngay
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay += CheckCondition;
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay -= CheckCondition;
    }

    public void CheckCondition()
    {
        if (targetQuest == null || QuestManager.Instance == null) return;

        QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(targetQuest);

        // 1. Kiểm tra xem người chơi đã Nhận (InProgress) hoặc đang đi Trả (ReadyToTurnIn) chưa?
        bool isConditionMet = (currentStatus == QuestStatus.InProgress || currentStatus == QuestStatus.ReadyToTurnIn);

        // 2. Nếu CHƯA NHẬN (Available), phải kiểm tra thêm xem ĐÃ TỚI NGÀY chưa và ĐÃ XONG NV TRƯỚC chưa?
        if (currentStatus == QuestStatus.Available)
        {
            bool isActuallyAvailable = true;

            // Check Nhiệm vụ trước
            if (targetQuest.requiredPreviousQuest != null)
            {
                if (QuestManager.Instance.GetQuestStatus(targetQuest.requiredPreviousQuest) != QuestStatus.Completed)
                {
                    isActuallyAvailable = false; // Bị kẹt cốt truyện
                }
            }

            // Check Ngày
            if (TimeManager.Instance != null && targetQuest.requiredDay > 0)
            {
                if (TimeManager.Instance.daysInGame < targetQuest.requiredDay)
                {
                    isActuallyAvailable = false; // Chưa tới ngày
                }
            }

            // Nếu vượt qua cả 2 bài test trên -> Đủ điều kiện tàng hình
            if (isActuallyAvailable) isConditionMet = true;
        }

        // TÀNG HÌNH: Nếu đã đủ điều kiện (Tới ngày 2 hoặc đã nhận NV)
        if (isConditionMet && currentStatus != QuestStatus.Completed)
        {
            gameObject.SetActive(false); // Cậu bé bốc hơi!
        }
    }
}