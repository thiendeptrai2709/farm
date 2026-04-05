using System;
using UnityEngine;

public enum WeatherState { Sunny, Raining }

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Trạng thái hiện tại")]
    public WeatherState currentWeather = WeatherState.Sunny;

    [Header("1. Cài đặt Mưa")]
    [Tooltip("Thời gian mưa kéo dài bao nhiêu giây đời thực?")]
    public float rainDuration = 60f; // 60 giây = 1 phút
    private float rainTimer;

    [Header("2. Cài đặt Bộ Chống Mưa (Cooldown)")]
    [Tooltip("1 ngày trong game bằng bao nhiêu phút đời thực?")]
    public float realMinutesPerGameDay = 24f;
    [Tooltip("Số ngày game TỐI THIỂU không có mưa")]
    public int minDaysCooldown = 3;
    [Tooltip("Số ngày game TỐI ĐA không có mưa")]
    public int maxDaysCooldown = 5;

    [SerializeField] // Hiện lên Inspector để ông dễ theo dõi thời gian đếm ngược
    private float cooldownTimer = 0f;

    [Header("3. Cài đặt Xổ Số (Sau khi hết khiên chống mưa)")]
    [Tooltip("Bao nhiêu giây thì quay xổ số 1 lần?")]
    public float rollInterval = 60f;
    [Tooltip("Tỷ lệ trúng mưa (%)")]
    public float rainChance = 15f;
    private float rollTimer;

    [Header("Hiệu ứng hình ảnh")]
    public GameObject rainParticleSystem;

    public AudioClip rainSound;
    private AudioSource rainAudioSource;

    public event Action<WeatherState> OnWeatherChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rainAudioSource = gameObject.AddComponent<AudioSource>();
        rainAudioSource.clip = rainSound;
        rainAudioSource.loop = true;       // Mưa kêu liên tục
        rainAudioSource.spatialBlend = 0f; // Chuẩn 2D: Đứng đâu trong map cũng nghe thấy
        rainAudioSource.playOnAwake = false;

    }

    private void Start()
    {
        // Vừa vào game thì cho trời nắng và đếm ngược xổ số luôn
        currentWeather = WeatherState.Sunny;
        rollTimer = rollInterval;
        UpdateWeatherVisuals();
    }

    private void Update()
    {
        // GIAI ĐOẠN 1: ĐANG MƯA
        if (currentWeather == WeatherState.Raining)
        {
            rainTimer -= Time.deltaTime;
            if (rainTimer <= 0f)
            {
                StopRain(); // Hết 1 phút -> Tắt mưa
            }
        }
        // GIAI ĐOẠN 2 & 3: ĐANG NẮNG
        else
        {
            // Giai đoạn 2: Đang trong thời gian "Khiên Chống Mưa"
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
            // Giai đoạn 3: Đã hết chống mưa -> Bắt đầu quay xổ số
            else
            {
                rollTimer -= Time.deltaTime;
                if (rollTimer <= 0f)
                {
                    rollTimer = rollInterval; // Reset đồng hồ quay
                    RollWeather();
                }
            }
        }
    }

    private void RollWeather()
    {
        float chance = UnityEngine.Random.Range(0f, 100f);
        if (chance <= rainChance)
        {
            StartRain();
        }
    }

    // ===============================================
    // CÁC HÀM XỬ LÝ CHUYỂN ĐỔI THỜI TIẾT
    // ===============================================
    private void StartRain()
    {
        currentWeather = WeatherState.Raining;
        rainTimer = rainDuration; // Cài giờ mưa đúng 1 phút

        UpdateWeatherVisuals();
        OnWeatherChanged?.Invoke(currentWeather);

        Debug.Log("[ĐÀI KHÍ TƯỢNG] Bắt đầu mưa! Kéo dài trong " + rainDuration + " giây.");
    }

    private void StopRain()
    {
        currentWeather = WeatherState.Sunny;

        // Tính toán bộ chống mưa (Cooldown)
        // Công thức: Đổi số Phút -> Giây (x60), rồi nhân với số ngày ngẫu nhiên
        float realSecondsPerDay = realMinutesPerGameDay * 60f;
        int randomDays = UnityEngine.Random.Range(minDaysCooldown, maxDaysCooldown + 1);
        cooldownTimer = randomDays * realSecondsPerDay;

        rollTimer = rollInterval; // Reset lại đồng hồ xổ số để chờ sẵn

        UpdateWeatherVisuals();
        OnWeatherChanged?.Invoke(currentWeather);

        Debug.Log($"[ĐÀI KHÍ TƯỢNG] Đã tạnh mưa! Bật khiên chống mưa trong {randomDays} ngày game ({cooldownTimer} giây đời thực).");
    }

    private void UpdateWeatherVisuals()
    {
        if (rainParticleSystem != null)
        {
            rainParticleSystem.SetActive(currentWeather == WeatherState.Raining);
        }

        if (rainAudioSource != null && rainSound != null)
        {
            if (currentWeather == WeatherState.Raining && !rainAudioSource.isPlaying)
            {
                rainAudioSource.Play();
            }
            else if (currentWeather == WeatherState.Sunny && rainAudioSource.isPlaying)
            {
                rainAudioSource.Stop();
            }
        }
    }
}

