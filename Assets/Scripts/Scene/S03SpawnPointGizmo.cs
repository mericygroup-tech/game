using UnityEngine;

public sealed class S03SpawnPointGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Draw a wire sphere at the spawn position
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        // Draw a small line indicating forward direction
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.2f);
    }
}
