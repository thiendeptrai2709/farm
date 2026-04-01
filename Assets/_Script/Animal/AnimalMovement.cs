using UnityEngine;
using UnityEngine.AI;

// [ĐÃ THÊM]: Trạng thái Jumping
public enum AnimalState { Wander, FindFood, Starving, Jumping, Held, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalMovement : MonoBehaviour, IInteractable
{
    [Header("Trạng thái hiện tại")]
    public AnimalState currentState = AnimalState.Wander;

    [Header("Hệ thống Sinh Tồn")]
    public float maxHunger = 100f;
    public float currentHunger;
    public float hungerDrainRate = 1f;
    public float starveTimeLimit = 30f;
    private float starveTimer;
    private float searchCooldown = 0f; // [THÊM MỚI]: Thời gian nghỉ giữa các lần quét đồ ăn

    [Header("Cài đặt đi dạo")]
    public float wanderRadius = 10f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    private NavMeshAgent agent;
    private Animator animator;
    private Collider animalCollider;
    private Vector3 startPosition;

    private float waitTimer;
    private bool isWaiting;

    private FarmPlot targetCrop;
    private FoodTrough targetTrough;
    
    // --- BIẾN CHO VIỆC NHẢY RÀO ---
    private Vector3 jumpStartPos;
    private Vector3 jumpEndPos;
    private float jumpProgress = 0f;
    public float jumpHeight = 2.5f; // Độ cao cú nhảy qua rào
    public float jumpSpeed = 1.5f;

    [Header("Hệ thống Sản Xuất")]
    public GameObject productPrefab; // Kéo Prefab Trứng/Sữa (có gắn PickupItem) vào đây
    public float produceInterval = 60f; // Mấy giây đẻ 1 lần?
    private float produceTimer;

    private bool isDying = false;

    private Rigidbody[] boneRigidbodies;
    private Collider[] boneColliders;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        animalCollider = GetComponent<Collider>();

        startPosition = transform.position;

        agent.speed = 1.5f;
        agent.stoppingDistance = 0.5f;
        agent.angularSpeed = 800f;
        agent.acceleration = 30f;

        currentHunger = maxHunger;
        starveTimer = starveTimeLimit;
        produceTimer = produceInterval;

        SetNewDestination();
    }

