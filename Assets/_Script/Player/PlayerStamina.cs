using UnityEngine;
using System;

public class PlayerStamina : MonoBehaviour
{
    public static PlayerStamina Instance { get; private set; }

    [Header("Stamina Stats")]
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("Action Costs")]
    public float hoeCost = 2f;
    public float waterCost = 1f;
    public float axeCost = 3f;
    public float fishCost = 4f;

    public bool isExhausted { get; private set; } = false;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public bool ConsumeStamina(float amount)
    {
        if (isExhausted) return false;

        currentStamina -= amount;
        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
            // Gọi sang file UI để nó lo việc hiện thông báo
            if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ShowExhaustedWarning();
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        if (currentStamina > 0f) isExhausted = false;
        if (currentStamina > maxStamina) currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void FullRestore()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    public void RestoreStaminaByHours(float hoursSlept)
    {
        // Cài đặt: Ngủ 8 tiếng là hồi tối đa (100%)
        float hoursForFullRestore = 8f;

        // Tính ra phần trăm hồi phục (Khóa ở mức tối đa 1.0 tức là 100%)
        float restorePercentage = Mathf.Clamp01(hoursSlept / hoursForFullRestore);

        // Tính số điểm thể lực nhận được
        float amountToRestore = maxStamina * restorePercentage;

        currentStamina += amountToRestore;

        if (currentStamina > 0f)
        {
            isExhausted = false;
        }

        if (currentStamina > maxStamina)
        {
            currentStamina = maxStamina;
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        Debug.Log($"Đã ngủ {hoursSlept} tiếng. Phục hồi được {restorePercentage * 100}% thể lực (+{amountToRestore} điểm).");
    }
    public void UpgradeMaxStamina(float amount, float absoluteMax)
    {
        if (maxStamina >= absoluteMax) return;

        maxStamina += amount;
        if (maxStamina > absoluteMax) maxStamina = absoluteMax;

        // Tự động hồi đầy máu coi như phần thưởng thăng cấp
        currentStamina = maxStamina;
        isExhausted = false;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}