using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class FarmQuestGuide : MonoBehaviour
{
    [Header("Cài đặt Nhiệm vụ")]
    public QuestData targetQuest;
    [Tooltip("Tên hành động nhặt được báo về QuestManager (VD: Pickup_Debris)")]
    public string actionToWatch = "Pickup_Debris";
    public float pathUpdateInterval = 0.1f;
    public float lineHeightOffset = 0.05f;

    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private NavMeshPath path;
    private float timer = 0f;

    // Biến lưu giữ trí nhớ
    private int initialProgress = -1;
    private bool isGuideActive = false;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        path = new NavMeshPath();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (QuestManager.Instance == null || targetQuest == null || playerTransform == null) return;

        QuestStatus status = QuestManager.Instance.GetQuestStatus(targetQuest);

        if (status == QuestStatus.InProgress)
        {
            // Lấy tiến trình nhặt rác TỔNG của người chơi từ trước đến nay
            int currentProgress = QuestManager.Instance.actionProgress.ContainsKey(actionToWatch) ? QuestManager.Instance.actionProgress[actionToWatch] : 0;

            // Ghi nhớ mốc ngay lúc bắt đầu nhiệm vụ (chỉ ghi 1 lần)
            if (initialProgress == -1)
            {
                initialProgress = currentProgress;
            }

            // Nếu số lượng rác nhặt được TĂNG LÊN ÍT NHẤT 1 so với lúc vừa nhận -> TẮT CẢ 2 LINE!
            if (currentProgress > initialProgress)
            {
                TurnOffGuide();
            }
            else
            {
                TurnOnGuide();
            }
        }
        else
        {
            // Nếu chưa nhận, hoặc đã hoàn thành nhiệm vụ
            initialProgress = -1;
            TurnOffGuide();
        }
    }

    private void TurnOnGuide()
    {
        if (isGuideActive) return;
        isGuideActive = true;

        lineRenderer.enabled = true;
        // Bật cờ ép khu vườn sáng viền lên
        if (FarmingZone.Instance != null) FarmingZone.Instance.forceShowBoundary = true;
    }

    private void TurnOffGuide()
    {
        if (!isGuideActive) return;
        isGuideActive = false;

        lineRenderer.enabled = false;
        // Tắt cờ đi trả lại sự yên bình cho khu vườn
        if (FarmingZone.Instance != null) FarmingZone.Instance.forceShowBoundary = false;
    }

    private void LateUpdate()
    {
        if (isGuideActive && playerTransform != null)
        {
            timer += Time.deltaTime;
            if (timer >= pathUpdateInterval)
            {
                timer = 0f;
                DrawPathToFarm();
            }

            if (lineRenderer.positionCount > 0)
            {
                lineRenderer.SetPosition(0, playerTransform.position + Vector3.up * lineHeightOffset);
            }
        }
    }

    private void DrawPathToFarm()
    {
        if (NavMesh.CalculatePath(playerTransform.position, transform.position, NavMesh.AllAreas, path))
        {
            lineRenderer.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++)
            {
                lineRenderer.SetPosition(i, path.corners[i] + Vector3.up * lineHeightOffset);
            }
        }
    }
}