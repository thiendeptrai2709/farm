using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOptimizer : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform player;

    [Header("Cài đặt tối ưu")]
    [Tooltip("Khoảng cách cắt điện (Nên đặt lớn hơn Max Distance của AudioSource 1 chút)")]
    public float disableDistance = 26f; // Nếu Max Distance là 25, thì 26 cắt điện là đẹp

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

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
        if (player == null) return;

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
        }
    }
}