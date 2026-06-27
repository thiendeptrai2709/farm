using UnityEngine;
using UnityEngine.AI;

public class NPCSocialBehavior : MonoBehaviour
{
    public float detectRadius = 4f;
    public float socialCooldown = 20f;
    public float socialDuration = 5f;
    public string greetTriggerName = "Chao";

    private NPCVillager myVillager;
    private NavMeshAgent myAgent;
    private float cooldownTimer;
    private float durationTimer;
    private NPCSocialBehavior partner;

    private float originalStoppingDistance;
    public float socialDistance = 1.5f;
    private bool isApproaching = false;
    private void Awake()
    {
        myVillager = GetComponent<NPCVillager>();
        myAgent = GetComponent<NavMeshAgent>();
        if (myAgent != null) originalStoppingDistance = myAgent.stoppingDistance;
        cooldownTimer = 5f;
    }

    private void Update()
    {
        if (myVillager == null || !myVillager.isInitialized || !myInitializedCheck()) return;

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (myVillager.isSocializingCustom)
        {
            HandleSocializing();
            return;
        }

        if (cooldownTimer <= 0 && !isNPCBusy())
        {
            FindPartner();
        }
    }

    private bool myInitializedCheck()
    {
        return myAgent != null;
    }

    private bool isNPCBusy()
    {
        bool isTalkingToPlayer = DialogueUIManager.Instance != null && DialogueUIManager.Instance.currentVillager == myVillager;
        return isTalkingToPlayer || myVillager.isCurrentlySitting || myVillager.isAtCryingLocation;
    }

    private void FindPartner()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            NPCSocialBehavior otherSocial = hit.GetComponent<NPCSocialBehavior>();
            if (otherSocial != null && !otherSocial.myVillager.isSocializingCustom && otherSocial.cooldownTimer <= 0 && !otherSocial.isNPCBusy())
            {
                StartSocializing(otherSocial);
                otherSocial.StartSocializing(this);
                break;
            }
        }
    }

    public void StartSocializing(NPCSocialBehavior other)
    {
        partner = other;
        myVillager.isSocializingCustom = true;
        isApproaching = true;
        if (myAgent.enabled && myAgent.isOnNavMesh)
        {
            myAgent.isStopped = false;
            myAgent.stoppingDistance = socialDistance;
        }
    }

    private void HandleSocializing()
    {
        
        if (partner == null || !partner.myVillager.isSocializingCustom || isNPCBusy() || partner.isNPCBusy())
        {
            if (partner != null) partner.EndSocializing(); // Kéo nó hủy cùng
            EndSocializing();
            return;
        }

        if (isApproaching)
        {
            myAgent.SetDestination(partner.transform.position);

            // [FIX LỖI MẠNH NHẤT]: Đo thẳng khoảng cách vật lý thực tế, dẹp mọe cái remainingDistance của NavMesh đi!
            float realDistance = Vector3.Distance(transform.position, partner.transform.position);

            if (realDistance <= socialDistance)
            {
                isApproaching = false;
                if (myAgent.enabled && myAgent.isOnNavMesh)
                {
                    myAgent.isStopped = true;
                    myAgent.velocity = Vector3.zero; // Phanh gấp chết cứng, chống trượt patin!
                }

                durationTimer = socialDuration;
                if (myVillager.npcAnimator != null) myVillager.npcAnimator.SetTrigger(greetTriggerName);
            }
        }
        else
        {
            durationTimer -= Time.deltaTime;

            // Xoay mặt nhìn nhau
            Vector3 lookPos = partner.transform.position;
            lookPos.y = transform.position.y;
            Quaternion targetRot = Quaternion.LookRotation(lookPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);

            // Hết giờ -> Đường ai nấy đi
            if (durationTimer <= 0)
            {
                partner.EndSocializing(); // Đảm bảo đồng bộ ngắt cả 2 thằng cùng 1 frame
                EndSocializing();
            }
        }
    }

    public void EndSocializing()
    {
        myVillager.isSocializingCustom = false;
        partner = null;
        isApproaching = false;
        cooldownTimer = socialCooldown;
        if (myAgent.enabled && myAgent.isOnNavMesh)
        {
            myAgent.stoppingDistance = originalStoppingDistance;
            myAgent.isStopped = false;
        }
    }
}