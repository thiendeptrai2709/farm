using UnityEngine;

public class MenuWeatherController : MonoBehaviour
{
    public GameObject rainParticle;
    public GameObject fireflyParticle;

    [Header("Music Settings")]
    public string rainMusicName = "BGM_Menu_Rain";
    public string clearMusicName = "BGM_Menu_Clear";

    [Header("Ambient SFX Settings")]
    public string rainAmbientSFX = "Rain_Loop";

    private void Start()
    {
        int randomChance = UnityEngine.Random.Range(1, 101);

        // TẠM THỜI SỬA THÀNH <= 100 ĐỂ ÉP TRỜI MƯA 100%
        if (randomChance <= 10)
        {
            rainParticle.SetActive(true);
            fireflyParticle.SetActive(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(rainMusicName);
                AudioManager.Instance.PlayLoopSFX(rainAmbientSFX);
            }
            else
            {
            }
        }
        else
        {
            rainParticle.SetActive(false);
            fireflyParticle.SetActive(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(clearMusicName);
                AudioManager.Instance.StopLoopSFX();
            }
        }
    }
}