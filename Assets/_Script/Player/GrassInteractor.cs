using UnityEngine;

public class GrassInteractor : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_PlayerPosition");

    void Update()
    {
        // Gắn tọa độ của Player vào biến toàn cục của Shader
        Shader.SetGlobalVector(PlayerPosId, transform.position);
    }
}