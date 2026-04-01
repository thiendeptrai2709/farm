using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// [ĐÃ THÊM]: IBeginDragHandler, IDragHandler, IEndDragHandler
public class UITroughDropSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Vị trí của ô này trong máng (0 -> 4)")]
    public int troughSlotIndex;

    [Header("Thành phần Giao diện")]
    public Image itemIcon;
    public TextMeshProUGUI amountText;

    private static GameObject ghostIcon;
    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    // ===============================================
    // HÀM HỨNG ĐỒ TỪ BALO (GIỮ NGUYÊN)
    // ===============================================
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        InventorySlotUI draggedSlot = droppedObj.GetComponent<InventorySlotUI>();
        if (draggedSlot != null)
        {
            if (FoodTroughUIManager.Instance != null)
            {
                FoodTroughUIManager.Instance.HandleItemDropped(draggedSlot, troughSlotIndex);
            }
        }
    }

    // ===============================================
    // LOGIC KÉO ĐỒ TỪ MÁNG (THÊM MỚI)
    // ===============================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Chỉ cho phép kéo nếu ô này ĐANG CÓ ĐỒ
        if (itemIcon.sprite == null || !itemIcon.enabled) return;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        // Tạo bóng mờ bay theo chuột
        ghostIcon = new GameObject("GhostIcon_Trough");
        ghostIcon.transform.SetParent(parentCanvas.transform, false);
        ghostIcon.transform.SetAsLastSibling();

        Image ghostImage = ghostIcon.AddComponent<Image>();
        ghostImage.sprite = itemIcon.sprite;
        ghostImage.raycastTarget = false; // Chuột xuyên qua cái bóng để rớt xuống dưới

        RectTransform ghostRect = ghostIcon.GetComponent<RectTransform>();
        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
        ghostRect.sizeDelta = iconRect.sizeDelta;
        ghostIcon.transform.position = eventData.position;

        // Làm mờ icon gốc lúc đang kéo
        Color c = itemIcon.color; c.a = 0.5f; itemIcon.color = c;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) ghostIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Phục hồi độ sáng icon gốc
        Color c = itemIcon.color; c.a = 1f; itemIcon.color = c;
        if (ghostIcon != null) Destroy(ghostIcon);
    }

    // ===============================================
    // CẬP NHẬT GIAO DIỆN (GIỮ NGUYÊN)
    // ===============================================
    public void UpdateVisual(TroughSlot slotData)
    {
        if (slotData != null && slotData.item != null && slotData.amount > 0)
        {
            if (itemIcon != null) { itemIcon.sprite = slotData.item.icon; itemIcon.enabled = true; }
            if (amountText != null) amountText.text = slotData.amount > 1 ? slotData.amount.ToString() : "";
        }
        else
        {
            if (itemIcon != null) { itemIcon.sprite = null; itemIcon.enabled = false; }
            if (amountText != null) amountText.text = "";
        }
    }
}