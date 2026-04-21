using UnityEngine;
using UnityEngine.UI;
public class AudioTabController : MonoBehaviour
{
    private bool isMuted = false;

    [Header("Giao diện Switch Tắt tiếng")]
    public Image switchImage;       
    public Sprite switchOnSprite;   
    public Sprite switchOffSprite;

    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMuteToggled(bool value)
    {
        isMuted = value;
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute(isMuted);
    }

    // [THÊM MỚI] Dùng cho Switch Custom (Button thông thường)
    public void OnCustomSwitchClicked()
    {
        // Đảo ngược trạng thái: đang tắt thành bật, đang bật thành tắt
        isMuted = !isMuted;

        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute(isMuted);
        if (switchImage != null)
        {
            switchImage.sprite = isMuted ? switchOffSprite : switchOnSprite;
        }
        // Ghi log để bạn dễ kiểm tra trong Console
        Debug.Log("Trạng thái Tắt tiếng: " + isMuted);
    }
}