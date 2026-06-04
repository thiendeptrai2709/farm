using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene bắt đầu game")]
    public string sceneToStart = "Tower";
    public GameObject gameplayCorePrefab;

    [Header("UI Panels")]
    public GameObject pressAnyKeyPanel;
    public GameObject mainButtonsPanel;
    public GameObject saveSlotPanel;
    public GameObject settingPanel;

    [Header("Hiệu ứng Nút bấm (Mới)")]
    [Tooltip("Kéo các nút (Chơi, Setting, Thoát...) vào đây theo thứ tự bạn muốn nó mọc ra")]
    public Transform[] animatedButtons;
    public float staggerTime = 0.15f; // Thời gian trễ giữa các nút (giây)
    public float popSpeed = 8f; // Tốc độ mọc ra của nút

    public TextMeshProUGUI[] slotTexts;

    [Header("Đa Ngôn Ngữ")]
    public LocalizedString textDay;
    public LocalizedString textSlot;

    private static bool isFirstTimeBoot = true;

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

        if (isFirstTimeBoot)
        {
            if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(true);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
            if (settingPanel != null) settingPanel.SetActive(false);
        }
        else
        {
            if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
            ShowMainButtons();
        }
    }

    private void Update()
    {
        if (pressAnyKeyPanel != null && pressAnyKeyPanel.activeSelf)
        {
            bool anyKeyPressed = false;

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                anyKeyPressed = true;
            }
            else if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                anyKeyPressed = true;
            }

            if (anyKeyPressed)
            {
                TransitionToMainMenu();
            }
        }
    }

    private void TransitionToMainMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        isFirstTimeBoot = false;

        if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
        ShowMainButtons();
    }

    // ==========================================
    // KHU VỰC ĐÃ SỬA: HIỆU ỨNG MỌC RA TỪNG NÚT
    // ==========================================
    public void ShowMainButtons()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);

        // Kích hoạt tiến trình (Coroutine) tạo hiệu ứng
        if (animatedButtons != null && animatedButtons.Length > 0)
        {
            StartCoroutine(ShowButtonsSequentially());
        }
    }

    private System.Collections.IEnumerator ShowButtonsSequentially()
    {
        // 1. Ép tất cả các nút thu nhỏ về 0 (Tàng hình) ngay lập tức
        foreach (Transform btn in animatedButtons)
        {
            if (btn != null) btn.localScale = Vector3.zero;
        }

        // 2. Lặp qua từng nút, gọi nó mọc ra, rồi đợi 0.15s mới gọi nút tiếp theo
        foreach (Transform btn in animatedButtons)
        {
            if (btn != null)
            {
                StartCoroutine(PopUpButton(btn));
                yield return new WaitForSeconds(staggerTime); // Khúc đợi chờ làm nên sự mượt mà
            }
        }
    }

    private System.Collections.IEnumerator PopUpButton(Transform btn)
    {
        float progress = 0;
        // Dùng toán học (Lerp) để kéo Scale của nút từ 0 lên 1
        while (progress < 1f)
        {
            progress += Time.deltaTime * popSpeed;
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
            yield return null; // Chờ sang frame tiếp theo để tiếp tục phình to
        }
        btn.localScale = Vector3.one; // Chốt hạ kích thước chuẩn tránh sai số
    }
    // ==========================================

    public void OpenSaveSlotPanel()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (saveSlotPanel != null) saveSlotPanel.SetActive(true);

        if (SaveManager.Instance != null && slotTexts != null)
        {
            for (int i = 0; i < slotTexts.Length; i++)
            {
                int slotIndex = i + 1;
                GameData slotData = SaveManager.Instance.PeekSlotData(slotIndex);

                if (slotData != null)
                {
                    string dayStr = textDay.IsEmpty ? "Ngày" : textDay.GetLocalizedString();
                    slotTexts[i].text = string.Format("{0}: {1} - {2:00}:{3:00}", dayStr, slotData.daysInGame, slotData.savedHour, slotData.savedMinute);
                }
                else
                {
                    string slotStr = textSlot.IsEmpty ? "Slot" : textSlot.GetLocalizedString();
                    slotTexts[i].text = slotStr + " " + slotIndex;
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