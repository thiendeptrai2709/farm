using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public PlayerInputHandler inputHandler;
    public GameObject pauseMenuPanel;
    public GameObject unsavedWarningPanel;
    public string menuSceneName = "Menu";

    private bool isPaused = false;
    private bool hasSaved = false;
    private bool isAttemptingToQuit = false;
    public GameObject settingsPanel;
    private void Start()
    {
        // Đảm bảo ẩn bảng cài đặt khi mới bắt đầu
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == menuSceneName) return;

        if (inputHandler != null && inputHandler.EscTriggered)
        {
            if (unsavedWarningPanel.activeSelf)
            {
                CancelWarning();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                // [ĐÃ THÊM]: Nếu đang mở Setting thì bấm ESC sẽ lùi lại bảng Pause
                CloseSettings();
            }
            // Chức năng: Ưu tiên kiểm tra xem có đang cầm bóng mờ xây dựng không, có thì cất đi
            else if (HammerBuildManager.Instance != null && HammerBuildManager.Instance.IsCurrentlyPlacing())
            {
                HammerBuildManager.Instance.CancelPlacement();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            CloseAllOtherUIs();
        }

        pauseMenuPanel.SetActive(isPaused);

        if (!isPaused && settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Gọi sang CameraManager để quản lý chuột đồng bộ với các UI khác
        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetPauseMenuOpenState(isPaused);
        }
    }
    private void CloseAllOtherUIs()
    {
        // Đóng Rương
        if (InventoryManager.Instance != null && InventoryManager.Instance.currentOpenChest != null)
            InventoryManager.Instance.CloseChest();

        // Đóng Balo cá nhân (Tên file UI của bác có thể là InventoryUI, hãy kiểm tra lại cho chuẩn)
        // if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen()) 
        //     InventoryUI.Instance.CloseUI();

        // Đóng UI Luống Đất
        if (FarmPlotUIManager.Instance != null && FarmPlotUIManager.Instance.IsOpen())
            FarmPlotUIManager.Instance.ClosePlotUI();

        // Đóng Cửa Hàng
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsOpen())
            ShopUIManager.Instance.CloseShop();

        // Đóng Bàn Thợ Mộc
        if (BuilderUIManager.Instance != null && BuilderUIManager.Instance.IsOpen())
            BuilderUIManager.Instance.CloseUI();

        // Đóng Công Trường Xây Dựng
        if (SiteConstructionUIManager.Instance != null && SiteConstructionUIManager.Instance.IsOpen())
            SiteConstructionUIManager.Instance.CloseUI();

        // Đóng Xe Bus
        if (BusUI.Instance != null && BusUI.Instance.IsOpen())
            BusUI.Instance.CloseUI();

        // Đóng Hội Thoại NPC
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsOpen())
            DialogueUIManager.Instance.CloseDialogue();

        // Đóng Máng Ăn
        if (FoodTroughUIManager.Instance != null && FoodTroughUIManager.Instance.IsOpen())
            FoodTroughUIManager.Instance.CloseTroughUI();

        // Đóng Chuồng Thú
        if (AnimalPenUIManager.Instance != null && AnimalPenUIManager.Instance.IsOpen())
            AnimalPenUIManager.Instance.CloseUI();

        if (HammerUIManager.Instance != null && HammerUIManager.Instance.IsOpen())
            HammerUIManager.Instance.CloseUI();
    }

    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // Gắn hàm này vào sự kiện OnClick của nút "Quay Lại / X" trong bảng Cài Đặt
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }
    public void ResumeGame()
    {
        if (isPaused) TogglePause();
    }

    public void SaveGame()
    {
        hasSaved = true;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("Không tìm thấy SaveManager trong scene!");
        }
    }

    public void ReturnToMenu()
    {
        if (!hasSaved)
        {
            isAttemptingToQuit = false;
            unsavedWarningPanel.SetActive(true);
        }
        else
        {
            ExecuteReturnToMenu();
        }
    }

    public void QuitGame()
    {
        if (!hasSaved)
        {
            isAttemptingToQuit = true;
            unsavedWarningPanel.SetActive(true);
        }
        else
        {
            ExecuteQuitGame();
        }
    }

    public void ConfirmSaveAndExit()
    {
        SaveGame();

        if (isAttemptingToQuit)
        {
            ExecuteQuitGame();
        }
        else
        {
            ExecuteReturnToMenu();
        }
    }

    public void ConfirmExitWithoutSaving()
    {
        if (isAttemptingToQuit)
        {
            ExecuteQuitGame();
        }
        else
        {
            ExecuteReturnToMenu();
        }
    }

    public void CancelWarning()
    {
        unsavedWarningPanel.SetActive(false);
    }

    private void ExecuteReturnToMenu()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Destroy(playerObj.transform.root.gameObject);
        }
        SceneManager.LoadScene(menuSceneName);
    }

    private void ExecuteQuitGame()
    {
        Application.Quit();
    }
}