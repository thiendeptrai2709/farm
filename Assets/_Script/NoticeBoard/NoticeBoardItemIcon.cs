using UnityEngine;
using UnityEngine.EventSystems;

public class NoticeBoardItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemData myItemData;
    private RectTransform myRect;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    public void SetupIcon(ItemData data)
    {
        // Chức năng: Nhận dữ liệu món đồ từ UI Manager truyền vào
        myItemData = data;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Chức năng: Khi chuột lia vào, gọi Tooltip UI
        if (myItemData != null && ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StartHover(myItemData, myRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Chức năng: Khi chuột lia ra, tắt Tooltip UI
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }
    private void OnDisable()
    {
        // Chức năng: Ép tắt Tooltip nếu bảng chứa tấm ảnh này bị tắt đột ngột (do đi ra xa, bấm Tab...)
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }
}