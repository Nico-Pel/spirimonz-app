using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    public Transform alignPoint;
    public MovableObject[] linkedMovables;
    [Min(0f)] public float movableInteractionDistance = 1.5f;

    public Vector3 GetWorldPosition()
    {
        return alignPoint != null ? alignPoint.position : transform.position;
    }

    public Vector3 GetAnchorPosition()
    {
        return transform.position;
    }

    public Quaternion GetAnchorRotation()
    {
        return transform.rotation;
    }

    public Quaternion GetWorldRotation()
    {
        return alignPoint != null ? alignPoint.rotation : transform.rotation;
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = alignPoint != null ? alignPoint : transform;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(target.position, 0.2f);
        Gizmos.DrawLine(target.position, target.position + target.forward * 0.5f);
    }
}
