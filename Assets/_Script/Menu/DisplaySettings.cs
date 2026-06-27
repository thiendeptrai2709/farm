using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
public class DisplaySettings : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Dropdown monitorDropdown;

    public LocalizedString localizedMonitorPrefix;

    // Bộ nhớ đệm chứa danh sách màn hình chuẩn mới
    private List<DisplayInfo> displays = new List<DisplayInfo>();

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += UpdateDropdownText;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= UpdateDropdownText;
    }

    private void Start()
    {
        Screen.GetDisplayLayout(displays);
        int monitorCount = displays.Count;

        if (monitorCount <= 1)
        {
            if (monitorDropdown != null) monitorDropdown.gameObject.SetActive(false);
            return;
        }

        if (monitorDropdown != null)
        {
            int savedMonitor = PlayerPrefs.GetInt("UnitySelectMonitor", 0);
            if (savedMonitor >= monitorCount) savedMonitor = 0;

            // Nạp chữ lần đầu tiên khi vừa mở bảng UI
            UpdateDropdownText(LocalizationSettings.SelectedLocale);

            monitorDropdown.value = savedMonitor;
            monitorDropdown.RefreshShownValue();

            monitorDropdown.onValueChanged.AddListener(SetMonitor);
        }
    }
    private void UpdateDropdownText(UnityEngine.Localization.Locale locale)
    {
        if (monitorDropdown == null || displays.Count <= 1) return;

        // Nếu quên chưa gài bảng dịch, mặc định dùng chữ "Màn hình"
        string prefix = localizedMonitorPrefix.IsEmpty ? "Màn hình" : localizedMonitorPrefix.GetLocalizedString();

        int currentValue = monitorDropdown.value; // Giữ lại lựa chọn hiện tại để không bị nhảy lung tung

        monitorDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < displays.Count; i++)
        {
            options.Add(prefix + " " + (i + 1));
        }
        monitorDropdown.AddOptions(options);

        monitorDropdown.value = currentValue;
        monitorDropdown.RefreshShownValue();
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