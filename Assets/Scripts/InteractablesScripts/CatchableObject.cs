using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ActivitySource))]
public class CatchableObject : GameBehaviour, IInteractable
{
    [Header("Item Settings")]
    public bool canBeGrabByPlayer = true;
    public bool canBeThrownByGhost = true;
    public bool canBeThrownByPlayer = true;
    public bool setRotZeroOnDrop = true;

    [Space] 
    
    public int priority;
    
    [Header("Base Components")]
    public Rigidbody rb;
    public ActivitySource activitySource;

    [ReadOnly] public bool isGrabbed;

    [Header("Collisions sounds")]
    public SoundParameters collisionSoundParameters;
    public float minForceToPlayCollision = 1f;

    protected bool _canCallCollisionSound = false;
    protected float _collisionSoundsMinDelay = 0.5f;
    protected float _collisionStartDelay = 1f;

    private Transform _currentHolder;

    private void Awake()
    {
        if (activitySource == null)
            activitySource = GetComponent<ActivitySource>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (!canBeGrabByPlayer)
            InteractionLocked = true;
        
        this.Invoke(_collisionStartDelay, () => _canCallCollisionSound = true);
        
        // Check toutes les 0.5 secondes
        InvokeRepeating(nameof(CheckFall), 1f, 2f);
    }

    // =========================
    // IInteractable
    // =========================

    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
        // NE FAIT RIEN pour l'instant
        // Le Player continue d'utiliser GrabObject()
    }

    public void OnInteractHold() { }

    public void OnInteractEnd() { }
    public bool InteractionLocked { get; set; }

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

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > minForceToPlayCollision)
            OnCollision(collision.transform, impactForce);
    }

    protected virtual void OnCollision(Transform other, float impactForce)
    {
        PlayCollisionSound(impactForce);
    }

    private void PlayCollisionSound(float impactForce)
    {
        if (collisionSoundParameters == null || !_canCallCollisionSound) return;
        
        _canCallCollisionSound = false;
        this.Invoke(_collisionSoundsMinDelay, () => _canCallCollisionSound = true);

        float volumeMultiplier = Mathf.Clamp01(impactForce / 10f); // normalise impactForce

        collisionSoundParameters.PlaySound(transform.position, collisionSoundParameters.volume * volumeMultiplier);
    }
    
    void CheckFall()
    {
        if (transform.position.y < -50f)
            Destroy(gameObject);
    }
}