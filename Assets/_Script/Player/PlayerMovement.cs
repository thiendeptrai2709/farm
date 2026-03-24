using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    // BIẾN NÀY ĐỂ KẾT NỐI VỚI TÚI ĐỒ
    public static PlayerMovement Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("Cài đặt tốc độ")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float slowSpeed = 3f;
    public float crouchSpeed = 2f;

    [Header("Quán tính trên không (Air Control)")]
    [Range(0f, 1f)]
    public float airControl = 0.2f; // 0 là rớt như cục đá, 1 là bay lượn như chim

    [Header("Camera")]
    public Transform mainCamera;
    public float turnSmoothTime = 0.05f;

    [Header("Cài đặt Ngồi")]
    public float crouchHeight = 1f; // Chiều cao lúc ngồi
    private float originalHeight;
    private Vector3 originalCenter;


    private CharacterController controller;
    private PlayerInputHandler inputHandler;
    private float turnSmoothVelocity;

    // Biến lưu trữ đà di chuyển ngang (trục X và Z)
    private Vector3 currentMomentum;

    public bool isActionLocked = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();

        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (isActionLocked)
        {
            // Triệt tiêu quán tính từ từ để dừng lại hẳn, không bị trượt
            currentMomentum = Vector3.Lerp(currentMomentum, Vector3.zero, 10f * Time.deltaTime);
            if (currentMomentum.magnitude >= 0.01f)
            {
                controller.Move(currentMomentum * Time.deltaTime);
            }

            // Return để ngắt toàn bộ code bên dưới, cấm nhân vật di chuyển hay xoay mặt
            return;
        }

        Vector2 input = inputHandler.MoveInput;
        float currentSpeed = walkSpeed;

        float targetHeight = originalHeight;
        float targetCenterY = originalCenter.y;

        if (inputHandler.IsCrouching)
        {
            currentSpeed = crouchSpeed;
            targetHeight = crouchHeight;
            targetCenterY = crouchHeight / 2f; // Tâm hạ xuống một nửa
        }

        if (input.y < -0.1f)
        {
            currentSpeed = slowSpeed;
        }
        else if (inputHandler.IsRunning)
        {
            currentSpeed = runSpeed;
        }

        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, new Vector3(originalCenter.x, targetCenterY, originalCenter.z), 10f * Time.deltaTime);

        // Luôn xoay mặt nhân vật theo hướng nhìn của Camera
        float targetAngle = mainCamera.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // Tính toán hướng đi mong muốn dựa trên phím bấm
        Vector3 targetDirection = transform.right * input.x + transform.forward * input.y;
        Vector3 targetVelocity = targetDirection.normalized * currentSpeed;

        // XỬ LÝ QUÁN TÍNH
        if (controller.isGrounded)
        {
            // Đang chạm đất -> Gán tốc độ thực tế bằng tốc độ phím bấm ngay lập tức
            currentMomentum = targetVelocity;
        }
        else
        {
            // Đang trên không -> Áp dụng đà quán tính cũ, chỉ cho phép bẻ lái một chút (Air Control)
            currentMomentum = Vector3.Lerp(currentMomentum, targetVelocity, airControl * Time.deltaTime * 5f);
        }

        // Thực hiện di chuyển bằng Vector quán tính
        if (currentMomentum.magnitude >= 0.1f || targetVelocity.magnitude >= 0.1f)
        {
            controller.Move(currentMomentum * Time.deltaTime);
        }
    }
}