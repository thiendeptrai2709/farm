using UnityEngine;
using System.Collections;

public class LandslideController : MonoBehaviour
{
    public static LandslideController Instance;

    [Header("Thành phần Vách Đá")]
    [Tooltip("Vách đá nguyên vẹn ban đầu (Nếu ông không dùng thì để trống ô này cũng được)")]
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

    // ==========================================
    // [ĐÃ THÊM]: THÀNH PHẦN KIỂM SOÁT DÒNG CHẢY
    // ==========================================
    [Header("Dòng chảy của suối")]
    [Tooltip("Kéo cái cục nước có chứa script WaterCurrent vào đây")]
    public WaterCurrent waterCurrentScript;

    private void Awake()
    {
        Instance = this;

        if (rockBridge != null) rockBridge.SetActive(false);

        // Đóng băng trọng lực của đống đá vụn
        foreach (var rock in fallingRocks)
        {
            if (rock != null)
            {
                rock.isKinematic = true;
            }
        }
    }

    public void TriggerLandslide()
    {
        StartCoroutine(LandslideRoutine());
    }

    private IEnumerator LandslideRoutine()
    {
        // 1. Tắt vách đá cũ và chỗ đứng ném
        if (intactRockWall != null) intactRockWall.SetActive(false);
        if (weakPointTrigger != null) weakPointTrigger.SetActive(false);

        // 2. Xịt bụi
        if (dustParticles != null) dustParticles.Play();

        // 3. Rã băng cho đá vụn rớt lộc cộc từ trái sang phải
        foreach (var rock in fallingRocks)
        {
            if (rock != null)
            {
                rock.isKinematic = false;
                rock.AddForce(pushForce, ForceMode.Impulse);
                rock.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);
            }
            yield return new WaitForSeconds(delayBetweenRocks);
        }

        // 4. Chờ 2 giây cho khói bụi và đá rớt cho xong
        yield return new WaitForSeconds(2f);

        // 5. Hiện đống đá tĩnh làm cầu, tắt tường cản
        if (rockBridge != null) rockBridge.SetActive(true);
        if (invisibleBlocker != null) invisibleBlocker.SetActive(false);

        // ==========================================
        // [ĐÃ THÊM]: TẮT DÒNG CHẢY SAU KHI LẤP SUỐI
        // ==========================================
        if (waterCurrentScript != null)
        {
            waterCurrentScript.gameObject.SetActive(false);
            Debug.Log("<color=cyan>[SẠT LỞ] Nước đã bị chặn, người chơi có thể lội qua an toàn!</color>");
        }

        // 6. Dọn dẹp đá vụn vật lý cho nhẹ máy (Xóa chúng đi)
        foreach (var rock in fallingRocks)
        {
            if (rock != null)
            {
                Destroy(rock.gameObject);
            }
        }

        Debug.Log("<color=orange>[SẠT LỞ] Đã sạt lở xong, cầu đá hiện ra!</color>");
    }
}