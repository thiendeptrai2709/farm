using UnityEngine;

public class NoticeBoard : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        // Chức năng: Hiển thị chữ tương tác UI
        return "[E] Xem Thông Báo Chợ";
    }

    public void Interact()
    {
        if (NoticeBoardUIManager.Instance != null)
        {
            NoticeBoardUIManager.Instance.OpenBoard(this.transform);
        }
    }
}