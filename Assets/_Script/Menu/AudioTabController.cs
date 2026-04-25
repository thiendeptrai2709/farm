using UnityEngine;
using UnityEngine.UI;

public class AudioTabController : MonoBehaviour
{
    private bool isMuted = false;

    [Header("Giao diện Switch Tắt tiếng")]
    public Image switchImage;
    public Sprite switchOnSprite;
    public Sprite switchOffSprite;

    // [ĐÃ THÊM] Bổ sung biến để quản lý thanh trượt âm lượng
    [Header("Giao diện Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    // [ĐÃ THÊM] Hàm này chạy NGAY LẬP TỨC mỗi khi ông mở Tab Audio lên
    private void OnEnable()
    {
        // 1. Đọc dữ liệu từ bộ nhớ máy (nếu chưa có thì lấy mặc định)
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        float savedMusicVol = PlayerPrefs.GetFloat("MusicVolume", 1f); // Giả sử 1f là max âm lượng
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // 2. Cập nhật lại hình ảnh cái Nút Tắt tiếng
        if (switchImage != null)
        {
            switchImage.sprite = isMuted ? switchOffSprite : switchOnSprite;
        }

        // 3. Kéo 2 thanh Slider về đúng vị trí đã lưu
        if (musicSlider != null) musicSlider.value = savedMusicVol;
        if (sfxSlider != null) sfxSlider.value = savedSFXVol;

        // 4. Báo cho AudioManager biết để áp dụng âm thanh thật
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute(isMuted);
            AudioManager.Instance.SetMusicVolume(savedMusicVol);
            AudioManager.Instance.SetSFXVolume(savedSFXVol);
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        // [ĐÃ THÊM] Lưu lại ngay khi kéo thanh trượt
        PlayerPrefs.SetFloat("MusicVolume", value);

        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        // [ĐÃ THÊM] Lưu lại ngay khi kéo thanh trượt
        PlayerPrefs.SetFloat("SFXVolume", value);

        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }

    // Dùng cho Switch Custom (Button thông thường)
    public void OnCustomSwitchClicked()
    {
        // Đảo ngược trạng thái
        isMuted = !isMuted;

        // [ĐÃ THÊM] Ghi nhớ trạng thái vào máy
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);

        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute(isMuted);

        if (switchImage != null)
        {
            switchImage.sprite = isMuted ? switchOffSprite : switchOnSprite;
        }

        Debug.Log("Trạng thái Tắt tiếng đã lưu: " + isMuted);
    }
}