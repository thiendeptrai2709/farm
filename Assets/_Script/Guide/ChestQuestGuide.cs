using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class ChestQuestGuide : MonoBehaviour
{
    [Header("Cài đặt Nhiệm vụ")]
    public QuestData targetQuest;
    public float pathUpdateInterval = 0.1f;
    public float lineHeightOffset = 0.2f;

    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private NavMeshPath path;
    private float timer = 0f;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        path = new NavMeshPath();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (QuestManager.Instance == null || targetQuest == null || playerTransform == null) return;

        QuestStatus status = QuestManager.Instance.GetQuestStatus(targetQuest);

        if (status == QuestStatus.InProgress)
        {
            lineRenderer.enabled = true;

            timer += Time.deltaTime;
            if (timer >= pathUpdateInterval)
            {
                timer = 0f;
                DrawPathToChest();
            }

            if (lineRenderer.positionCount > 0)
            {
                lineRenderer.SetPosition(0, playerTransform.position + Vector3.up * lineHeightOffset);
            }
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void DrawPathToChest()
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