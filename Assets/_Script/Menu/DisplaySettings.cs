using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DisplaySettings : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Dropdown monitorDropdown;

    // Bộ nhớ đệm chứa danh sách màn hình chuẩn mới
    private List<DisplayInfo> displays = new List<DisplayInfo>();

    private void Start()
    {
        // 1. Quét phần cứng lấy danh sách màn hình (Chuẩn DisplayInfo)
        Screen.GetDisplayLayout(displays);
        int monitorCount = displays.Count;

        if (monitorCount <= 1)
        {
            if (monitorDropdown != null) monitorDropdown.gameObject.SetActive(false);
            return;
        }

        if (monitorDropdown != null)
        {
            monitorDropdown.ClearOptions();
            List<string> options = new List<string>();

            for (int i = 0; i < monitorCount; i++)
            {
                options.Add("Màn hình " + (i + 1));
            }
            monitorDropdown.AddOptions(options);

            int savedMonitor = PlayerPrefs.GetInt("UnitySelectMonitor", 0);
            if (savedMonitor >= monitorCount) savedMonitor = 0;

            monitorDropdown.value = savedMonitor;
            monitorDropdown.RefreshShownValue();

            monitorDropdown.onValueChanged.AddListener(SetMonitor);
        }
    }

    public void SetMonitor(int monitorIndex)
    {
        if (monitorIndex >= 0 && monitorIndex < displays.Count)
        {
            // [ĐÃ SỬA]: Gửi thẳng DisplayInfo vào hàm
            Screen.MoveMainWindowTo(displays[monitorIndex], new Vector2Int(0, 0));

            PlayerPrefs.SetInt("UnitySelectMonitor", monitorIndex);
            PlayerPrefs.Save();

            Debug.Log($"Đã chuyển game sang Màn hình {monitorIndex + 1}");
        }
    }
}