using UnityEngine;
using UnityEngine.UI;

public class CameraInvertManager : MonoBehaviour
{
    private bool isInverted = false;

    [Header("Giao diện Switch Đảo chuột")]
    public Image switchImage;
    public Sprite switchOnSprite;
    public Sprite switchOffSprite;

    private void Start()
    {
        // Đọc dữ liệu đã lưu (Mặc định 0 tức là Tắt)
        isInverted = PlayerPrefs.GetInt("CameraInvert", 0) == 1;

        if (switchImage != null)
        {
            switchImage.sprite = isInverted ? switchOnSprite : switchOffSprite;
        }
    }

    // Gắn hàm này vào sự kiện OnClick() của cái nút Switch
    public void OnCustomSwitchClicked()
    {
        // Đảo trạng thái tắt <-> bật
        isInverted = !isInverted;

        PlayerPrefs.SetInt("CameraInvert", isInverted ? 1 : 0);

        if (switchImage != null)
        {
            switchImage.sprite = isInverted ? switchOnSprite : switchOffSprite;
        }

        // Ép camera cập nhật ngay nếu đang ở trong trận
        CameraSensitivityApplier applier = Object.FindFirstObjectByType<CameraSensitivityApplier>();
        if (applier != null)
        {
            applier.ApplySensitivity();
        }

        Debug.Log("Trạng thái Đảo chuột: " + isInverted);
    }
}