using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene bắt đầu game")]
    public string sceneToStart = "Tower";
    public GameObject gameplayCorePrefab;
    private void Start()
    {
        // 1. Tắt Camera Manager (Khỏi giành giật con trỏ chuột)
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.enabled = false;

        // 2. GIẤU TOÀN BỘ UI TRONG GAME (Hotbar, Balo...) đi!
        if (InventoryUI.Instance != null) InventoryUI.Instance.ToggleInGameUI(false);

        // 3. ĐÓNG BĂNG THẰNG PLAYER: Tắt script nhận nút bấm, để nó không nghe được nút TAB, nút đánh, nút chạy nữa.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null) input.enabled = false;
        }

        // Hiện chuột của Windows lên để bấm nút Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        if (LoadingManager.Instance != null)
        {
            // THAY "Tower_North" BẰNG ĐÚNG CÁI ID MÀ BẠN ĐÃ ĐẶT Ở MAP BẮT ĐẦU NHÉ!
            LoadingManager.Instance.LoadScene(sceneToStart, "Tower_North", gameplayCorePrefab);
        }
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Item_Pickup");

        Debug.Log("Đang thoát game...");
        Application.Quit();
    }
}