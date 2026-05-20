using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOptimizer : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform player;

    [Header("Cài đặt tối ưu")]
    [Tooltip("Khoảng cách cắt điện (Nên đặt lớn hơn Max Distance của AudioSource 1 chút)")]
    public float disableDistance = 26f; // Nếu Max Distance là 25, thì 26 cắt điện là đẹp

    // [ĐÃ THÊM]: Lưu lại âm lượng ban đầu ông setup trên Inspector
    private float baseVolume;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Chốt âm lượng gốc ngay từ lúc game mới bắt đầu chạy
        baseVolume = audioSource.volume;

        // Tự động tìm Player trong map
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
        }
    }

    private void Update()
    {
        // Nếu không tìm thấy Player thì thôi không làm gì cả
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform; // Đã tìm thấy!
            }
            else
            {
                // BẢO HIỂM: Nếu vẫn chưa thấy Player xuất hiện, tạm thời "cắt điện" để game tĩnh lặng
                if (audioSource.isPlaying)
                {
                    audioSource.Pause();
                }
                return; // Dừng Update tại đây, chờ frame sau tìm tiếp
            }
        }
        // Đo khoảng cách từ Thác nước đến Player
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > disableDistance)
        {
            // Đi quá xa -> Cắt điện (Pause để tiết kiệm 100% CPU)
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
        else
        {
            // Lại gần -> Cấp điện lại (Phát tiếp từ chỗ vừa tạm dừng)
            if (!audioSource.isPlaying)
            {
                audioSource.UnPause();
            }

            // [ĐÃ SỬA]: Lấy Âm lượng gốc nhân với tỷ lệ phần trăm của thanh trượt
            if (AudioManager.Instance != null)
            {
                audioSource.volume = baseVolume * AudioManager.Instance.GetSFXVolume();
            }
        }
    }

    // [ĐÃ THÊM]: Vẽ vòng Gizmo màu đỏ để căn khoảng cách cắt điện
    private void OnDrawGizmosSelected()
    {
        // Chọn màu đỏ, hơi trong suốt một chút nếu ông thích
        Gizmos.color = Color.red;

        // Vẽ vòng tròn bao quanh object dựa trên biến disableDistance
        Gizmos.DrawWireSphere(transform.position, disableDistance);
    }
}