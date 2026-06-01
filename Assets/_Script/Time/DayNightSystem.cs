using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class SkyboxMapping
{
    public string name;
    public float hour;
    public Material skyboxMat;
    [Range(0, 1)] public float targetBlend;
}

[RequireComponent(typeof(TimeSystem))]
public class DayNightSystem : MonoBehaviour
{
    private TimeSystem timeClock;

    [Header("Ánh sáng & UI")]
    public Light sunLight;
    public Light moonLight;
    public GameObject sunSphere;
    public TextMeshProUGUI timeUI;
    private Behaviour lensFlareComponent;

    [Header("Danh sách Skybox Phase")]
    public List<SkyboxMapping> skyboxPhases;
    public float transitionSpeed = 0.5f;

    [Header("Màu Ánh sáng Ban ngày Gốc")]
    public Color defaultSunColor = Color.white;

    [Header("Hiệu ứng Âm U khi Mưa")]
    public float sunnyIntensity = 1.0f;
    public float rainyIntensity = 0.3f;
    public float lightChangeSpeed = 0.5f;

    [Header("Cường độ Ánh sáng Đêm")]
    public float moonIntensity = 0.5f;

    private bool dayTriggerLocked = false;

    [Header("Hiệu ứng Sương Mù (Fog)")]
    public float sunnyFogDensity = 0.002f;
    public float rainyFogDensity = 0.03f;
    public Color sunnyFogColor = new Color(0.6f, 0.8f, 1.0f);
    public Color rainyFogColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightFogColor = new Color(0.1f, 0.1f, 0.2f);

    // --- CÁC BIẾN NHẬN LỆNH TỪ RỪNG SƯƠNG MÙ ---
    [HideInInspector] public bool isInsideDarkZone = false;
    [HideInInspector] public float darkZoneIntensity;
    [HideInInspector] public Color darkZoneSunColor;
    [HideInInspector] public float darkZoneFogDensity;
    [HideInInspector] public Color darkZoneFogColor;

    private float darkZoneLerpSpeed = 3f;
    private bool isTransitioningDarkZone = false;

    private void Start()
    {
        timeClock = GetComponent<TimeSystem>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        if (moonLight != null) moonLight.intensity = 0f;
        if (sunLight != null) sunLight.color = defaultSunColor;
        if (sunLight != null)
        {
            lensFlareComponent = sunLight.GetComponent("UnityEngine.Rendering.LensFlareComponentSRP") as Behaviour;

            // Nếu bạn lỡ dùng cục Lens Flare bản cũ thì nó sẽ dùng dòng này vớt lại
            if (lensFlareComponent == null) lensFlareComponent = sunLight.GetComponent("LensFlare") as Behaviour;
        }
    }

    public void SetDarkZone(bool isInside, float intensity, Color sunCol, float fogDensity, Color fogCol)
    {
        isInsideDarkZone = isInside;
        darkZoneIntensity = intensity;
        darkZoneSunColor = sunCol;
        darkZoneFogDensity = fogDensity;
        darkZoneFogColor = fogCol;

        isTransitioningDarkZone = true;

        // NẾU BƯỚC RA NGOÀI LÀ BẬT SKYBOX NGAY LẬP TỨC
        if (!isInside && Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }
    }

    private void Update()
    {
        float p = timeClock.TimePercent;
        float currentHour = timeClock.hour;

        float rotX = (p * 360f) - 90f;
        if (sunLight != null) sunLight.transform.localRotation = Quaternion.Euler(rotX, 170f, 0f);

        if (moonLight != null)
        {
            moonLight.transform.localRotation = Quaternion.Euler(rotX + 180f, 170f, 0f);
        }

        if (Camera.main != null && sunSphere != null && sunLight != null)
        {
            float maxDistance = Camera.main.farClipPlane - 50f;
            sunSphere.transform.position = Camera.main.transform.position - sunLight.transform.forward * maxDistance;
        }

        float targetSunIntensity = sunnyIntensity;
        Color targetSunColor = defaultSunColor;
        float targetMoonIntensity = 0f;
        float targetFog = sunnyFogDensity;
        Color targetFogColor = sunnyFogColor;

        bool isNight = currentHour < 6f || currentHour > 19f;

        if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == WeatherState.Raining)
        {
            targetSunIntensity = rainyIntensity;
            targetFog = rainyFogDensity;
            targetFogColor = rainyFogColor;
        }

