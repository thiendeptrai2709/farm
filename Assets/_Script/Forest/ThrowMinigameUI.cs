using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class ThrowMinigameUI : MonoBehaviour
{
    public static ThrowMinigameUI Instance;

    [Header("UI Components")]
    public GameObject minigamePanel;
    public RectTransform targetTemplate;
    public TextMeshProUGUI shotText;

    [Header("Cài đặt")]
    public int totalShots = 3;
    public float targetDuration = 1.2f;

    private int currentShots = 0;
    private int successHits = 0;
    private bool isMinigameActive = false;
    private System.Action<bool> onResultCallback;

    private void Awake()
    {
        Instance = this;
        if (minigamePanel != null) minigamePanel.SetActive(false);
    }

    public bool IsMinigameActive() { return isMinigameActive; }

    public void StartMinigame(System.Action<bool> result)
    {
        onResultCallback = result;
        isMinigameActive = true;

        // [ÉP CỨNG]: Ghi đè thông số Inspector, bắt buộc phải là 3 shot!
        totalShots = 3;
        currentShots = 0;
        successHits = 0;

        minigamePanel.SetActive(true);
        UpdateUI();

        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetAimCrosshairCursor();
        StartCoroutine(MinigameRoutine());
    }
    private void Update()
    {
        if (isMinigameActive)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
    private IEnumerator MinigameRoutine()
    {
        // [CHỐNG CƯỚP CÒ]: Đợi người chơi nhả nút click chuột trái lúc mồi game ra đã!
        yield return new WaitUntil(() => Mouse.current == null || !Mouse.current.leftButton.isPressed);

        while (currentShots < totalShots)
        {
            // Sinh ngẫu nhiên vị trí hồng tâm
            Vector2 randomPos = new Vector2(Random.Range(-250f, 250f), Random.Range(-150f, 150f));
            targetTemplate.anchoredPosition = randomPos;
            targetTemplate.gameObject.SetActive(true);

            float timer = targetDuration;
            bool hitThisTurn = false;
            bool playerClicked = false; // Check xem có bấm không, hay để thời gian trôi tự do

            while (timer > 0)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    playerClicked = true;
                    Vector2 mousePos = Mouse.current.position.ReadValue();

                    // [TÍNH TỌA ĐỘ CHUẨN]: Check xem chuột có nằm trong đúng cái viền của hình Target hay không
                    if (RectTransformUtility.RectangleContainsScreenPoint(targetTemplate, mousePos, null))
                    {
                        successHits++;
                        hitThisTurn = true;
                    }
                    else
                    {
                        hitThisTurn = false; // Bấm ra ngoài -> Tính là trượt
                    }

                    break; // [LUẬT MỚI]: Bấm 1 phát là hết lượt luôn, không được spam!
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            // Kết thúc 1 lượt
            targetTemplate.gameObject.SetActive(false);
            currentShots++;
            UpdateUI();

            // Đổi màu chữ báo hiệu
            if (shotText != null)
            {
                if (playerClicked)
                    shotText.color = hitThisTurn ? Color.green : Color.red;
                else
                    shotText.color = Color.red; // Ngồi nhìn không bấm hết giờ cũng tính là trượt (Đỏ)
            }

            yield return new WaitForSeconds(0.4f); // Nháy màu xíu rồi sang lượt mới

            if (shotText != null) shotText.color = Color.white;
        }

        EndMinigame();
    }

    private void UpdateUI()
    {
        if (shotText != null) shotText.text = $"Lượt bắn: {totalShots - currentShots}/{totalShots}";
    }

    private void EndMinigame()
    {
        isMinigameActive = false;
        minigamePanel.SetActive(false);

        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetNormalCursor();
        // [CHỐT LUẬT]: Phải trúng cả 3 lần (3 hit) mới được tính là văng đá thành công!
        bool isSuccess = (successHits >= 3);

        onResultCallback?.Invoke(isSuccess);
    }
}