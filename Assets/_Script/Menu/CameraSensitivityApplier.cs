using Unity.Cinemachine;
using UnityEngine;

public class CameraSensitivityApplier : MonoBehaviour
{
    public CinemachineInputAxisController inputController;

    private float defaultXSpeed;
    private float defaultYSpeed;

    private void Start()
    {
        if (inputController == null)
        {
            inputController = GetComponent<CinemachineInputAxisController>();
        }

        if (inputController != null && inputController.Controllers.Count >= 2)
        {
            defaultXSpeed = inputController.Controllers[0].Input.Gain;
            defaultYSpeed = inputController.Controllers[1].Input.Gain;

            ApplySensitivity();
        }
    }

    public void ApplySensitivity()
    {
        if (inputController != null && inputController.Controllers.Count >= 2)
        {
            float multiplier = PlayerPrefs.GetFloat("CameraSensitivity", 1f);

            // Đọc 1 trạng thái duy nhất cho trục Y
            int invertY = PlayerPrefs.GetInt("CameraInvert", 0) == 1 ? -1 : 1;

            // Trục X (Trái/Phải) chỉ nhân độ nhạy, không kẹp dấu âm
            var xInput = inputController.Controllers[0].Input;
            xInput.Gain = defaultXSpeed * multiplier;
            inputController.Controllers[0].Input = xInput;

            // Trục Y (Lên/Xuống) nhân độ nhạy và kẹp dấu âm nếu nút đang bật
            var yInput = inputController.Controllers[1].Input;
            yInput.Gain = defaultYSpeed * multiplier * invertY;
            inputController.Controllers[1].Input = yInput;
        }
    }
}