        if (isInsideDarkZone)
        {
            if (!isNight)
            {
                targetSunIntensity = darkZoneIntensity;
                targetSunColor = darkZoneSunColor;
            }
            targetFog = darkZoneFogDensity;
            targetFogColor = darkZoneFogColor;
        }
        else if (isTransitioningDarkZone)
        {
            // ÉP MÀU SƯƠNG TRỞ LẠI BÌNH THƯỜNG NGAY LẬP TỨC (KHÔNG LERP MÀU NỮA)
            RenderSettings.fogColor = targetFogColor;
        }

        if (isNight)
        {
            targetSunIntensity = 0f;
            targetMoonIntensity = moonIntensity;

            // CHỈ đổi sang sương mù đêm (màu xanh tím) nếu đang Ở NGOÀI RỪNG
            if (!isInsideDarkZone)
            {
                if (WeatherManager.Instance == null || WeatherManager.Instance.currentWeather != WeatherState.Raining)
                {
                    targetFogColor = nightFogColor;
                }
            }
        }
        // TẮT NGAY LẬP TỨC nếu bước vào rừng hoặc trời tối
        bool hideSunAndFlare = isInsideDarkZone || isNight;

        if (hideSunAndFlare)
        {
            if (sunSphere != null && sunSphere.activeSelf) sunSphere.SetActive(false);
            if (lensFlareComponent != null && lensFlareComponent.enabled) lensFlareComponent.enabled = false;
        }
        else
        {
            // BẬT NGAY LẬP TỨC khi ra khỏi rừng
            if (sunSphere != null && !sunSphere.activeSelf) sunSphere.SetActive(true);
            if (lensFlareComponent != null && !lensFlareComponent.enabled) lensFlareComponent.enabled = true;
        }

        // ==========================================
        // ÁP DỤNG THAY ĐỔI ÁNH SÁNG & SƯƠNG MÙ
        // ==========================================
        float currentSpeed = isTransitioningDarkZone ? darkZoneLerpSpeed : lightChangeSpeed;
        float speed = Time.deltaTime * currentSpeed;

        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetSunIntensity, speed);
            sunLight.color = Color.Lerp(sunLight.color, targetSunColor, speed);
        }

        if (moonLight != null) moonLight.intensity = Mathf.Lerp(moonLight.intensity, targetMoonIntensity, speed);

        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFog, speed);

        // Chỉ Lerp màu nếu không phải đang trong giai đoạn đi ra khỏi rừng
        if (!isTransitioningDarkZone || isInsideDarkZone)
        {
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, speed);
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = RenderSettings.fogColor;

            // Nếu ĐANG TRONG RỪNG và sương bắt đầu che (50%), thì biến bầu trời thành sương mù
            if (isInsideDarkZone && RenderSettings.fogDensity >= targetFog * 0.5f)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        if (isTransitioningDarkZone && Mathf.Abs(RenderSettings.fogDensity - targetFog) < 0.001f)
        {
            isTransitioningDarkZone = false;
        }

        if (timeUI != null)
        {
            int h = timeClock.CurrentHour;
            int m = timeClock.CurrentMinute;
            timeUI.text = string.Format("{0:00}:{1:00}", h, m);
        }

        HandleSkyboxTransition(currentHour);

        if (timeClock.CurrentHour == 0 && timeClock.CurrentMinute == 0 && !dayTriggerLocked)
        {
            if (TimeManager.Instance != null) TimeManager.Instance.TriggerNextDay();
            dayTriggerLocked = true;
        }
        else if (timeClock.CurrentHour != 0)
        {
            dayTriggerLocked = false;
        }
    }

    private void HandleSkyboxTransition(float hour)
    {
        SkyboxMapping currentPhase = null;

        foreach (var phase in skyboxPhases)
        {
            if (hour >= phase.hour) currentPhase = phase;
        }

        if (currentPhase != null && currentPhase.skyboxMat != null)
        {
            if (RenderSettings.skybox != currentPhase.skyboxMat)
            {
                RenderSettings.skybox = currentPhase.skyboxMat;
            }

            if (RenderSettings.skybox.HasProperty("_Blend"))
            {
                float currentBlend = RenderSettings.skybox.GetFloat("_Blend");
                float nextBlend = Mathf.MoveTowards(currentBlend, currentPhase.targetBlend, Time.deltaTime * transitionSpeed);
                RenderSettings.skybox.SetFloat("_Blend", nextBlend);
            }
        }
    }
}