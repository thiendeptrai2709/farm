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
            if (quest.questType == QuestType.Action && quest.requiredActions != null)
            {
                foreach (var act in quest.requiredActions)
                {
                    actionProgress[quest.questID + "_" + act.actionName] = 0;
                }
            }
            Debug.Log($"Đã nhận nhiệm vụ: {quest.questName}");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Quest_Accept");
        }
    }

    // 2. TRẢ NHIỆM VỤ
    public void TurnInQuest(QuestData quest)
    {
        if (activeQuests.Contains(quest) && CheckQuestCondition(quest))
        {
            // 1. Thu hồi vật phẩm
            if (quest.questType == QuestType.FetchItem && quest.requiredItems != null)
            {
                foreach (var req in quest.requiredItems)
                {
                    if (req.item != null && req.amount > 0 && req.consumeItem)
                    {
                        InventoryManager.Instance.ConsumePersonalItems(req.item, req.amount);
                    }
                }
            }

            // 2. Trả thưởng Tiền
            if (quest.coinReward > 0)
            {
                InventoryManager.Instance.AddItem(MarketManager.Instance.coinItem, quest.coinReward, false);
            }

            // 3. Trả thưởng Đồ vật (Quét danh sách để cộng nhiều món)
            if (quest.itemRewards != null)
            {
                foreach (var reward in quest.itemRewards)
                {
                    if (reward.item != null && reward.amount > 0)
                    {
                        InventoryManager.Instance.AddItem(reward.item, reward.amount, false);
                    }
                }
            }

            // Cập nhật danh sách
            activeQuests.Remove(quest);
            completedQuests.Add(quest.questID);

            if (TimeManager.Instance != null)
            {
                actionProgress[quest.questID + "_CompletedDay"] = TimeManager.Instance.daysInGame;
            }
            Debug.Log($"Đã hoàn thành nhiệm vụ: {quest.questName}");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Coin_Trade");
        }
    }

    // 3. KIỂM TRA XEM CÓ ĐỦ ĐIỀU KIỆN TRẢ CHƯA (Đủ đồ trong túi chưa)
    // [ĐÃ FIX]: Dọn dẹp lỗi copy lặp hàm
    public bool CheckQuestCondition(QuestData quest)
    {
        if (quest.questType == QuestType.FetchItem)
        {
            // Nếu không yêu cầu gì thì coi như thỏa mãn
            if (quest.requiredItems == null || quest.requiredItems.Count == 0) return true;

            // Quét qua toàn bộ danh sách đồ yêu cầu
            foreach (var req in quest.requiredItems)
            {
                if (req.item != null && InventoryManager.Instance.GetPersonalItemCount(req.item) < req.amount)
                {
                    return false; // Chỉ cần thiếu 1 loại vật phẩm thôi là kẹt ngay, trả về false
                }
            }
            return true; // Đã đếm đủ số lượng của tất cả các món đồ
        }
        else if (quest.questType == QuestType.Action)
        {
            if (quest.requiredActions == null || quest.requiredActions.Count == 0) return true;
            foreach (var act in quest.requiredActions)
            {
                string key = quest.questID + "_" + act.actionName;

                int progress = actionProgress.ContainsKey(key) ? actionProgress[key] : 0;
                if (progress < act.amount) return false;
            }
            return true;
        }
        return false;
    }

    public void ReportAction(string actionName, int amount = 1)
    {
        bool hasChanged = false;
        foreach (QuestData q in activeQuests)
        {
            if (q.questType == QuestType.Action && q.requiredActions != null)
            {
                foreach (var act in q.requiredActions)
                {
                    if (act.actionName == actionName)
                    {
                        string key = q.questID + "_" + actionName;
                        if (!actionProgress.ContainsKey(key)) actionProgress[key] = 0;
                        actionProgress[key] += amount;
                        hasChanged = true;
                        Debug.Log($"Tiến độ {q.questName}: {actionProgress[key]}/{act.amount}");
                    }
                }
            }
        }
        if (hasChanged && QuestJournalUIManager.Instance != null && QuestJournalUIManager.Instance.IsOpen())
            QuestJournalUIManager.Instance.RefreshJournal();
    }
    public QuestStatus GetQuestStatus(QuestData quest)
    {
        if (quest == null) return QuestStatus.Completed;

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
            if (q != null && !string.IsNullOrEmpty(q.questID))
                data.activeQuestIDs.Add(q.questID);
        }

        data.completedQuestIDs = new List<string>(completedQuests);

        data.actionProgressList.Clear();
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
                if (q != null)
                {
                    activeQuests.Add(q);
                }
                else
                {
                    Debug.LogError($"[QuestManager] LỖI NGHIÊM TRỌNG: Nhiệm vụ ID '{id}' đã được lưu trong file Save, nhưng KHÔNG TÌM THẤY trong allQuestsDatabase! Hãy kiểm tra và kéo file ScriptableObject của nhiệm vụ này vào danh sách của QuestManager.");
                }
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
                if (item != null && !string.IsNullOrEmpty(item.questID))
                    actionProgress[item.questID] = item.progress;
            }
        }
        Debug.Log("[QuestManager] Đã tải xong tiến độ nhiệm vụ từ File.");
    }
}