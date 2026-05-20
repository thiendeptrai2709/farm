using UnityEngine;
using UnityEngine.AI;

public class TutorialLawyer : MonoBehaviour
{
    public enum LawyerLocation { TownBusStop, FarmEntrance }

    [Header("Cài đặt Vị trí của Luật Sư")]
    public LawyerLocation myLocation;

    [Header("ID Nhiệm vụ (Để check điều kiện)")]
    public string townTutorialQuestID = "Tut_Town";
    public string farmTutorialQuestID = "Tut_Farm";

    private bool hasCheckedVisibility = false;

    private NavMeshAgent myAgent;
    private NPCVillager myVillager;

    // --- BIẾN KỊCH BẢN ---
    private Transform hiddenSitPoint;
    private bool hasWalkedToChair = false;
    private bool isCurrentlyTalking = false;

    // [ĐÃ THÊM]: Bộ đếm giờ để chống "Chớp UI"
    private float closeTimer = 0f;

    private void Start()
    {
        myAgent = GetComponent<NavMeshAgent>();
        myVillager = GetComponent<NPCVillager>();

        if (myLocation == LawyerLocation.TownBusStop)
        {
            if (myAgent != null) myAgent.isStopped = true;
            if (myVillager != null) myVillager.canWander = false;
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            // [ĐÃ SỬA]: Chỉ cất cái ghế vào biến tạm chứ không xóa ngay tại Start,
            // phải đợi QuestManager nạp xong dữ liệu từ file Save rồi mới quyết định.
            if (myVillager != null)
            {
                hiddenSitPoint = myVillager.sitPoint;
                myVillager.canWander = false;
            }
        }

        CheckVisibility();
    }

    private void CheckVisibility()
    {
        if (QuestManager.Instance == null) return;

        bool townDone = QuestManager.Instance.completedQuests.Contains(townTutorialQuestID);
        bool farmDone = QuestManager.Instance.completedQuests.Contains(farmTutorialQuestID);

        if (myLocation == LawyerLocation.TownBusStop)
        {
            if (townDone) gameObject.SetActive(false);
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            if (!townDone || farmDone) gameObject.SetActive(false);
        }
        hasCheckedVisibility = true;
    }

    private void Update()
    {
        if (!gameObject.activeSelf || QuestManager.Instance == null) return;

        if (!hasCheckedVisibility)
        {
            CheckVisibility();

            // [ĐÃ THÊM]: Xử lý khôi phục lại cái ghế khi người chơi Load/Reload lại game
            if (myLocation == LawyerLocation.FarmEntrance && myVillager != null)
            {
                // Nếu file danh sách đã xong có chứa Quest xe bus (nghĩa là ông đã trả Quest trước khi thoát game)
                if (QuestManager.Instance.completedQuests.Contains(townTutorialQuestID))
                {
                    hasWalkedToChair = true;
                    myVillager.sitPoint = hiddenSitPoint; // Trả lại ghế luôn để NPCVillager tự kích hoạt logic ngồi
                }
                else
                {
                    myVillager.sitPoint = null; // Nếu chưa làm xong Quest xe bus thì mới thực sự giấu ghế
                }
            }

            if (!gameObject.activeSelf) return;
        }
        // =====================================
        // 2. KỊCH BẢN NÔNG TRẠI
        // =====================================
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            if (!hasWalkedToChair && DialogueUIManager.Instance != null)
            {
                bool isUIOpenWithMe = DialogueUIManager.Instance.IsOpen() && (DialogueUIManager.Instance.currentVillager == myVillager);

                if (isUIOpenWithMe)
                {
                    isCurrentlyTalking = true;
                    closeTimer = 0f; // Bảng đang mở thì liên tục reset đồng hồ về 0
                }
                else if (isCurrentlyTalking)
                {
                    // [BẢN VÁ LỖI CHỚP UI]: Bảng vừa tắt, bắt đầu đếm giờ
                    closeTimer += Time.deltaTime;

                    // Nếu bảng tắt liên tục quá 0.5 giây (Tức là ông đã bấm X hoặc nói xong hẳn)
                    if (closeTimer >= 0.5f)
                    {
                        isCurrentlyTalking = false; // Chốt là kết thúc hội thoại

                        // Kiểm tra xem đã trả nhiệm vụ xe bus chưa
                        bool isBusQuestDone = QuestManager.Instance.completedQuests.Contains(townTutorialQuestID);

                        if (isBusQuestDone)
                        {
                            hasWalkedToChair = true;

                            // 1. Trả lại cái ghế
                            if (myVillager != null)
                            {
                                myVillager.sitPoint = hiddenSitPoint;
                            }

                            // 2. Ép bước đi ngay lập tức ra ghế
                            if (myAgent != null && myAgent.isOnNavMesh && hiddenSitPoint != null)
                            {
                                myAgent.isStopped = false;
                                myAgent.SetDestination(hiddenSitPoint.position);
                            }
                        }
                    }
                }
            }

            // =====================================
            // 3. HOÀN THÀNH QUEST NÔNG TRẠI -> BIẾN MẤT
            // =====================================
            if (QuestManager.Instance.completedQuests.Contains(farmTutorialQuestID))
            {
                if (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.IsOpen())
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}