using UnityEngine;
using UnityEngine.InputSystem;
using System;
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerControls controls; 
    private PlayerMovement playerMovement;
    // Các biến public để script khác đọc được
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool MenuTriggered { get; private set; }
    public bool ClickTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }
    public bool InventoryTriggered { get; private set; }
    public float ScrollValue { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool ArrowUpTriggered { get; private set; }
    public bool ArrowDownTriggered { get; private set; }
    public bool ArrowLeftTriggered { get; private set; }
    public bool ArrowRightTriggered { get; private set; }
    public bool BuildMenuTriggered { get; private set; }
    public event Action OnSplitActionTriggered;
    private void Awake()
    {
        controls = new PlayerControls();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        // Liên tục cập nhật trạng thái nút bấm mỗi frame
        MoveInput = controls.Player.Move.ReadValue<Vector2>();
        IsRunning = controls.Player.Run.IsPressed();
        JumpTriggered = controls.Player.Jump.WasPressedThisFrame();
        MenuTriggered = controls.Player.ToggleMenu.WasPressedThisFrame();
        ClickTriggered = controls.Player.Click.WasPressedThisFrame();
        InteractTriggered = controls.Player.Interact.WasPressedThisFrame(); 
        InventoryTriggered = controls.Player.Inventory.WasPressedThisFrame();
        BuildMenuTriggered = controls.Player.BuildMenu.WasPressedThisFrame();

        ScrollValue = controls.Player.HotbarScroll.ReadValue<Vector2>().y;

        bool canChangeItem = (playerMovement == null || !playerMovement.isActionLocked);
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) InventoryManager.Instance.UseHotbarSlot(7);

            ArrowUpTriggered = Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
            ArrowDownTriggered = Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame;
            ArrowLeftTriggered = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            ArrowRightTriggered = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
        }
        else
        {
            // Tránh lỗi null nếu không cắm bàn phím
            ArrowUpTriggered = ArrowDownTriggered = ArrowLeftTriggered = ArrowRightTriggered = false;
        }
        if (controls.Player.Split.WasPressedThisFrame())
        {
            OnSplitActionTriggered?.Invoke();
        }
        // 3. XỬ LÝ LĂN CHUỘT KHI ĐANG CHƠI (Không cuộn khi mở Balo)
        bool isPlacingBuilding = (HammerBuildManager.Instance != null && HammerBuildManager.Instance.IsCurrentlyPlacing());

        if (!isPlacingBuilding)
        {
            InventoryManager.Instance.ScrollHotbar(ScrollValue);
        }
        if (controls.Player.Crouch.WasPressedThisFrame())
        {
            IsCrouching = !IsCrouching; // Đảo trạng thái
        }
        if (IsRunning && IsCrouching)
        {
            IsCrouching = false;
        }
    }
}