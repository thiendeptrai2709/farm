using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Thành phần giao diện")]
    public Image icon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI slotNumberText;
    public GameObject highlightOutline;

    // --- ĐỘ BỀN (ĐÃ FIX SANG SLIDER) ---
    [Header("Thanh Độ Bền (Slider)")]
    public GameObject durabilityContainer; // Cục chứa Slider để bật/tắt
    public Slider durabilitySlider; // Kéo nguyên cái Slider vào đây
    public Image durabilityFillImage; // Kéo object "Fill" (nằm trong Slider) vào đây để nó tự đổi màu

    // Các biến nhận diện gốc gác của ô này
    public StorageType storageType;
    public int slotIndex;

    private static GameObject ghostIcon;
    private Canvas parentCanvas;

    private PlayerInputHandler playerInput;
    private bool isHovered = false;
    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }
    private void Start()
    {
        playerInput = FindAnyObjectByType<PlayerInputHandler>();
        if (playerInput != null)
        {
            playerInput.OnSplitActionTriggered += TrySplitItem;
        }
    }
    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.OnSplitActionTriggered -= TrySplitItem;
        }
    }
    private void TrySplitItem()
    {
        if (isHovered)
        {
            InventoryManager.Instance.SplitItem(storageType, slotIndex);
        }
    }
    private void OnDisable()
    {
        if (ghostIcon != null) Destroy(ghostIcon);
        SetIconAlpha(1f);

        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        if (slotNumberText != null) slotNumberText.text = (index + 1).ToString();
    }

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot != null && slot.item != null)
        {
            icon.sprite = slot.item.icon;
            SetIconAlpha(1f);
            icon.enabled = true;
            amountText.text = slot.amount > 1 ? slot.amount.ToString() : "";

            // ==========================================
            // HIỂN THỊ ĐỘ BỀN BẰNG SLIDER
            // ==========================================
            if (slot.item is ToolItemData tool && slot.currentDurability >= 0)
            {
                if (durabilityContainer != null) durabilityContainer.SetActive(true);

                if (durabilitySlider != null)
                {
                    // Tính % máu và gán vào Slider
                    float durabilityPercent = slot.currentDurability / tool.durability;
                    durabilitySlider.value = durabilityPercent;

                    // Đổi màu cái ruột (Fill) của Slider
                    if (durabilityFillImage != null)
                    {
                        if (durabilityPercent > 0.5f)
                            durabilityFillImage.color = Color.green;
                        else if (durabilityPercent > 0.2f)
                            durabilityFillImage.color = Color.yellow;
                        else
                            durabilityFillImage.color = Color.red;
                    }
                }
            }
            else
            {
                // Tắt thanh máu nếu không phải là Tool
                if (durabilityContainer != null) durabilityContainer.SetActive(false);
            }
        }
        else
        {
            ClearSlot();
        }

        if (highlightOutline != null)
        {
            bool isSelected = (storageType == StorageType.Hotbar && slotIndex == InventoryManager.Instance.selectedHotbarIndex);

            if (slot != null && slot.item is ConsumableItemData)
            {
                isSelected = false;
            }

            highlightOutline.SetActive(isSelected);
        }
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
        amountText.text = "";

        // Nhớ ẩn thanh máu khi ô đồ trống rỗng
        if (durabilityContainer != null) durabilityContainer.SetActive(false);
    }

    // ==========================================
    // NHẬN DIỆN CLICK & SHIFT CHUYỂN NHANH
    // ==========================================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (icon.sprite == null) return;

        // 1. CHUỘT PHẢI: Ăn / Dùng đồ
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryManager.Instance.ConsumeItem(storageType, slotIndex);
            return;
        }

        // 2. CHUỘT TRÁI
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            PlayerInputHandler playerInput = FindAnyObjectByType<PlayerInputHandler>();

            // Check phím Shift
            bool isShiftPressed = (playerInput != null && playerInput.IsRunning) ||
                                  (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed);

            if (isShiftPressed)
            {
                // --- ƯU TIÊN 1: NẾU ĐANG MỞ MÁNG ĂN VÀ ĐANG BẤM VÀO BALO ---
                if (FoodTroughUIManager.Instance != null && FoodTroughUIManager.Instance.troughUIPanel.activeSelf &&
                    (storageType == StorageType.Inventory || storageType == StorageType.Hotbar))
                {
                    InventorySlot slotData = null;
                    if (storageType == StorageType.Inventory) slotData = InventoryManager.Instance.inventorySlots[slotIndex];
                    else if (storageType == StorageType.Hotbar) slotData = InventoryManager.Instance.hotbarSlots[slotIndex];

                    if (slotData != null && slotData.item != null && slotData.amount > 0)
                    {
                        int remainingAmount = slotData.amount;
                        bool itemMoved = FoodTroughUIManager.Instance.TryAddFoodFromShiftClick(slotData.item, slotData.amount, out remainingAmount);

                        if (itemMoved)
                        {
                            slotData.amount = remainingAmount;
                            if (slotData.amount <= 0)
                            {
                                slotData.item = null;
                                slotData.currentDurability = -1f;
                            }
                            InventoryManager.Instance.RefreshInventoryUI();
                            return; // Dừng lại tại đây, ĐÃ CHUYỂN VÀO MÁNG XONG!
                        }
                    }
                }

                // --- ƯU TIÊN 2: TRẢ LẠI QUYỀN CHO CODE GỐC CỦA M ---
                // (Chuyển Rương -> Balo, Balo -> Rương, Balo -> Mua Bán...)
                InventoryManager.Instance.ShiftClickItem(storageType, slotIndex);
            }
        }
    }

    // ==========================================
    // LOGIC KÉO THẢ (DRAG & DROP)
    // ==========================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (icon.sprite == null) return;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        SetIconAlpha(0.5f);

        ghostIcon = new GameObject("GhostIcon");
        ghostIcon.transform.SetParent(parentCanvas.transform, false);
        ghostIcon.transform.SetAsLastSibling();

        Image ghostImage = ghostIcon.AddComponent<Image>();
        ghostImage.sprite = icon.sprite;
        ghostImage.raycastTarget = false;

        RectTransform ghostRect = ghostIcon.GetComponent<RectTransform>();
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        ghostRect.sizeDelta = iconRect.sizeDelta;

        ghostIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) ghostIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetIconAlpha(1f);
        if (ghostIcon != null) Destroy(ghostIcon);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            InventorySlotUI draggedSlot = droppedObject.GetComponentInParent<InventorySlotUI>();

            if (draggedSlot != null && draggedSlot != this)
            {
                ItemData draggedItem = GetItemData(draggedSlot.storageType, draggedSlot.slotIndex);
                ItemData targetItem = GetItemData(this.storageType, this.slotIndex);

                if (this.storageType == StorageType.Hotbar && draggedItem != null)
                {
                    if (draggedItem.itemType == ItemType.Material || draggedItem.itemType == ItemType.Misc)
                    {
                        Debug.Log("Không thể đặt Nguyên liệu vào Hotbar!");
                        return;
                    }
                }

                if (draggedSlot.storageType == StorageType.Hotbar && targetItem != null)
                {
                    if (targetItem.itemType == ItemType.Material || targetItem.itemType == ItemType.Misc)
                    {
                        Debug.Log("Không thể hoán đổi Nguyên liệu vào Hotbar!");
                        return;
                    }
                }
                InventoryManager.Instance.SwapItems(draggedSlot.storageType, draggedSlot.slotIndex, this.storageType, this.slotIndex);
                return;
            }
            UITroughDropSlot draggedTroughSlot = droppedObject.GetComponent<UITroughDropSlot>();
            if (draggedTroughSlot != null)
            {
                if (FoodTroughUIManager.Instance != null)
                {
                    FoodTroughUIManager.Instance.HandleItemTakenBack(draggedTroughSlot.troughSlotIndex, this);
                }
            }
        }
    }

    private ItemData GetItemData(StorageType type, int index)
    {
        if (type == StorageType.Hotbar) return InventoryManager.Instance.hotbarSlots[index].item;
        if (type == StorageType.Inventory) return InventoryManager.Instance.inventorySlots[index].item;
        if (type == StorageType.Chest && InventoryManager.Instance.currentOpenChest != null)
        {
            if (index < InventoryManager.Instance.currentOpenChest.chestSlots.Count)
                return InventoryManager.Instance.currentOpenChest.chestSlots[index].item;
        }
        return null;
    }

    private void SetIconAlpha(float alpha)
    {
        if (icon == null) return;
        Color color = icon.color;
        color.a = alpha;
        icon.color = color;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ItemData item = GetItemData(storageType, slotIndex);
        if (item != null && ItemTooltipUI.Instance != null)
        {
            RectTransform myRect = GetComponent<RectTransform>();
            ItemTooltipUI.Instance.StartHover(item, myRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }
}