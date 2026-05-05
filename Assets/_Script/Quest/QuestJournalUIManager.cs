using UnityEngine;
using TMPro;
using UnityEngine.Localization;
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
            // [ĐÃ SỬA]: Kéo chữ trống ra từ bảng dịch
            journalText.text = emptyJournalText.GetLocalizedString();
            return;
        }

        // [ĐÃ SỬA]: Kéo tiêu đề từ bảng dịch
        journalText.text = $"<color=yellow>{headerText.GetLocalizedString()}</color>\n\n";

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

                // [ĐÃ SỬA]: Lấy actionDescription thông qua hàm GetActionDescription()
                string actDesc = q.GetActionDescription();
                string displayName = string.IsNullOrEmpty(actDesc) ? q.requiredAction : actDesc;
                progress = $"{count}/{q.requiredAmount} {displayName}";
            }

            // [ĐÃ SỬA]: Gọi GetQuestName() và lấy chữ "Tiến độ" từ bảng dịch
            journalText.text += $"<b>{q.GetQuestName()}</b>\n{progressLabelText.GetLocalizedString()} {progress}\n\n";
        }
    }

    public bool IsOpen() => journalPanel != null && journalPanel.activeSelf;
}