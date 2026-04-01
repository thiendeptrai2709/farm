using UnityEngine;

[RequireComponent(typeof(Animator), typeof(PlayerInputHandler))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private PlayerInputHandler inputHandler;
    private CharacterController controller;

    // Thêm biến tham chiếu đến script trọng lực
    private PlayerGravityAndJump gravityScript;

    [Header("Làm mượt Animation")]
    public float dampTime = 0.1f;
    private float lastRotationY;

    [Header("Tinh chỉnh Rơi (Chống giật)")]
    public float fallTimeout = 0.15f; // Thời gian châm chước (0.15s là chuẩn mực game AAA)
    private float fallTimeoutDelta;

    private bool wasLocked = false;
    private void Start()
    {
        anim = GetComponent<Animator>();
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
        gravityScript = GetComponent<PlayerGravityAndJump>(); // Khởi tạo

        lastRotationY = transform.eulerAngles.y;
        fallTimeoutDelta = fallTimeout; // Nạp đầy bình đếm ngược khi mới vào game
    }

    private void Update()
    {
        HandleLocomotionAnimation();
        HandleJumpAndFallAnimation();
    }

    private void HandleLocomotionAnimation()
    {

        Vector2 input = inputHandler.MoveInput;
        bool isMoving = input.magnitude > 0.1f;

        bool isLocked = PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked;

        // Detect đúng cái moment locked -> unlocked
        if (wasLocked && !isLocked)
        {
            // Bơm thẳng giá trị vào Animator NGAY LẬP TỨC để đè cái Idle xuống
            anim.SetBool("IsMoving", isMoving);

            if (isMoving)
            {
                // Nếu đang đè phím, ép nó vọt luôn không cần làm mượt
                anim.SetFloat("InputX", input.x);
                anim.SetFloat("InputY", input.y);
                anim.CrossFadeInFixedTime("Locomotion", 0.15f);
            }
            else
            {
                anim.SetFloat("InputX", 0f);
                anim.SetFloat("InputY", 0f);
            }
        }
        wasLocked = isLocked;

        if (isLocked)
        {
            anim.SetBool("IsMoving", false);

            anim.SetFloat("InputX", 0f, dampTime, Time.deltaTime);
            anim.SetFloat("InputY", 0f, dampTime, Time.deltaTime);
            anim.SetFloat("Turn", 0f, dampTime, Time.deltaTime);
            lastRotationY = transform.eulerAngles.y;
            return;
        }

        anim.SetBool("IsMoving", isMoving);


        float currentRotationY = transform.eulerAngles.y;
        float deltaAngle = Mathf.DeltaAngle(lastRotationY, currentRotationY);
        lastRotationY = currentRotationY;

        float turnSpeed = deltaAngle / (Time.deltaTime * 100f);
        float turnNormalized = Mathf.Clamp(turnSpeed, -1f, 1f);

        if (isMoving)
        {
            float targetX = input.x;
            float targetY = input.y;

            bool isMovingBackward = input.y < -0.1f;

            if (inputHandler.IsRunning && !isMovingBackward)
            {
                targetX *= 2f;
                targetY *= 2f;
            }

            if (PlayerMovement.Instance != null && PlayerMovement.Instance.isActionLocked)
            {
                anim.SetFloat("InputX", targetX); // Truyền thẳng, không dampTime
                anim.SetFloat("InputY", targetY);
            }
            else
            {
                // Lúc bình thường thì vẫn làm mượt cho bước đi tự nhiên
                anim.SetFloat("InputX", targetX, dampTime, Time.deltaTime);
                anim.SetFloat("InputY", targetY, dampTime, Time.deltaTime);
            }

            anim.SetFloat("Turn", 0f, dampTime, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Turn", turnNormalized, dampTime, Time.deltaTime);
            anim.SetFloat("InputX", 0f, dampTime, Time.deltaTime);
            anim.SetFloat("InputY", 0f, dampTime, Time.deltaTime);
        }

        anim.SetBool("IsCrouching", inputHandler.IsCrouching);
    }

    private void HandleJumpAndFallAnimation()
    {
        // [FIX BỆNH NHẠY CẢM]: Dùng bộ đếm thời gian thay vì tin tưởng mù quáng vào controller.isGrounded
        if (controller.isGrounded)
        {
            fallTimeoutDelta = fallTimeout; // Đang chạm đất -> Nạp đầy thời gian châm chước
            anim.SetBool("IsGrounded", true);
        }
        else
        {
            // Vừa hụt chân -> Trừ dần thời gian châm chước
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // Hết thời gian châm chước mà vẫn chưa chạm đất -> Đích thị là đang rơi tự do rồi!
                anim.SetBool("IsGrounded", false);
            }
        }

        // TRUYỀN VẬN TỐC Y VÀO ANIMATOR
        anim.SetFloat("VelocityY", gravityScript.VelocityY);

        if (gravityScript.JustJumped)
        {
            anim.SetTrigger("Jump");
        }
    }
}