    void Update()
    {
        if (currentState == AnimalState.Held || currentState == AnimalState.Dead) return;

        if (animator != null && agent.enabled)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        HandleHunger();
        HandleProduction();
        switch (currentState)
        {
            case AnimalState.Wander:
                HandleWanderState();
                break;
            case AnimalState.FindFood:
                HandleFindFoodState();
                break;
            case AnimalState.Starving:
                HandleStarvingState();
                break;
            case AnimalState.Jumping:
                HandleJumpingState();
                break;
        }
    }
    private void HandleProduction()
    {
        if (productPrefab == null) return;

        produceTimer -= Time.deltaTime;
        if (produceTimer <= 0)
        {
            produceTimer = produceInterval;

            // Tính toán vị trí đẻ (Rớt ra phía sau đuôi con gà 1 chút cho tự nhiên)
            Vector3 spawnPos = transform.position - transform.forward * 0.5f;


            boneRigidbodies = GetComponentsInChildren<Rigidbody>();
            boneColliders = GetComponentsInChildren<Collider>();

            // 2. Lúc còn sống -> Tắt vật lý của xương đi để Animator điều khiển
            ToggleRagdoll(false);
            // Nhấc lên 1 xíu để nó rớt xuống cho đẹp
            spawnPos.y += 0.5f;

            // Đẻ ra cục đồ!
            Instantiate(productPrefab, spawnPos, Quaternion.identity);
            Debug.Log("Gà vừa đẻ ra 1 cục đồ!");
        }
    }
    private void ToggleRagdoll(bool isRagdollActive)
    {
        // Nếu bật Ragdoll -> Tắt Animator và ngược lại
        if (animator != null) animator.enabled = !isRagdollActive;

        foreach (Rigidbody rb in boneRigidbodies)
        {
            // isKinematic = true nghĩa là ko bị trọng lực kéo. False là rớt tự do.
            rb.isKinematic = !isRagdollActive;
        }

        foreach (Collider col in boneColliders)
        {
            // Bật/tắt va chạm của từng khúc xương (nhưng chừa cái Collider TỔNG ra)
            if (col != animalCollider)
            {
                col.enabled = isRagdollActive;
            }
        }

        // Tắt cái va chạm TỔNG đi để nó ko bị văng map
        if (animalCollider != null) animalCollider.enabled = !isRagdollActive;
    }
    public string GetInteractText()
    {
        if (currentState == AnimalState.Dead) return "";

        // [THÊM ĐIỀU KIỆN]: Kiểm tra xem tay rảnh không. Đang cầm đồ thì ẩn luôn nút E
        if (PlayerEquipment.Instance != null && !PlayerEquipment.Instance.IsHandEmpty())
        {
            return "";
        }

        if (PlayerPickupManager.Instance != null && !PlayerPickupManager.Instance.IsHoldingAnimal())
        {
            return "[E] Bế vật nuôi";
        }
        return "";
    }
    public void Interact()
    {
        if (currentState == AnimalState.Dead) return;

        // [THÊM ĐIỀU KIỆN]: Tay đang bận cầm vũ khí/cuốc/rìu thì đá văng lệnh bấm E
        if (PlayerEquipment.Instance != null && !PlayerEquipment.Instance.IsHandEmpty())
        {
            Debug.LogWarning("Phải cất vũ khí đi mới bế được gà!");
            return;
        }

        if (PlayerPickupManager.Instance != null && currentState != AnimalState.Held)
        {
            PlayerPickupManager.Instance.PickUpAnimal(this);
        }
    }

    // Hàm xử lý lúc bị bế lên
    public void OnPickedUp(Transform holdParent)
    {
        currentState = AnimalState.Held;

        // 1. Tắt AI và Collider
        if (agent.enabled) agent.ResetPath();
        agent.enabled = false;
        if (animalCollider != null) animalCollider.enabled = false;

        // 2. Chuyển sang làm "con" của HoldPoint trên người Player
        transform.SetParent(holdParent);

        // 3. Reset vị trí và góc xoay về 0 để nằm gọn trong tay
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 4. Bật Animation "Bị bế" của con vật (Nếu có, VD: co chân lại)
    }

    // Hàm xử lý lúc bị thả xuống
    public void OnDropped(Vector3 dropPosition)
    {
        // 1. Thoát ly khỏi tay Player
        transform.SetParent(null);

        // 2. Đặt con vật xuống vị trí thả (nhớ check NavMesh để ko bị rớt xuống vực)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(dropPosition, out hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            transform.position = dropPosition; // Fallback nếu ko tìm thấy NavMesh
        }

        // 3. Bật lại Collider và AI
        if (animalCollider != null) animalCollider.enabled = true;
        agent.enabled = true;

        // 4. Cập nhật lại tâm đi dạo mới là chỗ vừa thả
        startPosition = transform.position;

