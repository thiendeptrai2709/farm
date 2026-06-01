using UnityEngine;

public class SkeletonInteract : MonoBehaviour, IInteractable
{
    // Cờ kiểm tra xem xương đã ngoi lên hẳn chưa (Chỉ cho phép tương tác khi đã ngoi lên)
    public bool isReadyToTalk = false;

    public void Interact()
    {
        if (!isReadyToTalk) return;

        // Tại đây bạn có thể gọi hệ thống hội thoại (Dialogue) của bạn ra
        Debug.Log("Bộ xương nói: Cảm ơn ngươi đã ráp ta lại!");
    }

    public string GetInteractText()
    {
        if (!isReadyToTalk) return string.Empty;

        return "[E] Trò chuyện";
    }
}