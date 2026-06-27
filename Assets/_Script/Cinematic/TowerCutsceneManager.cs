using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;
using System.Collections;

public class TowerCutsceneManager : MonoBehaviour
{
    public PlayableDirector director;

    [Header("Tích hợp Xe Bus")]
    public BusVehicle cutsceneBus;
    public float delayBusStart = 4f;

    [Header("Thời gian hiện Player")]
    [Tooltip("Bật Player lên trước khi hết phim bao nhiêu giây? (Khớp với thời gian Blend Out)")]
    public float showPlayerBeforeEnd = 2.5f;

    private void OnEnable()
    {
        LoadingManager.OnPlayerReady += StartCutscene;
    }

    private void OnDisable()
    {
        LoadingManager.OnPlayerReady -= StartCutscene;
    }

    private void StartCutscene()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            if (SaveManager.Instance.GetCurrentData().hasSeenTowerIntro)
            {
                Debug.Log("[CUTSCENE] Đã xem Intro ở Tower rồi, bỏ qua!");
                return;
            }
        }

        CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain == null)
        {
            Debug.LogError("[CUTSCENE] LỖI: Không tìm thấy Brain trên Main Camera!");
            return;
        }

        foreach (var output in director.playableAsset.outputs)
        {
            if (output.outputTargetType == typeof(CinemachineBrain))
            {
                director.SetGenericBinding(output.sourceObject, brain);
                break;
            }
        }

        // 1. CHUẨN BỊ: Khóa di chuyển và tàng hình Player ngay từ đầu
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.isActionLocked = true;
            SetPlayerRenderers(false);
        }
        if (InventoryUI.Instance != null) InventoryUI.Instance.ToggleInGameUI(false);
        if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ToggleVisibility(false);
        if (PlayerCameraManager.Instance != null && PlayerCameraManager.Instance.cameraInputProvider != null)
        {
            PlayerCameraManager.Instance.cameraInputProvider.enabled = false;
        }

        PauseMenuManager.isCutscenePlaying = true;

        if (cutsceneBus != null) StartCoroutine(DelayStartBus());

        director.stopped += OnCutsceneEnd;
        director.Play();

        // CHẠY BỘ ĐẾM THỜI GIAN ĐỂ GỌI PLAYER LÊN
        StartCoroutine(ShowPlayerAtRightTime());
    }

    // LUỒNG MỚI: Tự động đếm giờ để bật Player
    private IEnumerator ShowPlayerAtRightTime()
    {
        // Lấy tổng thời gian Timeline trừ đi số giây m muốn bật trước
        double targetTime = director.duration - showPlayerBeforeEnd;
        if (targetTime < 0) targetTime = 0;

        // Chờ cho đến khi Timeline chạy đến đúng giây đó
        while (director.state == PlayState.Playing && director.time < targetTime)
        {
            yield return null;
        }

        // Đã đến lúc! Bật Player lên ngay lập tức!
        if (PlayerMovement.Instance != null)
        {
            SetPlayerRenderers(true);
            Debug.Log("<color=cyan>[CODE] Đã ép bật Player thành công trước khi kết thúc phim " + showPlayerBeforeEnd + " giây!</color>");
        }
    }

    private void OnCutsceneEnd(PlayableDirector pd)
    {
        PauseMenuManager.isCutscenePlaying = false;

        // Đánh dấu là đã xem và lưu data khi phim đã kết thúc hoàn toàn
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentData() != null)
        {
            SaveManager.Instance.GetCurrentData().hasSeenTowerIntro = true;
            SaveManager.Instance.SaveGame();
        }

        // Chốt chặn an toàn: Đảm bảo hết phim thì player phải hiện, tay phải được thả
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.isActionLocked = false;
            SetPlayerRenderers(true);
        }

        if (InventoryUI.Instance != null) InventoryUI.Instance.ToggleInGameUI(true);
        if (StaminaUIManager.Instance != null) StaminaUIManager.Instance.ToggleVisibility(true);
        if (PlayerCameraManager.Instance != null && PlayerCameraManager.Instance.cameraInputProvider != null)
        {
            PlayerCameraManager.Instance.cameraInputProvider.enabled = true;
        }

        director.stopped -= OnCutsceneEnd;
    }
    private IEnumerator DelayStartBus()
    {
        yield return new WaitForSeconds(delayBusStart);
        if (cutsceneBus != null) cutsceneBus.StartDrivingIn("Cutscene_Mode", "None");
    }

    private void SetPlayerRenderers(bool state)
    {
        Renderer[] playerRenderers = PlayerMovement.Instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in playerRenderers)
        {
            r.enabled = state;
        }
    }
}