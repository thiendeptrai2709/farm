using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SceneMusicConfig
{
    public string sceneName;
    public string[] musicTracks;
    public string ambientSFX;
}

public class CoreMusicManager : MonoBehaviour
{
    [Header("Cấu hình Nhạc cho từng Map")]
    public List<SceneMusicConfig> sceneConfigs = new List<SceneMusicConfig>();

    private void OnEnable()
    {
        LoadingManager.OnPlayerReady += StartTriggerSceneMusic;
    }

    private void OnDisable()
    {
        LoadingManager.OnPlayerReady -= StartTriggerSceneMusic;
    }

    private void Start()
    {
        if (LoadingManager.Instance == null || !LoadingManager.Instance.loadingPanel.activeSelf)
        {
            StartTriggerSceneMusic();
        }
    }

    // [MỚI] Dùng cái chốt chặn này để gọi Coroutine
    private void StartTriggerSceneMusic()
    {
        StartCoroutine(DelayedMusicTrigger());
    }

    // [MỚI] Đợi màn hình kéo rèm xong xuôi, game ổn định rồi mới lôi đàn ra gảy
    private IEnumerator DelayedMusicTrigger()
    {
        // Nhường CPU cho các hệ thống khác (NPC, Đất, Đồ vật) khởi động xong xuôi
        yield return new WaitForSeconds(0.5f);

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Menu") yield break;

        SceneMusicConfig config = sceneConfigs.Find(c => c.sceneName == currentScene);

        if (config != null && config.musicTracks != null && config.musicTracks.Length > 0)
        {
            int randomIndex = Random.Range(0, config.musicTracks.Length);
            string selectedTrack = config.musicTracks[randomIndex];

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(selectedTrack);

                if (!string.IsNullOrEmpty(config.ambientSFX))
                {
                    AudioManager.Instance.PlayLoopSFX(config.ambientSFX);
                }
                else
                {
                    AudioManager.Instance.StopLoopSFX();
                }
            }
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.StopLoopSFX();
            }
            Debug.LogWarning($"[CoreMusicManager] Map '{currentScene}' chưa được cấu hình nhạc.");
        }
    }
}