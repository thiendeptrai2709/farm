using UnityEngine;
using UnityEngine.Localization;

public class NoticeBoard : MonoBehaviour, IInteractable
{
    [Header("Đa Ngôn Ngữ")]
    public LocalizedString interactText;

    public string GetInteractText()
    {
        // Chức năng: Hiển thị chữ tương tác UI
        return interactText.IsEmpty ? "[E] Xem Thông Báo Chợ" : interactText.GetLocalizedString();
    }

    public void Interact()
    {
        if (NoticeBoardUIManager.Instance != null)
        {
            NoticeBoardUIManager.Instance.OpenBoard(this.transform);
        }
    }
}