using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(ActivitySource))]
public class CatchableObject : GameBehaviour, IInteractable
{
    public bool canBeGrabByPlayer = true;
    public bool canBeThrownByGhost = true;
    public bool canBeThrownByPlayer = true;
    public bool setRotZeroOnDrop = true;

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
        
        OnGrab();
    }

    public void Drop(Vector3 dropPosition, Vector3 throwForce)
    {
        if (!isGrabbed) return;

        transform.parent = House.Instance.transform;
        transform.position = dropPosition;

        rb.isKinematic = false;
        rb.AddForce(throwForce, ForceMode.Impulse);

        isGrabbed = false;
        _currentHolder = null;

        if (throwForce != Vector3.zero)
        {
            OnThrow();
        }
        else
        {
            OnDrop();
        }
    }

    public virtual void OnDrop()
    {
        if (setRotZeroOnDrop)
        {
            transform.DORotate(Vector3.zero, 0.5f);
        }
    }

    public virtual void OnGrab()
    {
        
    }
    public virtual void OnThrow()
    {
        
    }
    
    public void ApplyForce(Vector3 force, Vector3 torque = default)
    {
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public virtual void SpecialActionInHandsOnClick()
    {
        
    }
}