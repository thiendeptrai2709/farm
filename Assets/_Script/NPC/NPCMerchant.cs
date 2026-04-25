using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMerchant : MonoBehaviour, IInteractable
{
    [Header("Shop Data")]
    public ShopData myShopData;
    public string greetingSound = "Merchant_Hello";

    [Header("Hội thoại Mặc định")]
    [Tooltip("Gõ các câu chào vào đây. Gõ \\n để xuống dòng.")]
    [TextArea(2, 4)]
    public string[] defaultDialogues = new string[]
    {
        "Xin chào! Cậu đến mua hàng hả?",
        "Hôm nay tớ có mấy món hàng mới về xịn lắm đấy."
    };

    [Header("Chuỗi Nhiệm vụ (Tùy chọn)")]
    public QuestData[] questLine;
    public QuestData secretStoryQuest; // [THÊM]: Hoàn thành ngầm cốt truyện

    [Header("Lịch trình & Vị trí")]
    public NPCSchedule schedule;
    public Transform workPoint;
    public Transform homePoint;

    [Header("Thành phần AI")]
    public Animator npcAnimator;
    private NavMeshAgent agent;

    private bool isAtWork = false;
    private bool isGoingHome = false;
    private bool isInsideHouse = false;

    private bool isInitialized = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
    }
    private void OnEnable()
    {
        LoadingManager.OnPlayerReady += InitPosition;
    }

    private void OnDisable()
    {
        LoadingManager.OnPlayerReady -= InitPosition;
    }
    private QuestData GetCurrentQuest()
    {
        if (questLine == null || questLine.Length == 0) return null;

        foreach (QuestData q in questLine)
        {
            if (QuestManager.Instance.GetQuestStatus(q) == QuestStatus.Completed) continue;

            // Dùng hàm check chuẩn của QuestManager
            if (!QuestManager.Instance.IsQuestLogicReady(q)) continue;

            return q;
        }
        return null;
    }

    private void Start()
    {
        // [ĐÃ SỬA] Phòng hờ nếu m test trực tiếp không qua Main Menu
        Invoke("InitPosition", 0.2f);
    }
    private void InitPosition()
    {
        if (isInitialized) return;

        TimeSystem timeSys = FindAnyObjectByType<TimeSystem>();
        if (timeSys != null && agent != null)
        {
            float currentTime = timeSys.hour;
            bool shouldBeAtWork = currentTime >= schedule.workStartTime && currentTime < schedule.workEndTime;

            if (shouldBeAtWork)
            {
                agent.Warp(workPoint.position);
                transform.rotation = workPoint.rotation;
                isAtWork = true;
                isInsideHouse = false;
                isGoingHome = false;
                SetNPCVisibility(true);
            }
            else
            {
                agent.Warp(homePoint.position);
                isAtWork = false;
                EnterHouse();
            }
        }
        isInitialized = true; // Mở khóa
    }

    private void Update()
    {
        if (!isInitialized) return;

        bool isTalkingToPlayer =
            (ShopUIManager.Instance != null && ShopUIManager.Instance.IsOpen() && ShopUIManager.Instance.currentShop == myShopData) ||
            (DialogueUIManager.Instance != null && DialogueUIManager.Instance.currentMerchant == this);

        if (isTalkingToPlayer)
        {
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 lookPos = player.transform.position;
                lookPos.y = transform.position.y;
                Quaternion targetRot = Quaternion.LookRotation(lookPos - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
        else
        {
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            UpdateRoutine();
        }

        UpdateAnimation();
    }

    private void UpdateRoutine()
    {
        TimeSystem timeSys = FindAnyObjectByType<TimeSystem>();
        if (timeSys == null || agent == null) return;

        float currentTime = timeSys.hour;
        bool shouldBeAtWork = currentTime >= schedule.workStartTime && currentTime < schedule.workEndTime;

        if (shouldBeAtWork)
        {
            if (isInsideHouse)
            {
                SetNPCVisibility(true);
                isInsideHouse = false;
            }

            GoToPoint(workPoint);
            isGoingHome = false;

            if (Vector3.Distance(transform.position, workPoint.position) < 0.5f)
            {
                isAtWork = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, workPoint.rotation, Time.deltaTime * 5f);
            }
            else
            {
                isAtWork = false;
            }
        }
        else
        {
            isAtWork = false;

            if (!isInsideHouse)
            {
                GoToPoint(homePoint);
                isGoingHome = true;

                if (Vector3.Distance(transform.position, homePoint.position) < 0.5f)
                {
                    EnterHouse();
                }
            }
        }
    }

    private void GoToPoint(Transform target)
    {
        if (target != null && agent.enabled)
        {
            agent.SetDestination(target.position);
        }
    }

    private void EnterHouse()
    {
        isGoingHome = false;
        isInsideHouse = true;
        SetNPCVisibility(false);
    }

    private void SetNPCVisibility(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = isVisible;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;

        agent.enabled = isVisible;
    }

    private void UpdateAnimation()
    {
        if (npcAnimator != null && agent.enabled)
        {
            float currentSpeed = agent.velocity.magnitude;
            npcAnimator.SetFloat("Speed", currentSpeed);
        }
        else if (npcAnimator != null && !agent.enabled)
        {
            npcAnimator.SetFloat("Speed", 0f);
        }
    }

    public string GetInteractText()
    {
        if (!isAtWork || isInsideHouse) return "";

        string shopName = myShopData != null ? myShopData.npcName : "Cửa hàng";

        QuestData activeQuest = GetCurrentQuest();
        if (activeQuest != null)
        {
            return $"[E] <color=yellow>!</color> Giao dịch với {shopName}";
        }

        return $"[E] Giao dịch với {shopName}";
    }

    public void Interact()
    {
        if (!isAtWork || isInsideHouse) return;

        // 1. Âm thầm hoàn thành cột mốc cốt truyện
        if (secretStoryQuest != null && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.completedQuests.Contains(secretStoryQuest.questID))
            {
                QuestManager.Instance.completedQuests.Add(secretStoryQuest.questID);
            }
        }

        if (myShopData != null)
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(greetingSound))
                AudioManager.Instance.PlaySFX(greetingSound);

            if (DialogueUIManager.Instance != null)
            {
                QuestData activeQuest = GetCurrentQuest();
                string[] linesToSay = defaultDialogues;
                bool isStoryDialogue = false;

                // [MỚI]: Logic phân loại nhiệm vụ cốt truyện giống hệt NPCVillager
                if (activeQuest != null)
                {
                    bool isHiddenStoryQuest = (activeQuest.requiredPreviousQuest != null || activeQuest.requiredDay > 0);

                    if (isHiddenStoryQuest)
                    {
                        QuestStatus status = QuestManager.Instance.GetQuestStatus(activeQuest);
                        if (status == QuestStatus.Available) linesToSay = activeQuest.offerLines;
                        else if (status == QuestStatus.InProgress) linesToSay = activeQuest.inProgressLines;
                        else if (status == QuestStatus.ReadyToTurnIn) linesToSay = activeQuest.completeLines;

                        isStoryDialogue = true;
                    }
                }

                // Gọi hộp thoại với chế độ có thể mở bảng Quest trực tiếp
                DialogueUIManager.Instance.OpenDialogueForMerchant(this, linesToSay, activeQuest, isStoryDialogue);
            }
        }
    }
}