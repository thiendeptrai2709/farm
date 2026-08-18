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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    public void ShowPrompt(Transform target, string text, bool showProgress = false, float progressValue = 0f)
    {
        promptText.text = text;
        promptPanel.SetActive(true);

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(showProgress);
            progressBar.value = Mathf.Clamp01(progressValue);
        }
    }

    public void HidePrompt()
    {
        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }
}