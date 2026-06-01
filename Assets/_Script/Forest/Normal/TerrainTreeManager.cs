using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainTreeProfile
{
    [Tooltip("Số thứ tự của cây trong thẻ Paint Trees (0, 1, 2)")]
    public int prototypeIndex;
    public int maxHealth = 3;

    public int requiredAxeTier = 1;

    [Header("Phần thưởng")]
    public ItemData dropItem;
    public int dropAmount = 3;
}

public class TerrainTreeManager : MonoBehaviour
{
    public static TerrainTreeManager Instance;

    [Header("Terrain Rừng")]
    public Terrain targetTerrain;
    public List<TerrainTreeProfile> treeProfiles = new List<TerrainTreeProfile>();

    private Dictionary<int, int> treeHealthMap = new Dictionary<int, int>();
    private Dictionary<int, TerrainTreeInteractable> virtualInteractables = new Dictionary<int, TerrainTreeInteractable>();

    [HideInInspector] public Transform dummyTreeTarget;

    private TreeInstance[] cachedTrees;

    // ==========================================
    // THUẬT TOÁN LƯỚI KHÔNG GIAN (CỨU FPS)
    // ==========================================
    private Dictionary<Vector2Int, List<int>> treeGrid = new Dictionary<Vector2Int, List<int>>();
    private float gridSize = 20f; // Chia rừng thành các chunk 20x20 mét

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;

        GameObject dummy = new GameObject("TerrainTree_UIPoint");
        dummy.transform.SetParent(transform);
        dummyTreeTarget = dummy.transform;
    }

    private void Start()
    {
        if (targetTerrain == null) return;
        cachedTrees = targetTerrain.terrainData.treeInstances;
        TerrainData tData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;

        // 1. Lọc ra danh sách ID của các loại cây ĐƯỢC PHÉP CHẶT (Giải quyết lỗi số 2)
        HashSet<int> validPrototypes = new HashSet<int>();
        foreach (var profile in treeProfiles)
        {
            validPrototypes.Add(profile.prototypeIndex);
        }

        // 2. Chia cây vào các ô lưới (Giải quyết lỗi số 3)
        for (int i = 0; i < cachedTrees.Length; i++)
        {
            // Nếu cây này không nằm trong danh sách được chặt -> Bỏ qua luôn, không đưa vào bộ nhớ
            if (!validPrototypes.Contains(cachedTrees[i].prototypeIndex)) continue;

            Vector3 treeWorldPos = Vector3.Scale(cachedTrees[i].position, tData.size) + terrainPos;

            // Xếp cây vào ô lưới tương ứng
            Vector2Int gridCoord = new Vector2Int(Mathf.FloorToInt(treeWorldPos.x / gridSize), Mathf.FloorToInt(treeWorldPos.z / gridSize));

            if (!treeGrid.ContainsKey(gridCoord))
            {
                treeGrid[gridCoord] = new List<int>();
            }
            treeGrid[gridCoord].Add(i);
        }
    }

    public Vector3 GetTreeWorldPosition(int treeIndex)
    {
        if (targetTerrain == null || cachedTrees == null || treeIndex < 0 || treeIndex >= cachedTrees.Length) return Vector3.zero;
        TerrainData tData = targetTerrain.terrainData;
        return Vector3.Scale(cachedTrees[treeIndex].position, tData.size) + targetTerrain.transform.position;
    }

    public TerrainTreeInteractable GetTreeInteractable(int treeIndex, Vector3 hitPoint)
    {
        if (!virtualInteractables.ContainsKey(treeIndex))
        {
            virtualInteractables[treeIndex] = new TerrainTreeInteractable(treeIndex, hitPoint);
        }
        return virtualInteractables[treeIndex];
    }

    // TÌM CÂY BẰNG LƯỚI KHÔNG GIAN (Chỉ tính toán 10 cây thay vì 50,000 cây)
    public int GetClosestTreeIndex(Vector3 hitPoint, float searchRadius = 1.5f)
    {
        if (targetTerrain == null || cachedTrees == null) return -1;

        // Tìm xem người chơi đang đứng ở ô lưới nào
        Vector2Int centerGridCoord = new Vector2Int(Mathf.FloorToInt(hitPoint.x / gridSize), Mathf.FloorToInt(hitPoint.z / gridSize));

        int closestIndex = -1;
        float closestSqrDist = searchRadius * searchRadius;
        TerrainData tData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;

        // Quét 9 ô lưới quanh người chơi (Ô đang đứng và 8 ô kề cạnh)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCoord = new Vector2Int(centerGridCoord.x + x, centerGridCoord.y + y);

                // Nếu ô này có cây
                if (treeGrid.TryGetValue(checkCoord, out List<int> treesInCell))
                {
                    foreach (int i in treesInCell)
                    {
                        if (cachedTrees[i].heightScale <= 0) continue; // Cây đã bị chặt

                        Vector3 treeWorldPos = Vector3.Scale(cachedTrees[i].position, tData.size) + terrainPos;
                        treeWorldPos.y = hitPoint.y; // Cân bằng trục Y

                        float sqrDist = (treeWorldPos - hitPoint).sqrMagnitude;

                        if (sqrDist < closestSqrDist)
                        {
                            closestSqrDist = sqrDist;
                            closestIndex = i;
                        }
                    }
                }
            }
        }
        return closestIndex;
    }

    public void ChopTerrainTree(int treeIndex, Vector3 hitPoint)
    {
        if (treeIndex < 0 || cachedTrees == null) return;

        TreeInstance tree = cachedTrees[treeIndex];
        TerrainTreeProfile profile = treeProfiles.Find(x => x.prototypeIndex == tree.prototypeIndex);

        if (profile == null) return;

        if (!treeHealthMap.ContainsKey(treeIndex))
        {
            treeHealthMap[treeIndex] = profile.maxHealth;
        }

        treeHealthMap[treeIndex]--;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Axe_Hit");
        if (InventoryManager.Instance != null) InventoryManager.Instance.DeductEquippedToolDurability(1f);

        if (treeHealthMap[treeIndex] <= 0)
        {
            FellTree(treeIndex, profile);
        }
    }

    private void FellTree(int treeIndex, TerrainTreeProfile profile)
    {
        // 1. Cập nhật dữ liệu (Struct cần gán lại)
        TreeInstance modifiedTree = cachedTrees[treeIndex];
        modifiedTree.widthScale = 0f;
        modifiedTree.heightScale = 0f;
        cachedTrees[treeIndex] = modifiedTree;

        // 2. HÀM NÀY GIẢI QUYẾT LỖI CÂY KHÔNG ĐỔ: Cập nhật thẳng vào 1 cây duy nhất!
        targetTerrain.terrainData.SetTreeInstance(treeIndex, modifiedTree);

        Collider terrainCollider = targetTerrain.GetComponent<Collider>();
        if (terrainCollider != null)
        {
            terrainCollider.enabled = false;
            terrainCollider.enabled = true;
        }
        // 4. Ép đồ
        if (profile.dropItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(profile.dropItem, profile.dropAmount);
        }
    }
    public bool IsTreeAlive(int treeIndex)
    {
        if (cachedTrees == null || treeIndex < 0 || treeIndex >= cachedTrees.Length) return false;
        return cachedTrees[treeIndex].heightScale > 0;
    }
    public int GetRequiredAxeTier(int treeIndex)
    {
        if (cachedTrees == null || treeIndex < 0 || treeIndex >= cachedTrees.Length) return 1;
        int protoIndex = cachedTrees[treeIndex].prototypeIndex;
        TerrainTreeProfile profile = treeProfiles.Find(x => x.prototypeIndex == protoIndex);
        return profile != null ? profile.requiredAxeTier : 1;
    }
}

