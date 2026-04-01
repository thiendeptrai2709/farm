using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerPickupManager : MonoBehaviour
{
    public static PlayerPickupManager Instance;

    [Header("Cài đặt vị trí bế")]
    public Transform holdPoint;
    public string holdingAnimParam = "IsHolding";

    private AnimalMovement currentlyHeldAnimal;
    private Animator animator;
    private PlayerInteraction playerInteract;
    private PlayerInputHandler inputHandler;

    // BIẾN MỚI: Cờ hiệu chặn bug bấm đúp 1 frame
    private bool justPickedUpThisFrame = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerInteract = GetComponent<PlayerInteraction>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        // 1. NẾU VỪA BẾ LÊN -> Hạ cờ hiệu xuống và BỎ QUA lệnh thả trong frame này
        if (justPickedUpThisFrame)
        {
            justPickedUpThisFrame = false;
            return;
        }

        // 2. NẾU ĐÃ BẾ TỪ TRƯỚC RỒI VÀ BẤM E -> Mới cho phép thả
        if (IsHoldingAnimal() && inputHandler.InteractTriggered)
        {
            DropAnimal();
        }
    }

    public bool IsHoldingAnimal()
    {
        return currentlyHeldAnimal != null;
    }

    public void PickUpAnimal(AnimalMovement animal)
    {
        if (IsHoldingAnimal()) return;

        currentlyHeldAnimal = animal;

        // Bật cờ hiệu đánh dấu là vừa mới bế
        justPickedUpThisFrame = true;

        if (animator != null) animator.SetBool(holdingAnimParam, true);

        currentlyHeldAnimal.OnPickedUp(holdPoint);

        if (playerInteract != null) playerInteract.enabled = false;

        Debug.Log($"Đã bế con {animal.gameObject.name} lên!");
    }

    public void DropAnimal()
    {
        if (!IsHoldingAnimal()) return;

        Vector3 dropPos = transform.position + transform.forward * 1f;

        currentlyHeldAnimal.OnDropped(dropPos);

        currentlyHeldAnimal = null;

        if (animator != null) animator.SetBool(holdingAnimParam, false);

        if (playerInteract != null) playerInteract.enabled = true;

        Debug.Log("Đã thả vật nuôi xuống đất!");
    }
}