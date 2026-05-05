using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;
[RequireComponent(typeof(NavMeshAgent))]
public class NPCVillager : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactTextNormal; // Chữ bình thường
    public LocalizedString interactTextQuest;

    [Header("Thông tin Dân làng")]
    public string npcName = "Dân Làng";
    public string greetingSound = "Villager_Hello";

    [Header("Hội thoại Mặc định")]
    [Tooltip("Gõ các câu chào vào đây. Gõ \\n để xuống dòng.")]
    public LocalizedString[] defaultDialogues;

    [Header("Chuỗi Nhiệm vụ (Tùy chọn)")]
    public QuestData[] questLine;
    public QuestData secretStoryQuest;

    [Header("Lịch trình & Địa điểm")]
    public NPCSchedule schedule;
    public Transform homePoint;

    [Header("Cài đặt Đi Dạo (Dành cho NPC thường)")]
    public Transform wanderCenter;
    public bool canWander = true;
    public float wanderRadius = 10f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("Cài đặt Ngồi (Dành cho NPC gốc cây/ghế)")]
    [Tooltip("Nếu kéo 1 điểm (Transform) vào đây, NPC sẽ không đi dạo nữa mà ra thẳng đây ngồi tới tối.")]
    public Transform sitPoint;
    public Transform standPoint;


    [Header("Thành phần AI")]
    public Animator npcAnimator;
    private NavMeshAgent agent;

    private bool isSleeping = false;
    private bool isGoingHome = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isInitialized = false;

    // [MỚI]: Biến quản lý trạng thái ngồi
    private bool isCurrentlySitting = false;
    private bool wasTalkingToPlayer = false; // Nhớ xem vừa nói chuyện xong chưa
    private float sitCooldownTimer = 0f;
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

        // Chỉ tạo wanderCenter ảo nếu thằng này KHÔNG CÓ chỗ ngồi
        if (wanderCenter == null && sitPoint == null)
        {
            GameObject tempCenter = new GameObject(npcName + "_TempWanderCenter");
            tempCenter.transform.position = transform.position;
            wanderCenter = tempCenter.transform;
        }

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
                // [ĐÃ SỬA]: Kiểm tra xem nó là loại đi dạo hay loại ngồi gốc cây
                if (sitPoint != null)
                {
                    agent.Warp(sitPoint.position);
                    SnapToSitPoint();
                }
                else
                {
                    agent.Warp(wanderCenter.position);
                }

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
        isInitialized = true;
    }
    private void Update()
    {
        if (!isInitialized) return;
        bool isTalkingToPlayer = DialogueUIManager.Instance != null && DialogueUIManager.Instance.currentVillager == this;

        if (isTalkingToPlayer)
        {
            wasTalkingToPlayer = true; // Đánh dấu là đang buôn chuyện

            if (isCurrentlySitting)
            {
                StandUpFromSitPoint();
            }

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
            // Bắt đúng khoảnh khắc người chơi vừa bấm nút tắt bảng thoại
            if (wasTalkingToPlayer)
            {
                wasTalkingToPlayer = false;
                sitCooldownTimer = 1.5f; // Vặn đồng hồ delay 1.5s
            }

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

                if (sitPoint != null) GoToPoint(sitPoint.position);
                else if (!canWander) GoToPoint(wanderCenter.position);
                else PickNewWanderPoint();
            }

            // [MỚI]: Chia nhánh hành vi ban ngày
            if (sitPoint != null)
            {
                HandleSitting();
            }
            else
            {
                HandleWandering();
            }
        }
        else
        {
            // [MỚI]: Buổi tối -> Đứng dậy đi về
            if (isCurrentlySitting)
            {
                StandUpFromSitPoint();
            }

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

    // ==========================================
    // LOGIC NGỒI GỐC CÂY
    // ==========================================
    private void HandleSitting()
    {
        if (isCurrentlySitting) return;

        if (sitCooldownTimer > 0)
        {
            sitCooldownTimer -= Time.deltaTime;
            return;
        }
        // Bỏ qua trục Y (chiều cao), chỉ đo khoảng cách bề ngang trên mặt đất (X và Z)
        Vector2 npcPosXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 sitPosXZ = new Vector2(sitPoint.position.x, sitPoint.position.z);

        // Nới lỏng khoảng cách ra 0.5f để dễ bắt trúng hơn
        if (Vector2.Distance(npcPosXZ, sitPosXZ) < 0.5f)
        {
            SnapToSitPoint();
        }
        else if (agent.enabled && !agent.pathPending)
        {
            GoToPoint(sitPoint.position);
        }
    }

    private void SnapToSitPoint()
    {
        isCurrentlySitting = true;

        // Tắt AI NavMesh đi để có thể dịch chuyển mông vào ghế/gốc cây
        if (agent.enabled) agent.enabled = false;

        transform.position = sitPoint.position;
        transform.rotation = sitPoint.rotation;
    }

    private void StandUpFromSitPoint()
    {
        isCurrentlySitting = false;
        if (standPoint != null)
        {
            transform.position = standPoint.position;
        }
        // Bật lại AI để đi về nhà
        agent.enabled = true;
    }
    // ==========================================

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

        // Chỉ bật agent nếu ko đang ngồi
        if (isVisible && !isCurrentlySitting) agent.enabled = true;
        else if (!isVisible) agent.enabled = false;
    }

    private void UpdateAnimation()
    {
        if (npcAnimator != null)
        {
            float speed = (agent.enabled && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            npcAnimator.SetFloat("Speed", speed);

            // [MỚI]: Kích hoạt State ngồi trong Animator
            npcAnimator.SetBool("IsSitting", isCurrentlySitting);
        }
    }

    public string GetInteractText()
    {
        if (isSleeping) return "";

        QuestData activeQuest = GetCurrentQuest();
        if (activeQuest != null && QuestManager.Instance.GetQuestStatus(activeQuest) != QuestStatus.Completed)
        {
            // Lấy chữ từ từ điển rồi cộng với tên NPC
            return $"{interactTextQuest.GetLocalizedString()} {npcName}";
        }

        // Lấy chữ từ từ điển rồi cộng với tên NPC
        return $"{interactTextNormal.GetLocalizedString()} {npcName}";
    }

    public void Interact()
    {
        if (isSleeping) return;

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
            // 1. Lọc lấy các câu thoại chào hỏi mặc định (An toàn)
            System.Collections.Generic.List<string> defaultLines = new System.Collections.Generic.List<string>();
            if (defaultDialogues != null)
            {
                for (int i = 0; i < defaultDialogues.Length; i++)
                {
                    if (defaultDialogues[i] != null && !defaultDialogues[i].IsEmpty)
                    {
                        defaultLines.Add(defaultDialogues[i].GetLocalizedString());
                    }
                }
            }

            QuestData activeQuest = GetCurrentQuest();
            System.Collections.Generic.List<string> finalLinesToSay = new System.Collections.Generic.List<string>();
            bool isStoryDialogue = false;

            // 2. KỊCH BẢN 1: Cốt truyện nối tiếp (Auto Story Quest)
            if (activeQuest != null && activeQuest.isAutoStoryQuest)
            {
                QuestStatus status = QuestManager.Instance.GetQuestStatus(activeQuest);

                if (status == QuestStatus.Available)
                {
                    if (questLine != null && questLine.Length > 0 && activeQuest == questLine[0])
                    {
                        finalLinesToSay.AddRange(defaultLines);
                    }
                    // Chức năng: Thêm thoại giao nhiệm vụ
                    finalLinesToSay.AddRange(activeQuest.GetOfferLines());
                }
                else if (status == QuestStatus.InProgress)
                {
                    // Đang làm dở: Bỏ qua chào hỏi, nói thẳng câu nhắc nhở
                    finalLinesToSay.AddRange(activeQuest.GetInProgressLines());
                }
                else if (status == QuestStatus.ReadyToTurnIn)
                {
                    // Trả nhiệm vụ: Bỏ qua chào hỏi, nói thẳng câu cảm ơn
                    finalLinesToSay.AddRange(activeQuest.GetCompleteLines());
                }

                isStoryDialogue = true;
            }
            // 3. KỊCH BẢN 2: Việc vặt (Có nút Quest) hoặc đang rảnh rỗi
            else
            {
                // Chức năng: Tìm nhiệm vụ đã hoàn thành cuối cùng trong mảng questLine
                QuestData lastCompletedQuest = null;
                if (questLine != null)
                {
                    for (int i = questLine.Length - 1; i >= 0; i--)
                    {
                        if (questLine[i] != null && QuestManager.Instance.GetQuestStatus(questLine[i]) == QuestStatus.Completed)
                        {
                            lastCompletedQuest = questLine[i];
                            break;
                        }
                    }
                }

                // Chức năng: Nếu đã hết sạch nhiệm vụ hiện tại và nhiệm vụ cuối là cốt truyện thì lặp lại câu trả nhiệm vụ
                if (activeQuest == null && lastCompletedQuest != null && lastCompletedQuest.isAutoStoryQuest)
                {
                    finalLinesToSay.AddRange(lastCompletedQuest.GetCompleteLines());
                    isStoryDialogue = true;
                }
                else
                {
                    // Chức năng: Nếu chưa làm nhiệm vụ nào hoặc là nhiệm vụ phụ thì nói câu mặc định
                    finalLinesToSay.AddRange(defaultLines);
                }
            }
            // Chốt chặn cuối cùng nếu không có câu thoại nào
            if (finalLinesToSay.Count == 0) finalLinesToSay.Add("...");

            DialogueUIManager.Instance.OpenDialogueForVillager(this, finalLinesToSay.ToArray(), activeQuest, isStoryDialogue);
        }
    }
}