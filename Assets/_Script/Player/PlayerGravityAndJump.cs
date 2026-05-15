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

    // [ĐÃ SỬA]: Nói thật với Animator! Nếu đang đứng trên đất thì báo Y = 0 để không bị nhảy animation Rơi
    public float VelocityY => IsStableGrounded ? 0f : velocity.y;

    public bool JustJumped { get; private set; }
    public bool IsStableGrounded { get; private set; }

    public float stickToGroundForce = -8f; // Tăng lực hút xuống để bám dốc tốt hơn
    public float groundedGraceTime = 0.1f; // Thời gian châm chước trước khi coi là đang rơi thật
    private float groundedGraceCounter;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (controller != null && !controller.enabled) return;

        HandleGravityAndJump();
    }

    private void HandleGravityAndJump()
    {
        JustJumped = false;

        // --- [ĐÃ SỬA] CẢM BIẾN DÒ MẶT ĐẤT BẰNG RAYCAST ---
        bool grounded = controller.isGrounded;

        // Nếu bộ đệm Unity báo lơ lửng, bắn tia laser từ gót chân xuống để check lại cho chắc ăn
        if (!grounded)
        {
            grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
        }

        isGrounded = grounded;

        if (isGrounded)
        {
            groundedGraceCounter = groundedGraceTime;
            IsStableGrounded = true;
        }
        else
        {
            groundedGraceCounter -= Time.deltaTime;
            if (groundedGraceCounter <= 0f)
            {
                IsStableGrounded = false;
            }
        }

        // [ĐÃ SỬA]: Ép duy trì lực hút xuống dốc trong suốt 0.1s châm chước
        if (IsStableGrounded && velocity.y <= 0)
        {
            velocity.y = stickToGroundForce;
        }

        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        bool isLocked = PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked;

        if (inputHandler.JumpTriggered)
        {
            if (isLocked)
            {
                jumpBufferCounter = 0f;
            }
            else
            {
                jumpBufferCounter = jumpBufferTime;
            }
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

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

            // [ĐÃ SỬA]: Hủy ngay 0.1s châm chước khi bấm nhảy để Animator biết đang bay lên
            IsStableGrounded = false;
            groundedGraceCounter = 0f;

            GetComponentInChildren<PlayerAudio>().PlayJump();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}