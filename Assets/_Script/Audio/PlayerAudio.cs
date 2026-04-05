using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Cài đặt Nhịp bước chân (Giây)")]
    public float walkStepInterval = 0.5f;   // Tốc độ nhịp đi bộ
    public float runStepInterval = 0.3f;    // Tốc độ nhịp chạy
    public float crouchStepInterval = 0.65f; // Tốc độ nhịp ngồi (chậm nhất)

    private float stepTimer;

    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;
    private CharacterController controller;

    private void Start()
    {
        // Lấy dữ liệu từ các script bạn vừa cung cấp
        inputHandler = GetComponentInParent<PlayerInputHandler>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        controller = GetComponentInParent<CharacterController>();

        // Ép timer về 0 để ngay khi vừa bấm phím di chuyển là kêu tiếng đầu tiên luôn
        stepTimer = 0f;
    }

    private void Update()
    {
        // 1. ĐIỀU KIỆN DỪNG: 
        // - Nếu bị khóa hành động (isActionLocked)
        // - Hoặc không bấm phím di chuyển (MoveInput.magnitude gần bằng 0)
        if (playerMovement.isActionLocked || inputHandler.MoveInput.magnitude < 0.1f)
        {
            stepTimer = 0f; // Trả đồng hồ về 0 chờ lần di chuyển tiếp theo
            return;
        }

        // 2. CHỐT CHẠM ĐẤT:
        // Đang nhảy hoặc rơi tự do thì ngắt đồng hồ, không phát tiếng
        if (controller != null && !controller.isGrounded)
        {
            return;
        }

        // 3. ĐẾM NGƯỢC
        stepTimer -= Time.deltaTime;

        // 4. HẾT GIỜ -> PHÁT ÂM THANH
        if (stepTimer <= 0f)
        {
            PlayFootstepSFX();

            // 5. CÀI ĐẶT LẠI ĐỒNG HỒ DỰA TRÊN TRẠNG THÁI HIỆN TẠI
            if (inputHandler.IsCrouching)
            {
                stepTimer = crouchStepInterval; // Đang ngồi
            }
            // Trong file Movement của bạn, đi lùi (y < -0.1f) bị ép về slowSpeed, nên không được tính là chạy
            else if (inputHandler.IsRunning && inputHandler.MoveInput.y >= -0.1f)
            {
                stepTimer = runStepInterval; // Đang chạy tới/chéo
            }
            else
            {
                stepTimer = walkStepInterval; // Đi bộ bình thường hoặc đi lùi
            }
        }
    }

    private void PlayFootstepSFX()
    {
        // Gắn vào Timer rồi thì tiếng kêu mượt 100%, không cần phải viết thêm logic Cooldown chống dội âm nữa
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Footstep");
        }
    }

    // Hàm này giữ nguyên để file PlayerGravityAndJump vẫn gọi được khi nhảy
    public void PlayJump()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Jump");
        }
    }
}