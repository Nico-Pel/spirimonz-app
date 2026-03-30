using UnityEngine;

public class GhostOrbsParticles : MonoBehaviour
{
    [Tooltip("Optional aim point for the tutorial ray check. If null, uses this transform.")]
    public Transform aimPoint;

    public Vector3 GetAimPosition()
    {
        return aimPoint != null ? aimPoint.position : transform.position;
    }
}
