using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Giấc ngủ")]
    [Tooltip("Giờ thức dậy (Ví dụ: 6 là 6h sáng)")]
    public float wakeUpTime = 6f;

    public string GetInteractText()
    {
        // Gắn chữ báo giờ lên UI luôn cho xịn
        return $"[E] Sleep until {wakeUpTime}:00 AM";
    }

    public void Interact()
    {
        // Tìm não bộ quản lý thời gian trên Scene
        TimeSystem timeSystem = FindAnyObjectByType<TimeSystem>();

        if (timeSystem != null)
        {
            // Gọi lệnh đi ngủ!
            timeSystem.SkipToMorning(wakeUpTime);

            // Ở đây sau này ông có thể thêm đoạn làm màn hình chớp đen (Fade Black) 
            // hoặc hồi lại 100% Máu/Thể lực cho Player
            Debug.Log("Zzz... Ngủ ngon! Mọi thứ đã được tua nhanh.");
        }
        else
        {
            Debug.LogError("Không tìm thấy TimeSystem trên Scene!");
        }
    }
}