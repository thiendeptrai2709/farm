using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class BlinkingText : MonoBehaviour
{
    [Header("Tốc độ nhấp nháy")]
    public float blinkSpeed = 3f;

    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        // Tự động tìm cái Text gắn cùng object
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (textMesh != null)
        {
            Color color = textMesh.color;
            // Dùng hàm Toán học (Sin) để làm giá trị Alpha (độ trong suốt) chạy lên chạy xuống từ 0 đến 1
            color.a = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            textMesh.color = color;
        }
    }
}