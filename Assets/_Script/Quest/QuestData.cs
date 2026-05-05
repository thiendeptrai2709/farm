using UnityEngine;
using UnityEngine.Localization; // Khai báo thư viện ngôn ngữ

// Định nghĩa 4 trạng thái của một nhiệm vụ
public enum QuestStatus
{
    Available,      // Có sẵn để nhận
    InProgress,     // Đang làm (chưa đủ đồ)
    ReadyToTurnIn,  // Đã đủ đồ, chờ trả
    Completed       // Đã hoàn thành, không nhận lại được nữa
}
public enum QuestType
{
    FetchItem,
    Action
}
[CreateAssetMenu(fileName = "New Quest", menuName = "RPG/Nhiệm Vụ Mới")]
public class QuestData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string questID; // KHÔNG DỊCH (Mã định danh hệ thống)
    public bool isAutoStoryQuest = false;

    public LocalizedString questName;
    public LocalizedString description;
    public QuestData requiredPreviousQuest;
    public int requiredDay = 0;

    [Header("Yêu cầu (Thu thập)")]
    public QuestType questType;
    public ItemData requiredItem;
    public int requiredAmount;

    [Header("Phần thưởng")]
    public int coinReward;
    public ItemData itemReward;
    public int itemRewardAmount;
    public string requiredAction; // KHÔNG DỊCH (Mã action nội bộ)
    public LocalizedString actionDescription;

    [Header("Kịch bản Hội thoại")]
    public LocalizedString[] offerLines;
    public LocalizedString[] inProgressLines;
    public LocalizedString[] completeLines;

    // ==========================================
    // CÁC HÀM HỖ TRỢ CHUYỂN ĐỔI (Đảm bảo logic cũ không lỗi)
    // ==========================================

    public string GetQuestName() => questName.GetLocalizedString();
    public string GetDescription() => description.GetLocalizedString();
    public string GetActionDescription() => actionDescription.GetLocalizedString();

    public string[] GetOfferLines()
    {
        if (offerLines == null || offerLines.Length == 0) return new string[0];
        string[] lines = new string[offerLines.Length];
        for (int i = 0; i < offerLines.Length; i++) lines[i] = offerLines[i].GetLocalizedString();
        return lines;
    }

    public string[] GetInProgressLines()
    {
        if (inProgressLines == null || inProgressLines.Length == 0) return new string[0];
        string[] lines = new string[inProgressLines.Length];
        for (int i = 0; i < inProgressLines.Length; i++) lines[i] = inProgressLines[i].GetLocalizedString();
        return lines;
    }

    public string[] GetCompleteLines()
    {
        if (completeLines == null || completeLines.Length == 0) return new string[0];
        string[] lines = new string[completeLines.Length];
        for (int i = 0; i < completeLines.Length; i++) lines[i] = completeLines[i].GetLocalizedString();
        return lines;
    }
}