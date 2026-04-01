using UnityEngine;

public class WaterCurrent : MonoBehaviour
{
    [Header("Cài đặt dòng chảy")]
    [Tooltip("Hướng nước chảy (Ví dụ: X=1 là chảy theo trục X)")]
    public Vector3 currentDirection = new Vector3(1, 0, 0);

    [Tooltip("Tốc độ trôi (Càng to càng lội không nổi)")]
    public float currentSpeed = 5f;

    private void OnTriggerStay(Collider other)
    {
        // Kiểm tra xem có phải Player chạm vào nước không
        if (other.CompareTag("Player"))
        {
            // [TƯƠNG THÍCH]: Hỗ trợ cả CharacterController (thường dùng cho 3rd Person) hoặc Rigidbody
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Ép CharacterController trôi đi
                cc.Move(currentDirection.normalized * currentSpeed * Time.deltaTime);
            }
            else
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Ép Rigidbody trôi đi
                    rb.AddForce(currentDirection.normalized * currentSpeed, ForceMode.Acceleration);
                }
                else
                {
                    // Fallback: Ép transform nếu không dùng 2 cái trên
                    other.transform.position += currentDirection.normalized * currentSpeed * Time.deltaTime;
                }
            }
        }
    }
}