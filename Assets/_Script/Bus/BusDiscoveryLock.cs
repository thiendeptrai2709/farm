using UnityEngine;

public class BusDiscoveryLock : MonoBehaviour
{
    [Header("Mã Trạm cần khám phá để hiện nút này")]
    [Tooltip("Ví dụ: Forest,Lake")]
    public string requiredStopID;

    public void RefreshLock()
    {
        if (BusUI.Instance != null && !string.IsNullOrEmpty(requiredStopID))
        {
            bool isDiscovered = BusUI.Instance.discoveredStops.Contains(requiredStopID);

            // Nếu đã tìm thấy trạm -> Hiện nút, chưa tìm thấy -> Ẩn nút
            gameObject.SetActive(isDiscovered);
        }
    }
}