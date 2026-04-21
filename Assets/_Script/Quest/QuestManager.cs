using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    public List<QuestData> allQuestsDatabase = new List<QuestData>();


    [Header("Danh sách quản lý (Dùng ID)")]
    public List<QuestData> activeQuests = new List<QuestData>();
    public List<string> completedQuests = new List<string>();
    public Dictionary<string, int> actionProgress = new Dictionary<string, int>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Chuyển map không bị mất dữ liệu
        }
        else Destroy(gameObject);
    }
    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            LoadQuestData(SaveManager.Instance.GetCurrentData());
        }
    }
    // 1. NHẬN NHIỆM VỤ
    public void AcceptQuest(QuestData quest)
    {
        if (!activeQuests.Contains(quest) && !completedQuests.Contains(quest.questID))
        {
            activeQuests.Add(quest);
            if (quest.questType == QuestType.Action) actionProgress[quest.questID] = 0; // Reset tiến độ

            Debug.Log($"Đã nhận nhiệm vụ: {quest.questName}");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Quest_Accept");
        }
    }

    // 2. TRẢ NHIỆM VỤ
    public void TurnInQuest(QuestData quest)
    {
        if (activeQuests.Contains(quest) && CheckQuestCondition(quest))
        {
            // Trừ đồ trong túi người chơi
            if (quest.requiredItem != null && quest.requiredAmount > 0)
            {
                InventoryManager.Instance.ConsumePersonalItems(quest.requiredItem, quest.requiredAmount);
            }

            // Trả thưởng Tiền
            if (quest.coinReward > 0)
            {
                InventoryManager.Instance.AddItem(MarketManager.Instance.coinItem, quest.coinReward, false);
            }

            // Trả thưởng Đồ vật (Ví dụ: Thưởng hạt giống, cúp vàng...)
            if (quest.itemReward != null && quest.itemRewardAmount > 0)
            {
                InventoryManager.Instance.AddItem(quest.itemReward, quest.itemRewardAmount, false);
            }

            // Cập nhật danh sách
            activeQuests.Remove(quest);
            completedQuests.Add(quest.questID);

            Debug.Log($"Đã hoàn thành nhiệm vụ: {quest.questName}");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Coin_Trade");
        }
    }

    // 3. KIỂM TRA XEM CÓ ĐỦ ĐIỀU KIỆN TRẢ CHƯA (Đủ đồ trong túi chưa)
    public bool CheckQuestCondition(QuestData quest)
    {
        if (quest.questType == QuestType.FetchItem)
        {
            if (quest.requiredItem == null || quest.requiredAmount <= 0) return true;
            return InventoryManager.Instance.GetPersonalItemCount(quest.requiredItem) >= quest.requiredAmount;
        }
        else
        {
            if (actionProgress.ContainsKey(quest.questID))
            {
                return actionProgress[quest.questID] >= quest.requiredAmount;
            }
            return false;
        }
    }
    public void ReportAction(string actionName, int amount = 1)
    {
        foreach (QuestData q in activeQuests)
        {
            if (q.questType == QuestType.Action && q.requiredAction == actionName)
            {
                if (!actionProgress.ContainsKey(q.questID)) actionProgress[q.questID] = 0;
                actionProgress[q.questID] += amount;
                Debug.Log($"Tiến độ {q.questName}: {actionProgress[q.questID]}/{q.requiredAmount}");

                // Refresh UI Nhật ký nếu đang mở
                if (QuestJournalUIManager.Instance != null && QuestJournalUIManager.Instance.IsOpen())
                    QuestJournalUIManager.Instance.RefreshJournal();
            }
        }
    }
    public QuestStatus GetQuestStatus(QuestData quest)
    {
        if (quest == null) return QuestStatus.Completed; // Hoặc một trạng thái vô hình nào đó

        if (completedQuests.Contains(quest.questID))
            return QuestStatus.Completed;

        if (activeQuests.Contains(quest))
        {
            if (CheckQuestCondition(quest))
                return QuestStatus.ReadyToTurnIn;
            else
                return QuestStatus.InProgress;
        }

        return QuestStatus.Available;
    }
    public bool IsQuestLogicReady(QuestData quest)
    {
        if (quest == null) return false;

        // Check nhiệm vụ trước
        if (quest.requiredPreviousQuest != null)
        {
            if (GetQuestStatus(quest.requiredPreviousQuest) != QuestStatus.Completed)
                return false;
        }

        // Check ngày
        if (TimeManager.Instance != null && quest.requiredDay > 0)
        {
            if (TimeManager.Instance.daysInGame < quest.requiredDay)
                return false;
        }

        return true;
    }
    public void SaveQuestData(GameData data)
    {
        if (data.activeQuestIDs == null) data.activeQuestIDs = new List<string>();
        if (data.completedQuestIDs == null) data.completedQuestIDs = new List<string>();
        if (data.actionProgressList == null) data.actionProgressList = new List<SavedQuestProgress>();

        data.activeQuestIDs.Clear();
        foreach (var q in activeQuests)
        {
            // Check an toàn lỡ trong list có phần tử Null
            if (q != null && !string.IsNullOrEmpty(q.questID))
                data.activeQuestIDs.Add(q.questID);
        }

        data.completedQuestIDs = new List<string>(completedQuests);

        data.actionProgressList.Clear();
        // [ĐÃ SỬA] Dùng vòng lặp an toàn hơn để ép kiểu Dictionary sang List
        List<string> keys = new List<string>(actionProgress.Keys);
        foreach (string key in keys)
        {
            SavedQuestProgress sqp = new SavedQuestProgress();
            sqp.questID = key;
            sqp.progress = actionProgress[key];
            data.actionProgressList.Add(sqp);
        }
        Debug.Log("[QuestManager] Đã đóng gói xong tiến độ nhiệm vụ.");
    }

    public void LoadQuestData(GameData data)
    {
        if (data == null) return;

        activeQuests.Clear();
        if (data.activeQuestIDs != null)
        {
            foreach (string id in data.activeQuestIDs)
            {
                QuestData q = allQuestsDatabase.Find(x => x != null && x.questID == id);
                if (q != null) activeQuests.Add(q);
            }
        }

        if (data.completedQuestIDs != null)
        {
            completedQuests = new List<string>(data.completedQuestIDs);
        }

        actionProgress.Clear();
        if (data.actionProgressList != null)
        {
            foreach (var item in data.actionProgressList)
            {
                // Kiểm tra kỹ tránh nhét Null vào Dictionary làm lỗi
                if (item != null && !string.IsNullOrEmpty(item.questID))
                    actionProgress[item.questID] = item.progress;
            }
        }
        Debug.Log("[QuestManager] Đã tải xong tiến độ nhiệm vụ từ File.");
    }
}