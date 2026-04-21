using UnityEngine;

public class SceneArrivalTrigger : MonoBehaviour
{
    [Header("Cột mốc Cốt truyện (Hoàn thành ngầm)")]
    [Tooltip("Kéo nhiệm vụ mốc (ví dụ: Q_DenRung) vào đây. Cứ vào Scene này là tự động hoàn thành.")]
    public QuestData secretMilestoneQuest;

    private void Start()
    {
        // Vừa load xong Scene (bước xuống xe bus) là chạy hàm này ngay
        if (secretMilestoneQuest != null && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.completedQuests.Contains(secretMilestoneQuest.questID))
            {
                QuestManager.Instance.completedQuests.Add(secretMilestoneQuest.questID);
                Debug.Log($"[Cốt truyện] Đã âm thầm mở khóa cột mốc: {secretMilestoneQuest.questName}");
            }
        }
    }
}