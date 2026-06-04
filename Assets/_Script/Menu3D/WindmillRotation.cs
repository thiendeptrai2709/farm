using UnityEngine;

public class WindmillRotation : MonoBehaviour
{
    // Tốc độ xoay trên các trục X, Y, Z
    public Vector3 rotationSpeed = new Vector3(0f, 0f, 50f);

    private void Update()
    {
        // Xoay vật thể theo thời gian thực dựa trên tốc độ đã thiết lập
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}