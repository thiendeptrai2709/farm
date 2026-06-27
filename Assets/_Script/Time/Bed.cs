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
        // Chức năng: Mở bảng chọn ngủ
        if (SleepUIManager.Instance != null)
        {
            SleepUIManager.Instance.OpenSleepPanel();
        }
    }
}