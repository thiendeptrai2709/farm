using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] // Tự động yêu cầu NPC phải có NavMeshAgent
public class NPCMerchant : MonoBehaviour, IInteractable
{
    [Header("Shop Data")]
    public ShopData myShopData;
    public string greetingSound = "Merchant_Hello";

    [Header("Lịch trình & Vị trí")]
    public NPCSchedule schedule;       // Gọi cái gói dữ liệu Lịch trình ở file kia sang
    public Transform workPoint;        // Cục Empty đặt ở sạp hàng
    public Transform homePoint;        // Cục Empty đặt trước cửa nhà

    [Header("Thành phần AI")]
    public Animator npcAnimator;
    private NavMeshAgent agent;

    // Trạng thái hiện tại
    private bool isAtWork = false;     // Đang đứng mở sạp
    private bool isGoingHome = false;  // Đang trên đường đi bộ về
    private bool isInsideHouse = false;// Đã tàng hình vào trong nhà

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // --- [ĐÃ SỬA]: KIỂM TRA XEM CÓ ĐANG GIAO DỊCH VỚI PLAYER KHÔNG ---
        bool isTalkingToPlayer = ShopUIManager.Instance != null &&
                                 ShopUIManager.Instance.IsOpen() &&
                                 ShopUIManager.Instance.currentShop == myShopData;

        if (isTalkingToPlayer)
        {
            // 1. Đang có khách: Khóa chân lại không cho bỏ đi về giữa chừng
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;

            // 2. Liên tục xoay mặt mượt mà nhìn theo Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 lookPos = player.transform.position;
                lookPos.y = transform.position.y; // Khóa trục Y để không bị nghển cổ
                Quaternion targetRot = Quaternion.LookRotation(lookPos - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Hết khách: Mở khóa chân và tiếp tục chạy Lịch trình hàng ngày
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

        // Kiểm tra xem giờ này có đang trong ca làm việc không
        bool shouldBeAtWork = currentTime >= schedule.workStartTime && currentTime < schedule.workEndTime;

        if (shouldBeAtWork)
        {
            // --- SÁNG ĐI LÀM ---
            if (isInsideHouse)
            {
                SetNPCVisibility(true); // Hiện hình lại
                isInsideHouse = false;
            }

            GoToPoint(workPoint);
            isGoingHome = false;

            // Kiểm tra xem đã tới sạp hàng chưa (Khoảng cách < 0.5m)
            if (Vector3.Distance(transform.position, workPoint.position) < 0.5f)
            {
                isAtWork = true;
                // Từ từ xoay mặt giống hướng của cái WorkPoint
                transform.rotation = Quaternion.Slerp(transform.rotation, workPoint.rotation, Time.deltaTime * 5f);
            }
            else
            {
                isAtWork = false; // Đang đi trên đường thì chưa bán hàng
            }
        }
        else
        {
            // --- CHIỀU VỀ NHÀ ---
            isAtWork = false;

            if (!isInsideHouse)
            {
                GoToPoint(homePoint);
                isGoingHome = true;

                // Kiểm tra xem đã đi tới cửa nhà chưa
                if (Vector3.Distance(transform.position, homePoint.position) < 0.5f)
                {
                    EnterHouse(); // Chui tọt vào nhà
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
        SetNPCVisibility(false); // Tắt hình ảnh và va chạm
    }

    private void SetNPCVisibility(bool isVisible)
    {
        // Ẩn/Hiện hình nhân vật
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = isVisible;

        // Tắt/Bật va chạm để người chơi không bị kẹt vào NPC vô hình
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;

        // Tắt/Bật AI đi đường
        agent.enabled = isVisible;
    }

    private void UpdateAnimation()
    {
        // Kiểm tra xem NPC có Animator và Agent có đang hoạt động không
        if (npcAnimator != null && agent.enabled)
        {
            // Lấy độ lớn vận tốc thực tế của Agent (0 là đứng yên, >0 là đang đi)
            float currentSpeed = agent.velocity.magnitude;

            // Đẩy giá trị này vào tham số "Speed" trong Animator
            // Dùng Mathf.Lerp hoặc m để im giá trị gốc cũng được
            npcAnimator.SetFloat("Speed", currentSpeed);
        }
        else if (npcAnimator != null && !agent.enabled)
        {
            // Nếu Agent bị tắt (đang ở trong nhà), ép về Idle cho chắc
            npcAnimator.SetFloat("Speed", 0f);
        }
    }

    // --- LOGIC TƯƠNG TÁC ---
    public string GetInteractText()
    {
        // Nếu không ở sạp (đang ngủ, hoặc lếch thếch trên đường) -> Tịt luôn không cho tương tác
        if (!isAtWork || isInsideHouse) return "";

        if (myShopData != null) return $"[E] Giao dịch với {myShopData.npcName}";
        return "[E] Nói chuyện";
    }

    public void Interact()
    {
        // Chốt chặn cuối: Gọi hàm Interact cũng vô dụng nếu chưa tới sạp
        if (!isAtWork || isInsideHouse) return;

        if (myShopData != null)
        {
            // Phát tiếng chào
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(greetingSound))
            {
                AudioManager.Instance.PlaySFX(greetingSound);
            }

            // Mở Shop
            if (ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.OpenShop(myShopData, this.transform);
            }
        }
    }
}