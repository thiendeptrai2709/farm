using UnityEngine;

[RequireComponent(typeof(PlayerScanner))] // Đảm bảo tự động yêu cầu file Scanner đi kèm
public class PlayerInteraction : MonoBehaviour
{
    [Header("Tham chiếu")]
    public PlayerInputHandler inputHandler;
    public Animator playerAnimator;
    public string interactTriggerName = "Pickup";

    private IInteractable autoActionTarget;
    private IInteractable pendingInteractable;
    private ConstructionSite pendingBuildSite;

    private PlayerScanner scanner; // Tham chiếu sang Mắt Radar
    public IInteractable currentTarget => scanner != null ? scanner.currentTarget : null;

    [Header("Hệ thống Ngồi")]
    public bool isSitting = false;
    private Chair currentChair;
    private int currentSeatIndex = -1;

    private void Awake()
    {
        scanner = GetComponent<PlayerScanner>();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnConsumeAnimationStart += PlayEatAnimation;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnConsumeAnimationStart -= PlayEatAnimation;
    }

    private void PlayEatAnimation() { if (playerAnimator != null) playerAnimator.Play("Eating", -1, 0f); }
    public void AE_FinishEating() { if (InventoryManager.Instance != null) InventoryManager.Instance.FinishConsumingItem(); }
    public void AE_ResetEating() { if (InventoryManager.Instance != null) InventoryManager.Instance.ResetConsumeState(); }

    public void PlayBuildAnimation(ConstructionSite site)
    {
        pendingBuildSite = site;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = true;
        if (playerAnimator != null) playerAnimator.CrossFadeInFixedTime("Hammering", 0.15f, -1, 0f);
    }

    public void AE_UnlockPlayer()
    {
        pendingInteractable = null;
        pendingBuildSite = null;

        // ==========================================
        // [ĐÃ SỬA]: LUÔN MỞ KHÓA CHÂN TRƯỚC TIÊN CHO CHẮC CÚ!
        // ==========================================
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = false;

        // KIỂM TRA XEM CÓ AUTO LÀM TIẾP KHÔNG
        if (autoActionTarget != null)
        {
            MonoBehaviour targetMono = autoActionTarget as MonoBehaviour;
            if (targetMono != null && targetMono.gameObject.activeInHierarchy)
            {
                if (IsActionable(autoActionTarget))
                {
                    
                    ExecuteInteraction(autoActionTarget);
                }
                else
                {
                    autoActionTarget = null;
                }
            }
            else
            {
                autoActionTarget = null;
            }
        }
    }

    public void AE_OnInteractImpact()
    {
        if (pendingInteractable != null)
        {
            MonoBehaviour targetMono = pendingInteractable as MonoBehaviour;
            if (targetMono != null && targetMono.gameObject.activeInHierarchy)
            {
                pendingInteractable.Interact();
            }
            pendingInteractable = null;
        }
        if (pendingBuildSite != null)
        {
            pendingBuildSite.FinishBuilding();
            pendingBuildSite = null;
        }
    }
    
    // HÀM 2: CÁI CHÌA KHÓA DUY NHẤT. Chỉ dành cho CancelFishing gọi
    public void AE_FinishFishing()
    {
        // Mở khóa Camera
        if (PlayerCameraManager.Instance != null)
            PlayerCameraManager.Instance.ToggleFishingCamera(false);

        // Mở khóa Chân
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = false;

        // Reset Hồ Cá
        if (currentTarget is FishingZone fz)
        {
            fz.currentState = FishingZone.FishingState.NotFishing;
        }
    }
    public void AE_ReleaseStone()
    {
        if (StoneThrower.Instance != null)
        {
            StoneThrower.Instance.ExecuteThrowAction();
        }
    }
    private void Update()
    {
        if (isSitting)
        {
            // Bấm E hoặc cố tình bấm WASD để di chuyển thì sẽ đứng lên
            if (inputHandler.InteractTriggered || inputHandler.MoveInput.sqrMagnitude > 0)
            {
                StandUp();
            }
            return; // THOÁT UPDATE, không cho radar quét nhặt đồ hay làm gì khác
        }

        if (inputHandler.MoveInput.sqrMagnitude > 0)
        {
            autoActionTarget = null;
        }

        bool isPlayingMinigame = ThrowMinigameUI.Instance != null && ThrowMinigameUI.Instance.IsMinigameActive();
        if (isPlayingMinigame)
        {
            autoActionTarget = null;
            return;
        }
        // Tắt hành động nếu chuột đang mở
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            autoActionTarget = null;
            return;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        bool isDoingAction = movement != null && movement.isActionLocked;

        if (StoneThrower.Instance != null && StoneThrower.Instance.IsAiming)
        {
            return;
        }

        IInteractable target = scanner.currentTarget;

        if (target is FishingZone)
        {
            isDoingAction = false;
        }

        if (!isDoingAction && target != null && inputHandler.InteractTriggered)
        {
            autoActionTarget = target;
            ExecuteInteraction(target);
        }
    }
    public void SitDown(Chair chair, int seatIndex)
    {
        if (chair == null || chair.sitPoints == null || seatIndex < 0 || seatIndex >= chair.sitPoints.Length) return;

        isSitting = true;
        currentChair = chair;
        currentSeatIndex = seatIndex; // Lưu số slot
        chair.occupiedSeats[seatIndex] = true; // Khóa slot

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = true;

        Transform targetSitPoint = chair.sitPoints[seatIndex];
        transform.position = targetSitPoint.position;
        transform.rotation = targetSitPoint.rotation;

        if (playerAnimator != null) playerAnimator.SetBool("IsSitting", true);

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(transform, "[E] / [WASD] Đứng lên", false, 0);
    }

