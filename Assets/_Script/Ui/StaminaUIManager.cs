using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class StaminaUIManager : MonoBehaviour
{
    public static StaminaUIManager Instance;

    [Header("Giao diện Thể Lực")]
    public Slider staminaSlider;


    [Header("Đa Ngôn Ngữ")]
    public LocalizedString locExhausted;
    public LocalizedString locNotEnough;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Đăng ký nhận tín hiệu để thanh Slider tự động chạy theo
        if (PlayerStamina.Instance != null)
        {
            PlayerStamina.Instance.OnStaminaChanged += UpdateStaminaUI;
            UpdateStaminaUI(PlayerStamina.Instance.currentStamina, PlayerStamina.Instance.maxStamina);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStamina.Instance != null)
        {
            PlayerStamina.Instance.OnStaminaChanged -= UpdateStaminaUI;
        }
    }

    private void UpdateStaminaUI(float current, float max)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = max;
            staminaSlider.value = current;
        }
    }

    public void ShowExhaustedWarning()
    {
        if (NotificationManager.Instance == null) return;

        // Dịch chữ xong ném sang cho NotificationManager lo hiệu ứng
        string msg = locExhausted != null && !locExhausted.IsEmpty ? locExhausted.GetLocalizedString() : "Bạn đã kiệt sức!";
        NotificationManager.Instance.ShowNotification(msg);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
    }

    public void ShowNotEnoughWarning()
    {
        if (NotificationManager.Instance == null) return;

        // Dịch chữ xong ném sang cho NotificationManager lo hiệu ứng
        string msg = locNotEnough != null && !locNotEnough.IsEmpty ? locNotEnough.GetLocalizedString() : "Không đủ thể lực!";
        NotificationManager.Instance.ShowNotification(msg);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Error");
    }
    public void ToggleVisibility(bool isVisible)
    {
        if (staminaSlider != null) staminaSlider.gameObject.SetActive(isVisible);
    }
}