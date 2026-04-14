using UnityEngine;

public class BusUI : MonoBehaviour
{
    public static BusUI Instance;

    public GameObject busPanel; // Kéo cái Panel chứa các nút bấm vào đây
    public Transform currentBusStop { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // Đảm bảo lúc mới vào game UI này bị tắt
        if (busPanel != null) busPanel.SetActive(false);
    }
    public bool IsOpen()
    {
        return busPanel != null && busPanel.activeSelf;
    }
    // Hàm này để cái Trạm xe bus gọi
    public void OpenUI(Transform busStopTransform)
    {
        currentBusStop = busStopTransform; // Lưu lại tọa độ
        busPanel.SetActive(true);

        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetBusUIOpenState(true);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

    }

    public void CloseUI()
    {
        currentBusStop = null; // Xóa tọa độ
        busPanel.SetActive(false);

        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetBusUIOpenState(false);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

    }

    // ==========================================
    // CÁC NÚT BẤM SẼ GỌI HÀM NÀY (Nhớ truyền tên map vào)
    // ==========================================
    public void OnClick_CallBusTo(string routingData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        // [QUAN TRỌNG]: Lưu tạm vị trí trạm trước khi CloseUI xóa sạch nó
        Transform savedStop = currentBusStop;

        CloseUI();

        // Chẻ dữ liệu ra làm 2 phần (Cắt ngay tại dấu phẩy)
        string[] data = routingData.Split(',');
        if (data.Length >= 2 && savedStop != null)
        {
            string sceneName = data[0].Trim();
            string spawnID = data[1].Trim();

            // Tìm cái trạm xe bus, lấy cái xe bus của nó và ra lệnh chạy
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
                // Ra lệnh đuổi xe về
                stopScript.myBus.ForceCancelBus();
            }
        }
    }
}