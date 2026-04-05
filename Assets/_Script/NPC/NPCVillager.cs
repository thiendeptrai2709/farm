using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCVillager : MonoBehaviour, IInteractable
{
    [Header("Thông tin Dân làng")]
    public string npcName = "Dân Làng";
    public string greetingSound = "Villager_Hello";

    [Header("Lịch trình & Địa điểm")]
    public NPCSchedule schedule;       // Tái sử dụng file Lịch trình cũ
    public Transform homePoint;        // Cửa nhà để đi ngủ
    public Transform wanderCenter;     // Tâm điểm của khu vực nó hay đi dạo (VD: Giữa quảng trường)
    public float wanderRadius = 10f;   // Bán kính đi dạo (Đi loanh quanh tâm điểm bao xa)

    [Header("Cài đặt Hành vi")]
    public float minWaitTime = 2f;     // Đứng im tối thiểu bao nhiêu giây rồi đi tiếp
    public float maxWaitTime = 5f;     // Đứng im tối đa bao nhiêu giây

    [Header("Thành phần AI")]
    public Animator npcAnimator;
    private NavMeshAgent agent;

    // Các biến đếm và trạng thái
    private bool isSleeping = false;
    private bool isGoingHome = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateRoutine();
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
            // --- SÁNG THỨC DẬY VÀ ĐI LANG THANG ---
            if (isSleeping)
            {
                SetNPCVisibility(true);
                isSleeping = false;
                isGoingHome = false;
                PickNewWanderPoint(); // Ngủ dậy thì chọn 1 điểm để đi dạo luôn
            }

            // Xử lý đi lang thang
            HandleWandering();
        }
        else
        {
            // --- CHIỀU TỐI LẾCH THẾCH VỀ NHÀ ---
            if (!isSleeping)
            {
                if (!isGoingHome)
                {
                    GoToPoint(homePoint.position);
                    isGoingHome = true;
                }

                // Nếu đã đi bộ đến tận cửa nhà
                if (Vector3.Distance(transform.position, homePoint.position) < 0.5f)
                {
                    EnterHouse();
                }
            }
        }
    }

    private void HandleWandering()
    {
        // 1. Nếu đang trên đường đi (chưa tới đích) thì thôi không làm gì cả
        if (agent.pathPending || agent.remainingDistance > 0.5f) return;

        // 2. Tới đích rồi -> Chuyển sang trạng thái Đứng chờ (Ngắm cảnh)
        if (!isWaiting)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime); // Random thời gian đứng chơi
        }

        // 3. Đếm ngược thời gian đứng chơi
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                // Hết giờ ngắm cảnh -> Chốt điểm đến mới và đi tiếp!
                isWaiting = false;
                PickNewWanderPoint();
            }
        }
    }

    private void PickNewWanderPoint()
    {
        if (wanderCenter == null) return;

        // Bốc đại 1 tọa độ ngẫu nhiên trong vòng tròn bán kính wanderRadius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += wanderCenter.position; // Lấy tâm điểm làm gốc

        NavMeshHit hit;
        // Kiểm tra xem cái điểm vừa bốc có nằm trên đường đi được (NavMesh) không
        // Bán kính dò tìm là 5f. Nếu thấy điểm hợp lệ thì lưu vào 'hit'
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

    // --- LOGIC TƯƠNG TÁC CHÀO HỎI ---
    public string GetInteractText()
    {
        if (isSleeping) return "";
        return $"[E] Trò chuyện với {npcName}";
    }

    public void Interact()
    {
        if (isSleeping) return;

        // Đứng lại nhìn Player
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 lookPos = player.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        // Phát tiếng chào
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(greetingSound))
        {
            AudioManager.Instance.PlaySFX(greetingSound);
        }

        // --- CODE MỞ BẢNG HỘI THOẠI CỦA M Ở ĐÂY ---
        Debug.Log($"[{npcName}]: Chào buổi sáng! Hôm nay trời đẹp quá ha!");

        // Chào xong thì cho nó đi tiếp (Hoặc m có thể đợi tắt UI hội thoại rồi mới cho đi)
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }
}