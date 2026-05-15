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

    private bool wasLocked = false;
    private void Start()
    {
        anim = GetComponent<Animator>();
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
        gravityScript = GetComponent<PlayerGravityAndJump>(); // Khởi tạo

        lastRotationY = transform.eulerAngles.y;
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
        // Chức năng: Đọc trực tiếp trạng thái chạm đất "đã qua xử lý chống giật" từ lõi Vật lý
        anim.SetBool("IsGrounded", gravityScript.IsStableGrounded);

        // TRUYỀN VẬN TỐC Y VÀO ANIMATOR
        anim.SetFloat("VelocityY", gravityScript.VelocityY);

        if (gravityScript.JustJumped)
        {
            anim.SetTrigger("Jump");
        }
    }
}