using UnityEngine;
using System.Collections;

public class LandslideController : MonoBehaviour
{
    public static LandslideController Instance;

    [Header("Thành phần Vách Đá")]
    [Tooltip("Vách đá nguyên vẹn ban đầu (Sẽ bị tắt đi)")]
    public GameObject intactRockWall;
    [Tooltip("Cái Trigger (WeakPointTarget) để ngắm ném")]
    public GameObject weakPointTrigger;

    [Header("Hiệu ứng Sạt Lở (Trái sang Phải)")]
    [Tooltip("Bụi mù mịt lúc vỡ đá")]
    public ParticleSystem dustParticles;
    [Tooltip("Kéo các tảng đá vụn vào đây (SẮP XẾP TỪ TRÁI SANG PHẢI)")]
    public Rigidbody[] fallingRocks;
    [Tooltip("Thời gian chờ giữa mỗi tảng đá rơi")]
    public float delayBetweenRocks = 0.15f;
    [Tooltip("Lực đẩy đá văng sang phải (Trục X dương) và xuống dưới")]
    public Vector3 pushForce = new Vector3(5f, -2f, 0f);

    [Header("Kết quả sau sạt lở")]
    [Tooltip("Đống đá dính chùm lấp suối tạo đường đi (ban đầu tắt)")]
    public GameObject rockBridge;
    [Tooltip("Bức tường tàng hình cản đường lúc đầu (nếu có)")]
    public GameObject invisibleBlocker;

    private void Awake()
    {
        Instance = this;

        // Vừa vào game là phải giấu đám đá vụn và cầu đá đi
        if (rockBridge != null) rockBridge.SetActive(false);

        foreach (var rock in fallingRocks)
        {
            if (rock != null) rock.gameObject.SetActive(false);
        }
    }

    // Hàm này được gọi từ file StoneThrower khi ném trúng 3/3
    public void TriggerLandslide()
    {
        StartCoroutine(LandslideRoutine());
    }

    private IEnumerator LandslideRoutine()
    {
        // 1. Tắt vách đá cũ, tắt luôn chức năng ném (WeakPoint)
        if (intactRockWall != null) intactRockWall.SetActive(false);
        if (weakPointTrigger != null) weakPointTrigger.SetActive(false);

        // 2. Bật bụi mù mịt
        if (dustParticles != null) dustParticles.Play();

        // 3. HIỆU ỨNG DOMINO: Cho đá rơi từ Trái sang Phải
        foreach (var rock in fallingRocks)
        {
            if (rock != null)
            {
                rock.gameObject.SetActive(true);
                // Đẩy tảng đá bay lộc cộc sang phải
                rock.AddForce(pushForce, ForceMode.Impulse);

                // Random thêm lực xoay cho đá lăn lộn tự nhiên
                rock.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);
            }
            // Đợi một nhịp rồi mới rụng cục tiếp theo
            yield return new WaitForSeconds(delayBetweenRocks);
        }

        // 4. Chờ đá rụng hết và khói bụi lắng xuống (khoảng 2 giây)
        yield return new WaitForSeconds(2f);

        // 5. Xóa/Tắt mấy tảng đá vụn vật lý đi cho đỡ nặng máy
        foreach (var rock in fallingRocks)
        {
            if (rock != null) rock.gameObject.SetActive(false);
        }

        // 6. Bật cái cầu đá tĩnh lên để đi qua, tắt tường cản
        if (rockBridge != null) rockBridge.SetActive(true);
        if (invisibleBlocker != null) invisibleBlocker.SetActive(false);

        Debug.Log("<color=orange>[SẠT LỞ] Đã sạt lở từ trái sang phải, đường đi mở mở!</color>");
    }
}