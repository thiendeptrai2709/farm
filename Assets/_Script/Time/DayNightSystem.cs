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
    public Light moonLight; // Điều khiển ánh sáng mặt trăng
    public GameObject sunSphere; // Quản lý ẩn hiện mô hình mặt trời 3D
    public Behaviour lensFlare; // Quản lý bật tắt component hiệu ứng lóa
    public TextMeshProUGUI timeUI;

    [Header("Danh sách Skybox Phase")]
    public List<SkyboxMapping> skyboxPhases;
    public float transitionSpeed = 0.5f;

    [Header("Hiệu ứng Âm U khi Mưa")]
    public float sunnyIntensity = 1.0f;
    public float rainyIntensity = 0.3f;
    public float lightChangeSpeed = 0.5f;

    [Header("Cường độ Ánh sáng Đêm")]
    public float moonIntensity = 0.5f; // Thiết lập độ sáng mặt trăng

    private bool dayTriggerLocked = false;

    [Header("Hiệu ứng Sương Mù (Fog)")]
    public float sunnyFogDensity = 0.002f;
    public float rainyFogDensity = 0.03f;
    public Color sunnyFogColor = new Color(0.6f, 0.8f, 1.0f);
    public Color rainyFogColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightFogColor = new Color(0.1f, 0.1f, 0.2f); // Màu sương mù ban đêm

    private void Start()
    {
        timeClock = GetComponent<TimeSystem>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        if (moonLight != null) moonLight.intensity = 0f; // Đặt cường độ mặt trăng ban đầu bằng 0
    }

    private void Update()
    {
        float p = timeClock.TimePercent;
        float currentHour = timeClock.hour;

        float rotX = (p * 360f) - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(rotX, 170f, 0f); // Cập nhật góc xoay mặt trời

        if (moonLight != null)
        {
            moonLight.transform.localRotation = Quaternion.Euler(rotX + 180f, 170f, 0f); // Cập nhật góc xoay mặt trăng ngược hướng mặt trời
        }

        if (Camera.main != null)
        {
            // CHỈ ép mỗi cục Sphere đi theo mắt người chơi, cách 500m về phía ngược lại của tia sáng mặt trời
            if (sunSphere != null)
            {
                sunSphere.transform.position = Camera.main.transform.position - sunLight.transform.forward * 500f;
            }
        }
        float targetSunIntensity = sunnyIntensity;
        float targetMoonIntensity = 0f;
        float targetFog = sunnyFogDensity;
        Color targetFogColor = sunnyFogColor;

        bool isNight = currentHour < 6f || currentHour > 18f; // Kiểm tra thời gian ban đêm

        if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == WeatherState.Raining)
        {
            targetSunIntensity = rainyIntensity;
            targetFog = rainyFogDensity;
            targetFogColor = rainyFogColor;
        }

        if (isNight)
        {
            targetSunIntensity = 0f; // Vô hiệu hóa ánh sáng mặt trời
            targetMoonIntensity = moonIntensity; // Kích hoạt ánh sáng mặt trăng
            if (WeatherManager.Instance == null || WeatherManager.Instance.currentWeather != WeatherState.Raining)
            {
                targetFogColor = nightFogColor; // Đổi màu sương mù ban đêm
            }
            if (sunSphere != null && sunSphere.activeSelf) sunSphere.SetActive(false); // Ẩn mô hình mặt trời 3D
            if (lensFlare != null && lensFlare.enabled) lensFlare.enabled = false; // Tắt component hiệu ứng lóa
        }
        else
        {
            if (sunSphere != null && !sunSphere.activeSelf) sunSphere.SetActive(true); // Hiển thị mô hình mặt trời 3D
            if (lensFlare != null && !lensFlare.enabled) lensFlare.enabled = true; // Bật component hiệu ứng lóa
        }

        sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetSunIntensity, Time.deltaTime * lightChangeSpeed); // Điều chỉnh mượt cường độ mặt trời
        if (moonLight != null) moonLight.intensity = Mathf.Lerp(moonLight.intensity, targetMoonIntensity, Time.deltaTime * lightChangeSpeed); // Điều chỉnh mượt cường độ mặt trăng

        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFog, Time.deltaTime * lightChangeSpeed); // Điều chỉnh mượt mật độ sương mù
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime * lightChangeSpeed); // Điều chỉnh mượt màu sương mù

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