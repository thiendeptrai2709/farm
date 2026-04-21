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

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == menuSceneName) return;

        if (inputHandler != null && inputHandler.EscTriggered)
        {
            if (unsavedWarningPanel.activeSelf)
            {
                CancelWarning();
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
        pauseMenuPanel.SetActive(isPaused);

        // Gọi sang CameraManager để quản lý chuột đồng bộ với các UI khác
        if (PlayerCameraManager.Instance != null)
        {
            PlayerCameraManager.Instance.SetPauseMenuOpenState(isPaused);
        }
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