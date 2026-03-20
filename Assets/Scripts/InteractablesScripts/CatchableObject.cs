using System;
using System.Collections;
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
    public Vector3 offsetPosInHands = Vector3.zero;
    public Vector3 offsetRotInHands = Vector3.zero;

    [Space] 
    
    public int priority;
    
    [Header("Base Components")]
    public Rigidbody rb;
    public ActivitySource activitySource;

    [ReadOnly] public bool isGrabbed;

    [Header("Collisions sounds")]
    public SoundParameters collisionSoundParameters;
    public float minForceToPlayCollision = 1f;

    [Header("Drop Collision Safety")]
    public float ignorePlayerCollisionDuration = 1f;

    protected bool _canCallCollisionSound = false;
    protected float _collisionSoundsMinDelay = 0.5f;
    protected float _collisionStartDelay = 1f;

    private Transform _currentHolder;
    private Coroutine _ignorePlayerCollisionRoutine;
    
    public UnityEvent onGrab;

    protected virtual void Awake()
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
        transform.localPosition = offsetPosInHands;
        transform.localRotation = Quaternion.Euler(offsetRotInHands);
        
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

        StartIgnorePlayerCollision();

        if (throwForce != Vector3.zero)
        {
            OnThrow();
        }
        else
        {
            OnDrop();
        }
    }

    private void StartIgnorePlayerCollision()
    {
        if (ignorePlayerCollisionDuration <= 0f)
            return;

        if (_ignorePlayerCollisionRoutine != null)
            StopCoroutine(_ignorePlayerCollisionRoutine);

        _ignorePlayerCollisionRoutine = StartCoroutine(IgnorePlayerCollisionRoutine(ignorePlayerCollisionDuration));
    }

    private IEnumerator IgnorePlayerCollisionRoutine(float duration)
    {
        Player player = Player.Instance;
        if (player == null)
            yield break;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);
        Collider[] objectColliders = GetComponentsInChildren<Collider>(true);

        if (playerColliders == null || playerColliders.Length == 0 || objectColliders == null || objectColliders.Length == 0)
            yield break;

        SetIgnoreCollisions(playerColliders, objectColliders, true);

        float endTime = Time.time + duration;
        while (Time.time < endTime)
            yield return null;

        while (IsOverlapping(playerColliders, objectColliders))
            yield return null;

        SetIgnoreCollisions(playerColliders, objectColliders, false);
        _ignorePlayerCollisionRoutine = null;
    }

    private void SetIgnoreCollisions(Collider[] playerColliders, Collider[] objectColliders, bool ignore)
    {
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCol = playerColliders[i];
            if (playerCol == null)
                continue;

            for (int j = 0; j < objectColliders.Length; j++)
            {
                Collider objectCol = objectColliders[j];
                if (objectCol == null)
                    continue;

                Physics.IgnoreCollision(playerCol, objectCol, ignore);
            }
        }
    }

    private bool IsOverlapping(Collider[] playerColliders, Collider[] objectColliders)
    {
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCol = playerColliders[i];
            if (playerCol == null || !playerCol.enabled || playerCol.isTrigger)
                continue;

            for (int j = 0; j < objectColliders.Length; j++)
            {
                Collider objectCol = objectColliders[j];
                if (objectCol == null || !objectCol.enabled || objectCol.isTrigger)
                    continue;

                if (Physics.ComputePenetration(
                        playerCol, playerCol.transform.position, playerCol.transform.rotation,
                        objectCol, objectCol.transform.position, objectCol.transform.rotation,
                        out _, out _))
                {
                    return true;
                }
            }
        }

        return false;
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
        onGrab?.Invoke();
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
