using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;
[RequireComponent(typeof(NavMeshAgent))]
public class NPCMerchant : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactTextNormal;
    public LocalizedString interactTextQuest;

    [Header("Shop Data")]
    public ShopData myShopData;
    public string greetingSound = "Merchant_Hello";

    [Header("Hội thoại Mặc định")]
    [Tooltip("Gõ các câu chào vào đây. Gõ \\n để xuống dòng.")]
    
    public LocalizedString[] defaultDialogues;

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
            (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsOpen() && DialogueUIManager.Instance.currentMerchant == this);

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
            return $"{interactTextQuest.GetLocalizedString()} {shopName}";
        }

        return $"{interactTextNormal.GetLocalizedString()} {shopName}";
    }

    public void Interact()
    {
        if (!isAtWork || isInsideHouse) return;

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

                if (finalLinesToSay.Count == 0) finalLinesToSay.Add("...");

                DialogueUIManager.Instance.OpenDialogueForMerchant(this, finalLinesToSay.ToArray(), activeQuest, isStoryDialogue);
            }
        }
    }
}