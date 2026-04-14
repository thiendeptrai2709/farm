using UnityEngine;
using UnityEngine.AI;

public class PlayerSpawnPoint : MonoBehaviour
{
    public string spawnPointID;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f); // Vẽ mũi tên chỉ hướng mặt
    }
}