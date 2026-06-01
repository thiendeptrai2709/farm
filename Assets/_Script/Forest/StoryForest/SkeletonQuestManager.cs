using UnityEngine;
using System.Collections.Generic;
using System;

public class SkeletonQuestManager : MonoBehaviour
{
    public static SkeletonQuestManager Instance { get; private set; }

    [Header("Danh sách xương cần tìm")]
    [Tooltip("Kéo thả trực tiếp các khúc xương ngoài Scene vào đây")]
    public List<BoneInteract> requiredBones = new List<BoneInteract>();

    private List<BoneInteract> collectedBones = new List<BoneInteract>();

    // Biến lưu trạng thái sống dậy của xương
    public bool isSkeletonRisen = false;
    [HideInInspector] public bool isQuestStarted = false;
    // Sự kiện bắn ra mỗi khi nhặt được xương để UI tự động cập nhật
    public event Action OnBoneCollected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
    }

    public void CollectBone(BoneInteract bone)
    {
        if (!collectedBones.Contains(bone) && requiredBones.Contains(bone))
        {
            collectedBones.Add(bone);
            OnBoneCollected?.Invoke(); // Gọi Bảng UI cập nhật
        }
    }

    public bool IsQuestComplete()
    {
        return requiredBones.Count > 0 && collectedBones.Count >= requiredBones.Count;
    }

    public bool HasCollected(BoneInteract bone)
    {
        return collectedBones.Contains(bone);
    }

    // ==========================================
    // LIÊN KẾT VỚI GAMEDATA CHUẨN JSON
    // ==========================================
    public void SaveQuestData(GameData data)
    {
        // Chống lỗi văng game nếu file save quá cũ chưa có mảng này
        if (data.collectedBoneIDs == null) data.collectedBoneIDs = new List<string>();

        data.collectedBoneIDs.Clear();
        foreach (BoneInteract bone in collectedBones)
        {
            if (bone != null) data.collectedBoneIDs.Add(bone.displayName);
        }
        data.isSkeletonRisen = this.isSkeletonRisen;
        data.isQuestStarted = this.isQuestStarted;
    }
    public void LoadQuestData(GameData data)
    {
        if (data == null) return;

        // Chống lỗi NullReference nếu nạp từ file Save cũ
        if (data.collectedBoneIDs == null) data.collectedBoneIDs = new List<string>();

        this.isSkeletonRisen = data.isSkeletonRisen;
        this.isQuestStarted = data.isQuestStarted;
        // BẮT BUỘC XÓA SẠCH RAM CŨ TRƯỚC KHI LOAD ĐỂ KHÔNG BỊ CỘNG DỒN
        collectedBones.Clear();

        foreach (BoneInteract bone in requiredBones)
        {
            if (bone != null && data.collectedBoneIDs.Contains(bone.displayName))
            {
                if (!collectedBones.Contains(bone))
                {
                    collectedBones.Add(bone);
                    bone.SyncCollectedState();
                }
            }
        }

        // Xử lý luôn việc đánh thức xương nếu data bảo là đã thức rồi
        if (this.isSkeletonRisen)
        {
            // THÊM LỆNH (FindObjectsInactive.Include): Ép Unity tìm cả những vật đang bị ForestFogTrigger giấu đi
            GraveInteract grave = FindFirstObjectByType<GraveInteract>(FindObjectsInactive.Include);
            if (grave != null && grave.skeletonRise != null)
            {
                grave.skeletonRise.ForceFinishRise();
                Collider col = grave.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}