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

    [Header("Throw Torque")]
    public bool addTorqueOnThrow = true;
    public Vector2 throwTorqueRange = new Vector2(0.2f, 0.6f);

    [Header("Drop Collision Safety")]
    public float ignorePlayerCollisionDuration = 1f;

    [Header("Secondary Use")]
    public float secondaryUseCooldown = 1f;

    [Header("Physics Optimization")]
    public bool autoSleepWhenIdle = true;
    [Min(0f)] public float autoSleepStartDelay = 1f;
    [Min(0f)] public float autoSleepCheckInterval = 0.5f;
    [Min(0f)] public float autoSleepMinStableTime = 0.5f;
    [Min(0f)] public float autoSleepVelocity = 0.02f;
    [Min(0f)] public float autoSleepAngularVelocity = 0.02f;

    protected bool _canCallCollisionSound = false;
    protected float _collisionSoundsMinDelay = 0.5f;
    protected float _collisionStartDelay = 1f;

    private Transform _currentHolder;
    private Coroutine _ignorePlayerCollisionRoutine;
    private Coroutine _autoSleepRoutine;
    private float _idleStableTime;
    private float _nextSecondaryUseTime;

    public UnityEvent onGrab;
    public UnityEvent onSecondaryUse;

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

        if (autoSleepWhenIdle)
            StartAutoSleep();
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
        ResetAutoSleepTimer();
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
        rb.WakeUp();
        rb.AddForce(throwForce, ForceMode.Impulse);
        ResetAutoSleepTimer();

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
        ApplyThrowTorque();
    }
    
    public void ApplyForce(Vector3 force, Vector3 torque = default)
    {
        rb.isKinematic = false;
        rb.WakeUp();
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
        ResetAutoSleepTimer();
    }

    private void ApplyThrowTorque()
    {
        if (!addTorqueOnThrow || rb == null || rb.isKinematic)
            return;

        float min = Mathf.Min(throwTorqueRange.x, throwTorqueRange.y);
        float max = Mathf.Max(throwTorqueRange.x, throwTorqueRange.y);
        if (max <= 0f)
            return;

        float magnitude = Random.Range(min, max);
        Vector3 torque = Random.onUnitSphere * magnitude;
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

    private void StartAutoSleep()
    {
        if (rb == null)
            return;

        if (_autoSleepRoutine != null)
            StopCoroutine(_autoSleepRoutine);

        _autoSleepRoutine = StartCoroutine(AutoSleepRoutine());
    }

    private IEnumerator AutoSleepRoutine()
    {
        if (autoSleepStartDelay > 0f)
            yield return new WaitForSeconds(autoSleepStartDelay);

        WaitForSeconds wait = autoSleepCheckInterval > 0f
            ? new WaitForSeconds(autoSleepCheckInterval)
            : null;

        while (true)
        {
            if (ShouldAutoSleep())
            {
                if (IsIdleForAutoSleep())
                {
                    float delta = autoSleepCheckInterval > 0f ? autoSleepCheckInterval : Time.deltaTime;
                    _idleStableTime += delta;

                    if (_idleStableTime >= autoSleepMinStableTime)
                    {
                        rb.Sleep();
                        _idleStableTime = 0f;
                    }
                }
                else
                {
                    _idleStableTime = 0f;
                }
            }
            else
            {
                _idleStableTime = 0f;
            }

            if (wait != null)
                yield return wait;
            else
                yield return null;
        }
    }

    private bool ShouldAutoSleep()
    {
        if (!autoSleepWhenIdle)
            return false;

        if (rb == null || rb.isKinematic || isGrabbed)
            return false;

        return !rb.IsSleeping();
    }

    private bool IsIdleForAutoSleep()
    {
        float maxVel = autoSleepVelocity;
        float maxAngVel = autoSleepAngularVelocity;
        return rb.velocity.sqrMagnitude <= (maxVel * maxVel) &&
               rb.angularVelocity.sqrMagnitude <= (maxAngVel * maxAngVel);
    }

    private void ResetAutoSleepTimer()
    {
        _idleStableTime = 0f;
    }

    public virtual void OnSecondaryUse()
    {
        if (secondaryUseCooldown > 0f && Time.time < _nextSecondaryUseTime)
            return;

        if (secondaryUseCooldown > 0f)
            _nextSecondaryUseTime = Time.time + secondaryUseCooldown;

        onSecondaryUse?.Invoke();
        SpecialActionInHandsOnClick();
    }

    public void EnableCollisionSoundImmediate()
    {
        _canCallCollisionSound = true;
    }
}
