using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Danh sách âm thanh SFX")]
    public AudioData[] sfxData;

    private Dictionary<string, AudioData> sfxDictionary;
    private AudioSource sfxSource;
    private AudioSource loopSource;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nguyên khi chuyển Scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động thêm một loa phát (AudioSource) vào AudioManager
        sfxSource = gameObject.AddComponent<AudioSource>();

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;

        // Chuyển mảng dữ liệu vào Từ điển (Dictionary) để gọi tên cho nhanh
        sfxDictionary = new Dictionary<string, AudioData>();
        foreach (var audio in sfxData)
        {
            sfxDictionary[audio.soundName] = audio;
        }
    }

    // Hàm công khai để các file khác gọi đến
    public void PlaySFX(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            // Thay đổi cao độ một chút xíu để tiếng bước chân không bị lặp lại nhàm chán
            sfxSource.pitch = data.pitch * Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(data.clip, data.volume);
        }
        else
        {
            Debug.LogWarning("AudioManager: Không tìm thấy âm thanh có tên -> " + name);
        }
    }
    public void PlayLoopSFX(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            loopSource.clip = data.clip;
            loopSource.volume = data.volume;
            loopSource.pitch = data.pitch;
            loopSource.Play();
        }
    }

    // [THÊM MỚI] Hàm Tắt tiếng lặp
    public void StopLoopSFX()
    {
        if (loopSource.isPlaying) loopSource.Stop();
    }
}