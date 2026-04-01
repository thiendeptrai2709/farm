using UnityEngine;
using System.Collections;

public class StoneThrower : MonoBehaviour
{
    public static StoneThrower Instance;

    [Header("Cấu hình Đạn")]
    public GameObject stonePrefab;
    public Transform throwPoint;
    public ItemData stoneData;

    [Header("Quỹ đạo bay")]
    public float throwDuration = 0.8f;
    public float arcHeight = 2.5f;

    [Header("Animation")]
    public string idleBool = "IsThrowIdle";
    public string throwTrigger = "Throw";

    public bool IsAiming { get; private set; }

    private Transform currentTarget;
    private Animator playerAnim;
    private bool pendingThrowResult = false;

    private void Awake()
    {
        Instance = this;
        playerAnim = GetComponent<Animator>();
    }

    public void StartAiming(Transform target)
    {
        IsAiming = true;
        currentTarget = target;

        // Bật Cam & Animator
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleThrowCamera(true);
        if (playerAnim != null) playerAnim.SetBool(idleBool, true);

        // Bật UI để chơi ngay
        if (ThrowMinigameUI.Instance != null)
        {
            ThrowMinigameUI.Instance.StartMinigame((bool result) => {
                TriggerThrowAnimation(result); // Callback này chỉ gọi khi kết thúc 3 lượt
            });
        }
    }

    public void StopAiming()
    {
        IsAiming = false;

        // Trả Cam & Tắt tư thế ném
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleThrowCamera(false);
        if (playerAnim != null) playerAnim.SetBool(idleBool, false);
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.isActionLocked = false;

        // Khóa con trỏ lại như thường để chơi tiếp
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetCursorState(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // [HÀM NÀY CHỈ ĐƯỢC GỌI KHI MINIGAME KẾT THÚC CẢ 3 LƯỢT]
    public void TriggerThrowAnimation(bool isSuccess)
    {
        IsAiming = false;
        pendingThrowResult = isSuccess;

        // Lúc này mới khóa con trỏ lại, vì Minigame đã chơi xong rồi!
        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.SetCursorState(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Chạy hoạt ảnh ném đá (đợi bắt Event để đá bay đi)
        if (playerAnim != null)
        {
            playerAnim.SetBool(idleBool, false);
            playerAnim.SetTrigger(throwTrigger);
        }
    }

    // BƯỚC 2: Gọi từ Animation Event (AE_ReleaseStone) khi tay vừa vung tới
    public void ExecuteThrowAction()
    {
        // [KHÓA AN TOÀN 1]
        if (currentTarget == null || throwPoint == null || stonePrefab == null)
        {
            Debug.LogWarning("[HỆ THỐNG NÉM] Hủy ném do chưa ngắm hoặc thiếu tham chiếu!");
            return;
        }

        // [KHÓA AN TOÀN 2]
        if (InventoryManager.Instance != null && stoneData != null)
        {
            if (InventoryManager.Instance.GetPersonalItemCount(stoneData) <= 0)
            {
                Debug.LogWarning("[HỆ THỐNG NÉM] Hết đá rồi, tay không vung gió à?");
                return;
            }
            // Trừ đá
            InventoryManager.Instance.ConsumePersonalItems(stoneData, 1);
        }

        // Truyền kết quả vào đường bay
        StartCoroutine(FlyingRoutine(pendingThrowResult));
    }

    private IEnumerator FlyingRoutine(bool isSuccess)
    {
        GameObject stone = Instantiate(stonePrefab, throwPoint.position, Quaternion.identity);
        float elapsed = 0f;
        Vector3 start = throwPoint.position;
        Vector3 end = currentTarget.position;

        // Logic ném xịt
        if (!isSuccess)
        {
            end += new Vector3(Random.Range(-4f, 4f), Random.Range(2f, 5f), Random.Range(-4f, 4f));
        }

        while (elapsed < throwDuration)
        {
            if (stone == null) break;
            elapsed += Time.deltaTime;
            float t = elapsed / throwDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            stone.transform.position = pos;
            stone.transform.Rotate(Vector3.one * 500 * Time.deltaTime);

            yield return null;
        }

        if (stone != null) Destroy(stone);

        // Trả kết quả chuẩn xác
        if (isSuccess)
        {
            Debug.Log("<color=green>[HỆ THỐNG] HOÀN HẢO 3/3! Đá đập trúng đích, chuẩn bị sạt lở!</color>");

            if (LandslideController.Instance != null) LandslideController.Instance.TriggerLandslide();
        }
        else
        {
            Debug.Log("<color=red>[HỆ THỐNG] Ném xịt cmnr (Chưa đủ 3 hit)! Viên đá đập vào vách rồi rơi xuống suối.</color>");
        }

        if (PlayerCameraManager.Instance != null) PlayerCameraManager.Instance.ToggleThrowCamera(false);
    }
}