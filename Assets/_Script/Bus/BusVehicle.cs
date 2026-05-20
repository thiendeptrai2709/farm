using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BusVehicle : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Di chuyển")]
    public Transform startPoint;
    public Transform stopPoint;
    public Transform exitPoint;
    public float driveSpeed = 10f;

    [Header("Tương tác Mặt đất (Bám đường)")]
    [Tooltip("Khoảng cách từ tâm xe xuống gầm xe (để xe không bị lún)")]
    public float rideHeight = 1.2f;
    public LayerMask groundLayer; // Nhớ ra ngoài Inspector chọn layer mặt đất (Terrain/Road) cho biến này

    [Header("Thành phần 3D")]
    public GameObject busModel;
    public Collider busCollider;

    [Header("Trục Bánh Xe (Chỉ kéo Transform của Mesh)")]
    // KHÔNG dùng WheelCollider nữa, chỉ kéo cái hình ảnh 3D của bánh xe vào đây
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;
    public float wheelRadius = 0.5f; // Bán kính bánh xe để tính tốc độ lăn

    [Header("Cài đặt Chờ & Âm thanh")]
    public float maxWaitTime = 10f;
    private float idleTimer = 0f;
    public AudioClip engineClip;
    public AudioClip brakeClip;
    public AudioClip hornClip;
    private AudioSource busAudio;

    private enum BusState { Hidden, Inbound, AtStop, Outbound }
    private BusState currentState = BusState.Hidden;

    private string targetScene = "";
    private string targetSpawnID = "";

    private Rigidbody rb;

    [Header("Tối ưu hóa đường dốc (Xe dáng dài)")]
    [Tooltip("Khoảng cách từ tâm xe ra đầu xe để bắn tia laser trước")]
    public float frontOffset = 3f;
    [Tooltip("Khoảng cách từ tâm xe ra đuôi xe để bắn tia laser sau")]
    public float rearOffset = 3f;

    private void Awake()
    {
        busAudio = gameObject.AddComponent<AudioSource>();
        busAudio.spatialBlend = 1f;
        busAudio.rolloffMode = AudioRolloffMode.Linear;
        busAudio.minDistance = 5f;
        busAudio.maxDistance = 50f;

        rb = GetComponent<Rigidbody>();
        // ÉP CHẾT VẬT LÝ: Xe bây giờ là bất khả chiến bại, không gì tông bay được nó
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        ResetBus();
    }

    private void Update()
    {
        if (busAudio != null && AudioManager.Instance != null)
        {
            busAudio.volume = AudioManager.Instance.GetSFXVolume();
        }

        switch (currentState)
        {
            case BusState.Inbound:
                MoveBusAndSnapToGround(stopPoint.position, BusState.AtStop);
                break;
            case BusState.Outbound:
                MoveBusAndSnapToGround(exitPoint.position, BusState.Hidden);
                break;
            case BusState.AtStop:
                idleTimer += Time.deltaTime;
                if (idleTimer >= maxWaitTime)
                {
                    StartDrivingOut();
                }
                break;
        }
    }

    private void MoveBusAndSnapToGround(Vector3 targetWaypoint, BusState nextState)
    {
        // 1. Chỉ lấy hướng X và Z để tìm đường đi
        Vector3 flatTarget = new Vector3(targetWaypoint.x, transform.position.y, targetWaypoint.z);
        Vector3 moveDir = (flatTarget - transform.position).normalized;

        float distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetWaypoint.x, targetWaypoint.z));

        if (distanceToTarget < 0.5f)
        {
            currentState = nextState;
            if (currentState == BusState.AtStop) OnArrived();
            if (currentState == BusState.Hidden) ResetBus();
            return;
        }

        // 2. Tính toán vị trí tịnh tiến tiếp theo (Tạm tính trên mặt phẳng)
        Vector3 newPosition = transform.position + moveDir * driveSpeed * Time.deltaTime;

        // 3. TÍNH TOÁN 2 ĐIỂM KHẢO SÁT: ĐẦU XE VÀ ĐUÔI XE
        Vector3 frontCheckPos = newPosition + moveDir * frontOffset;
        Vector3 rearCheckPos = newPosition - moveDir * rearOffset;

        // Khởi tạo các biến lưu tọa độ mặt đất dưới đầu và đuôi xe
        Vector3 groundFrontPoint = frontCheckPos;
        Vector3 groundRearPoint = rearCheckPos;
        Vector3 combinedNormal = Vector3.up;

        bool frontHitSuccess = Physics.Raycast(frontCheckPos + Vector3.up * 5f, Vector3.down, out RaycastHit frontHit, 10f, groundLayer);
        bool rearHitSuccess = Physics.Raycast(rearCheckPos + Vector3.up * 5f, Vector3.down, out RaycastHit rearHit, 10f, groundLayer);

        // Nếu cả trước và sau đều chạm đất (Trường hợp đang đi trên dốc)
        if (frontHitSuccess && rearHitSuccess)
        {
            groundFrontPoint = frontHit.point;
            groundRearPoint = rearHit.point;

            // Lấy trung bình cộng pháp tuyến của cả 2 điểm để tính độ nghiêng trái/phải
            combinedNormal = (frontHit.normal + rearHit.normal).normalized;

            // Chiều cao của xe bằng trung bình cộng chiều cao của đầu và đuôi
            newPosition.y = (groundFrontPoint.y + groundRearPoint.y) / 2f + rideHeight;

            // Hướng tiến của xe chạy dọc theo đường nối từ điểm đất phía sau lên điểm đất phía trước
            Vector3 slopeForwardDirection = (groundFrontPoint - groundRearPoint).normalized;

            // Ép xe xoay theo độ dốc dọc thân xe và độ nghiêng của mặt đường
            Quaternion targetRotation = Quaternion.LookRotation(slopeForwardDirection, combinedNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        else if (frontHitSuccess) // Phòng hờ chỉ có đầu xe chạm đất
        {
            newPosition.y = frontHit.point.y + rideHeight;
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, frontHit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

        // Cập nhật vị trí chuẩn cuối cùng sau khi đã tính dốc
        transform.position = newPosition;

        // 4. Quay bánh xe giả lập theo tốc độ thật
        RotateWheelsVisually();
    }

    private void RotateWheelsVisually()
    {
        // Công thức tính số độ quay: (Tốc độ / Bán kính) * Chuyển đổi sang Độ
        float rotationAmount = (driveSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

        if (frontLeftWheel) frontLeftWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (frontRightWheel) frontRightWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (rearLeftWheel) rearLeftWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (rearRightWheel) rearRightWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
    }

    // ==========================================
    // CÁC HÀM LOGIC CŨ (Đã được dọn dẹp lại cho mượt)
    // ==========================================

    public void StartDrivingIn(string destinationScene, string spawnID)
    {
        if (currentState == BusState.Inbound || currentState == BusState.Outbound) return;

        if (currentState == BusState.AtStop)
        {
            targetScene = destinationScene;
            targetSpawnID = spawnID;
            idleTimer = 0f;
            return;
        }

        targetScene = destinationScene;
        targetSpawnID = spawnID;
        busModel.SetActive(true);
        if (busCollider != null) busCollider.enabled = false;

        idleTimer = 0f;
        currentState = BusState.Inbound;
        PlayEngineSound();
    }

    private void OnArrived()
    {
        if (busCollider != null) busCollider.enabled = true;
        busAudio.Stop();
        if (brakeClip) busAudio.PlayOneShot(brakeClip);
        if (hornClip) busAudio.PlayOneShot(hornClip);
    }

    public string GetInteractText()
    {
        if (currentState != BusState.AtStop) return "";
        return $"[E] Lên xe đi tới {targetScene}";
    }

    public void Interact()
    {
        if (currentState == BusState.AtStop && !string.IsNullOrEmpty(targetScene))
        {
            TimeSystem timeSys = FindFirstObjectByType<TimeSystem>();
            if (timeSys != null) timeSys.AddBusTravelTime(1f);

            if (QuestManager.Instance != null && targetScene == "Farm")
            {
                QuestManager.Instance.ReportAction("Travel_To_Farm");
            }

            LoadingManager.Instance.LoadScene(targetScene, targetSpawnID);
            StartDrivingOut();
        }
    }

    public void ForceCancelBus()
    {
        if (currentState == BusState.AtStop) StartDrivingOut();
    }

    private void StartDrivingOut()
    {
        if (busCollider != null) busCollider.enabled = false;
        currentState = BusState.Outbound;
        PlayEngineSound();
    }

    private void PlayEngineSound()
    {
        if (engineClip)
        {
            busAudio.clip = engineClip;
            busAudio.loop = true;
            busAudio.Play();
        }
    }

    private void ResetBus()
    {
        currentState = BusState.Hidden;
        targetScene = "";
        busModel.SetActive(false);
        if (busCollider != null) busCollider.enabled = false;

        if (startPoint != null)
        {
            transform.position = startPoint.position;
            Vector3 dir = (stopPoint.position - startPoint.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (busAudio != null) busAudio.Stop();
    }

    private void OnDrawGizmos()
    {
        if (startPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(startPoint.position, 0.5f); }
        if (stopPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(stopPoint.position, 0.5f); }
        if (exitPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(exitPoint.position, 0.5f); }
    }
}