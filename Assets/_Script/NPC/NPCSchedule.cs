using UnityEngine;

// [ĐÃ SỬA]: Bỏ MonoBehaviour đi, thêm [System.Serializable] để nó hiện ra trong bảng Inspector của NPC
[System.Serializable]
public class NPCSchedule
{
    [Tooltip("Giờ bắt đầu đi làm (Ví dụ: 7.5 = 7h30 sáng)")]
    public float workStartTime = 7.5f;

    [Tooltip("Giờ tan ca về nhà (Ví dụ: 17.5 = 5h30 chiều)")]
    public float workEndTime = 17.5f;
}