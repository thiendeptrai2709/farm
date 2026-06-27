using System.Collections;
using UnityEngine;

public class FenceGate : MonoBehaviour, IInteractable
{
    public float openAngle = 90f;
    public float openSpeed = 4f;

    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    public string GetInteractText()
    {
        return isOpen ? "[E] Đóng cổng" : "[E] Mở cổng";
    }

    public void Interact()
    {
        if (!isMoving)
        {
            StartCoroutine(AnimateGateRoutine());
        }
    }

    private IEnumerator AnimateGateRoutine()
    {
        // Chức năng: Xoay cánh cổng mở hoặc đóng mượt mà theo thời gian
        isMoving = true;
        isOpen = !isOpen;

        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        float elapsed = 0f;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Door_Open");

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }

        transform.localRotation = targetRot;
        isMoving = false;
    }
}