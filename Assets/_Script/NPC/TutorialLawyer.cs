using UnityEngine;
using UnityEngine.AI;

public class TutorialLawyer : MonoBehaviour
{
    public enum LawyerLocation { TownBusStop, FarmEntrance }

    [Header("Cài đặt Vị trí của Luật Sư này")]
    public LawyerLocation myLocation;

    [Header("ID Nhiệm vụ Hướng dẫn (Từ QuestData)")]
    public string townTutorialQuestID = "Tut_Town";
    public string farmTutorialQuestID = "Tut_Farm";

    private bool hasCheckedVisibility = false;

    private void Start()
    {
        // ==========================================
        // 1. ÉP BUỘC ĐỨNG IM BẰNG CODE (Khỏi lo cài sai Inspector)
        // ==========================================
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true; // Khóa NavMesh
        }

        NPCVillager villagerScript = GetComponent<NPCVillager>();
        if (villagerScript != null)
        {
            villagerScript.canWander = false; // Cấm đi dạo
        }

        // [ĐÃ SỬA]: Gọi thẳng hàm check ngay lập tức ở Frame 0. 
        // Game sẽ dọn dẹp thằng Luật sư trước khi hình ảnh kịp xuất hiện lên màn hình!
        CheckVisibility();
    }

    private void CheckVisibility()
    {
        if (QuestManager.Instance == null) return;

        bool townDone = QuestManager.Instance.completedQuests.Contains(townTutorialQuestID);
        bool farmDone = QuestManager.Instance.completedQuests.Contains(farmTutorialQuestID);

        if (myLocation == LawyerLocation.TownBusStop)
        {
            // Đã nói chuyện ở Thị trấn xong -> Tắt NPC ở bến xe
            if (townDone) gameObject.SetActive(false);
        }
        else if (myLocation == LawyerLocation.FarmEntrance)
        {
            // NPC ở Nông trại CHỈ XUẤT HIỆN khi: Đã xong Town VÀ chưa xong Farm
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
            if (!gameObject.activeSelf) return; // Nếu check xong mà bị tàng hình thì thoát Update luôn
        }
        // Quét liên tục xem Quest đã được NPCVillager âm thầm hoàn thành chưa
        if (myLocation == LawyerLocation.TownBusStop && QuestManager.Instance.completedQuests.Contains(townTutorialQuestID))
        {
            // Phải đợi người chơi bấm tắt bảng thoại đi thì NPC mới được phép biến mất
            if (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.IsOpen())
            {
                Debug.Log("Luật sư Thị trấn: Tôi đi trước về Nông trại đợi cậu nhé!");
                gameObject.SetActive(false); // Có thể chèn thêm Particle Effect khói bùm 1 cái ở đây
            }
        }
        else if (myLocation == LawyerLocation.FarmEntrance && QuestManager.Instance.completedQuests.Contains(farmTutorialQuestID))
        {
            if (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.IsOpen())
            {
                Debug.Log("Luật sư Nông trại: Xong việc, tôi về thành phố đây!");
                gameObject.SetActive(false);
            }
        }
    }
}