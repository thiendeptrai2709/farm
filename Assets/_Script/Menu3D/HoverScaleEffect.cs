using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Yêu cầu script này phải được gắn trên một đối tượng có component UI (như Image/Button)
[RequireComponent(typeof(RectTransform))]
public class HoverScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 normalScale = Vector3.one;       // Kích thước bình thường
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f); // Kích thước khi lia chuột
    public float scaleSpeed = 5f;                 // Tốc độ phóng to/thu nhỏ (càng lớn càng nhanh)

    private Coroutine scaleCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Sự kiện lia chuột vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopRunningCoroutine();
        scaleCoroutine = StartCoroutine(SmoothScale(hoverScale));
    }

    // Sự kiện lia chuột ra
    public void OnPointerExit(PointerEventData eventData)
    {
        StopRunningCoroutine();
        scaleCoroutine = StartCoroutine(SmoothScale(normalScale));
    }

    private void StopRunningCoroutine()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
    }

    private IEnumerator SmoothScale(Vector3 targetScale)
    {
        while (rectTransform.localScale != targetScale)
        {
            // Sử dụng MoveTowards để đảm bảo không bị quá lố
            rectTransform.localScale = Vector3.MoveTowards(
                rectTransform.localScale,
                targetScale,
                scaleSpeed * Time.unscaledDeltaTime // Time.unscaledDeltaTime để mượt cả khi pause game
            );
            yield return null; // Chờ đến frame tiếp theo
        }
    }
}