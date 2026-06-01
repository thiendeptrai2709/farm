using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkeletonGraveUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Kéo khối Panel TỔNG (chứa cả 2 cái dưới) vào đây")]
    public GameObject panelUI;

    [Tooltip("Kéo Panel chứa Lời thoại vào đây (Khung nền của text)")]
    public GameObject textPanel;
    [Tooltip("Kéo Text chứa Lời thoại nhiệm vụ vào đây")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("Kéo Panel chứa Danh sách (chính là cái Scroll View) vào đây")]
    public GameObject listPanel;
    [Tooltip("Kéo Text chứa danh sách xương 0/1 vào đây")]
    public TextMeshProUGUI checklistText;

    [Header("Cài đặt Lời thoại")]
    [TextArea]
    public string questText = "Kẻ nào dám đánh thức giấc ngủ của ta? Hãy tìm lại các mảnh xương đang lưu lạc trong khu rừng này...";
    public float typeSpeed = 0.05f;

    [Header("Cài đặt Hiển thị")]
    public bool alwaysOnTop = true;
    public float autoCloseDistance = 4f;

    private Coroutine typingCoroutine;
    private Camera mainCamera;
    private Transform playerTransform;

    private void Start()
    {
        mainCamera = Camera.main;

        // Xuyên thấu vật thể: Ép UI hiển thị đè lên trên tất cả đồ vật 3D
        if (alwaysOnTop && panelUI != null)
        {
            Graphic[] uiGraphics = panelUI.GetComponentsInChildren<Graphic>(true);
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
                    mat.SetInt("unity_GUIZTestMode", 8); // 8 là chế độ Always (Luôn hiển thị)
                    g.material = mat;
                }
            }
        }

        if (panelUI != null) panelUI.SetActive(false);

        if (SkeletonQuestManager.Instance != null)
        {
            SkeletonQuestManager.Instance.OnBoneCollected += UpdateUI;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void LateUpdate()
    {
        if (panelUI != null && panelUI.activeSelf)
        {
            // Giữ lại mỗi việc xoay mặt Panel nhìn thẳng về phía Camera
            if (mainCamera != null)
            {
                panelUI.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void OnDestroy()
    {
        // Gỡ lắng nghe khi chuyển scene để tránh lỗi
        if (SkeletonQuestManager.Instance != null)
        {
            SkeletonQuestManager.Instance.OnBoneCollected -= UpdateUI;
        }
    }

    // Hàm này sẽ được gọi khi người chơi bấm E vào ngôi mộ
    public void TogglePanel()
    {
        if (panelUI != null)
        {
            bool isActive = !panelUI.activeSelf;
            panelUI.SetActive(isActive);

            if (isActive)
            {
                bool hasStarted = SkeletonQuestManager.Instance != null && SkeletonQuestManager.Instance.isQuestStarted;

                if (!hasStarted)
                {
                    // CHỐNG LỖI 1: Đánh dấu là đã bắt đầu NGAY LẬP TỨC!
                    if (SkeletonQuestManager.Instance != null)
                    {
                        SkeletonQuestManager.Instance.isQuestStarted = true;
                    }

                    if (listPanel != null) listPanel.SetActive(false);
                    if (textPanel != null) textPanel.SetActive(true);

                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                    typingCoroutine = StartCoroutine(TypeTextCoroutine());
                }
                else
                {
                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                    descriptionText.text = questText;

                    if (textPanel != null) textPanel.SetActive(true);
                    if (listPanel != null) listPanel.SetActive(true);

                    UpdateUI();
                }
            }
            else
            {
                // Nếu Panel bị đóng (do bấm E lại hoặc đi ra xa), dập tắt ngay hiệu ứng chạy chữ
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            }
        }
    }

    private System.Collections.IEnumerator TypeTextCoroutine()
    {
        if (descriptionText == null) yield break;

        descriptionText.text = "";

        foreach (char c in questText.ToCharArray())
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Việc lưu trạng thái đã được đẩy lên lúc mở Panel nên ở đây ta bỏ đi

        // Xong xuôi mới bật cái Danh sách xương lên
        if (listPanel != null) listPanel.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (checklistText == null || SkeletonQuestManager.Instance == null) return;

        string content = "";

        foreach (BoneInteract bone in SkeletonQuestManager.Instance.requiredBones)
        {
            if (bone == null) continue;

            if (SkeletonQuestManager.Instance.HasCollected(bone))
            {
                // Tìm thấy thì hiện 1/1 và đổi màu xanh
                content += $"<color=green>{bone.displayName}: 1/1</color>\n";
            }
            else
            {
                // Chưa tìm thấy thì hiện 0/1 và để màu trắng/mặc định
                content += $"{bone.displayName}: 0/1\n";
            }
        }

        checklistText.text = content;
    }
}