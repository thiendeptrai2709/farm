using UnityEngine;
using TMPro;

public class QuestJournalUIManager : MonoBehaviour
{
    public static QuestJournalUIManager Instance;

    public GameObject journalPanel;
    public TextMeshProUGUI journalText; // Kéo text rộng ra để in được nhiều dòng

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
            journalText.text = "Bạn không có nhiệm vụ nào đang nhận.";
            return;
        }

        journalText.text = "<color=yellow>--- NHIỆM VỤ ĐANG LÀM ---</color>\n\n";

        foreach (QuestData q in QuestManager.Instance.activeQuests)
        {
            string progress = "";
            if (q.questType == QuestType.FetchItem && q.requiredItem != null)
            {
                int count = InventoryManager.Instance.GetPersonalItemCount(q.requiredItem);
                progress = $"{count}/{q.requiredAmount} {q.requiredItem.displayName}";
            }
            else if (q.questType == QuestType.Action)
            {
                int count = 0;
                if (QuestManager.Instance.actionProgress.ContainsKey(q.questID)) count = QuestManager.Instance.actionProgress[q.questID];

                // [ĐÃ SỬA]: Lấy actionDescription ra in
                string displayName = string.IsNullOrEmpty(q.actionDescription) ? q.requiredAction : q.actionDescription;
                progress = $"{count}/{q.requiredAmount} {displayName}";
            }

            journalText.text += $"<b>{q.questName}</b>\n> Tiến độ: {progress}\n\n";
        }
    }

    public bool IsOpen() => journalPanel != null && journalPanel.activeSelf;
}