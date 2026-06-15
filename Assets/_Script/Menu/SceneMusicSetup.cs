using UnityEngine;
using System.Collections;

public class SceneMusicSetup : MonoBehaviour
{
    [Header("Cài đặt Âm thanh Map")]
    public string sceneMusicName = "BGM_Farm"; // Đổi tên này tùy từng Scene

    [Tooltip("Điền tên tiếng gió/chim chóc nếu có, bỏ trống nếu muốn tắt tiếng của Map trước")]
    public string ambientSfxName = "";

    private void Start()
    {
        StartCoroutine(PlayMusicAfterLoading());
    }

    private IEnumerator PlayMusicAfterLoading()
    {
        // Chờ LoadingManager tắt hẳn màn hình
        if (LoadingManager.Instance != null && LoadingManager.Instance.loadingPanel.activeSelf)
        {
            yield return new WaitUntil(() => !LoadingManager.Instance.loadingPanel.activeSelf);
        }

        if (AudioManager.Instance != null)
        {
            // Bật nhạc mới đè lên nhạc cũ
            if (!string.IsNullOrEmpty(sceneMusicName))
            {
                AudioManager.Instance.PlayMusic(sceneMusicName);
            }

            // Bật tiếng môi trường (nếu có), hoặc dọn dẹp tiếng môi trường của Scene trước
            if (!string.IsNullOrEmpty(ambientSfxName))
            {
                AudioManager.Instance.PlayLoopSFX(ambientSfxName);
            }
            else
            {
                AudioManager.Instance.StopLoopSFX();
            }
        }
    }
}