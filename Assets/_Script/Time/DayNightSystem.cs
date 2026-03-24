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

    private bool dayTriggerLocked = false;

    private void Start()
    {
        timeClock = GetComponent<TimeSystem>();
    }

    private void Update()
    {
        float p = timeClock.TimePercent;
        float currentHour = timeClock.hour;

        // 1. Xoay mặt trời
        float rotX = (p * 360f) - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(rotX, 170f, 0f);

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