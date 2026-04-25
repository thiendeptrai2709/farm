using UnityEngine;

public class Chair : MonoBehaviour, IInteractable
{
    [Header("Điểm neo")]
    [Tooltip("Tạo 1 GameObject trống làm con của ghế, canh tọa độ đặt đúng vào mặt ghế để mông nhân vật dính vào đây")]
    public Transform sitPoint;

    [Tooltip("Chỗ nhân vật sẽ xuất hiện sau khi đứng lên (Tránh bị kẹt vào model ghế)")]
    public Transform exitPoint;

    [HideInInspector] public bool isOccupied = false;

    public string GetInteractText()
    {
        if (isOccupied) return ""; // Lúc đang ngồi thì hệ thống Player sẽ tự hiện chữ báo thoát
        return "[E] Ngồi nghỉ";
    }

    public void Interact()
    {
        if (!isOccupied)
        {
            // Gọi thằng Player đến ngồi
            PlayerInteraction playerInteract = Object.FindFirstObjectByType<PlayerInteraction>();
            if (playerInteract != null)
            {
                playerInteract.SitDown(this);
            }
        }
    }
}