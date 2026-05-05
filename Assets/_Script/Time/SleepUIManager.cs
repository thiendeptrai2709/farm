using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SleepUIManager : MonoBehaviour
{
    public static SleepUIManager Instance { get; private set; }

    [Header("Thành phần UI")]
    public GameObject sleepPanel;
    public Slider sleepSlider;
    public TextMeshProUGUI sleepTimeText;

    private void Awake()
    {
        // Chức năng: Khởi tạo Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Chức năng: Gán sự kiện thay đổi giá trị cho Slider
        sleepSlider.onValueChanged.AddListener(UpdateSleepText);
    }

    // Chức năng: Mở bảng chọn giờ ngủ
    public void OpenSleepPanel()
    {
        sleepPanel.SetActive(true);
        sleepSlider.minValue = 1f;
        sleepSlider.maxValue = 24f; // Chức năng: Ngủ tối đa 24 tiếng
        sleepSlider.value = 8f;     // Chức năng: Đặt mặc định là 8 tiếng
        UpdateSleepText(sleepSlider.value);
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetSleepUIOpenState(true);
    }

    // Chức năng: Cập nhật chữ hiển thị số giờ ngủ
    private void UpdateSleepText(float value)
    {
        sleepTimeText.text = value.ToString("0") + " Tiếng";
    }

    // Chức năng: Nút xác nhận ngủ
    public void ConfirmSleep()
    {
        TimeSystem timeSystem = FindAnyObjectByType<TimeSystem>();
        if (timeSystem != null)
        {
            float hoursToSleep = sleepSlider.value;
            float wakeUpTime = (timeSystem.hour + hoursToSleep) % 24f; // Chức năng: Tính toán giờ thức dậy

            timeSystem.SkipToMorning(wakeUpTime);
        }
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetSleepUIOpenState(false);
        sleepPanel.SetActive(false);

    }

    // Chức năng: Nút hủy
    public void CancelSleep()
    {
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetSleepUIOpenState(false);
        sleepPanel.SetActive(false);
    }
}