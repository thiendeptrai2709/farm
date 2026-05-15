using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCVillager : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactTextNormal; // Chữ bình thường
    public LocalizedString localizedNpcName;

    [Header("Thông tin Dân làng")]
    public string npcName
    {
        get
        {
            return localizedNpcName.IsEmpty ? gameObject.name : localizedNpcName.GetLocalizedString();
        }
    }

    public string greetingSound = "Villager_Hello";

    [Header("Hội thoại Mặc định")]
    [Tooltip("Gõ các câu chào vào đây. Gõ \\n để xuống dòng.")]
    public LocalizedString[] defaultDialogues;

    [Header("Chuỗi Nhiệm vụ (Tùy chọn)")]
    public QuestData[] questLine;
    public QuestData secretStoryQuest;
    public QuestData disableAfterQuestCompleted;

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

    [Header("Cài đặt Nhìn (IK)")]
    public float lookRadius = 5f;
    private Transform playerTransform;
    private float currentLookWeight = 0f;

    private bool isSleeping = false;
    private bool isGoingHome = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isInitialized = false;

    // Biến quản lý trạng thái ngồi
    private bool isCurrentlySitting = false;
    private bool wasTalkingToPlayer = false;
    private float sitCooldownTimer = 0f;

    private Chair currentDynamicChair;
    private int currentChairSeatIndex = -1;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        LoadingManager.OnPlayerReady += InitPosition;
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay += InitPosition;
    }

    private void OnDisable()
    {
        LoadingManager.OnPlayerReady -= InitPosition;
        if (TimeManager.Instance != null) TimeManager.Instance.OnNewDay -= InitPosition;
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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void InitPosition()
    {
        if (isInitialized) return;

        if (disableAfterQuestCompleted != null && QuestManager.Instance != null)
        {
            if (QuestManager.Instance.GetQuestStatus(disableAfterQuestCompleted) == QuestStatus.Completed)
            {
                gameObject.SetActive(false);
                isInitialized = true;
                return;
            }
        }

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
                if (sitPoint != null)
                {
                    agent.Warp(sitPoint.position);
                    // [ĐÃ SỬA LỖI]: Truyền đúng biến sitPoint vào hàm
                    SnapToSitPoint(sitPoint);
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
            wasTalkingToPlayer = true;

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
            if (wasTalkingToPlayer)
            {
                wasTalkingToPlayer = false;
                sitCooldownTimer = 1.5f;
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

            if (sitPoint != null || currentDynamicChair != null)
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

        Transform targetSitPoint = sitPoint;
        if (targetSitPoint == null && currentDynamicChair != null)
        {
            targetSitPoint = currentDynamicChair.sitPoints[currentChairSeatIndex];
        }

        if (targetSitPoint == null) return;

        Vector2 npcPosXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 sitPosXZ = new Vector2(targetSitPoint.position.x, targetSitPoint.position.z);

        if (Vector2.Distance(npcPosXZ, sitPosXZ) < 0.5f)
        {
            SnapToSitPoint(targetSitPoint);
        }
        else if (agent.enabled && !agent.pathPending)
        {
            GoToPoint(targetSitPoint.position);
        }
    }

    private bool TryFindAndTargetChair()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, wanderRadius);
        foreach (Collider hit in hits)
        {
            Chair chair = hit.GetComponentInParent<Chair>();
            if (chair == null) chair = hit.GetComponent<Chair>();

            if (chair != null)
            {
                int seatIndex = chair.NPCTryOccupy();
                if (seatIndex != -1) // Có ghế trống!
                {
                    currentDynamicChair = chair;
                    currentChairSeatIndex = seatIndex;
                    return true;
                }
            }
        }
        return false;
    }

    // [ĐÃ SỬA LỖI]: Khai báo hàm chuẩn xác có nhận biến Transform
    private void SnapToSitPoint(Transform targetPoint)
    {
        isCurrentlySitting = true;

        if (agent.enabled) agent.enabled = false;

        transform.position = targetPoint.position;
        transform.rotation = targetPoint.rotation;
    }

    private void StandUpFromSitPoint()
    {
        isCurrentlySitting = false;

        Transform targetStandPoint = standPoint;

        if (currentDynamicChair != null)
        {
            targetStandPoint = currentDynamicChair.exitPoint;
            currentDynamicChair.LeaveChair(currentChairSeatIndex);
            currentDynamicChair = null;
            currentChairSeatIndex = -1;
        }

        if (targetStandPoint != null)
        {
            transform.position = targetStandPoint.position;
        }

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

                if (Random.value < 0.3f && TryFindAndTargetChair())
                {
                    return;
                }

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

        if (isVisible && !isCurrentlySitting) agent.enabled = true;
        else if (!isVisible) agent.enabled = false;
    }

    private void UpdateAnimation()
    {
        if (npcAnimator != null)
        {
            float speed = (agent.enabled && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            npcAnimator.SetFloat("Speed", speed);

            npcAnimator.SetBool("IsSitting", isCurrentlySitting);
        }
    }

    public string GetInteractText()
    {
        if (isSleeping) return "";

        string interactStr = interactTextNormal.IsEmpty ? "[E] Nói chuyện" : interactTextNormal.GetLocalizedString();
        return $"{interactStr} {npcName}";
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

            if (activeQuest != null && activeQuest.isAutoStoryQuest)
            {
                QuestStatus status = QuestManager.Instance.GetQuestStatus(activeQuest);

                if (status == QuestStatus.Available)
                {
                    if (questLine != null && questLine.Length > 0 && activeQuest == questLine[0])
                    {
                        if (activeQuest.requiredPreviousQuest == null)
                        {
                            finalLinesToSay.AddRange(defaultLines);
                        }
                    }
                    finalLinesToSay.AddRange(activeQuest.GetOfferLines());
                }
                else if (status == QuestStatus.InProgress)
                {
                    finalLinesToSay.AddRange(activeQuest.GetInProgressLines());
                }
                else if (status == QuestStatus.ReadyToTurnIn)
                {
                    finalLinesToSay.AddRange(activeQuest.GetCompleteLines());
                }

                isStoryDialogue = true;
            }
            else
            {
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

                if (activeQuest == null && lastCompletedQuest != null && lastCompletedQuest.isAutoStoryQuest)
                {
                    finalLinesToSay.AddRange(lastCompletedQuest.GetCompleteLines());
                    isStoryDialogue = true;
                }
                else
                {
                    finalLinesToSay.AddRange(defaultLines);
                }
            }
            if (finalLinesToSay.Count == 0) finalLinesToSay.Add("...");

            DialogueUIManager.Instance.OpenDialogueForVillager(this, finalLinesToSay.ToArray(), activeQuest, isStoryDialogue);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (npcAnimator == null || !isInitialized) return;

        if (isSleeping)
        {
            npcAnimator.SetLookAtWeight(0f);
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        bool isTalking = false;
        if (DialogueUIManager.Instance != null)
        {
            isTalking = (DialogueUIManager.Instance.currentVillager == this);
        }

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (isTalking || (distanceToPlayer <= lookRadius && angleToPlayer < 75f))
        {
            currentLookWeight = Mathf.Lerp(currentLookWeight, 1f, Time.deltaTime * 4f);
        }
        else
        {
            currentLookWeight = Mathf.Lerp(currentLookWeight, 0f, Time.deltaTime * 3f);
        }

        npcAnimator.SetLookAtWeight(currentLookWeight, 0.1f, 0.8f, 1f, 0.5f);

        npcAnimator.SetLookAtPosition(playerTransform.position + Vector3.up * 1.5f);
    }

    private void OnDrawGizmosSelected()
    {
        if (canWander)
        {
            Gizmos.color = Color.green;
            Vector3 centerPos = (wanderCenter != null) ? wanderCenter.position : transform.position;
            Gizmos.DrawWireSphere(centerPos, wanderRadius);
        }

        if (sitPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(sitPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, sitPoint.position);
        }

        if (standPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(standPoint.position, 0.3f);
        }

        if (homePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(homePoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, homePoint.position);
        }
    }
}