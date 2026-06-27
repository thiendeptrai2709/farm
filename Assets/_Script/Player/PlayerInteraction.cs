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

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.isActionLocked = false;

        // KIỂM TRA XEM CÓ AUTO LÀM TIẾP KHÔNG
        if (autoActionTarget != null)
        {
            bool isValid = false;

            // Nếu là Object thật trên Scene thì check xem nó có đang bật không
            if (autoActionTarget is MonoBehaviour targetMono)
            {
                // [ĐÃ FIX LỖI]: Bắt buộc phải kiểm tra targetMono != null theo chuẩn Unity 
                // để phát hiện xem đồ vật đã bị Destroy (như lúc đập rương/hàng rào) hay chưa!
                if (targetMono != null)
                {
                    isValid = targetMono.gameObject.activeInHierarchy;
                }
                else
                {
                    isValid = false; // Đã bị xóa -> Không hợp lệ
                }
            }
            // Nếu là Object ảo thuần C# (như cây rừng trên Terrain) thì luôn hợp lệ để tiếp tục
            else
            {
                isValid = true;
            }

            if (isValid && IsActionable(autoActionTarget))
            {
                ExecuteInteraction(autoActionTarget);
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
            bool isDismantling = pendingInteractable is PlacedProp || (pendingInteractable as MonoBehaviour)?.GetComponent<PlacedProp>() != null;
            if (isDismantling && PlayerStamina.Instance != null)
            {
                PlayerStamina.Instance.ConsumeStamina(PlayerStamina.Instance.axeCost);
            }

            MonoBehaviour targetMono = pendingInteractable as MonoBehaviour;

            // Nếu nó là Object thật trên màn hình (như Luống đất, Cây trồng 4 ô, Rương...)
            if (targetMono != null)
            {
                if (targetMono.gameObject.activeInHierarchy)
                {
                    pendingInteractable.Interact();
                }
            }
            // Nếu nó là Object ảo thuần C# (như Cây trên Terrain)
            else
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
            if (!(target is Chest) && !(target is FoodTrough) && !(target is FenceGate)) return false;
        }

        if (target is FarmPlot plot)
        {
            bool isHoldingHoe = false;
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Hoe) isHoldingHoe = true;
            }

            if (isHoldingHoe) return true; // Cầm cuốc là luôn được phép tương tác (để xóa bỏ)
            if (plot.currentState == PlotState.Tilled || plot.currentState == PlotState.Grown) return true;
            if (plot.currentState == PlotState.Planted) return plot.CanBeWatered() || plot.CanBeFertilized();
            return false;
        }
        else if (target is TerrainTreeInteractable virtualTree)
        {
            if (TerrainTreeManager.Instance != null && !TerrainTreeManager.Instance.IsTreeAlive(virtualTree.treeIndex))
            {
                return false;
            }

            bool isHoldingAxe = false;
            if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
            {
                ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
                if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe) isHoldingAxe = true;
            }
            return isHoldingAxe; // Chỉ được tương tác nếu đang cầm Rìu
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
        ItemData holdingItem = selectedIndex != -1 ? InventoryManager.Instance.hotbarSlots[selectedIndex].item : null;

        // ==========================================
        // BƯỚC 1: KIỂM TRA THỂ LỰC VÀ CHẶN MỌI HOẠT ẢNH
        // ==========================================
        if (PlayerStamina.Instance != null)
        {
            float requiredStamina = 0f;

            if (holdingItem is ToolItemData tool)
            {
                if (tool.toolType == ToolType.Hoe && (target is FarmingZone || target is FarmPlot)) requiredStamina = PlayerStamina.Instance.hoeCost;
                else if (tool.toolType == ToolType.Axe && (target is TreePit || target is TerrainTreeInteractable || (target as MonoBehaviour)?.GetComponent<PlacedProp>() != null)) requiredStamina = PlayerStamina.Instance.axeCost;
                else if (tool.toolType == ToolType.WateringCan && (target is TreePit || target is FarmPlot)) requiredStamina = PlayerStamina.Instance.waterCost;
                else if (tool.toolType == ToolType.FishingRod && target is FishingZone fishingZone)
                {
                    if (fishingZone.currentState == FishingZone.FishingState.NotFishing)
                    {
                        requiredStamina = PlayerStamina.Instance.fishCost;
                    }
                }
            }

            // [LỖI Ở ĐÂY ĐÃ ĐƯỢC FIX]: Check thêm nếu đang nhắm vào luống đất/cây (Bón phân, tưới nước ngầm)
            if (target is FarmPlot plot)
            {
                if (plot.currentState == PlotState.Planted && (plot.CanBeWatered() || plot.CanBeFertilized()))
                    requiredStamina = PlayerStamina.Instance.waterCost;
            }
            else if (target is TreePit pit)
            {
                if ((pit.currentState == TreePit.PitState.Planted || pit.currentState == TreePit.PitState.Grown_Empty) && (pit.CanBeWatered() || pit.CanBeFertilized()))
                    requiredStamina = PlayerStamina.Instance.waterCost;
            }

            // CHẶN TẬN GỐC: NẾU THIẾU THỂ LỰC -> ĐỨNG IM
            if (requiredStamina > 0 && PlayerStamina.Instance.currentStamina < requiredStamina)
            {
                if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ShowNotEnoughWarning();
                autoActionTarget = null;
                return; // Thoát ngay, không cho múa
            }
        }

        // ==========================================
        // BƯỚC 2: CHẠY ANIMATION (Vì đã chắc chắn đủ thể lực)
        // ==========================================
        if (holdingItem is ToolItemData toolAct)
        {
            if (toolAct.toolType == ToolType.Hoe && (target is FarmingZone || target is FarmPlot))
            {
                requiresLockAndEvent = true;
                animToPlay = "Digging";
                autoActionTarget = null;
            }
            else if (toolAct.toolType == ToolType.Axe)
            {
                if (target is TreePit || target is TerrainTreeInteractable)
                {
                    requiresLockAndEvent = true;
                    animToPlay = "Chopping";
                }
                else
                {
                    PlacedProp pProp = (target as MonoBehaviour)?.GetComponent<PlacedProp>();
                    if (pProp != null)
                    {
                        requiresLockAndEvent = true;
                        animToPlay = "Chopping";
                        target = pProp;
                        if (InventoryManager.Instance != null) InventoryManager.Instance.DeductEquippedToolDurability(1f);
                    }
                }
            }
            else if (toolAct.toolType == ToolType.FishingRod && target is FishingZone)
            {
                isFishing = true;
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
            if (!(target is AnimalMovement) && !(target is NPCMerchant) && !(target is BusStop) && !(target is BusVehicle) && !(target is Chair) && !(target is NPCVillager) && !(target is BearNPC))
            {
                if (playerAnimator != null) playerAnimator.Play("Gathering", -1, 0f);
            }
            target.Interact();
        }

        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }
}