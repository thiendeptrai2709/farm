using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    [Header("Cài đặt thời gian thực")]
    [Tooltip("Độ dài 1 ngày trong game tính bằng phút thực (24 phút)")]
    public float dayDurationInMinutes = 24f;

    [Header("Thời gian hiện tại")]
    [Range(0, 24)]
    public float hour = 12f;

    // Biến để các script khác đọc tỷ lệ thời gian (0 đến 1)
    public float TimePercent => hour / 24f;

    // THÊM: Lấy giờ nguyên
    public int CurrentHour => Mathf.FloorToInt(hour);
    // THÊM: Lấy phút (Phần dư của hour * 60)
    public int CurrentMinute => Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60);

    private void Update()
    {
        // 24h game / (số phút thực * 60 giây)
        float speed = 24f / (dayDurationInMinutes * 60f);
        hour += Time.deltaTime * speed;

        if (hour >= 24f) hour = 0f;
    }
    public void SkipToMorning(float wakeUpHour = 6f) // Mặc định báo thức 6h sáng
    {
        float inGameHoursSkipped = 0f;

        // Tính xem đã ngủ qua bao nhiêu tiếng trong game
        if (hour < wakeUpHour)
        {
            // Ngủ từ 2h sáng đến 6h sáng -> Trôi qua 4 tiếng
            inGameHoursSkipped = wakeUpHour - hour;
        }
        else
        {
            // Ngủ từ 22h đêm đến 6h sáng hôm sau -> Trôi qua 8 tiếng
            inGameHoursSkipped = (24f - hour) + wakeUpHour;

            // Ép hệ thống nhảy sang Ngày Mới ngay lập tức
            if (TimeManager.Instance != null) TimeManager.Instance.TriggerNextDay();
        }

        // Cài lại đồng hồ thành 6h sáng
        hour = wakeUpHour;

        // ĐỔI TỪ GIỜ GAME SANG GIÂY NGOÀI ĐỜI
        // Công thức: 1 Giờ Game = (Tổng giây 1 ngày) / 24
        float realSecondsPerHour = (dayDurationInMinutes * 60f) / 24f;
        float realSecondsToFastForward = inGameHoursSkipped * realSecondsPerHour;

        // TÌM TẤT CẢ Ô ĐẤT TRÊN BẢN ĐỒ VÀ BƠM GIÂY CHO CHÚNG NÓ
        FarmPlot[] allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);

        foreach (FarmPlot plot in allPlots)
        {
            plot.FastForwardTime(realSecondsToFastForward);
        }
        TreePit[] allTrees = FindObjectsByType<TreePit>(FindObjectsSortMode.None);
        foreach (TreePit tree in allTrees) tree.FastForwardTime(realSecondsToFastForward);
        Debug.Log($"Đã ngủ dậy! Trôi qua {inGameHoursSkipped} giờ in-game. Ép cây trồng cộng thêm {realSecondsToFastForward} giây thật.");
    }
}