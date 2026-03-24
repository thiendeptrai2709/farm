using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam != null)
        {
            // Ép cái UI luôn nhìn thẳng vào Camera
            transform.forward = mainCam.transform.forward;
        }
    }
}