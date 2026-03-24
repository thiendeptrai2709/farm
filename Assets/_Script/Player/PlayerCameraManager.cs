using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerCameraManager : MonoBehaviour
{
    private PlayerInputHandler inputHandler;
    public static PlayerCameraManager Instance;

    public bool isCursorLocked { get; private set; } = true;

    // Quản lý trạng thái các bảng UI
    private bool isInventoryOpen = false;
    private bool isChestOpen = false;
    private bool isPlotUIOpen = false;
    private bool isShopOpen = false;
    private bool isBuilderOpen = false;
    private bool isSiteUIOpen = false;

    public Behaviour cameraInputProvider;
    public GameObject cameraObject;

    [Header("Cài đặt Con Trỏ Chuột (Custom Cursor)")]
    public Texture2D cursorTexture; 
    public Vector2 cursorHotspot = Vector2.zero;

    public GameObject fishingCamera;
    private bool isFishingCameraActive = false;
    private void Awake()
    {
        // [ĐÃ THÊM 1.1]: Khởi tạo Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }

        SetCursorState(true);

        if (cameraObject != null)
        {
            cameraInputProvider = cameraObject.GetComponent("CinemachineInputAxisController") as Behaviour;

            if (cameraInputProvider == null)
            {
                Debug.LogError("Cảnh báo: Không tìm thấy CinemachineInputAxisController trên Camera!");
            }
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChestToggled += (isOpen) => {
                isChestOpen = isOpen;
                UpdateCursorState();
            };
        }
        if (FarmPlotUIManager.Instance != null)
        {
            FarmPlotUIManager.Instance.OnPlotUIToggled += (isOpen) => {
                isPlotUIOpen = isOpen;

                // Đồng bộ biến isInventoryOpen vì khi mở Plot UI, cái Balo cũng bị ép mở theo
                isInventoryOpen = isOpen;

                UpdateCursorState();
            };
        }
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnShopUIToggled += (isOpen) => {
                isShopOpen = isOpen;

                // Đồng bộ biến isInventoryOpen vì khi mở Shop, Balo cũng mở theo
                isInventoryOpen = isOpen;
                UpdateCursorState();
            };
        }
        if (BuilderUIManager.Instance != null)
        {
            BuilderUIManager.Instance.OnBuilderUIToggled += (isOpen) => {
                isBuilderOpen = isOpen;
                UpdateCursorState();
            };
        }
        if (SiteConstructionUIManager.Instance != null)
        {
            SiteConstructionUIManager.Instance.OnSiteConstructionUIToggled += (isOpen) => {
                isSiteUIOpen = isOpen;

                // Đồng bộ luôn: Vì bảng Nộp đồ mở ra là ép Balo mở theo, nên phải báo cho hệ thống biết là Balo cũng đang mở!
                isInventoryOpen = isOpen;

                UpdateCursorState();
            };
        }
    }

    private void Update()
    {
        // 1. NÚT TAB: Bật/Tắt túi đồ
        if (inputHandler.InventoryTriggered)
        {
            if (isChestOpen)
            {
                InventoryManager.Instance.CloseChest();
                return;
            }
            if (isBuilderOpen)
            {
                if (BuilderUIManager.Instance != null) BuilderUIManager.Instance.CloseUI();
                return; // Thoát luôn, chặn không cho Balo nhảy lên
            }
            if (isSiteUIOpen)
            {
                if (SiteConstructionUIManager.Instance != null) SiteConstructionUIManager.Instance.CloseUI();
                return; // Thoát, không cho Balo tự tắt mở lung tung
            }

            isInventoryOpen = !isInventoryOpen;

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.TogglePanel(isInventoryOpen);

            UpdateCursorState();
        }



        // 3. Chuột TRÁI: Nếu đang mở UI mà click ra ngoài viền thì khóa chuột lại
        if (inputHandler.ClickTriggered && !isCursorLocked && !isInventoryOpen && !isChestOpen && !isShopOpen && !isPlotUIOpen && !isBuilderOpen)
        {
            SetCursorState(true);
        }
    }

    // Hàm tổng hợp: Chỉ khóa chuột khi TẤT CẢ các bảng UI đều đang tắt
    private void UpdateCursorState()
    {
        if (isInventoryOpen || isChestOpen || isPlotUIOpen || isShopOpen || isBuilderOpen || isSiteUIOpen)
        {
            SetCursorState(false); // Nhả chuột ra để kéo thả UI

            // Đóng băng Camera (Tắt script đọc chuột của Cinemachine)
            if (cameraInputProvider != null) cameraInputProvider.enabled = false;
        }
        else
        {
            SetCursorState(true);  // Giấu chuột đi chơi tiếp

            // Mở khóa Camera
            if (cameraInputProvider != null)
            {
                cameraInputProvider.enabled = !isFishingCameraActive;
            }
        }
    }

    public void SetCursorState(bool isLocked)
    {
        isCursorLocked = isLocked;
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
    public void ToggleFishingCamera(bool isFishing)
    {
        isFishingCameraActive = isFishing; // Chốt cờ đang câu cá

        if (fishingCamera != null)
        {
            // Bật ảo ảnh Camera này lên, Cinemachine sẽ tự động mượt mà lướt tới
            fishingCamera.SetActive(isFishing);
        }
        UpdateCursorState();
    }
}