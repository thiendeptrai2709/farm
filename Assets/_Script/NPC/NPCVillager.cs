using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCVillager : MonoBehaviour, IInteractable
{
    [Header("Thông tin Dân làng")]
    public string npcName = "Dân Làng";
    public string greetingSound = "Villager_Hello";

    [Header("Hội thoại Mặc định")]
    [Tooltip("Gõ các câu chào vào đây. Gõ \\n để xuống dòng.")]
    [TextArea(2, 4)]
    public string[] defaultDialogues = new string[]
    {
        "Chào buổi sáng! Dạo này trang trại của cậu phát triển tốt chứ?",
        "Thôi tớ đi dạo tiếp đây, gặp lại sau nhé!"
    };

    [Header("Chuỗi Nhiệm vụ (Tùy chọn)")]
    public QuestData[] questLine; // Khai báo dạng mảng (Nhiều nhiệm vụ)
    public QuestData secretStoryQuest;

    [Header("Lịch trình & Địa điểm")]
    public NPCSchedule schedule;
    public Transform homePoint;
    public Transform wanderCenter;
    public float wanderRadius = 10f;

    [Header("Cài đặt Hành vi")]
    public bool canWander = true;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("Thành phần AI")]
    public Animator npcAnimator;
    private NavMeshAgent agent;

    private bool isSleeping = false;
    private bool isGoingHome = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;
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

            // Kiểm tra xem Quest này đã đủ điều kiện thời gian/cốt truyện chưa
            if (!QuestManager.Instance.IsQuestLogicReady(q)) continue;

            return q;
        }
        return null;
    }

    private void Start()
    {
        Invoke("InitPosition", 0.2f);
    }
    private void InitPosition()
    {
        if (isInitialized) return;
        if (wanderCenter == null)
        {
            GameObject tempCenter = new GameObject(npcName + "_TempWanderCenter");
            tempCenter.transform.position = transform.position;
            wanderCenter = tempCenter.transform;
        }

        // Vẫn phải bắt buộc có nhà để về ngủ
        if (homePoint == null)
        {
            Debug.LogError($"[NPC] Cảnh báo: {npcName} chưa được gắn vị trí nhà (homePoint) trên Inspector!");
            isInitialized = true;
            return;
        }
        TimeSystem timeSys = FindAnyObjectByType<TimeSystem>();
        if (timeSys != null && agent != null)
        {
            float currentTime = timeSys.hour;
            bool isDayTime = currentTime >= schedule.workStartTime && currentTime < schedule.workEndTime;

            if (isDayTime)
            {
                agent.Warp(wanderCenter.position);
                isSleeping = false;
                isGoingHome = false;
                SetNPCVisibility(true);
            }
            else
            {
                agent.Warp(homePoint.position);
                EnterHouse();
            }
        }
        isInitialized = true; // Mở khóa
    }
    private void Update()
    {
        if (!isInitialized) return;
        bool isTalkingToPlayer = DialogueUIManager.Instance != null && DialogueUIManager.Instance.currentVillager == this;

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
        bool isDayTime = currentTime >= schedule.workStartTime && currentTime < schedule.workEndTime;

        if (isDayTime)
        {
            if (isSleeping)
            {
                SetNPCVisibility(true);
                isSleeping = false;
                isGoingHome = false;
                if (!canWander) GoToPoint(wanderCenter.position);
                else PickNewWanderPoint();
            }
            HandleWandering();
        }
        else
        {
            if (!isSleeping)
            {
                if (!isGoingHome)
                {
                    GoToPoint(homePoint.position);
                    isGoingHome = true;
                }

                if (Vector3.Distance(transform.position, homePoint.position) < 0.5f)
                {
                    EnterHouse();
                }
            }
        }
    }

    private void HandleWandering()
    {
        if (!canWander) return;

        if (agent.pathPending || agent.remainingDistance > 0.5f) return;

        if (!isWaiting)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                PickNewWanderPoint();
            }
        }
    }

    private void PickNewWanderPoint()
    {
        if (wanderCenter == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += wanderCenter.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
        {
            GoToPoint(hit.position);
        }
    }

    private void GoToPoint(Vector3 targetPos)
    {
        if (agent.enabled)
        {
            agent.SetDestination(targetPos);
        }
    }

    private void EnterHouse()
    {
        isGoingHome = false;
        isSleeping = true;
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
        if (npcAnimator != null)
        {
            float speed = (agent.enabled && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            npcAnimator.SetFloat("Speed", speed);
        }
    }

    public string GetInteractText()
    {
        if (isSleeping) return "";

        // [ĐÃ SỬA]: Dùng hàm GetCurrentQuest() thay vì myQuest
        QuestData activeQuest = GetCurrentQuest();
        if (activeQuest != null && QuestManager.Instance.GetQuestStatus(activeQuest) != QuestStatus.Completed)
        {
            return $"[E] <color=yellow>!</color> Trò chuyện với {npcName}";
        }

        return $"[E] Trò chuyện với {npcName}";
    }

    public void Interact()
    {
        if (isSleeping) return;

        // Âm thầm hoàn thành cột mốc cốt truyện
        if (secretStoryQuest != null && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.completedQuests.Contains(secretStoryQuest.questID))
            {
                QuestManager.Instance.completedQuests.Add(secretStoryQuest.questID);
            }
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(greetingSound))
        {
            AudioManager.Instance.PlaySFX(greetingSound);
        }

        if (DialogueUIManager.Instance != null)
        {
            QuestData activeQuest = GetCurrentQuest();
            string[] linesToSay = defaultDialogues;
            bool isStoryDialogue = false;

            if (activeQuest != null)
            {
                // [ĐIỂM QUAN TRỌNG]: Tự động phân biệt Nhiệm vụ bình thường và Nhiệm vụ Cốt truyện
                bool isHiddenStoryQuest = (activeQuest.requiredPreviousQuest != null || activeQuest.requiredDay > 0);

                if (isHiddenStoryQuest)
                {
                    // Nếu là cốt truyện: Đọc luôn thoại khóc lóc, bỏ qua câu chào
                    QuestStatus status = QuestManager.Instance.GetQuestStatus(activeQuest);
                    if (status == QuestStatus.Available) linesToSay = activeQuest.offerLines;
                    else if (status == QuestStatus.InProgress) linesToSay = activeQuest.inProgressLines;
                    else if (status == QuestStatus.ReadyToTurnIn) linesToSay = activeQuest.completeLines;

                    isStoryDialogue = true; // Báo cho UI biết đây là chuyện gấp!
                }
            }

            DialogueUIManager.Instance.OpenDialogueForVillager(this, linesToSay, activeQuest, isStoryDialogue);
        }
    }
}