public class TerrainTreeInteractable : IInteractable
{
    public int treeIndex;
    public Vector3 hitPoint;

    public TerrainTreeInteractable(int index, Vector3 point)
    {
        treeIndex = index;
        hitPoint = point;
    }

    public string GetInteractText()
    {
        bool isHoldingAxe = false;
        int currentAxeTier = 0;

        if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe)
            {
                isHoldingAxe = true;
                currentAxeTier = tool.toolTier;
            }
        }

        if (isHoldingAxe)
        {
            int reqTier = TerrainTreeManager.Instance.GetRequiredAxeTier(treeIndex);

            if (currentAxeTier < reqTier)
            {
                return $"<color=#FF5555>Cần Rìu Cấp {reqTier} để chặt</color>";
            }
            return "[E] Chop Forest Tree";
        }
        return "";
    }

    public void Interact()
    {
        bool isHoldingAxe = false;
        int currentAxeTier = 0;

        if (InventoryManager.Instance != null && InventoryManager.Instance.selectedHotbarIndex != -1)
        {
            ItemData holdingItem = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.selectedHotbarIndex].item;
            if (holdingItem is ToolItemData tool && tool.toolType == ToolType.Axe)
            {
                isHoldingAxe = true;
                currentAxeTier = tool.toolTier;
            }
        }

        if (isHoldingAxe && TerrainTreeManager.Instance != null)
        {
            int reqTier = TerrainTreeManager.Instance.GetRequiredAxeTier(treeIndex);

            if (currentAxeTier >= reqTier)
            {
                TerrainTreeManager.Instance.ChopTerrainTree(treeIndex, hitPoint);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("UI_Error");
            }
        }
    }
}