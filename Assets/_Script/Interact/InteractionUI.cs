using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    [Header("Thành phần UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;
    public Slider progressBar;

    [Header("Cài đặt hiển thị")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private Transform currentTargetTransform;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainCamera = Camera.main;
        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (currentTargetTransform != null && promptPanel.activeSelf)
        {
            transform.position = currentTargetTransform.position + offset;
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    // NÂNG CẤP: Nhận thêm biến showProgress và progressValue (0 -> 1)
    public void ShowPrompt(Transform target, string text, bool showProgress = false, float progressValue = 0f)
    {
        currentTargetTransform = target;
        promptText.text = text;
        promptPanel.SetActive(true);

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(showProgress);

            // Ép giá trị luôn nằm trong khoảng an toàn từ 0 đến 1
            progressBar.value = Mathf.Clamp01(progressValue);
        }
    }

    public void HidePrompt()
    {
        currentTargetTransform = null;
        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }
}