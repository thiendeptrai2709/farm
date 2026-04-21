using System.Collections;
using TMPro;
using Unity.Cinemachine; // Khai báo chuẩn Cinemachine v3
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;
    public static event Action OnPlayerReady;

    [Header("UI References")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    private string targetSpawnPointID = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Nhận trực tiếp Prefab vào đây
    public void LoadScene(string sceneName, string spawnID, GameObject prefab = null)
    {
        targetSpawnPointID = spawnID;

        // [ĐÃ THÊM]: Ép Scene hiện tại phải nộp lại dữ liệu lưu tạm vào RAM trước khi nó bị xóa sổ
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            ChestManager currentMapChestManager = UnityEngine.Object.FindFirstObjectByType<ChestManager>();
            if (currentMapChestManager != null)
            {
                currentMapChestManager.SaveAllChestsToData(SaveManager.Instance.GetCurrentData());
                Debug.Log("[LoadingManager] Đã đồng bộ Rương vào RAM trước khi chuyển Map!");
            }
        }

        // Truyền thẳng cái prefab xuống cho Giai đoạn 2 để nó đẻ Player
        StartCoroutine(LoadAsynchronously(sceneName, prefab));
    }

    private IEnumerator LoadAsynchronously(string sceneName, GameObject gameplayCorePrefab)
    {
        loadingPanel.SetActive(true);
        progressBar.value = 0;
        progressText.text = "0%";

        // ==========================================
        // GIAI ĐOẠN 1: LOAD MAP (0% -> 80%)
        // ==========================================
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float loadProgress = (operation.progress / 0.9f) * 0.8f;
            progressBar.value = loadProgress;
            progressText.text = (loadProgress * 100f).ToString("F0") + "%";
            yield return null; // Chỉ đợi frame, không đếm thời gian
        }

        operation.allowSceneActivation = true;

        // Chờ đến khi Scene mới thực sự hiện ra (Trạng thái IsDone = true)
        yield return new WaitUntil(() => operation.isDone);

        // ==========================================
        // GIAI ĐOẠN 2: ÉP CAMERA LÀM VIỆC (80% -> 100%)
        // ==========================================
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        GameObject coreInstance = null;

        // BƯỚC 1: XÁC ĐỊNH ĐỐI TƯỢNG PLAYER
        if (playerObj == null && gameplayCorePrefab != null)
        {
            // Nếu chưa có (Lần đầu load từ Bootstrap/Menu) -> Sinh ra mới
            coreInstance = Instantiate(gameplayCorePrefab);
            DontDestroyOnLoad(coreInstance);

            // [QUAN TRỌNG ĐÃ SỬA]: Phải tìm lại Player sau khi đẻ ra cái cục Prefab
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }
        else if (playerObj != null)
        {
            // Nếu đã có sẵn (Di chuyển giữa Farm <-> Tower)
            coreInstance = playerObj.transform.root.gameObject;
        }

        if (playerObj != null)
        {
            // [ĐÃ THÊM LOGIC ĐỌC SAVE]
            if (targetSpawnPointID == "SavedPosition" && SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
            {
                GameData data = SaveManager.Instance.GetCurrentData();

                // 1. Dịch chuyển vị trí
                MovePlayerToSavedPosition(playerObj, data.playerPosition);

                // 2. [ĐÃ THÊM]: Đổ đồ vào lại túi đồ
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.LoadInventoryData(data);
                }
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.LoadSavedDay(data.daysInGame);
                }

                TimeSystem timeSys = UnityEngine.Object.FindFirstObjectByType<TimeSystem>();
                if (timeSys != null)
                {
                    // Trả lại định dạng số thập phân cho biến hour (Ví dụ 6h30 = 6.5f)
                    timeSys.hour = data.savedHour + (data.savedMinute / 60f);
                }
            }
            else
            {
                // [GIỮ NGUYÊN LOGIC CŨ KHI CHẠY XE BUS HOẶC NEW GAME]
                PlayerSpawnPoint[] allPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
                PlayerSpawnPoint targetPoint = null;

                foreach (var point in allPoints)
                {
                    if (point.spawnPointID == targetSpawnPointID)
                    {
                        targetPoint = point;
                        break;
                    }
                }

                if (targetPoint == null && allPoints.Length > 0)
                {
                    targetPoint = allPoints[0];
                    Debug.LogWarning($"Không tìm thấy SpawnID '{targetSpawnPointID}', dùng điểm mặc định.");
                }

                if (targetPoint != null)
                {
                    MovePlayerToSpawnPoint(playerObj, targetPoint.transform);
                }
            }

            // Xử lý Camera cho đối tượng vừa xác định (Vẫn dùng coreInstance vì Cam nằm ở root)
            if (coreInstance != null)
            {
                var targetCam = coreInstance.GetComponentInChildren<CinemachineVirtualCameraBase>();
                var brain = FindFirstObjectByType<CinemachineBrain>();

                if (brain != null && targetCam != null)
                {
                    targetCam.PreviousStateIsValid = false;

                    while (brain.IsBlending || (UnityEngine.Object)brain.ActiveVirtualCamera != (UnityEngine.Object)targetCam)
                    {
                        if (brain.IsBlending && brain.ActiveBlend != null)
                        {
                            float blendProgress = brain.ActiveBlend.TimeInBlend / brain.ActiveBlend.Duration;
                            progressBar.value = 0.8f + (blendProgress * 0.2f);
                            progressText.text = (progressBar.value * 100f).ToString("F0") + "%";
                        }
                        yield return null;
                    }
                }
            }
        }

        // ==========================================
        // KẾT THÚC: Tắt UI NGAY LẬP TỨC khi vòng lặp Camera kết thúc
        // ==========================================
        progressBar.value = 1f;
        progressText.text = "100%";

        yield return null;

        OnPlayerReady?.Invoke();

        loadingPanel.SetActive(false);
    }

    // [ĐÃ SỬA]: Dịch chuyển chính xác thằng Player
    private void MovePlayerToSpawnPoint(GameObject actualPlayer, Transform spawnTransform)
    {
        // Lấy đúng CharacterController trên người thằng Player (thay vì GetComponentsInChildren)
        CharacterController cc = actualPlayer.GetComponent<CharacterController>();

        // Tắt CC để tránh xung đột vật lý bật ngược lại
        if (cc != null) cc.enabled = false;

        actualPlayer.transform.position = spawnTransform.position;
        actualPlayer.transform.rotation = spawnTransform.rotation;

        Physics.SyncTransforms();
        // Bật lại CC sau khi đã đặt yên vị
        if (cc != null) cc.enabled = true;

        Debug.Log("Đã đưa Player về đúng vị trí SpawnPoint!");
    }
    private void MovePlayerToSavedPosition(GameObject actualPlayer, Vector3 savedPos)
    {
        CharacterController cc = actualPlayer.GetComponent<CharacterController>();

        // Tắt CC để tránh xung đột vật lý
        if (cc != null) cc.enabled = false;

        actualPlayer.transform.position = savedPos;

        Physics.SyncTransforms();

        // Bật lại CC
        if (cc != null) cc.enabled = true;

        Debug.Log("Đã đưa Player về đúng tọa độ Save Game!");
    }
}
