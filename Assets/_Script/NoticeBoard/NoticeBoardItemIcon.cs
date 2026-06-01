using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoticeBoardItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemData myItemData;
    private RectTransform myRect;

    [Header("UI Components")]
    [Tooltip("Kéo tấm ảnh dùng làm viền (hoặc nền) thể hiện Tier vào đây")]
    public Image tierMarkerImage;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    public void SetupIcon(ItemData data)
    {
        myItemData = data;

        // [ĐÃ THÊM]: Cập nhật màu sắc Tier khi tải dữ liệu nhiệm vụ
        UpdateTierVisuals();
    }

    // [ĐÃ THÊM]: Hàm xử lý màu Tier
    private void UpdateTierVisuals()
    {
        if (tierMarkerImage != null)
        {
            if (myItemData is FishItemData fish)
            {
                tierMarkerImage.gameObject.SetActive(true);
                tierMarkerImage.color = GetColorFromTier(fish.tier);
            }
            else if (myItemData is ToolItemData toolItem)
            {
                tierMarkerImage.gameObject.SetActive(true);
                tierMarkerImage.color = GetColorFromToolTier(toolItem.toolTier);
            }
            else
            {
                // Nếu là đồ bình thường (khoáng sản, lúa mạch) thì tắt cái viền Tier đi
                tierMarkerImage.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myItemData != null && ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StartHover(myItemData, myRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }

    private void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.StopHover();
        }
    }

    // ==========================================
    // BỘ HÀM CHUYỂN ĐỔI MÀU TIER
    // ==========================================
    private Color GetColorFromTier(FishTier tier)
    {
        switch (tier)
        {
            case FishTier.Common: return Color.white;
            case FishTier.Uncommon: return new Color(0.2f, 1f, 0.2f);
            case FishTier.Rare: return new Color(0.2f, 0.6f, 1f);
            case FishTier.Epic: return new Color(0.8f, 0.2f, 1f);
            case FishTier.Legendary: return new Color(1f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }

    private Color GetColorFromToolTier(int tier)
    {
        switch (tier)
        {
            case 1: return new Color(0.5f, 0.5f, 0.5f);
            case 2: return new Color(0.8f, 0.4f, 0.15f);
            case 3: return new Color(0.75f, 0.75f, 0.8f);
            case 4: return new Color(1f, 0.85f, 0f);
            default: return Color.white;
        }
    }
}