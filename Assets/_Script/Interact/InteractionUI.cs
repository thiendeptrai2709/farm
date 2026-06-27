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

    [Header("Cài đặt Neo Chữ")]
    [Tooltip("Khoảng cách nâng chữ lên thêm một chút so với đỉnh đầu vật thể cho thoáng")]
    public float topPadding = 0.35f;
    public Vector3 fallbackOffset = new Vector3(0, 1.5f, 0);

    public bool alwaysOnTop = true;

    private Transform currentTargetTransform;
    private Camera mainCamera;
    private Vector3 lockedLocalOffset;

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

        // --- XUYÊN THẤU VẬT THỂ (CHỐNG CHE KHUẤT) ---
        if (alwaysOnTop && promptPanel != null)
        {
            Graphic[] uiGraphics = promptPanel.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in uiGraphics)
            {
                if (g is TextMeshProUGUI tmp)
                {
                    Material newMat = new Material(tmp.fontMaterial);
                    Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
                    if (overlayShader != null) newMat.shader = overlayShader;
                    tmp.fontMaterial = newMat;
                }
                else
                {
                    Material mat = new Material(g.material);
                    mat.SetInt("unity_GUIZTestMode", 8); // 8 = Always (Luôn vẽ đè lên trên cùng)
                    g.material = mat;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (currentTargetTransform != null && promptPanel.activeSelf)
        {
            transform.position = currentTargetTransform.TransformPoint(lockedLocalOffset);

            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    public void ShowPrompt(Transform target, string text, bool showProgress = false, float progressValue = 0f)
    {
        // Chỉ tính toán tọa độ đúng 1 lần khi tiếp cận mục tiêu mới
        if (currentTargetTransform != target)
        {
            currentTargetTransform = target;
            CalculateTopCenterPosition(target);
        }

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
        currentTargetTransform = null;
        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    // ==========================================
    // THUẬT TOÁN TÌM ĐỈNH ĐẦU & TÂM VẬT THỂ
    // ==========================================
    private void CalculateTopCenterPosition(Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            // Dùng thẳng mainCamera làm mốc đo (đỡ phải gọi biến playerTransform đã bị xóa)
            Vector3 referencePos = mainCamera != null ? mainCamera.transform.position : target.position;
            Vector3 finalXZ = combinedBounds.center;

            if (combinedBounds.size.x > 3.0f || combinedBounds.size.z > 3.0f)
            {
                Vector3 closestPointOnBounds = combinedBounds.ClosestPoint(referencePos);
                finalXZ.x = closestPointOnBounds.x;
                finalXZ.z = closestPointOnBounds.z;
            }

            float finalY = combinedBounds.max.y + topPadding;
            if ((combinedBounds.max.y - target.position.y) > 3.0f)
            {
                finalY = target.position.y + 2.2f;
            }

            Vector3 worldTopCenter = new Vector3(finalXZ.x, finalY, finalXZ.z);
            lockedLocalOffset = target.InverseTransformPoint(worldTopCenter);
        }
        else
        {
            lockedLocalOffset = target.InverseTransformPoint(target.position + fallbackOffset);
        }
    }
}