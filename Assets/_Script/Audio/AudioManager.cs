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
            musicSource.clip = data.clip;
            musicSource.volume = data.volume * musicVolume;
            musicSource.Play();
        }
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
        if (sfxDictionary.TryGetValue(name, out AudioData data))
        {
            loopSource.clip = data.clip;
            loopSource.volume = isMuted ? 0 : data.volume * sfxVolume;
            loopSource.pitch = data.pitch;
            loopSource.Play();
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
}