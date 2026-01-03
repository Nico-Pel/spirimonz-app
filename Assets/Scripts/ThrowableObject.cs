using UnityEngine;

[RequireComponent(typeof(ActivitySource))]
public class ThrowableObject : GameBehaviour, IInteractable
{
    public bool canBeGrabByPlayer = true;

    public Rigidbody rb;
    public ActivitySource activitySource;

    [ReadOnly] public bool isGrabbed;

    private Transform _currentHolder;

    private void Awake()
    {
        if (activitySource == null)
            activitySource = GetComponent<ActivitySource>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    // =========================
    // IInteractable
    // =========================

    public void OnInteractStart()
    {
        // NE FAIT RIEN pour l'instant
        // Le Player continue d'utiliser GrabObject()
    }

    public void OnInteractHold() { }

    public void OnInteractEnd() { }

    // =========================
    // Logique EXISTANTE (copiée du Player)
    // =========================

    public void Grab(Transform handPosition)
    {
        if (isGrabbed) return;

        isGrabbed = true;
        _currentHolder = handPosition;

        transform.SetParent(handPosition);
        rb.isKinematic = true;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop(Transform dropPosition, Vector3 throwForce)
    {
        if (!isGrabbed) return;

        transform.SetParent(null);
        transform.position = dropPosition.position;

        rb.isKinematic = false;
        rb.AddForce(throwForce, ForceMode.Impulse);

        isGrabbed = false;
        _currentHolder = null;
    }
    
    public void ApplyForce(Vector3 force, Vector3 torque = default)
    {
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
    }
}