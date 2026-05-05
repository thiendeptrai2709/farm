using UnityEngine;

public class BusVehicle : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Di chuyển")]
    public Transform startPoint;
    public Transform stopPoint;
    public Transform exitPoint; // Điểm xe sẽ chạy đến để biến mất
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

    // [ĐÃ SỬA]: Quản lý toàn bộ bằng Enum này, dẹp bỏ isDriving và hasArrived
    private enum BusState { Hidden, Inbound, AtStop, Outbound }
    private BusState currentState = BusState.Hidden;

    private string targetScene = "";
    private string targetSpawnID = "";

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
        ResetBus();
    }

    private void Update()
    {
        switch (currentState)
        {
            case BusState.Inbound:
                MoveBus(stopPoint.position, BusState.AtStop);
                if (currentState == BusState.AtStop) OnArrived();
                break;

            case BusState.AtStop:
                // Đếm ngược nếu người chơi không lên xe
                idleTimer += Time.deltaTime;
                if (idleTimer >= maxWaitTime)
                {
                    Debug.Log("Quá thời gian chờ, xe bus rời bến.");
                    StartDrivingOut();
                }
                break;

            case BusState.Outbound:
                // Chạy thêm một đoạn tới ExitPoint rồi mới biến mất
                MoveBus(exitPoint.position, BusState.Hidden);
                if (currentState == BusState.Hidden) ResetBus();
                break;
        }
    }

    private void MoveBus(Vector3 target, BusState nextState)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, driveSpeed * Time.deltaTime);
        transform.LookAt(target);
        if (Vector3.Distance(transform.position, target) < 0.1f) currentState = nextState;
    }

    public void StartDrivingIn(string destinationScene, string spawnID)
    {
        // Đang chạy trên đường (vào hoặc ra) thì cấm cản địa
        if (currentState == BusState.Inbound || currentState == BusState.Outbound) return;

        // Nếu xe ĐÃ ĐỖ TRẠM mà đổi vé
        if (currentState == BusState.AtStop)
        {
            targetScene = destinationScene;
            targetSpawnID = spawnID;
            idleTimer = 0f; // Reset lại đồng hồ chờ cho khách mới đổi vé
            Debug.Log($"Đã đổi vé sang: {targetScene} - {targetSpawnID}");
            return;
        }

        // Nếu xe đang GIẤU, gọi xe ra
        targetScene = destinationScene;
        targetSpawnID = spawnID;
        busModel.SetActive(true);
        if (busCollider != null) busCollider.enabled = false;

        idleTimer = 0f;
        currentState = BusState.Inbound; // Bắt đầu chạy vào
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
        // Chỉ hiện chữ khi xe đang đỗ chờ
        if (currentState != BusState.AtStop) return "";
        return $"[E] Lên xe đi tới {targetScene}";
    }

    public void Interact()
    {
        if (currentState == BusState.AtStop && !string.IsNullOrEmpty(targetScene))
        {
            TimeSystem timeSys = FindFirstObjectByType<TimeSystem>();
            if (timeSys != null)
            {
                timeSys.AddBusTravelTime(1f); // Mất 1 tiếng
            }
            if (QuestManager.Instance != null && targetScene == "Farm")
            {
                // Báo cho QuestManager biết người chơi đã làm hành động này
                QuestManager.Instance.ReportAction("Travel_To_Farm");
            }
            // Bấm lên xe -> Load map
            LoadingManager.Instance.LoadScene(targetScene, targetSpawnID);

            // [ĐÃ SỬA]: Ép xe chạy ra Exit chứ không tàng hình cái rụp
            StartDrivingOut();
        }
    }

    public void ForceCancelBus()
    {
        if (currentState == BusState.AtStop)
        {
            Debug.Log("Người chơi đã hủy chuyến, xe bus từ từ rời bến.");
            // [ĐÃ SỬA]: Hủy chuyến thì xe cũng nổ máy chạy đi thẳng
            StartDrivingOut();
        }
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
        if (startPoint != null) transform.position = startPoint.position;
        if (busAudio != null) busAudio.Stop();
    }
}