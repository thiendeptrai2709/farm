using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class MainMenuController : MonoBehaviour
{
    [Header("Scene bắt đầu game")]
    public string sceneToStart = "Tower";
    public GameObject gameplayCorePrefab;

    [Header("UI Panels")]
    public GameObject mainButtonsPanel;
    public GameObject saveSlotPanel;
    public GameObject settingPanel;

    public TextMeshProUGUI[] slotTexts;
    private void Start()
    {
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.enabled = false;
        if (InventoryUI.Instance != null) InventoryUI.Instance.ToggleInGameUI(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null) input.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMainButtons();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("BGM_Menu");
    }

    public void ShowMainButtons()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
    }

    public void OpenSaveSlotPanel()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (saveSlotPanel != null) saveSlotPanel.SetActive(true);

        // [ĐÃ THÊM]: Cập nhật Text cho từng nút Slot
        if (SaveManager.Instance != null && slotTexts != null)
        {
            for (int i = 0; i < slotTexts.Length; i++)
            {
                int slotIndex = i + 1; // Tương đương Slot 1, 2, 3
                GameData slotData = SaveManager.Instance.PeekSlotData(slotIndex);

                if (slotData != null)
                {
                    // Nếu đã có Save -> Hiện Ngày và Thời gian
                    slotTexts[i].text = string.Format("Ngày: {0} - {1:00}:{2:00}", slotData.daysInGame, slotData.savedHour, slotData.savedMinute);
                }
                else
                {
                    // Nếu rỗng -> Trả về mặc định
                    slotTexts[i].text = "Slot " + slotIndex;
                }
            }
        }
    }

    public void OpenSettingPanel()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(true);
    }

    public void SelectSlotAndPlay(int slotIndex)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
        Debug.Log("Bắt đầu chơi ở Slot: " + slotIndex);
        if (LoadingManager.Instance != null)
        {
            string sceneToLoad = sceneToStart;
            bool hasSaveData = false;

            if (SaveManager.Instance != null)
            {
                hasSaveData = SaveManager.Instance.HasSaveFile(slotIndex);
                SaveManager.Instance.SetCurrentSlotAndLoad(slotIndex);

                if (hasSaveData)
                {
                    GameData data = SaveManager.Instance.GetCurrentData();
                    if (data != null && !string.IsNullOrEmpty(data.lastSceneName))
                    {
                        sceneToLoad = data.lastSceneName;
                    }
                }
            }

            string spawnSignal = hasSaveData ? "SavedPosition" : "1";

            LoadingManager.Instance.LoadScene(sceneToLoad, spawnSignal, gameplayCorePrefab);
        }
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
        Application.Quit();
    }
}