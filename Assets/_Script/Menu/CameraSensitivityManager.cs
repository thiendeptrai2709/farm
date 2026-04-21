using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CameraSensitivityManager : MonoBehaviour
{
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityText;
    private void Start()
    {
        // Tải thông số
        float savedSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 1f);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;
            UpdateText(savedSensitivity);
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
        }
    }

    // Lưu và cập nhật thông số
    private void UpdateSensitivity(float multiplier)
    {
        UpdateText(multiplier);
        // In ra để kiểm tra UI
        Debug.Log("[UI Check] Kéo thanh trượt, giá trị lưu vào máy: " + multiplier);

        PlayerPrefs.SetFloat("CameraSensitivity", multiplier);

        CameraSensitivityApplier applier = Object.FindFirstObjectByType<CameraSensitivityApplier>();
        if (applier != null)
        {
            applier.ApplySensitivity();
        }
        else
        {
            // Cảnh báo khi không có camera
            Debug.LogWarning("[UI Check] Đã lưu thông số, nhưng Camera chưa xuất hiện trong Scene.");
        }
    }
    private void UpdateText(float value)
    {
        if (sensitivityText != null)
        {
            // F1 nghĩa là lấy 1 chữ số thập phân (Ví dụ: 1.5). Nếu m thích 2 số (1.50) thì đổi thành "F2"
            sensitivityText.text = value.ToString("F1");
        }
    }
}