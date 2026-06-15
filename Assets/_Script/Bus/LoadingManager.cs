using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
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
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName, string spawnID, GameObject prefab = null)
    {
        targetSpawnPointID = spawnID;

        // Đồng bộ dữ liệu map hiện tại vào RAM trước khi chuyển scene
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            GameData currentData = SaveManager.Instance.GetCurrentData();

            ChestManager currentMapChestManager = FindFirstObjectByType<ChestManager>();
            if (currentMapChestManager != null)
            {
                currentMapChestManager.SaveAllChestsToData(currentData);
                Debug.Log("[LoadingManager] Đã đồng bộ Rương vào RAM trước khi chuyển Map!");
            }

            AnimalPen[] allPens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
            foreach (var pen in allPens)
            {
                pen.SaveAnimalData(currentData);
            }

            DroppedItemManager currentMapItemManager = FindFirstObjectByType<DroppedItemManager>();
            if (currentMapItemManager != null)
            {
                currentMapItemManager.SaveDroppedItemsToData(currentData);
            }

            PlacedPropManager currentPropManager = FindFirstObjectByType<PlacedPropManager>();
            if (currentPropManager != null)
            {
                currentPropManager.SavePropsToData(currentData);
            }

            if (FarmingZone.Instance != null)
            {
                FarmingZone.Instance.SaveAllPlots(currentData);
                Debug.Log("[LoadingManager] Đã đồng bộ Cây Trồng vào RAM trước khi chuyển Map!");
            }

            if (SkeletonQuestManager.Instance != null)
            {
                SkeletonQuestManager.Instance.SaveQuestData(currentData);
            }

            FoodTrough[] allTroughs = FindObjectsByType<FoodTrough>(FindObjectsSortMode.None);
            foreach (var trough in allTroughs)
            {
                trough.SaveTroughData(currentData);
            }

            if (MarketManager.Instance != null)
            {
                MarketManager.Instance.SaveShopData(currentData);
                Debug.Log("[LoadingManager] Đã đồng bộ Chợ vào RAM trước khi chuyển Map!");
            }

            SaveManager.Instance.SaveAllNPCsToData(currentData);
        }

        StartCoroutine(LoadAsynchronously(sceneName, prefab));
    }

    private IEnumerator LoadAsynchronously(string sceneName, GameObject gameplayCorePrefab)
    {
        loadingPanel.SetActive(true);
        progressBar.value = 0;
        progressText.text = "0%";

        // ===============================
        // GIAI ĐOẠN 1: LOAD SCENE
        // ===============================
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float loadProgress = (operation.progress / 0.9f) * 0.8f;
            progressBar.value = loadProgress;
            progressText.text = (loadProgress * 100f).ToString("F0") + "%";
            yield return null;
        }

        operation.allowSceneActivation = true;
        yield return new WaitUntil(() => operation.isDone);

        // ===============================
        // GIAI ĐOẠN 2: TÌM / TẠO PLAYER
        // ===============================
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        GameObject coreInstance = null;

        if (playerObj == null && gameplayCorePrefab != null)
        {
            coreInstance = Instantiate(gameplayCorePrefab);
            DontDestroyOnLoad(coreInstance);

            playerObj = GameObject.FindGameObjectWithTag("Player");
        }
        else if (playerObj != null)
        {
            coreInstance = playerObj.transform.root.gameObject;
        }

        // ===============================
        // GIAI ĐOẠN 3: LOAD DỮ LIỆU MAP
        // ===============================
        if (SaveManager.Instance != null)
        {
            GameData currentData = SaveManager.Instance.GetCurrentData();

            if (currentData != null && FarmingZone.Instance != null)
            {
                FarmingZone.Instance.LoadAllPlots(currentData);
                yield return null;
            }

            if (currentData != null)
            {
                AnimalPen[] allPens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
                foreach (var pen in allPens)
                {
                    pen.LoadAnimalData(currentData);
                }
                yield return null;

                DroppedItemManager newMapItemManager = FindFirstObjectByType<DroppedItemManager>();
                if (newMapItemManager != null)
                {
                    newMapItemManager.LoadDroppedItemsFromData(currentData);
                }
                yield return null;

                PlacedPropManager newPropManager = FindFirstObjectByType<PlacedPropManager>();
                if (newPropManager != null)
                {
                    newPropManager.LoadPropsFromData(currentData);
                }
                yield return null;

                FoodTrough[] newTroughs = FindObjectsByType<FoodTrough>(FindObjectsSortMode.None);
                foreach (var trough in newTroughs)
                {
                    trough.LoadTroughData(currentData);
                }
                yield return null;

                MarketManager newMapMarketManager = FindFirstObjectByType<MarketManager>();
                if (newMapMarketManager != null)
                {
                    newMapMarketManager.LoadShopData(currentData);
                }

                if (SkeletonQuestManager.Instance != null)
                {
                    SkeletonQuestManager.Instance.LoadQuestData(currentData);
                }

                yield return null;
            }
        }

        // ===============================
        // GIAI ĐOẠN 4: ĐẶT PLAYER VÀO ĐÚNG VỊ TRÍ
        // ===============================
        if (playerObj != null)
        {
            if (targetSpawnPointID == "SavedPosition" && SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
            {
                GameData data = SaveManager.Instance.GetCurrentData();

                MovePlayerToSavedPosition(playerObj, data.playerPosition, data.playerRotation, data.cameraAngles);

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.LoadInventoryData(data);
                    InventoryManager.Instance.selectedHotbarIndex = data.selectedHotbarIndex;
                    InventoryManager.Instance.RefreshInventoryUI();
                }

                if (BusUI.Instance != null && data.unlockedBusStops != null)
                {
                    BusUI.Instance.discoveredStops = new List<string>(data.unlockedBusStops);
                }

                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.LoadSavedDay(data.daysInGame);
                }

                TimeSystem timeSys = FindFirstObjectByType<TimeSystem>();
                if (timeSys != null)
                {
                    timeSys.hour = data.savedHour + (data.savedMinute / 60f);
                }
            }
            else
            {
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

            // ===============================
            // GIAI ĐOẠN 5: XỬ LÝ CAMERA
            // ===============================
            if (coreInstance != null)
            {
                CinemachineVirtualCameraBase targetCam = coreInstance.GetComponentInChildren<CinemachineVirtualCameraBase>();
                CinemachineBrain brain = FindFirstObjectByType<CinemachineBrain>();

                Debug.Log($"Brain: {brain}, Cam: {targetCam}, CoreInstance: {coreInstance}");

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

        progressBar.value = 1f;
        progressText.text = "100%";

        // Tạm tắt Cinemachine Brain trong lúc ổn định scene
        CinemachineBrain sceneBrain = FindFirstObjectByType<CinemachineBrain>();
        if (sceneBrain != null)
        {
            sceneBrain.enabled = false;
        }

        yield return null;


        // Đợi Physics / NavMesh ổn định một chút
        yield return new WaitForSeconds(0.3f);

        // ===============================
        // GIAI ĐOẠN 6: LOAD VỊ TRÍ NPC TỐI ƯU HƠN
        // ===============================
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            GameData data = SaveManager.Instance.GetCurrentData();

            if (data.savedNPCs != null && data.savedNPCs.Count > 0)
            {
                Dictionary<string, Vector3> savedNPCPositions = new Dictionary<string, Vector3>();

                foreach (SavedNPCData npcData in data.savedNPCs)
                {
                    if (!savedNPCPositions.ContainsKey(npcData.npcName))
                    {
                        savedNPCPositions.Add(npcData.npcName, npcData.position);
                    }
                }

                NPCVillager[] villagers = FindObjectsByType<NPCVillager>(FindObjectsSortMode.None);

                int villagerCounter = 0;
                foreach (var v in villagers)
                {
                    if (savedNPCPositions.TryGetValue(v.gameObject.name, out Vector3 savedPosition))
                    {
                        UnityEngine.AI.NavMeshAgent agent = v.GetComponent<UnityEngine.AI.NavMeshAgent>();

                        if (agent != null)
                        {
                            agent.enabled = false;
                            v.transform.position = savedPosition;
                            agent.enabled = true;
                        }
                        else
                        {
                            v.transform.position = savedPosition;
                        }
                    }

                    villagerCounter++;

                    // Cứ xử lý 10 NPC thì nhường 1 frame để tránh spike CPU
                    if (villagerCounter % 10 == 0)
                    {
                        yield return null;
                    }
                }

                yield return null;

                NPCMerchant[] merchants = FindObjectsByType<NPCMerchant>(FindObjectsSortMode.None);

                int merchantCounter = 0;
                foreach (var m in merchants)
                {
                    if (savedNPCPositions.TryGetValue(m.gameObject.name, out Vector3 savedPosition))
                    {
                        UnityEngine.AI.NavMeshAgent agent = m.GetComponent<UnityEngine.AI.NavMeshAgent>();

                        if (agent != null)
                        {
                            agent.enabled = false;
                            m.transform.position = savedPosition;
                            agent.enabled = true;
                        }
                        else
                        {
                            m.transform.position = savedPosition;
                        }
                    }

                    merchantCounter++;

                    // Cứ xử lý 10 NPC thì nhường 1 frame để tránh spike CPU
                    if (merchantCounter % 10 == 0)
                    {
                        yield return null;
                    }
                }
            }
        }

        yield return null;

        if (sceneBrain != null)
        {
            sceneBrain.enabled = true;
        }

        // Chờ vài frame để Unity kịp render/culling/shadow/camera ổn định
        // Cái này giúp tránh tình trạng vừa tắt loading là spike ngay
        for (int i = 0; i < 10; i++)
        {
            progressBar.value = 1f;
            progressText.text = "100%";
            yield return null;
        }

        // Chờ thêm rất ngắn để terrain/detail/cỏ/shadow ổn định
        yield return new WaitForSeconds(0.2f);

        // Lúc này mới báo player sẵn sàng
        OnPlayerReady?.Invoke();

        // Chờ thêm 1 frame sau khi các script nhận OnPlayerReady chạy xong
        yield return null;

        // Tắt loading sau cùng
        loadingPanel.SetActive(false);
    }

    private void MovePlayerToSpawnPoint(GameObject actualPlayer, Transform spawnTransform)
    {
        CharacterController cc = actualPlayer.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        actualPlayer.transform.position = spawnTransform.position;

        PlayerMovement pm = actualPlayer.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ForceCameraLook(spawnTransform.rotation, spawnTransform.eulerAngles);

            // Ép luôn Camera thật ở frame này để cắt đứt quán tính quay cổ của PlayerMovement trong lúc loading
            if (Camera.main != null)
            {
                Camera.main.transform.rotation = spawnTransform.rotation;
            }
        }
        else
        {
            actualPlayer.transform.rotation = spawnTransform.rotation;
        }

        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        Debug.Log("Đã đưa Player về đúng vị trí và hướng nhìn SpawnPoint!");
    }

    private void MovePlayerToSavedPosition(GameObject actualPlayer, Vector3 savedPos, Quaternion savedRot, Vector3 savedCamAngles)
    {
        CharacterController cc = actualPlayer.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        actualPlayer.transform.position = savedPos;

        PlayerMovement pm = actualPlayer.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ForceCameraLook(savedRot, savedCamAngles);

            // Ép luôn Camera thật ở frame này để cắt đứt quán tính quay cổ của PlayerMovement
            if (Camera.main != null)
            {
                Camera.main.transform.rotation = Quaternion.Euler(savedCamAngles.x, savedCamAngles.y, 0f);
            }
        }
        else
        {
            actualPlayer.transform.rotation = savedRot;
        }

        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        Debug.Log("Đã đưa Player về đúng tọa độ và hướng nhìn Save Game!");
    }
}