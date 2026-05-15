using UnityEngine;
using UnityEngine.Localization;

public class BusStop : MonoBehaviour, IInteractable
{
    public BusVehicle myBus;
    public LocalizedString interactText; // Chức năng: Lưu trữ key đa ngôn ngữ

    public string GetInteractText()
    {
        // Chức năng: Trả về văn bản đã dịch hoặc văn bản mặc định nếu chưa gán key
        if (interactText != null && !interactText.IsEmpty)
        {
            return interactText.GetLocalizedString();
        }
        return "[E] Xem lịch trình Xe Bus";
    }

    public void Interact()
    {
        if (BusUI.Instance != null)
        {
            BusUI.Instance.OpenUI(this.transform);
        }
    }
}