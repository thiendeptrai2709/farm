using UnityEngine;

public class ForestFogTrigger : MonoBehaviour
{
    [Header("Cài đặt Khu Vực Tối (Dark Zone)")]
    public Color zoneFogColor = Color.gray;
    public float zoneFogDensity = 0.05f;
    public Color zoneSunColor = new Color(0.3f, 0.3f, 0.4f);
    public float zoneSunIntensity = 0.2f;

    [Header("Những Vật Thể Bí Mật (Chỉ hiện khi ở trong rừng)")]
    [Tooltip("Kéo các vật thể (như ngôi mộ, đốm sáng...) vào đây")]
    public GameObject[] secretObjects;

    private DayNightSystem dayNightSystem;

    private void Start()
    {
        dayNightSystem = FindFirstObjectByType<DayNightSystem>();

        // Tự động GIẤU ĐI tất cả các vật thể bí mật khi game vừa chạy
        if (secretObjects != null)
        {
            foreach (var obj in secretObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (dayNightSystem == null)
            {
                dayNightSystem = FindFirstObjectByType<DayNightSystem>();
            }

            if (dayNightSystem != null)
            {
                dayNightSystem.SetDarkZone(true, zoneSunIntensity, zoneSunColor, zoneFogDensity, zoneFogColor);
            }

            // HIỆN tất cả vật thể bí mật ra khi bước vào rừng
            if (secretObjects != null)
            {
                foreach (var obj in secretObjects)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (dayNightSystem == null) dayNightSystem = FindFirstObjectByType<DayNightSystem>();

            if (dayNightSystem != null)
            {
                dayNightSystem.SetDarkZone(false, 0f, Color.white, 0f, Color.white);
            }

            // GIẤU LẠI tất cả vật thể bí mật khi bước ra ngoài
            if (secretObjects != null)
            {
                foreach (var obj in secretObjects)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
    }
}