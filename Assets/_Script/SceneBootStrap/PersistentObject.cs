using UnityEngine;
using System.Collections.Generic; // [ĐÃ THÊM]: Bắt buộc phải có để dùng List

public class PersistentObject : MonoBehaviour
{
    private static PersistentObject instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            ApplySavedHardwareSettings();
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void ApplySavedHardwareSettings()
    {
        int savedMonitor = PlayerPrefs.GetInt("UnitySelectMonitor", 0);

        // [ĐÃ SỬA]: Dùng chuẩn API mới lấy danh sách DisplayInfo
        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

        if (savedMonitor > 0 && savedMonitor < displays.Count)
        {
            Screen.MoveMainWindowTo(displays[savedMonitor], new Vector2Int(0, 0));
            Debug.Log($"[Hệ thống] Đã tự động chuyển game sang Màn hình {savedMonitor + 1} lúc khởi động.");
        }
    }
}