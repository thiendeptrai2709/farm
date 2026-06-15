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
        StartCoroutine(SetupWeatherAndMusic());
    }

    private System.Collections.IEnumerator SetupWeatherAndMusic()
    {
        // [ĐÃ THÊM]: Cầm chân luồng code, chờ đến khi màn hình Loading tắt hẳn mới chạy tiếp
        if (LoadingManager.Instance != null && LoadingManager.Instance.loadingPanel.activeSelf)
        {
            yield return new WaitUntil(() => !LoadingManager.Instance.loadingPanel.activeSelf);
        }

        int randomChance = UnityEngine.Random.Range(1, 101);

        if (randomChance <= 10)
        {
            rainParticle.SetActive(true);
            fireflyParticle.SetActive(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(rainMusicName);
                AudioManager.Instance.PlayLoopSFX(rainAmbientSFX);
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