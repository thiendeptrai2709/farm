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

    // Quản lý việc chờ UI đóng
    private bool isCurrentlyTalking = false;
    private float closeTimer = 0f;

    private void Start()
    {
        myAgent = GetComponent<NavMeshAgent>();
        myVillager = GetComponent<NPCVillager>();

        if (myLocation == LawyerLocation.TownBusStop)
        {
            if (myAgent != null)
            {
                myAgent.isStopped = true;
                myAgent.enabled = false; // Tắt hẳn để tránh bị NPCVillager can thiệp lúc Init
            }
            if (myVillager != null) myVillager.canWander = false;
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            if (myVillager != null)
            {
                // BƯỚC 1: Backup lại vị trí ghế
                hiddenSitPoint = myVillager.sitPoint;

                // BƯỚC 2 (QUAN TRỌNG): Tẩy não NPC, xóa trí nhớ về cái ghế để ép nó đứng im tại chỗ!
                myVillager.sitPoint = null;

                myVillager.canWander = false;
            }
        }

        // T đã xóa lệnh CheckVisibility() ở đây.
        // Bắt buộc phải chờ sang hàm Update() để QuestManager nạp xong file Save thì mới được check.
    }

    private bool IsQuestActive(string questID)
    {
        if (QuestManager.Instance == null) return false;

        // Lặp mảng để tìm đúng nhiệm vụ đang làm
        foreach (var q in QuestManager.Instance.activeQuests)
        {
            if (q != null && q.questID == questID) return true;
        }
        return false;
    }

    private bool IsQuestDone(string questID)
    {
        if (QuestManager.Instance == null) return false;
        return QuestManager.Instance.completedQuests.Contains(questID);
    }

    private void CheckVisibility()
    {
        if (QuestManager.Instance == null) return;

        bool isTownQuestActive = IsQuestActive(townTutorialQuestID);
        bool isTownQuestDone = IsQuestDone(townTutorialQuestID);
        bool isFarmQuestDone = IsQuestDone(farmTutorialQuestID);

        if (myLocation == LawyerLocation.TownBusStop)
        {
            if (isTownQuestActive || isTownQuestDone)
            {
                gameObject.SetActive(false);
            }
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            if (!isTownQuestActive && !isTownQuestDone)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf || QuestManager.Instance == null) return;

        // KIỂM TRA ĐIỀU KIỆN LẦN ĐẦU TIÊN (Sau khi QuestManager đã sẵn sàng)
        if (!hasCheckedVisibility)
        {
            CheckVisibility();

            if (myLocation == LawyerLocation.FarmEntrance && myVillager != null)
            {
                if (IsQuestActive(farmTutorialQuestID) || IsQuestDone(farmTutorialQuestID))
                {
                    hasWalkedToChair = true;
                    myVillager.sitPoint = hiddenSitPoint;
                }
            }

            // Đánh dấu là đã check xong, không chạy lại khối lệnh này nữa
            hasCheckedVisibility = true;
            if (!gameObject.activeSelf) return;
        }
        if (DialogueUIManager.Instance != null)
        {
            bool isUIOpenWithMe = DialogueUIManager.Instance.IsOpen() && (DialogueUIManager.Instance.currentVillager == myVillager);

            if (isUIOpenWithMe)
            {
                isCurrentlyTalking = true;
                closeTimer = 0f;
            }
            else if (isCurrentlyTalking)
            {
                closeTimer += Time.deltaTime;

                if (closeTimer >= 0.5f) // Bảng tắt hẳn nửa giây mới tính là nói chuyện xong
                {
                    isCurrentlyTalking = false;
                    OnDialogueClosed();
                }
            }
        }
    }

    private void OnDialogueClosed()
    {
        if (myLocation == LawyerLocation.TownBusStop)
        {
            // TẠI TOWN: Nhận xong nhiệm vụ đi xe bus là tàng hình luôn
            if (IsQuestActive(townTutorialQuestID) || IsQuestDone(townTutorialQuestID))
            {
                gameObject.SetActive(false);
            }
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            if (!hasWalkedToChair)
            {
                // TẠI FARM: Chỉ khi nào bạn ĐÃ NHẬN nhiệm vụ đầu tiên ở Farm thì ổng mới đi ra ghế
                if (IsQuestActive(farmTutorialQuestID) || IsQuestDone(farmTutorialQuestID))
                {
                    hasWalkedToChair = true;

                    if (myVillager != null)
                    {
                        // 1. Trả lại trí nhớ về cái ghế
                        myVillager.sitPoint = hiddenSitPoint;
                        // 2. Chốt luôn không cho đi dạo linh tinh
                        myVillager.canWander = false;
                    }

                    // 3. Ép NavMeshAgent đi ra ghế NGAY LẬP TỨC (Bỏ qua thời gian đứng nhìn của AI)
                    if (myAgent != null && myAgent.isOnNavMesh && hiddenSitPoint != null)
                    {
                        myAgent.isStopped = false;
                        myAgent.SetDestination(hiddenSitPoint.position);
                    }
                }
            }
        }
    }
}