    public void StandUp()
    {
        isSitting = false;
        if (currentChair != null && currentSeatIndex != -1)
        {
            currentChair.LeaveChair(currentSeatIndex); // Nhả đúng cái slot đang ngồi ra
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = false;

        if (playerAnimator != null) playerAnimator.SetBool("IsSitting", false);

        if (currentChair != null && currentChair.exitPoint != null)
        {
            transform.position = currentChair.exitPoint.position;
        }
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        currentChair = null;
        currentSeatIndex = -1;
        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }
    private bool IsActionable(IInteractable target)
    {
        PlacedProp placedProp = (target as MonoBehaviour)?.GetComponent<PlacedProp>();
        if (placedProp != null)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe)
                {
                    Chest chestTarget = placedProp.GetComponent<Chest>();
                    if (chestTarget != null && !chestTarget.IsEmpty())
                    {
                        return false; // Rương còn đồ -> Trả về false -> Cấm vung rìu!
                    }

                    return true; // Rương rỗng (hoặc là Hàng rào/Lò rèn) -> Cho phép bổ!
                }
            }
            if (!(target is Chest) && !(target is FoodTrough)) return false;
        }

        if (target is FarmPlot plot)
        {
            if (plot.currentState == PlotState.Tilled || plot.currentState == PlotState.Grown) return true;
            if (plot.currentState == PlotState.Planted) return plot.CanBeWatered() || plot.CanBeFertilized();
        }
        else if (target is TreePit pit)
        {
            bool isHoldingAxe = false;
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe) isHoldingAxe = true;
            }

            if (isHoldingAxe) return true;
            if (pit.currentState == TreePit.PitState.Grown_Fruited) return true;
            if (pit.currentState == TreePit.PitState.Planted || pit.currentState == TreePit.PitState.Grown_Empty)
            {
                return pit.CanBeWatered() || pit.CanBeFertilized();
            }
            return false;
        }
        else if (target is WaterWell)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
                if (slot.item is ToolItemData tool && tool.toolType == ToolType.WateringCan)
                {
                    return slot.currentDurability < tool.durability;
                }
            }
            return false;
        }
        else if (target is ConstructionSite site)
        {
            if (site.currentState == ConstructionSite.SiteState.Pending)
            {
                if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
                {
                    ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                    if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hammer)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        else if (target is FishingZone)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                InventorySlot slot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex];
                if (slot.item is ToolItemData tool && tool.toolType == ToolType.FishingRod)
                {
                    return slot.currentDurability > 0;
                }
            }
            return false;
        }
        else if (target is PickupItem pickup)
        {
            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasSpaceFor(pickup.itemData, pickup.amount))
            {
                return false; // Balo đầy -> Trả về false -> CẤM TƯƠNG TÁC, CẤM VUNG TAY NHẶT
            }
            return true;
        }
        return true;
    }

    private void ExecuteInteraction(IInteractable target)
    {
        if (!IsActionable(target))
        {
            autoActionTarget = null;

            return;
        }

        if (InventoryManager.Instance != null) InventoryManager.Instance.CancelPendingConsume();

        bool requiresLockAndEvent = false;
        string animToPlay = "Gathering";
        bool isFishing = false;

        int selectedIndex = InventoryManager.Instance != null ? InventoryManager.Instance.selectedHotbarIndex : -1;
        if (selectedIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[selectedIndex].item;
            if (holdingItem is ToolItemData tool)
            {
                if (tool.toolType == ToolType.Hoe && target is FarmingZone)
                {
                    requiresLockAndEvent = true;
                    animToPlay = "Digging";
                    autoActionTarget = null;
                }
                else if (tool.toolType == ToolType.Axe)
                {
                    if (target is TreePit)
                    {
                        requiresLockAndEvent = true;
                        animToPlay = "Chopping";
                    }
                    else
                    {
                        // [MẸO TRÁO MỤC TIÊU]: Nếu đang cầm rìu chĩa vào đồ tự xây
                        PlacedProp pProp = (target as MonoBehaviour)?.GetComponent<PlacedProp>();
                        if (pProp != null)
                        {
                            requiresLockAndEvent = true;
                            animToPlay = "Chopping";

                            // Ghi đè: Ép cái target thành PlacedProp để nó chạy hàm Destroy thay vì hàm Mở rương!
                            target = pProp;

                            // Đập đồ cũng tụt máu rìu
                            if (InventoryManager.Instance != null) InventoryManager.Instance.DeductEquippedToolDurability(1f);
                        }
                    }
                }
                else if (tool.toolType == ToolType.FishingRod && target is FishingZone)
                {
                    isFishing = true;
                }
            }
        }

        if (isFishing)
        {
            autoActionTarget = null;
            target.Interact();
        }
        else if (requiresLockAndEvent)
        {
            pendingInteractable = target;
            if (playerAnimator != null) playerAnimator.CrossFadeInFixedTime(animToPlay, 0.15f, -1, 0f);

            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.isActionLocked = true;
        }
        else
        {
            autoActionTarget = null;
            if (!(target is AnimalMovement) && !(target is NPCMerchant) && !(target is BusStop) && !(target is BusVehicle) && !(target is Chair))
            {
                if (playerAnimator != null) playerAnimator.Play("Gathering", -1, 0f);
            }
            target.Interact();
        }

        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }
}