        // 5. Trở về trạng thái đi dạo bình thường
        currentState = AnimalState.Wander;
        starveTimer = starveTimeLimit; // Reset time chết đói cho công bằng
        SetNewDestination();
    }
    private void HandleHunger()
    {
        // Giảm thời gian chờ quét đồ ăn
        if (searchCooldown > 0) searchCooldown -= Time.deltaTime;

        if (currentHunger > 0)
        {
            currentHunger -= hungerDrainRate * Time.deltaTime;

            // [ĐÃ SỬA LỖI GIẬT GIẬT]: Chỉ cho phép tìm đồ ăn nếu Cooldown đã hết
            if (currentHunger <= 30f && currentState == AnimalState.Wander && searchCooldown <= 0f)
            {
                currentState = AnimalState.FindFood;
                targetCrop = null;
            }
        }
        else if (currentState != AnimalState.Starving && currentState != AnimalState.Jumping)
        {
            currentState = AnimalState.Starving;
            if (agent.enabled) agent.ResetPath();
        }
    }

    private void HandleWanderState()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
            else
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    SetNewDestination();
                }
            }
        }
    }

    private void HandleFindFoodState()
    {
        if (targetCrop == null && targetTrough == null)
        {
            FindNearestFoodSource();

            if (targetCrop == null && targetTrough == null && currentState != AnimalState.Jumping)
            {
                searchCooldown = 5f;
                currentState = AnimalState.Wander;
                SetNewDestination();
                return;
            }
        }

        // ƯU TIÊN 1: ĐI ĐẾN MÁNG ĂN
        if (targetTrough != null && agent.enabled)
        {
            agent.SetDestination(targetTrough.transform.position);

            // Tới rìa máng (1.5m) là đớp ngay lập tức trong 1 frame
            if (!agent.pathPending && agent.remainingDistance <= 1.5f)
            {
                bool success = targetTrough.BeEatenByAnimal();
                if (success)
                {
                    Debug.Log("Ăn cám trong máng no quá!");
                    FinishEating();
                }
                else targetTrough = null;
            }
            return;
        }

        // ƯU TIÊN 2: ĐI ĐẾN LUỐNG CỦ CẢI (ĂN TRỘM)
        if (targetCrop != null && agent.enabled)
        {
            agent.SetDestination(targetCrop.transform.position);

            // Nới lỏng khoảng cách ăn củ cải
            if (!agent.pathPending && agent.remainingDistance <= 1.5f)
            {
                bool success = targetCrop.BeEatenByAnimal();
                if (success)
                {
                    Debug.Log("Ăn trộm củ cải ngon lành cành đào!");
                    FinishEating();
                }
                else targetCrop = null;
            }
        }
    }
    private void FinishEating()
    {
        currentHunger = maxHunger;
        starveTimer = starveTimeLimit;
        targetCrop = null;
        targetTrough = null;
        currentState = AnimalState.Wander;
        searchCooldown = 0f;
        SetNewDestination();
    }
    private void FindNearestFoodSource()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20f);

        float closestTroughDist = Mathf.Infinity;
        float closestCropDist = Mathf.Infinity;
        float closestBlockedCropDist = Mathf.Infinity;

        FoodTrough bestTrough = null;
        FarmPlot bestCrop = null;
        FarmPlot blockedCrop = null;

        foreach (var hit in hitColliders)
        {
            // --- QUÉT MÁNG ĂN TRƯỚC ---
            FoodTrough trough = hit.GetComponent<FoodTrough>();
            if (trough != null && trough.HasFood())
            {
                NavMeshPath path = new NavMeshPath();

                // [ĐIỂM CHỐT LỖI]: Lấy điểm đứng ngoài rìa máng thay vì đâm vào giữa!
                Vector3 edgePoint = hit.ClosestPoint(transform.position);

                bool canReach = agent.CalculatePath(edgePoint, path) && path.status == NavMeshPathStatus.PathComplete;
                float dist = Vector3.Distance(transform.position, edgePoint);

                if (canReach || dist <= 2.0f)
                {
                    if (dist < closestTroughDist)
                    {
                        closestTroughDist = dist;
                        bestTrough = trough;
                    }
                }
            }

            // --- QUÉT LUỐNG ĐẤT ---
            FarmPlot plot = hit.GetComponent<FarmPlot>();
            if (plot != null && (plot.currentState == PlotState.Planted || plot.currentState == PlotState.Grown))
            {
                NavMeshPath path = new NavMeshPath();
                Vector3 edgePoint = hit.ClosestPoint(transform.position);

                bool canReach = agent.CalculatePath(edgePoint, path) && path.status == NavMeshPathStatus.PathComplete;
                float dist = Vector3.Distance(transform.position, edgePoint);

                if (canReach || dist <= 2.0f)
                {
                    if (dist < closestCropDist) { closestCropDist = dist; bestCrop = plot; }
                }
                else
                {
                    if (dist < closestBlockedCropDist) { closestBlockedCropDist = dist; blockedCrop = plot; }
                }
            }
        }

        if (bestTrough != null)
        {
            targetTrough = bestTrough;
            targetCrop = null;
        }
        else if (bestCrop != null)
        {
            targetCrop = bestCrop;
            targetTrough = null;
        }
        else if (blockedCrop != null && currentHunger <= 10f)
        {
            targetCrop = blockedCrop;
            targetTrough = null;
            StartDesperateJump(blockedCrop.transform.position);
        }
    }

    // ===============================================
    // CÁC HÀM XỬ LÝ NHẢY QUA HÀNG RÀO
    // ===============================================
    private void StartDesperateJump(Vector3 targetPos)
    {
        Debug.LogWarning("Con Gà đói rã họng, nó quyết định VƯỢT RÀO!!!");
        currentState = AnimalState.Jumping;

        // Tắt AI đi để có thể lướt xuyên không gian
        agent.enabled = false;

        jumpStartPos = transform.position;

        // [ĐÃ SỬA]: Kéo giãn khoảng cách đáp đất ra xa hơn!
        Vector3 dir = (transform.position - targetPos).normalized;

        // Đẩy điểm rơi ra xa 1.5 mét (thay vì 0.5 mét) để nó đứng CẠNH mép đất, ko đâm đầu vào luống
        Vector3 proposedPos = targetPos + dir * 1.5f;

        // Chốt tọa độ Y bằng với mặt đất hiện tại của con gà để đáp đất cho chuẩn
        jumpEndPos = new Vector3(proposedPos.x, transform.position.y, proposedPos.z);

        jumpProgress = 0f;
    }

    private void HandleJumpingState()
    {
        jumpProgress += Time.deltaTime * jumpSpeed;

        if (jumpProgress >= 1f)
        {
            // Đáp đất thành công
            jumpProgress = 1f;
            transform.position = jumpEndPos;

            // Bật lại AI tìm đường
            agent.enabled = true;

            // Trả về trạng thái tìm đồ ăn (lúc này nó đã ở ngay cạnh luống đất rồi)
            currentState = AnimalState.FindFood;
            searchCooldown = 0f;
            return;
        }

        // Toán học lượn sóng Parabol để tạo cảm giác "Nhảy"
        Vector3 currentPos = Vector3.Lerp(jumpStartPos, jumpEndPos, jumpProgress);
        currentPos.y += Mathf.Sin(jumpProgress * Mathf.PI) * jumpHeight;

        transform.position = currentPos;

        // Ép con vật xoay mặt về phía điểm rơi
        Vector3 lookPos = new Vector3(jumpEndPos.x, transform.position.y, jumpEndPos.z);
        transform.LookAt(lookPos);
    }

    private void HandleStarvingState()
    {
        // Nếu đang trong quá trình hấp hối rồi thì KHÔNG GỌI LẠI NỮA
        if (isDying) return;

        starveTimer -= Time.deltaTime;
        if (starveTimer <= 0)
        {
            StartCoroutine(DieRoutine());
        }
    }

    private System.Collections.IEnumerator DieRoutine()
    {
        isDying = true;
        currentState = AnimalState.Dead;

        // 1. Tắt AI đi bộ
        if (agent.enabled) agent.enabled = false;
        if (animator != null) animator.enabled = false;

        if (animalCollider != null)
        {
            animalCollider.enabled = true;   // Bắt buộc phải bật để hứng mặt đất
            animalCollider.isTrigger = false; // Biến thành vật rắn đập vào đất
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>(); // Chưa có thì code tự gắn luôn

        rb.isKinematic = false; // Nhả trọng lực
        rb.useGravity = true;

        // Đẩy 1 lực ngẫu nhiên cho nó văng nhẹ/ngã lăn quay ra
        Vector3 randomTorque = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f));
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // 3. Chờ 10 giây
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }

    void SetNewDestination()
    {
        if (!agent.enabled) return;

        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += startPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            isWaiting = false;
        }
    }
}