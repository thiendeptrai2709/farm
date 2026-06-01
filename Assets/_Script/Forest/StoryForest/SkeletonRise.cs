using UnityEngine;

public class SkeletonRise : MonoBehaviour
{
    [Header("Cài đặt trồi lên")]
    public float riseSpeed = 1f;
    public float riseHeight = 2f;

    private Vector3 targetPos;
    private bool isRising = false;

    // Biến khóa an toàn
    private bool isInitialized = false;

    // Chốt tọa độ đích ngay cả khi hàm Start chưa chạy
    private void InitIfNeeded()
    {
        if (!isInitialized)
        {
            targetPos = transform.position + Vector3.up * riseHeight;
            isInitialized = true;
        }
    }

    private void Start()
    {
        InitIfNeeded();
    }

    private void Update()
    {
        if (isRising)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, riseSpeed * Time.deltaTime);
            if (transform.position == targetPos)
            {
                isRising = false;

                // Mở khóa cho phép nói chuyện khi đã lên tới nơi
                SkeletonInteract interact = GetComponent<SkeletonInteract>();
                if (interact != null) interact.isReadyToTalk = true;
            }
        }
    }

    public void TriggerRise()
    {
        InitIfNeeded(); // Đảm bảo đã có tọa độ
        isRising = true;
    }

    // Gọi lên ngay lập tức (bỏ qua diễn hoạt animation) khi vừa load game
    public void ForceFinishRise()
    {
        InitIfNeeded(); // Đảm bảo đã có tọa độ trước khi dịch chuyển

        transform.position = targetPos;
        SkeletonInteract interact = GetComponent<SkeletonInteract>();
        if (interact != null) interact.isReadyToTalk = true;
    }

    // VẼ GIZMO ĐỂ DỄ CHỈNH VỊ TRÍ TRONG SCENE
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 endPos = Application.isPlaying && isInitialized ? targetPos : transform.position + Vector3.up * riseHeight;

        Gizmos.DrawLine(transform.position, endPos);
        Gizmos.DrawWireCube(endPos + Vector3.up * 0.75f, new Vector3(0.5f, 1.5f, 0.5f));

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPos, 0.1f);
    }
}