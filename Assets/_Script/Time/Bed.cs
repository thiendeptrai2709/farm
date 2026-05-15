using UnityEngine;
using UnityEngine.Localization;

public class Bed : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactText;

    [Header("Cài đặt Giấc ngủ")]
    public float canSleepAfterHour = 22f; // Chức năng: Cài đặt giờ bắt đầu được phép ngủ (10h tối)
    public float canSleepBeforeHour = 6f; // Chức năng: Cài đặt giờ kết thúc giới hạn ngủ (6h sáng)

    public string GetInteractText()
    {
        // Chức năng: Trả về chữ tương tác
        return interactText.IsEmpty ? "[E] Đi Ngủ" : interactText.GetLocalizedString();
    }

    public void Interact()
    {
        TimeSystem timeSystem = FindAnyObjectByType<TimeSystem>();

        if (timeSystem != null)
        {
            // Chức năng: Kiểm tra điều kiện thời gian từ 22h đêm đến 6h sáng
            if (timeSystem.hour >= canSleepAfterHour || timeSystem.hour <= canSleepBeforeHour)
            {
                if (SleepUIManager.Instance != null)
                {
                    SleepUIManager.Instance.OpenSleepPanel();
                }
            }
            else
            {
                // Chức năng: Thông báo nếu chưa đến giờ ngủ
                Debug.Log("Chưa đến 10h tối, không thể ngủ được!");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy TimeSystem trên Scene!");
        }
    }
}