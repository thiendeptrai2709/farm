using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BusVehicle : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Di chuyển")]
    public Transform startPoint;
    public Transform stopPoint;
    public Transform exitPoint;
    public float driveSpeed = 10f;

    [Header("Thành phần 3D")]
    public GameObject busModel;
    public Collider busCollider;

    [Header("Cài đặt Chờ")]
    public float maxWaitTime = 10f;
    private float idleTimer = 0f;

    [Header("Cài đặt Âm thanh 3D")]
    public AudioClip engineClip;
    public AudioClip brakeClip;
    public AudioClip hornClip;

    private AudioSource busAudio;

    private enum BusState { Hidden, Inbound, AtStop, Outbound }
    private BusState currentState = BusState.Hidden;

    private string targetScene = "";
    private string targetSpawnID = "";

    public Rigidbody rb;
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float stopDistance = 1.5f;

    // ── CenterOfMass: kéo vào GameObject con có Y = -0.5 (xem hướng dẫn D) ──
    public Transform centerOfMass;

    public WheelCollider frontLeftW, frontRightW, rearLeftW, rearRightW;
    public Transform frontLeftT, frontRightT, rearLeftT, rearRightT;



    private void Awake()
    {
        busAudio = gameObject.AddComponent<AudioSource>();
        busAudio.spatialBlend = 1f;
        busAudio.rolloffMode = AudioRolloffMode.Linear;
        busAudio.minDistance = 5f;
        busAudio.maxDistance = 50f;
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null && centerOfMass != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }

        ResetBus();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case BusState.Inbound:
                MoveBus(stopPoint.position, BusState.AtStop);
                break;
            case BusState.Outbound:
                MoveBus(exitPoint.position, BusState.Hidden);
                break;
            case BusState.AtStop:
                break;
        }
    }

    // Xử lý thời gian chờ
    private void Update()
    {
        if (currentState == BusState.AtStop)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= maxWaitTime)
            {
                StartDrivingOut();
            }
        }

        UpdateWheelPoses();
    }

    private void MoveBus(Vector3 target, BusState nextState)
    {
        // Sử dụng khoảng cách 3D để tính toán chính xác khi lên dốc
        Vector3 currentPos = transform.position;
        float dist = Vector3.Distance(currentPos, target);

        if (dist < 0.5f)
        {
            // Kiểm tra trạng thái isKinematic trước khi gán vận tốc
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
            currentState = nextState;

            if (currentState == BusState.AtStop) OnArrived();
            if (currentState == BusState.Hidden) ResetBus();
        }
        else
        {
            Vector3 moveDir = (target - transform.position).normalized;
            rb.linearVelocity = moveDir * driveSpeed;

            if (moveDir != Vector3.zero)
            {
                // Lấy góc xoay hướng về đích
                Vector3 euler = Quaternion.LookRotation(moveDir).eulerAngles;
                // Ép cứng trục Z (Roll) về 0 để xe không bao giờ bị lật nghiêng sang hai bên
                Quaternion targetRot = Quaternion.Euler(euler.x, euler.y, 0f);
                // Dùng Time.fixedDeltaTime vì hàm này được gọi trong FixedUpdate
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 5f));
            }
        }
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftW, frontLeftT);
        UpdateWheelPose(frontRightW, frontRightT);
        UpdateWheelPose(rearLeftW, rearLeftT);
        UpdateWheelPose(rearRightW, rearRightT);
    }

    private void UpdateWheelPose(WheelCollider col, Transform meshTransform)
    {
        if (col == null || meshTransform == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        meshTransform.position = pos;
        meshTransform.rotation = rot;
    }

    private void OnDrawGizmos()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(startPoint.position, 0.5f);
            Gizmos.DrawRay(startPoint.position, startPoint.forward * 3f);
        }
        if (stopPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stopPoint.position, 0.5f);
        }
        if (exitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(exitPoint.position, 0.5f);
        }
    }
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

        // Cho phép xe chịu tác động vật lý để lăn bánh
        if (rb != null) rb.isKinematic = false;

        idleTimer = 0f;
        currentState = BusState.Inbound;
        PlayEngineSound();
    }

    private void OnArrived()
    {
        // Khóa chết xe bằng isKinematic khi đã đến bến, người chơi đẩy thoải mái không xê dịch
        if (rb != null) rb.isKinematic = true;

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

        // Mở khóa vật lý để xe chạy ra khỏi bến
        if (rb != null) rb.isKinematic = false;

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

        if (rb != null)
        {
            // Xóa vận tốc trước khi bật isKinematic
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        if (busAudio != null) busAudio.Stop();
    }
}