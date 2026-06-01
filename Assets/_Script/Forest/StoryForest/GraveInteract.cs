using UnityEngine;

public class GraveInteract : MonoBehaviour, IInteractable
{
    [Header("Tham chiếu")]
    [Tooltip("Kéo cái vật thể chứa SkeletonGraveUI (nằm trên Canvas) vào đây")]
    public SkeletonGraveUI graveUI;

    [Tooltip("Kéo bộ xương có gắn script SkeletonRise vào đây")]
    public SkeletonRise skeletonRise;

    private Transform playerTransform;

    private void Start()
    {
        if (graveUI == null) graveUI = GetComponent<SkeletonGraveUI>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Update()
    {
        // TỰ ĐỘNG TẮT PANEL: Đo khoảng cách từ Ngôi Mộ 3D đến Người chơi
        if (graveUI != null && graveUI.panelUI != null && graveUI.panelUI.activeSelf)
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > graveUI.autoCloseDistance)
            {
                graveUI.TogglePanel(); // Đang mở thì gọi lệnh này nó sẽ tự tắt
            }
        }
    }

    public void Interact()
    {
        if (SkeletonQuestManager.Instance == null) return;

        if (SkeletonQuestManager.Instance.isSkeletonRisen) return;

        if (SkeletonQuestManager.Instance.IsQuestComplete())
        {
            if (graveUI != null && graveUI.panelUI != null && graveUI.panelUI.activeSelf)
            {
                graveUI.TogglePanel();
            }

            if (skeletonRise != null)
            {
                skeletonRise.TriggerRise();
                SkeletonQuestManager.Instance.isSkeletonRisen = true;

                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
        else
        {
            if (graveUI != null)
            {
                graveUI.TogglePanel();
            }
        }
    }

    public string GetInteractText()
    {
        if (SkeletonQuestManager.Instance == null) return string.Empty;
        if (SkeletonQuestManager.Instance.isSkeletonRisen) return string.Empty;

        // ẨN CHỮ E: Nếu bảng Panel đang bật, trả về chuỗi rỗng để Radar tự giấu nút đi
        if (graveUI != null && graveUI.panelUI != null && graveUI.panelUI.activeSelf)
        {
            return string.Empty;
        }

        if (SkeletonQuestManager.Instance.IsQuestComplete())
        {
            return "[E] Đánh thức Bộ Xương";
        }
        else
        {
            return "[E] Xem xét Ngôi Mộ";
        }
    }
}