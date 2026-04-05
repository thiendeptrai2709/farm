using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterAudioEvent : MonoBehaviour
{
    [Header("Âm thanh bước chân NPC")]
    public AudioClip footstepClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Cài đặt cứng thành Loa 3D
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 1 = 3D (Xa nhỏ, gần to)
        audioSource.minDistance = 2f;  // Dưới 2m nghe to nhất
        audioSource.maxDistance = 15f; // Quá 15m là tịt ngòi
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    // Hàm gọi từ Animation Event
    public void AE_PlayFootstep()
    {
        if (footstepClip != null)
        {
            // Chỉ phát đúng 1 cái clip m ném vào
            audioSource.PlayOneShot(footstepClip);
        }
    }
}