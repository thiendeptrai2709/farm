using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    [Header("Tham chiếu UI (Kéo thả vào đây)")]
    public GameObject tooltipPanel; // Cái bảng nền
    public TextMeshProUGUI nameText; // Tên vật phẩm
    public TextMeshProUGUI typeText; // Loại (Tool, Seed...)
    public TextMeshProUGUI statsText; // Chỉ số động (Sát thương, Hồi máu...)
    public TextMeshProUGUI priceText; // Giá bán

    [Header("Cài đặt")]
    public float showDelay = 0.25f; // Rê chuột để yên 0.25s mới hiện

    private float currentDelay;
    private ItemData currentItem;
    private bool isHovering = false;
    private RectTransform targetSlotRect;

    public float secondsPerGameDay = 1440f;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideTooltip(); // Tắt ngay khi mới vào game
    }

    private void Update()
    {
        // Nếu chuột đang nằm trong ô đồ và có đồ bên trong
        if (isHovering && currentItem != null)
        {
            currentDelay -= Time.deltaTime;

            // Hết thời gian chờ -> Bật bảng lên
            if (currentDelay <= 0f && !tooltipPanel.activeSelf)
            {
                ShowPanel();
            }
        }
    }

    public void StartHover(ItemData item, RectTransform slotRect)
    {
        if (item == null) return;
        currentItem = item;
        targetSlotRect = slotRect; // Lưu định vị lại
        isHovering = true;
        currentDelay = showDelay;
    }

    public void StopHover()
    {
        isHovering = false;
        currentItem = null;
        HideTooltip();
    }

    private void ShowPanel()
    {
        tooltipPanel.SetActive(true);

        // 1. CHUNG: Tên vật phẩm và Phân loại
        nameText.text = currentItem.displayName;
        typeText.text = currentItem.itemType.ToString();

        // 2. TẮT GIÁ BÁN Ở ĐÂY (Giải quyết Vấn đề 2 bên dưới)
        if (priceText != null) priceText.gameObject.SetActive(false);

        if (typeText != null) typeText.gameObject.SetActive(false);
        // 3. ĐỘNG: Đọc chỉ số chi tiết và TỰ ĐỘNG ẨN nếu không có gì
        string finalStats = BuildStatsString(currentItem);
        if (string.IsNullOrWhiteSpace(finalStats))
        {
            // Nếu không có chỉ số gì (như Gỗ, Đá) -> Tắt luôn cục Text để bảng tự co lại khít rịt
            statsText.gameObject.SetActive(false);
        }
        else
        {
            // Nếu có chỉ số -> Bật lên và điền chữ vào
            statsText.gameObject.SetActive(true);
            statsText.text = finalStats;
        }

        SnapToBottomRight();
    }
    private void SnapToBottomRight()
    {
        if (targetSlotRect == null || tooltipPanel == null) return;

        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0f, 1f); // Pivot luôn ở góc Trên - Trái

        // Ép Unity tính toán lại kích thước bảng NGAY LẬP TỨC dựa trên lượng chữ mới
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        // Lấy vị trí góc Dưới - Phải của ô đồ
        Vector3[] slotCorners = new Vector3[4];
        targetSlotRect.GetWorldCorners(slotCorners);
        Vector3 targetPos = slotCorners[3];

        // Đo kích thước thực tế của Tooltip trên màn hình
        float tooltipWidth = panelRect.rect.width * panelRect.lossyScale.x;
        float tooltipHeight = panelRect.rect.height * panelRect.lossyScale.y;

        // --- HỆ THỐNG CHỐNG TRÀN ---
        // Nếu tràn viền Phải màn hình -> Kéo dịch sang trái
        if (targetPos.x + tooltipWidth > Screen.width)
        {
            targetPos.x = Screen.width - tooltipWidth;
        }

        // Nếu tràn viền Dưới màn hình -> Kéo dịch lên trên
        // (Do trục Y của màn hình bắt đầu từ 0 ở đáy)
        if (targetPos.y - tooltipHeight < 0)
        {
            targetPos.y = tooltipHeight;
        }

        // Áp dụng vị trí cuối cùng an toàn
        panelRect.position = targetPos;
    }
    private void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
    // ==========================================
    // BỘ MÁY ĐỌC CHỈ SỐ THÔNG MINH
    // ==========================================
    private string BuildStatsString(ItemData item)
    {
        StringBuilder builder = new StringBuilder();

        // --- NẾU LÀ CÔNG CỤ / VŨ KHÍ ---
        if (item is ToolItemData tool)
        {
            builder.AppendLine($"Durability: {tool.durability}");
        }

        // --- NẾU LÀ HẠT GIỐNG ---
        else if (item is SeedItemData seed)
        {
            // Tính số ngày (Làm tròn lên: ví dụ 1.2 ngày -> 2 ngày)
            int days = Mathf.CeilToInt(seed.growTime / secondsPerGameDay);
            builder.AppendLine($"Grow time: {days} days");
            // Phân loại phương thức thu hoạch
            if (seed.isBigTree)
            {
                builder.AppendLine("Yields fruit continuously");
            }
            else if (seed.isMultiHarvest)
            {
                builder.AppendLine("Multi-harvest crop");
            }
            else
            {
                builder.AppendLine("Single harvest");
            }
        }

        // --- NẾU LÀ ĐỒ ĂN THỨC UỐNG ---
        else if (item is ConsumableItemData consumable)
        {
            if (consumable.hungerRestore > 0)
                builder.AppendLine($"Restores Hunger: +{consumable.hungerRestore}");
            if (consumable.thirstRestore > 0)
                builder.AppendLine($"Restores Thirst: +{consumable.thirstRestore}");
            if (consumable.healthRestore > 0)
                builder.AppendLine($"Restores Health: +{consumable.healthRestore}");
        }

        return builder.ToString();
    }
}