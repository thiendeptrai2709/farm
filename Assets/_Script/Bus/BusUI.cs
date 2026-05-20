using UnityEngine;
using System.Collections.Generic;

public class BusUI : MonoBehaviour
{
    public static BusUI Instance;

    public GameObject busPanel;
    public Transform currentBusStop { get; private set; }

    [Header("Trí nhớ: Các trạm Rừng đã khám phá")]
    public List<string> discoveredStops = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (busPanel != null) busPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return busPanel != null && busPanel.activeSelf;
    }

    public void OpenUI(Transform busStopTransform)
    {
        currentBusStop = busStopTransform;

        // 1. NHỚ TRẠM MỚI: Nếu trạm này có ID, tự động lưu vào danh sách khám phá
        BusStop stopScript = busStopTransform.GetComponent<BusStop>();
        if (stopScript != null && !string.IsNullOrEmpty(stopScript.stationRoutingData))
        {
            if (!discoveredStops.Contains(stopScript.stationRoutingData))
            {
                discoveredStops.Add(stopScript.stationRoutingData);
                Debug.Log($"[Khám phá] Đã lưu trạm xe bus mới vào sổ: {stopScript.stationRoutingData}");
            }
        }

        // [ĐÃ THÊM]: Lùng sục tìm TẤT CẢ các nút đang gắn script BusDiscoveryLock (kể cả những nút đang bị ẩn) để ép chúng nó cập nhật hình ảnh
        BusDiscoveryLock[] discoveryLocks = busPanel.GetComponentsInChildren<BusDiscoveryLock>(true);
        foreach (BusDiscoveryLock dLock in discoveryLocks)
        {
            dLock.RefreshLock();
        }

        // 2. MỞ PANEL LÊN: Khi Panel bật, các file Lock gắn trên nút sẽ tự động chạy hàm OnEnable() để quyết định Ẩn hay Hiện
        busPanel.SetActive(true);

        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetBusUIOpenState(true);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
    }

    public void CloseUI()
    {
        currentBusStop = null;
        busPanel.SetActive(false);

        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetBusUIOpenState(false);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
    }

    public void OnClick_CallBusTo(string routingData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        Transform savedStop = currentBusStop;
        CloseUI();

        string[] data = routingData.Split(',');
        if (data.Length >= 2 && savedStop != null)
        {
            string sceneName = data[0].Trim();
            string spawnID = data[1].Trim();

            BusStop stopScript = savedStop.GetComponent<BusStop>();
            if (stopScript != null && stopScript.myBus != null)
            {
                stopScript.myBus.StartDrivingIn(sceneName, spawnID);
            }
        }
        else
        {
            Debug.LogError("Dữ liệu nút bấm xe bus sai định dạng! Vui lòng viết kiểu: TênScene,MãID");
        }
    }

    public void OnClick_CancelBus()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        Transform savedStop = currentBusStop;
        CloseUI();

        if (savedStop != null)
        {
            BusStop stopScript = savedStop.GetComponent<BusStop>();
            if (stopScript != null && stopScript.myBus != null)
            {
                stopScript.myBus.ForceCancelBus();
            }
        }
    }
}