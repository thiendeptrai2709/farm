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
    [Tooltip("Khoảng cách mặc định nếu không đo được mô hình")]
    public Vector3 defaultOffset = new Vector3(0, 1.5f, 0);

    [Tooltip("Giới hạn độ cao TỐI ĐA của chữ. Tránh việc chữ bay lên nóc với các công trình to như Nhà Kho!")]
    public float maxHeightLimit = 2.5f;

    private Transform currentTargetTransform;
    private Camera mainCamera;
    private Transform playerTransform; 
    private Vector3 dynamicOffset;
    private Collider[] targetColliders;
    private Vector3 lockedLocalOffset;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        promptPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        if (currentTargetTransform != null && promptPanel.activeSelf)
        {
            // Dịch ngược từ Local Space ra ngoài World Space theo góc đứng hiện tại của món đồ
            Vector3 worldLockedPos = currentTargetTransform.TransformPoint(lockedLocalOffset);

            Vector3 finalPosition = currentTargetTransform.position;
            finalPosition.x = worldLockedPos.x;
            finalPosition.z = worldLockedPos.z;

            // Gắn vào vị trí đã chốt + chiều cao đo được
            transform.position = finalPosition + dynamicOffset;

            // Xoay mặt UI hướng về Camera
            transform.rotation = mainCamera.transform.rotation;
        }
    }
    // NÂNG CẤP: Nhận thêm biến showProgress và progressValue (0 -> 1)
    // NÂNG CẤP: Nhận thêm biến showProgress và progressValue (0 -> 1)
    public void ShowPrompt(Transform target, string text, bool showProgress = false, float progressValue = 0f)
    {
        // ==========================================
        // [CHỐNG TRƯỢT BĂNG]: Chỉ đo đạc tọa độ 1 lần duy nhất khi Radar bắt được mục tiêu MỚI!
        // ==========================================
        if (currentTargetTransform != target)
        {
            currentTargetTransform = target;
            targetColliders = target.GetComponentsInChildren<Collider>();

            // 1. [ĐO CHIỀU CAO (TRỤC Y)]
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float highestY = -Mathf.Infinity;
                foreach (Renderer r in renderers)
                {
                    if (r is ParticleSystemRenderer) continue;
                    if (r.bounds.max.y > highestY) highestY = r.bounds.max.y;
                }

                if (highestY != -Mathf.Infinity)
                {
                    float pivotY = target.position.y;
                    float clampedHeight = Mathf.Min(highestY - pivotY, maxHeightLimit);
                    dynamicOffset = new Vector3(0, clampedHeight + 0.3f, 0);
                }
                else dynamicOffset = defaultOffset;
            }
            else dynamicOffset = defaultOffset;

            // 2. [ĐÓNG ĐINH CHIỀU NGANG (TRỤC X, Z)]
            Vector3 absoluteClosestPoint = target.position;

            if (targetColliders != null && targetColliders.Length > 0)
            {
                float minDistance = float.MaxValue;

                // Chức năng: Lấy vị trí của Player làm tâm đo đạc. (Nếu xui xẻo ko có Player thì xài tạm Camera)
                Vector3 referencePos = playerTransform != null ? playerTransform.position : mainCamera.transform.position;

                foreach (Collider col in targetColliders)
                {
                    // Đo điểm trên mặt công trình nằm gần MẶT NHÂN VẬT nhất
                    Vector3 closestPoint = col.ClosestPoint(referencePos);
                    float dist = Vector3.Distance(referencePos, closestPoint);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        absoluteClosestPoint = closestPoint;
                    }
                }
            }

            // Chốt hạ tọa độ
            lockedLocalOffset = target.InverseTransformPoint(absoluteClosestPoint);
            lockedLocalOffset.y = 0;
        }
        // ==========================================

        // Phần này nằm ngoài khối If, nó sẽ chạy MỖI FRAME để cập nhật nội dung chữ & thanh máu 
        // mà KHÔNG dính dáng gì đến việc tính toán lại vị trí.
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
}