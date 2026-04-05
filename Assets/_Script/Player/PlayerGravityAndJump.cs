using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler))]
public class PlayerGravityAndJump : MonoBehaviour
{
    [Header("Cài đặt Nhảy")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Tinh chỉnh cảm giác nhảy")]
    public float jumpBufferTime = 0.2f;

    private CharacterController controller;
    private PlayerInputHandler inputHandler;
    private Vector3 velocity;
    private bool isGrounded;
    private float jumpBufferCounter;

    [Header("Chống Spam")]
    public float jumpCooldown = 0.25f; 
    private float jumpCooldownTimer;
    public float VelocityY => velocity.y;
    public bool JustJumped { get; private set; }
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        HandleGravityAndJump();
    }

    private void HandleGravityAndJump()
    {
        JustJumped = false;

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
        bool isLocked = PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked;

        // --- [SỬA SỐ 2]: Hủy lưu trữ phím Nhảy nếu đang bị khóa chân ---
        if (inputHandler.JumpTriggered)
        {
            if (isLocked)
            {
                jumpBufferCounter = 0f; // Đang bận -> Hủy bỏ lệnh nhảy ngay lập tức
            }
            else
            {
                jumpBufferCounter = jumpBufferTime; // Rảnh rỗi -> Cho phép lưu lệnh
            }
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // --- KIỂM TRA ĐIỀU KIỆN NHẢY CUỐI CÙNG ---
        bool canJump = true;

        if (isLocked)
        {
            canJump = false;
        }

        if (jumpCooldownTimer > 0f || !isGrounded)
        {
            canJump = false;
        }

        // --- THỰC THI NHẢY ---
        if (jumpBufferCounter > 0f && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f; 
            JustJumped = true;
            jumpCooldownTimer = jumpCooldown;

            GetComponentInChildren<PlayerAudio>().PlayJump();

        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}