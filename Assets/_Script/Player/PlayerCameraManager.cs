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
    private bool isHammerOpen = false;
    private bool isAnimalUIOpen = false;
    private bool isFoodTroughOpen = false;

    public Behaviour cameraInputProvider;
    public GameObject cameraObject;

    [Header("Cài đặt Con Trỏ Chuột (Custom Cursor)")]
    public Texture2D cursorTexture; 
    public Vector2 cursorHotspot = Vector2.zero;

    public Texture2D aimCrosshairTexture;
    public Vector2 crosshairHotspot = new Vector2(16, 16);

    public GameObject fishingCamera;
    private bool isFishingCameraActive = false;

    public GameObject throwCamera; // Kéo Virtual Camera ngắm ném vào đây
    private bool isThrowCameraActive = false;
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
        SetNormalCursor();

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
                isInventoryOpen = isOpen;

                UpdateCursorState();
            };
        }
        if (HammerUIManager.Instance != null)
        {
            HammerUIManager.Instance.OnHammerUIToggled += (isOpen) => {
                isHammerOpen = isOpen;
                UpdateCursorState();
            };
        }
        if (AnimalPenUIManager.Instance != null)
        {
            AnimalPenUIManager.Instance.OnAnimalUIToggled += (isOpen) => {
                isAnimalUIOpen = isOpen;
                UpdateCursorState();
            };
        }
        if (FoodTroughUIManager.Instance != null)
        {
            FoodTroughUIManager.Instance.OnTroughUIToggled += (isOpen) => {
                isFoodTroughOpen = isOpen;
                isInventoryOpen = isOpen;
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.TogglePanel(isOpen);

                UpdateCursorState();
            };
        }
    }
    private void Update()
    {
        // 1. NÚT TAB: Bật/Tắt túi đồ
        if (inputHandler.InventoryTriggered)
        {
            if (isHammerOpen)
            {
                if (HammerUIManager.Instance != null) HammerUIManager.Instance.CloseUI();
                return;
            }
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
            if (isFoodTroughOpen)
            {
                if (FoodTroughUIManager.Instance != null) FoodTroughUIManager.Instance.CloseTroughUI();
                return; // Thoát luôn, chặn không cho Balo tự tắt/mở lung tung
            }
            isInventoryOpen = !isInventoryOpen;

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.TogglePanel(isInventoryOpen);

            UpdateCursorState();
        }
        // 3. Chuột TRÁI: Nếu đang mở UI mà click ra ngoài viền thì khóa chuột lại
        if (inputHandler.ClickTriggered && !isCursorLocked && !isInventoryOpen && !isChestOpen && !isShopOpen && !isPlotUIOpen && !isBuilderOpen && !isHammerOpen && !isAnimalUIOpen && !isFoodTroughOpen)
        {
            SetCursorState(true);
        }
    }

    // Hàm tổng hợp: Chỉ khóa chuột khi TẤT CẢ các bảng UI đều đang tắt
    private void UpdateCursorState()
    {
        if (isInventoryOpen || isChestOpen || isPlotUIOpen || isShopOpen || isBuilderOpen || isSiteUIOpen || isHammerOpen || isAnimalUIOpen || isFoodTroughOpen)
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
                cameraInputProvider.enabled = !isFishingCameraActive && !isThrowCameraActive;
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
    public void SetNormalCursor()
    {
        if (cursorTexture != null)
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    public void SetAimCrosshairCursor()
    {
        if (aimCrosshairTexture != null)
            Cursor.SetCursor(aimCrosshairTexture, crosshairHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Nếu không có ảnh thì về chuột nhọn mặc định
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
    public void ToggleThrowCamera(bool isAiming)
    {
        isThrowCameraActive = isAiming;
        if (throwCamera != null)
        {
            throwCamera.SetActive(isAiming);
        }
        UpdateCursorState();
    }
}