using UnityEngine;

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
    public string questID; // Ví dụ: "Q01_Intro" (Bắt buộc phải không trùng nhau)
    public string questName;
    [TextArea(2, 4)]
    public string description;
    public QuestData requiredPreviousQuest;
    public int requiredDay = 0;
    
    [Header("Yêu cầu (Thu thập)")]
    public QuestType questType;
    public ItemData requiredItem; // Món đồ NPC cần (Dùng ItemData hiện có của bạn)
    public int requiredAmount;    // Số lượng cần

    [Header("Phần thưởng")]
    public int coinReward;        // Thưởng bao nhiêu tiền
    public ItemData itemReward;   // (Tùy chọn) Thưởng vật phẩm
    public int itemRewardAmount;
    public string requiredAction;
    public string actionDescription;

    [Header("Kịch bản Hội thoại")]
    [Tooltip("NPC nói gì để giao nhiệm vụ này?")]
    public string[] offerLines;

    [Tooltip("NPC nói gì khi bạn đang làm mà chưa có đủ đồ?")]
    public string[] inProgressLines;

    [Tooltip("NPC nói gì khi bạn trả nhiệm vụ thành công?")]
    public string[] completeLines;
}