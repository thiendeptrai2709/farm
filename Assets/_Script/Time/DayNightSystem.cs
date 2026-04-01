using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class SkyboxMapping
{
    public string name;
    public float hour;
    public Material skyboxMat;
    [Range(0, 1)] public float targetBlend; // THÊM: 0 là Ngày, 1 là Đêm
}

[RequireComponent(typeof(TimeSystem))]
public class DayNightSystem : MonoBehaviour
{
    private TimeSystem timeClock;

    [Header("Ánh sáng & UI")]
    public Light sunLight;
    public TextMeshProUGUI timeUI;

    [Header("Danh sách Skybox Phase")]
    public List<SkyboxMapping> skyboxPhases;
    public float transitionSpeed = 0.5f; // Tốc độ hòa trộn màu

    [Header("Hiệu ứng Âm U khi Mưa")]
    public float sunnyIntensity = 1.0f;  // Độ chói lúc nắng
    public float rainyIntensity = 0.3f;  // Độ chói lúc mưa (âm u)
    public float lightChangeSpeed = 0.5f;

    private bool dayTriggerLocked = false;

    [Header("Hiệu ứng Sương Mù (Fog)")]
    public float sunnyFogDensity = 0.002f; // Nắng: Sương mỏng dính (gần như ko có)
    public float rainyFogDensity = 0.03f;  // Mưa: Sương mù mịt che khuất tầm nhìn
    public Color rainyFogColor = new Color(0.5f, 0.5f, 0.5f);

    private void Start()
    {
        timeClock = GetComponent<TimeSystem>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared; // Kiểu sương mù đẹp và chân thực nhất
        RenderSettings.fogColor = rainyFogColor;



    }

    private void Update()
    {
        float p = timeClock.TimePercent;
        float currentHour = timeClock.hour;

        // 1. Xoay mặt trời
        float rotX = (p * 360f) - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(rotX, 170f, 0f);

        float targetIntensity = sunnyIntensity;
        float targetFog = sunnyFogDensity;

        if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == WeatherState.Raining)
        {
            targetIntensity = rainyIntensity;
            targetFog = rainyFogDensity;
        }
        // Từ từ làm tối/sáng mặt trời cho mượt
        sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, Time.deltaTime * lightChangeSpeed);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFog, Time.deltaTime * lightChangeSpeed);

        // 2. HIỆN GIỜ VÀ PHÚT LÊN UI
        if (timeUI != null)
        {
            int h = timeClock.CurrentHour;
            int m = timeClock.CurrentMinute;
            timeUI.text = string.Format("{0:00}:{1:00}", h, m);
        }

        // 3. Xử lý Skybox Cubemap
        HandleSkyboxTransition(currentHour);

        // 4. Trigger ngày mới
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

        // Tìm phase phù hợp nhất với giờ hiện tại
        foreach (var phase in skyboxPhases)
        {
            if (hour >= phase.hour) currentPhase = phase;
        }

        if (currentPhase != null && currentPhase.skyboxMat != null)
        {
            // Gán material vào skybox nếu chưa đúng
            if (RenderSettings.skybox != currentPhase.skyboxMat)
            {
                RenderSettings.skybox = currentPhase.skyboxMat;
            }

            // Thay vì tăng lên 1, chúng ta nhắm tới targetBlend của phase đó
            if (RenderSettings.skybox.HasProperty("_Blend"))
            {
                float currentBlend = RenderSettings.skybox.GetFloat("_Blend");
                float nextBlend = Mathf.MoveTowards(currentBlend, currentPhase.targetBlend, Time.deltaTime * transitionSpeed);
                RenderSettings.skybox.SetFloat("_Blend", nextBlend);
            }
        }
    }
}