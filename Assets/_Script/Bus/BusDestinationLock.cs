using UnityEngine;
using UnityEngine.UI;

public class BusDestinationLock : MonoBehaviour
{
    [Header("Điều Kiện Mở Khóa")]
    public QuestData requiredQuest;
    public QuestStatus statusToUnlock = QuestStatus.InProgress;

    [Header("Nút bấm cần điều khiển (Kéo nút vào đây)")]
    public Button targetButton; // [ĐÃ SỬA]: Điều khiển từ xa, không tự lấy của bản thân nữa

    private void OnEnable()
    {
        // Script này giờ sẽ không bị chết lâm sàng nữa
        if (requiredQuest == null || targetButton == null || QuestManager.Instance == null) return;

        QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(requiredQuest);
        bool isUnlocked = false;

        if (statusToUnlock == QuestStatus.InProgress)
        {
            if (currentStatus == QuestStatus.InProgress ||
                currentStatus == QuestStatus.ReadyToTurnIn ||
                currentStatus == QuestStatus.Completed)
            {
                isUnlocked = true;
            }
        }
        else if (statusToUnlock == QuestStatus.Completed && currentStatus == QuestStatus.Completed)
        {
            isUnlocked = true;
        }

        // Bật sáng (Interactable) hoặc làm xám nút bấm
        targetButton.interactable = isUnlocked;
    }
}