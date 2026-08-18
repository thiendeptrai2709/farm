using UnityEngine;

public class PlayerScanner : MonoBehaviour
{
    [Header("Cài đặt vùng nhặt đồ (Radar)")]
    public float interactRadius = 1.5f;
    [Range(0, 360)]
    public float interactAngle = 90f;
    public Vector3 checkOffset = new Vector3(0, 1f, 0);
    public LayerMask interactableLayer;
    public int outlineLayerIndex = 8;

    public IInteractable currentTarget { get; private set; }
    private GameObject currentOutlineObject;
    private System.Collections.Generic.Dictionary<GameObject, int> originalLayers = new System.Collections.Generic.Dictionary<GameObject, int>();

    private void Update()
    {
        // Chuột đang thả rông (mở túi/menu) thì tắt Radar luôn
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            ClearTarget();
            return;
        }

        Vector3 checkPosition = transform.position + checkOffset;
        FindClosestInteractable(checkPosition);
    }

    public void ClearTarget()
    {
        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();

        if (currentTarget is FarmPlot p) p.SetTargeted(false);
        else if (currentTarget is TreePit t) t.SetTargeted(false);

        DisableCurrentOutline();

        currentTarget = null;
    }

    private void FindClosestInteractable(Vector3 checkPosition)
    {
        Collider[] hitColliders = Physics.OverlapSphere(checkPosition, interactRadius, interactableLayer);
        IInteractable bestInteractable = null;
        float bestScore = Mathf.Infinity;

        foreach (var hit in hitColliders)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();

            // --- TÍCH HỢP CHẶT RỪNG TERRAIN ---
            if (interactable == null && hit.GetComponent<Terrain>() != null && TerrainTreeManager.Instance != null)
            {
                // Hỏi quản lý rừng xem có cây nào quanh điểm quét không
                int treeIndex = TerrainTreeManager.Instance.GetClosestTreeIndex(checkPosition, interactRadius + 1.0f);
                if (treeIndex != -1)
                {
                    interactable = TerrainTreeManager.Instance.GetTreeInteractable(treeIndex, checkPosition);
                }
            }

            if (interactable != null)
            {
                if (interactable is FarmingZone)
                {
                    float distanceToZone = Vector3.Distance(checkPosition, hit.ClosestPoint(checkPosition));
                    float scoreZone = distanceToZone - 10f;

                    if (scoreZone < bestScore)
                    {
                        bestScore = scoreZone;
                        bestInteractable = interactable;
                    }
                    continue;
                }
                Vector3 closestSurfacePoint;
                if (interactable is TerrainTreeInteractable virtualTree && TerrainTreeManager.Instance != null)
                {
                    TerrainData tData = TerrainTreeManager.Instance.targetTerrain.terrainData;
                    Vector3 treePos = tData.treeInstances[virtualTree.treeIndex].position;
                    closestSurfacePoint = Vector3.Scale(treePos, tData.size) + TerrainTreeManager.Instance.targetTerrain.transform.position;
                }
                // 2. Lưới bảo vệ: Tránh lỗi API với TerrainCollider hoặc lưới Mesh không lồi (non-convex)
                else if (hit is TerrainCollider || (hit is MeshCollider mc && !mc.convex))
                {
                    closestSurfacePoint = hit.ClosestPointOnBounds(checkPosition);
                }
                // 3. Vật thể 3D cơ bản (Box, Sphere...) thì dùng hàm cũ bình thường
                else
                {
                    closestSurfacePoint = hit.ClosestPoint(checkPosition);
                }

                Vector3 directionToTarget = (closestSurfacePoint - checkPosition).normalized;
                directionToTarget.y = 0;
                Vector3 forward = transform.forward;
                forward.y = 0;

                float angleToTarget = Vector3.Angle(forward, directionToTarget);

                if (angleToTarget <= interactAngle / 2f)
                {
                    float distance = Vector3.Distance(checkPosition, closestSurfacePoint);
                    float score = distance + (angleToTarget * 0.05f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestInteractable = interactable;
                    }
                }
            }
        }

        if (currentTarget != bestInteractable)
        {
            DisableCurrentOutline();

            if (currentTarget is FarmPlot oldPlot) oldPlot.SetTargeted(false);
            else if (currentTarget is TreePit oldPit) oldPit.SetTargeted(false);

            currentTarget = bestInteractable;

            if (currentTarget is FarmPlot newPlot) newPlot.SetTargeted(true);
            else if (currentTarget is TreePit newPit) newPit.SetTargeted(true);

            if (currentTarget is MonoBehaviour newMono)
            {
                EnableOutline(newMono.gameObject);
            }
        }

        if (currentTarget != null)
        {
            UpdateInteractionUI();
        }
        else
        {
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
        }
    }

    private void UpdateInteractionUI()
    {
        if (currentTarget == null) return;

        string interactText = currentTarget.GetInteractText();
        if (!string.IsNullOrEmpty(interactText))
        {
            Transform promptTarget = null;

            // Nếu là vật thể thật thì lấy Transform của nó
            if (currentTarget is MonoBehaviour mono)
            {
                promptTarget = mono.transform;
            }
            // Nếu là cây ảo, kéo điểm neo tàng hình tới gốc cây
            else if (currentTarget is TerrainTreeInteractable virtualTree && TerrainTreeManager.Instance != null)
            {
                Vector3 treePos = TerrainTreeManager.Instance.GetTreeWorldPosition(virtualTree.treeIndex);
                TerrainTreeManager.Instance.dummyTreeTarget.position = treePos + new Vector3(0, 1.5f, 0); // Nhích lên 1.5m cho vừa mắt
                promptTarget = TerrainTreeManager.Instance.dummyTreeTarget;
            }

            if (promptTarget != null && InteractionUI.Instance != null)
            {
                bool isGrowing = false;
                float progress = 0f;

                if (currentTarget is FarmPlot farmPlot && farmPlot.currentState == PlotState.Planted)
                {
                    isGrowing = true;
                    progress = farmPlot.GetGrowProgress();
                }
                else if (currentTarget is TreePit treePit && (treePit.currentState == TreePit.PitState.Planted || treePit.currentState == TreePit.PitState.Grown_Empty))
                {
                    isGrowing = true;
                    progress = treePit.GetGrowProgress();
                }

                if (string.IsNullOrEmpty(interactText) && !isGrowing)
                {
                    InteractionUI.Instance.HidePrompt();
                }
                else
                {
                    InteractionUI.Instance.ShowPrompt(promptTarget.transform, interactText, isGrowing, progress);
                }
            }
        }
        else
        {
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPos = transform.position + checkOffset;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(checkPos, interactRadius);

        Gizmos.color = Color.red;
        Vector3 leftBound = Quaternion.Euler(0, -interactAngle / 2f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, interactAngle / 2f, 0) * transform.forward;

        Gizmos.DrawRay(checkPos, leftBound * interactRadius);
        Gizmos.DrawRay(checkPos, rightBound * interactRadius);
    }
    private void EnableOutline(GameObject targetObj)
    {
        currentOutlineObject = targetObj;
        originalLayers.Clear();
        StoreAndSetLayer(targetObj.transform, outlineLayerIndex);
    }

    private void DisableCurrentOutline()
    {
        if (currentOutlineObject == null) return;

        foreach (var kvp in originalLayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.layer = kvp.Value;
            }
        }
        originalLayers.Clear();
        currentOutlineObject = null;
    }

    private void StoreAndSetLayer(Transform parent, int newLayer)
    {
        originalLayers[parent.gameObject] = parent.gameObject.layer;
        parent.gameObject.layer = newLayer;

        foreach (Transform child in parent)
        {
            StoreAndSetLayer(child, newLayer);
        }
    }
}