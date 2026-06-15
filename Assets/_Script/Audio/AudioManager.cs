using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioData[] sfxData;

    private Dictionary<string, AudioData> sfxDictionary;
    private AudioSource sfxSource;
    private AudioSource loopSource;
    private AudioSource musicSource;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool isMuted = false;

    public float fadeDuration = 1f;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        sfxDictionary = new Dictionary<string, AudioData>();
        foreach (var audio in sfxData)
        {
            sfxDictionary[audio.soundName] = audio;
        }

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Kích hoạt ngay lập tức các trạng thái vừa nạp
        ToggleMute(isMuted);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    public void PlayMusic(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeMusic(data));
        }
    }

    private System.Collections.IEnumerator FadeMusic(AudioData newData)
    {
        float targetVolume = newData.volume * musicVolume;

        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }
            musicSource.volume = 0f;
        }

        musicSource.clip = newData.clip;
        musicSource.Play();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public void PlaySFX(string name)
    {
        if (isMuted) return;
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            sfxSource.pitch = data.pitch * Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(data.clip, data.volume * sfxVolume);
        }
    }

    public void PlayLoopSFX(string name)
    {
        Debug.Log("[AudioManager] Đang gọi PlayLoopSFX với tên: " + name);
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            loopSource.clip = data.clip;
            loopSource.volume = isMuted ? 0 : data.volume * sfxVolume;
            loopSource.pitch = data.pitch;
            loopSource.Play();
            Debug.Log("[AudioManager] Đã phát loopSFX thành công. Volume hiện tại của loa là: " + loopSource.volume);
        }
        else
        {
            Debug.LogError("[AudioManager] Không tìm thấy âm thanh nào tên là: " + name);
        }
    }

    public void StopLoopSFX()
    {
        if (loopSource.isPlaying) loopSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (!isMuted) musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (!isMuted) loopSource.volume = sfxVolume;
    }

    public void ToggleMute(bool mute)
    {
        isMuted = mute;
        musicSource.mute = isMuted;
        sfxSource.mute = isMuted;
        loopSource.mute = isMuted;
        AudioListener.pause = isMuted;
    }
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
    public void PlaySFXAtPosition(string name, Vector3 position)
    {
        if (isMuted) return;
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            // 1. Tạo một GameObject ảo tại đúng vị trí của con vật/NPC
            GameObject tempAudioObj = new GameObject("TempAudio_3D_" + name);
            tempAudioObj.transform.position = position;

            // 2. Gắn cái loa (AudioSource) vào cục ảo đó
            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = data.clip;
            tempSource.volume = data.volume * sfxVolume;
            tempSource.pitch = data.pitch * Random.Range(0.9f, 1.1f);

            // 3. THẦN CHÚ BIẾN THÀNH 3D (0 là 2D, 1 là 3D hoàn toàn)
            tempSource.spatialBlend = 1f;

            // 4. Cài đặt khoảng cách nghe (Có thể chỉnh sửa thông số này)
            tempSource.minDistance = 2f;  // Dưới 2 mét nghe to nhất
            tempSource.maxDistance = 20f; // Quá 20 mét thì tịt, không nghe thấy gì
            tempSource.rolloffMode = AudioRolloffMode.Linear;

            // 5. Phát tiếng kêu
            tempSource.Play();

            // 6. Lệnh cho cái loa này "tự sát" sau khi phát xong để không làm nặng máy
            Destroy(tempAudioObj, data.clip.length);
        }
    }
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
    }
}