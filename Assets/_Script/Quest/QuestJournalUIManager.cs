using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using System.Collections.Generic;

public class QuestJournalUIManager : MonoBehaviour
{
    public static QuestJournalUIManager Instance;

    public GameObject journalPanel;
    public TextMeshProUGUI journalText; // Kéo text rộng ra để in được nhiều dòng

    [Header("Đa Ngôn Ngữ cho Chữ tĩnh")]
    public LocalizedString emptyJournalText;  // Text "Bạn không có nhiệm vụ..."
    public LocalizedString headerText;        // Text "--- NHIỆM VỤ ĐANG LÀM ---"
    public LocalizedString progressLabelText;

    private PlayerInputHandler playerInput;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (journalPanel != null) journalPanel.SetActive(false);
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        if (IsOpen())
        {
            RefreshJournal();
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Update()
    {
        if (playerInput == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerInput = player.GetComponent<PlayerInputHandler>();
        }

        if (playerInput != null && playerInput.JournalTriggered)
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        bool isOpening = !journalPanel.activeSelf;
        journalPanel.SetActive(isOpening);

        if (isOpening) RefreshJournal();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Click");

        if (PlayerCameraManager.Instance != null)
            PlayerCameraManager.Instance.SetShopOpenState(isOpening); // Khóa chuột khi xem
    }

    public void RefreshJournal()
    {
        if (QuestManager.Instance.activeQuests.Count == 0)
        {
            journalText.text = emptyJournalText.GetLocalizedString();
            return;
        }

        journalText.text = $"<color=yellow>{headerText.GetLocalizedString()}</color>\n\n";

        foreach (QuestData q in QuestManager.Instance.activeQuests)
        {
            string progress = "";

            // [ĐÃ SỬA]: Quét qua danh sách các vật phẩm yêu cầu
            if (q.questType == QuestType.FetchItem && q.requiredItems != null && q.requiredItems.Count > 0)
            {
                List<string> reqList = new List<string>();
                foreach (var req in q.requiredItems)
                {
                    if (req.item != null)
                    {
                        int count = InventoryManager.Instance.GetPersonalItemCount(req.item);
                        reqList.Add($"{count}/{req.amount} {req.item.displayName}");
                    }
                }
                progress = string.Join("\n  - ", reqList);
                if (reqList.Count > 1) progress = "\n  - " + progress; // Thêm căn lề nếu có nhiều dòng
            }
            else if (q.questType == QuestType.Action && q.requiredActions != null)
            {
                List<string> actList = new List<string>();
                foreach (var act in q.requiredActions)
                {
                    int count = 0;
                    string key = q.questID + "_" + act.actionName;
                    if (QuestManager.Instance.actionProgress.ContainsKey(key)) count = QuestManager.Instance.actionProgress[key];

                    string actDesc = act.actionDescription.GetLocalizedString();
                    string displayName = string.IsNullOrEmpty(actDesc) ? act.actionName : actDesc;
                    actList.Add($"{count}/{act.amount} {displayName}");
                }
                progress = string.Join("\n  - ", actList);
                if (actList.Count > 1) progress = "\n  - " + progress;
            }

            journalText.text += $"<b>{q.GetQuestName()}</b>\n{progressLabelText.GetLocalizedString()} {progress}\n\n";
        }
    }

    public bool IsOpen() => journalPanel != null && journalPanel.activeSelf;
}