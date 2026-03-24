using UnityEngine;
using TMPro;
using System; // THÊM DÒNG NÀY: Để dùng thư viện Sự kiện (Action)

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Thời gian trong Game")]
    public int daysInGame = 1;
    public TextMeshProUGUI dayUI;

    // THÊM DÒNG NÀY: Tạo ra một cái sự kiện (cái loa) tên là OnNewDay
    public event Action OnNewDay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateDayUI();
    }

    public void TriggerNextDay()
    {
        daysInGame++;
        UpdateDayUI();

        // THÊM DÒNG NÀY: Khi nhảy ngày mới, bấm còi phát tín hiệu cho các script khác!
        OnNewDay?.Invoke();
    }

    private void UpdateDayUI()
    {
        if (dayUI != null) dayUI.text = "Day: " + daysInGame;
    }
}