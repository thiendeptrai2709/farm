using UnityEngine;
using TMPro; // Dùng TextMeshPro cho chữ đẹp và không bị vỡ
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Giao diện UI")]
    public TextMeshProUGUI notificationText;
    public CanvasGroup canvasGroup;

    [Header("Cài đặt thời gian")]
    public float displayTime = 2f; // Thời gian hiện rõ
    public float fadeTime = 0.5f;  // Thời gian mờ dần rồi biến mất

    private Coroutine currentCoroutine;

    private void Awake()
    {
        // Khởi tạo Singleton để gọi từ bất kỳ file nào
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ban đầu giấu thông báo đi
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void ShowNotification(string message)
    {
        // Nếu đang có 1 thông báo khác hiện ra thì tắt nó đi để hiện cái mới
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(FadeRoutine(message));
    }

    private IEnumerator FadeRoutine(string message)
    {
        notificationText.text = message;
        canvasGroup.alpha = 1f; // Hiện rõ 100%

        // Chờ 2 giây
        yield return new WaitForSeconds(displayTime);

        // Bắt đầu làm mờ dần (Fade out)
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f; // Tắt hẳn
    }
}