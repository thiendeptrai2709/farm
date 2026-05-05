using UnityEngine;
using UnityEngine.UI;

public class NoticeBoardUIManager : MonoBehaviour
{
    public static NoticeBoardUIManager Instance { get; private set; }

    [Header("Thành phần UI")]
    public GameObject boardPanel;
    public Transform contentTransform;

    [Header("Prefabs")]
    public GameObject shopBlockPrefab;
    public GameObject itemIconPrefab;
    public Transform currentBoardTransform;

    private void Awake()
    {
        // Chức năng: Khởi tạo Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public bool IsOpen()
    {
        return boardPanel != null && boardPanel.activeSelf;
    }
    public void OpenBoard(Transform boardTransform)
    {
        currentBoardTransform = boardTransform; // Chức năng: Ghi nhớ vị trí bảng

        // Chức năng: Bật bảng UI và tải dữ liệu
        boardPanel.SetActive(true);
        RefreshBoard();

        // Chức năng: Nhả chuột và khóa Camera
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetNoticeBoardOpenState(true);
    }

    public void CloseBoard()
    {
        // Chức năng: Đóng bảng UI
        boardPanel.SetActive(false);

        // Chức năng: Giấu chuột và mở khóa Camera
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetNoticeBoardOpenState(false);
    }

    private void RefreshBoard()
    {
        // Chức năng: Xóa các khối cửa hàng cũ từ ngày hôm trước
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        if (MarketManager.Instance == null || MarketManager.Instance.allShopsInGame == null) return;

        // Chức năng: Duyệt qua tất cả cửa hàng để tạo khối mới
        foreach (ShopData shop in MarketManager.Instance.allShopsInGame)
        {
            if (shop == null || shop.itemsForSale.Count == 0) continue;

            // Chức năng: Tạo khối UI cho cửa hàng này
            GameObject shopObj = Instantiate(shopBlockPrefab, contentTransform);
            NoticeBoardShopBlock blockScript = shopObj.GetComponent<NoticeBoardShopBlock>();

            if (blockScript != null)
            {
                blockScript.SetupBlock(shop.npcName);

                // Chức năng: Duyệt qua các món đồ và chỉ tạo ảnh Image (không có Text số lượng/giá)
                foreach (ShopInventoryItem sItem in shop.itemsForSale)
                {
                    // Giả định ItemData của bạn có biến chứa ảnh tên là "icon"
                    if (sItem.item != null && sItem.item.icon != null)
                    {
                        GameObject iconObj = Instantiate(itemIconPrefab, blockScript.itemGridTransform);
                        Image img = iconObj.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite = sItem.item.icon;
                        }
                        NoticeBoardItemIcon iconHoverScript = iconObj.GetComponent<NoticeBoardItemIcon>();
                        if (iconHoverScript != null)
                        {
                            iconHoverScript.SetupIcon(sItem.item);
                        }
                    }
                }
            }
        }
    }
}