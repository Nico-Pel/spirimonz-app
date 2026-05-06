using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Door : GameBehaviour, IInteractable
{
    [Header("Door Components")]
    public HingeJoint hingeJoint;
    public Rigidbody rb;
    public ActivitySource activitySource;
    public PrintSource[] printSources;
    public Sprite cursorHand;
    public Sprite cursorGrab;
    public float cursorGrabSize = 2f;
    public float cursorHandSize = 3f;

    [Header("Door Settings")]
    public float autoCloseSpeed = 10f;
    public float fullOpenSpeed = 35f;
    [Range(0f, 1f)] public float clickCloseThreshold = 0.1f;
    public float clickOpenProtectionDuration = 0.2f;
    public float clickOpenAngle = 105f;
    public float clickOpenSpeedMultiplier = 2.34375f;
    public float clickCloseSpeedMultiplier = 6.25f;
    public float clickCloseForceMultiplier = 4f;
    [Range(0.05f, 1f)] public float holdScreenTravelFraction = 0.2f;
    public float holdDeadZonePixels = 24f;
    public float holdMoveSpeed = 45f;
    public float holdSpringForce = 900f;
    public float holdSpringDamper = 120f;
    public bool invertHoldHorizontalDirection = false;
    public float checkDelay = 0.2f;
    public float slamAngleDetected = 20;
    public float slamDetectionDuration = 0.2f;
    public float closeAnglePermissiveness = 5f;
    public float spirimonzOpenAngleThreshold = 20f;
    public float spirimonzOpenSpeed = 50f;
    public float spirimonzOpenMinPercent = 0.8f;

    [Header("Door Sounds")] 
    public float volume = 0.7f;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip slamSound;
    public float spirimonzOpenVolumeOffset = -0.25f;
    public float spirimonzOpenPitchOffset = 0.05f;

    [Header("Audio Occlusion")] 
    public AudioOccluder mAudioOccluder;
    public AudioOccluder[] connectedWallOccluders;
    [FormerlySerializedAs("connectedDoors")] public Door[] doorsSharingSameSoundOcclusion;
    public Door twinDoor;

    [ReadOnly] public bool isOpen = false;
    [ReadOnly] public bool opensTowardNegative;
    [ReadOnly] public float closeAngle;
    [ReadOnly] public float openFullAngle;

    protected float _lastAngle;
    protected float _almostCloseAngle;
    protected bool _askedForGhostSlam;
    protected bool _ghostJustInteracted;
    protected bool _isGrabbed;
    private bool _audioOcclusionInitialized;
    private bool _audioOcclusionState;
    private readonly List<Collider> _spirimonzColliders = new List<Collider>();
    private Collider[] _doorColliders;
    private bool _wasConsideredOpen;
    private float _lastOpenSoundTime = -999f;
    protected float _lastSpirimonzOpenRequestTime = -999f;
    protected bool _pendingTapCloseSound;
    protected bool _pendingTapCloseIgnoreOcclusion;
    protected bool _pendingTapCloseForcedSlam;
    protected float _holdStartAngle;
    protected float _holdDragDirectionSign = 1f;
    protected bool _holdMovedDuringGrab;

    public UnityEvent<Door> onGhostInteracted;

    //private float _lockTimeAfterGhostInteraction = 1.5f;
    protected Vector3 _basePosition;
    protected Quaternion _baseRotation;

    protected virtual bool UsesHinge => true;

    protected virtual void Start()
    {
        _basePosition = transform.position;
        _baseRotation = transform.rotation;
        
        if (UsesHinge && hingeJoint == null)
        {
            Debug.LogError($"{name} : Missing HingeJoint");
            return;
        }
        
        if (UsesHinge && hingeJoint != null)
        {
            closeAngle = hingeJoint.angle;

            JointLimits limits = hingeJoint.limits;
            float distToMin = Mathf.Abs(limits.min - closeAngle);
            float distToMax = Mathf.Abs(limits.max - closeAngle);

            opensTowardNegative = distToMin > distToMax;
            openFullAngle = opensTowardNegative ? limits.min : limits.max;

            _almostCloseAngle = Mathf.Abs(closeAngle) + closeAnglePermissiveness;
        }
        
        SetCursor(cursorHand, cursorHandSize);

        RefreshAudioOcclusionState(force: true);
        _wasConsideredOpen = IsDoorConsideredOpen(this);
    }

    #region Grab / Release

    public virtual void Grab()
    {
        CancelPendingTapCloseSound();
        _lastAngle = hingeJoint.angle;
        _isGrabbed = true;
        InvokeRepeating(nameof(CheckAngle), checkDelay, checkDelay);
        _askedForGhostSlam = false;
        
        SetCursor(cursorGrab, cursorGrabSize);
    }

    public virtual void Release()
    {
        _isGrabbed = false;
        CancelInvoke(nameof(CheckAngle));

        if (_holdMovedDuringGrab && IsNearClosedForManualRelease())
        {
            isOpen = false;
            EnableAudioOcclusions(true);
            StopDoor();
            PlaySound(closeSound, ignoreOcclusion: true);
            _holdMovedDuringGrab = false;
            SetCursor(cursorHand, cursorHandSize);
            return;
        }

        if (isOpen && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
            CloseDoor(autoCloseSpeed, ignoreAudioOcclusions:true);
        else
            StopDoor();

        _holdMovedDuringGrab = false;
        
        SetCursor(cursorHand, cursorHandSize);
    }

    public virtual bool CanBeGrabbed()
    {
        return rb != null && hingeJoint != null;
    }

    public virtual void StartGrab()
    {
        Grab();
        _holdStartAngle = hingeJoint != null ? hingeJoint.angle : closeAngle;
        _holdDragDirectionSign = GetHoldDragDirectionSign();
        _holdMovedDuringGrab = false;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = false;
        }

        if (hingeJoint != null)
        {
            hingeJoint.useMotor = false;
            JointSpring spring = hingeJoint.spring;
            spring.spring = holdSpringForce;
            spring.damper = holdSpringDamper;
            spring.targetPosition = _holdStartAngle;
            hingeJoint.spring = spring;
            hingeJoint.useSpring = true;
        }
    }

    public virtual void EndGrab()
    {
        Release();
        if (hingeJoint != null)
            hingeJoint.useSpring = false;
        if (rb != null)
            rb.useGravity = true;
    }

    public virtual void ApplyGrabMovement(Vector3 targetPosition, float velocityMultiplier)
    {
        if (rb == null)
            return;

        rb.velocity = (targetPosition - rb.position) * velocityMultiplier;
    }

    public virtual void ApplyGrabHorizontalDelta(float totalScreenDeltaX, float screenWidth)
    {
        if (hingeJoint == null || screenWidth <= 0.001f)
            return;

        float effectiveDelta = ApplyHoldDeadZone(totalScreenDeltaX);
        float travelPixels = Mathf.Max(1f, screenWidth * holdScreenTravelFraction);
        float normalized = Mathf.Clamp(effectiveDelta / travelPixels, -1f, 1f);
        if (Mathf.Abs(normalized) <= 0.00001f)
            return;

        _holdMovedDuringGrab = true;

        rb.freezeRotation = false;
        CancelPendingTapCloseSound();

        float openRange = Mathf.Abs(openFullAngle - closeAngle);
        float targetAngle = _holdStartAngle + (normalized * openRange * Mathf.Sign(openFullAngle - closeAngle) * _holdDragDirectionSign);
        JointLimits limits = hingeJoint.limits;
        targetAngle = Mathf.Clamp(targetAngle, limits.min, limits.max);
        float targetRatio = GetOpenRatioFromAngle(targetAngle);

        if (targetRatio > 0.001f)
        {
            isOpen = true;
            EnableAudioOcclusions(false);
        }
        else
        {
            isOpen = false;
            EnableAudioOcclusions(true);
        }

        JointSpring spring = hingeJoint.spring;
        spring.spring = holdSpringForce;
        spring.damper = holdSpringDamper;
        spring.targetPosition = targetAngle;
        hingeJoint.spring = spring;
        hingeJoint.useMotor = false;
        hingeJoint.useSpring = true;
    }

    #endregion

    #region Door Actions

    public virtual void GhostDoorInteraction(float openPercentage, float moveSpeed, bool slam = false, bool openedBySpirimonz = false)
    {
        CancelPendingTapCloseSound();
        rb.freezeRotation = false;

        _ghostJustInteracted = true;
        _askedForGhostSlam = slam;
        Invoke(nameof(ResetGhostInteraction), 0.75f);

        float targetAngle = GetTargetedAngle(openPercentage);

        // 🔒 Clamp dans les limits
        JointLimits limits = hingeJoint.limits;
        targetAngle = Mathf.Clamp(targetAngle, limits.min, limits.max);

        float delta = targetAngle - hingeJoint.angle;

        // ❌ Empêche le fantôme de pousser dans le mauvais sens
        if (openPercentage > 0 && Mathf.Sign(delta) != Mathf.Sign(openFullAngle - closeAngle))
            return;

        if (openPercentage > 0)
        {
            if (!isOpen)
                isOpen = true;

            EnableAudioOcclusions(false);

            if (openedBySpirimonz)
            {
                _lastSpirimonzOpenRequestTime = Time.time;
            }
        }

        ForcedHinge(targetAngle, moveSpeed);
        
        if(!openedBySpirimonz)
            onGhostInteracted?.Invoke(this);
    }

    public virtual void CloseDoor(float closeSpeed, bool forcedSlam = false, bool ignoreAudioOcclusions = false)
    {
        CancelPendingTapCloseSound();
        if (rb != null)
            rb.freezeRotation = false;

        isOpen = false;
        EnableAudioOcclusions(true);
        HingeClose(closeSpeed);

        PlaySound((forcedSlam || IsSlamDetected()) ? slamSound : closeSound, ignoreAudioOcclusions);
    }

    #endregion

    #region Hinge Control

    private void ForcedHinge(float targetAngle, float moveSpeed)
    {
        float currentAngle = hingeJoint.angle;
        float delta = targetAngle - currentAngle;

        if (Mathf.Abs(delta) < 0.5f)
        {
            hingeJoint.useMotor = false;
            return;
        }

        JointMotor motor = hingeJoint.motor;
        motor.force = 100f;
        motor.targetVelocity = Mathf.Sign(delta) * Mathf.Min(moveSpeed, Mathf.Abs(delta) * 10f);
        hingeJoint.motor = motor;
        hingeJoint.useMotor = true;
    }

    private void HingeClose(float closeSpeed, float forceMultiplier = 1f)
    {
        JointMotor motor = hingeJoint.motor;
        motor.force = 100f * Mathf.Max(0.01f, forceMultiplier);

        float delta = closeAngle - hingeJoint.angle;
        motor.targetVelocity = Mathf.Sign(delta) * Mathf.Abs(closeSpeed);

        hingeJoint.motor = motor;
        hingeJoint.useMotor = true;
    }

    protected virtual void StopDoor()
    {
        hingeJoint.useMotor = false;
        rb.freezeRotation = true;
    }

    #endregion

    #region Update / Detection

    protected virtual void Update()
    {
        RefreshAudioOcclusionState();
        TrackOpenSound();

        if (_isGrabbed)
            return;

        UpdateDoor();
    }

    protected virtual void FixedUpdate()
    {
        FixedUpdateDoor();
    }

    protected virtual void UpdateDoor()
    {
        if (hingeJoint == null)
            return;

        if (_pendingTapCloseSound && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
        {
            FinalizePendingTapCloseSound();
            return;
        }

        if (isOpen && !_ghostJustInteracted && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
            CloseDoor(autoCloseSpeed, _askedForGhostSlam, ignoreAudioOcclusions:false);
        else if (isOpen && !_ghostJustInteracted && Mathf.Abs(hingeJoint.velocity) < 2f)
            StopDoor();
    }

    protected virtual void FixedUpdateDoor()
    {
        transform.position = _basePosition;
        RefreshSpirimonzCollisionIgnores();

        Vector3 currentEuler = transform.rotation.eulerAngles;
        Vector3 baseEuler = _baseRotation.eulerAngles;
        if (Mathf.Abs(currentEuler.x - baseEuler.x) > 0.01f || Mathf.Abs(currentEuler.z - baseEuler.z) > 0.01f)
        {
            transform.rotation = Quaternion.Euler(baseEuler.x, currentEuler.y, baseEuler.z);
        }
    }

    protected virtual void CheckAngle()
    {
        float currentAngle = hingeJoint.angle;

        if (Mathf.Abs(_lastAngle - currentAngle) > slamAngleDetected)
        {
            Invoke(nameof(ResetSlam), slamDetectionDuration);
        }

        if (!isOpen && Mathf.Abs(currentAngle) > _almostCloseAngle)
        {
            isOpen = true;
            EnableAudioOcclusions(false);
        }

        _lastAngle = currentAngle;
    }

    protected bool IsSlamDetected() => IsInvoking(nameof(ResetSlam));

    protected void ResetSlam() { }

    protected void ResetGhostInteraction()
    {
        _ghostJustInteracted = false;
    }

    #endregion

    #region Utility

    public float GetTargetedAngle(float openPercentage)
    {
        return Mathf.Lerp(closeAngle, openFullAngle, Mathf.Clamp01(openPercentage));
    }

    public virtual float GetAngleFromClose()
    {
        if (hingeJoint == null)
            return 0f;

        return Mathf.Abs(hingeJoint.angle - closeAngle);
    }

    public void HandleSpirimonzContact(Spirimonz spirimonz, Collider spirimonzCollider = null)
    {
        if (spirimonz != null)
        {
            RegisterSpirimonzColliders(spirimonz);
        }
        if (spirimonzCollider != null)
        {
            RegisterSpirimonzCollider(spirimonzCollider);
        }

        float angleFromClose = GetAngleFromClose();
        if (angleFromClose > spirimonzOpenAngleThreshold)
        {
            if (spirimonzCollider != null)
                SetIgnoreForSpirimonz(spirimonzCollider, true);
            return;
        }

        if (spirimonz != null && spirimonz.openDoorsOnItsWay && !IsGrabbed())
        {
            float currentRatio = GetOpenRatio();
            float targetPercentage = Random.Range(Mathf.Max(currentRatio, spirimonzOpenMinPercent), 1f);
            GhostDoorInteraction(targetPercentage, spirimonzOpenSpeed, openedBySpirimonz: true);
        }

        if (spirimonzCollider != null)
            SetIgnoreForSpirimonz(spirimonzCollider, true);
    }

    protected void PlaySound(AudioClip clip, bool ignoreOcclusion)
    {
        PlaySound(clip, ignoreOcclusion, volume, 1f);
    }

    protected void PlaySound(AudioClip clip, bool ignoreOcclusion, float volumeToUse, float pitchToUse)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(clip, transform.position, volume: volumeToUse, pitch: pitchToUse, ignoreAudioOcclusion:ignoreOcclusion);
    }

    private void PlayOpenSound(bool openedBySpirimonz, bool ignoreOcclusion)
    {
        if (openSound == null)
            return;

        if (openedBySpirimonz)
        {
            float adjustedVolume = Mathf.Max(0f, volume + spirimonzOpenVolumeOffset);
            float adjustedPitch = 1f + spirimonzOpenPitchOffset;
            PlaySound(openSound, ignoreOcclusion, adjustedVolume, adjustedPitch);
        }
        else
        {
            PlaySound(openSound, ignoreOcclusion);
        }

        _lastOpenSoundTime = Time.time;
    }

    private void TrackOpenSound()
    {
        bool consideredOpen = IsDoorConsideredOpen(this);
        if (!_wasConsideredOpen && consideredOpen)
        {
            if (Time.time - _lastOpenSoundTime > 0.05f)
            {
                bool openedBySpirimonz = Time.time - _lastSpirimonzOpenRequestTime <= 0.75f;
                bool openedByPlayer = _isGrabbed;
                bool ignoreOcclusion = openedByPlayer || openedBySpirimonz;
                PlayOpenSound(openedBySpirimonz, ignoreOcclusion);
            }
        }

        _wasConsideredOpen = consideredOpen;
    }

    public bool IsGrabbed() => _isGrabbed;

    public virtual float GetOpenVelocity()
    {
        if (hingeJoint == null)
            return 0f;

        return hingeJoint.velocity;
    }

    public PrintSource GetRandomPrintSource()
    {
        if (printSources == null || printSources.Length == 0)
            return null;

        List<PrintSource> available = new List<PrintSource>();
        foreach (var ps in printSources)
            if (!ps.IsActivated())
                available.Add(ps);

        return available.Count > 0 ? available[Random.Range(0, available.Count)] : null;
    }

    #endregion

    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }

    public bool InteractionLocked { get; set; }

    public virtual Vector3 GetGrabAnchorPosition()
    {
        if (rb != null)
            return rb.position;

        return transform.position;
    }

    public void SetCursor(Sprite sprite, float size = 1)
    {
        SpecialCursor = sprite;
        CursorSize = size;
    }

    public virtual float GetOpenRatio()
    {
        float angle = Mathf.Abs(hingeJoint.angle);
        return Mathf.InverseLerp(
            Mathf.Abs(closeAngle),
            Mathf.Abs(openFullAngle),
            angle
        );
    }

    public virtual void ToggleOpenClosed()
    {
        if (InteractionLocked)
            return;

        if (IsClosingFromTap())
        {
            OpenDoorFully();
            return;
        }

        float ratio = GetOpenRatio();
        if (ratio >= clickCloseThreshold)
            BeginTapClose();
        else
            OpenDoorFully();
    }

    public virtual void OpenDoorFully()
    {
        if (InteractionLocked)
            return;

        CancelPendingTapCloseSound();
        rb.freezeRotation = false;
        _ghostJustInteracted = true;
        CancelInvoke(nameof(ResetGhostInteraction));
        Invoke(nameof(ResetGhostInteraction), clickOpenProtectionDuration);

        float targetAngle = GetClickOpenTargetAngle();
        JointLimits limits = hingeJoint.limits;
        targetAngle = Mathf.Clamp(targetAngle, limits.min, limits.max);

        if (!isOpen)
            isOpen = true;

        EnableAudioOcclusions(false);
        ForcedHinge(targetAngle, fullOpenSpeed * clickOpenSpeedMultiplier);
    }

    protected virtual void BeginTapClose()
    {
        CancelPendingTapCloseSound();
        _askedForGhostSlam = false;

        if (rb != null)
            rb.freezeRotation = false;

        isOpen = false;
        EnableAudioOcclusions(true);
        HingeClose(autoCloseSpeed * clickCloseSpeedMultiplier, clickCloseForceMultiplier);
        _pendingTapCloseSound = true;
        _pendingTapCloseIgnoreOcclusion = true;
        _pendingTapCloseForcedSlam = false;
    }

    protected virtual bool IsClosingFromTap()
    {
        return _pendingTapCloseSound || (!_isGrabbed && !isOpen && GetOpenVelocity() < -0.01f);
    }

    protected virtual float GetClickOpenTargetAngle()
    {
        float maxOpenDelta = Mathf.Abs(openFullAngle - closeAngle);
        float targetDelta = Mathf.Min(clickOpenAngle, maxOpenDelta);
        return closeAngle + Mathf.Sign(openFullAngle - closeAngle) * targetDelta;
    }

    protected float GetOpenRatioFromAngle(float angle)
    {
        float min = Mathf.Abs(closeAngle);
        float max = Mathf.Abs(openFullAngle);
        return Mathf.InverseLerp(min, max, Mathf.Abs(angle));
    }

    protected float ApplyHoldDeadZone(float totalScreenDeltaX)
    {
        float abs = Mathf.Abs(totalScreenDeltaX);
        if (abs <= holdDeadZonePixels)
            return 0f;

        return Mathf.Sign(totalScreenDeltaX) * (abs - holdDeadZonePixels);
    }

    protected virtual float GetHoldDragDirectionSign()
    {
        Vector3 referenceWorldPos;
        Camera cam = Camera.main;
        if (cam != null)
        {
            referenceWorldPos = cam.transform.position;
        }
        else if (Player.Instance != null)
        {
            referenceWorldPos = Player.Instance.transform.position;
        }
        else
        {
            return invertHoldHorizontalDirection ? -1f : 1f;
        }

        Vector3 localReferencePos = Quaternion.Inverse(_baseRotation) * (referenceWorldPos - _basePosition);
        float sign = localReferencePos.z >= 0f ? -1f : 1f;
        return invertHoldHorizontalDirection ? -sign : sign;
    }

    protected virtual bool TryGetHoldReferencePoint(out Vector3 localPoint)
    {
        localPoint = Vector3.zero;
        EnsureDoorColliders();
        if (_doorColliders == null || _doorColliders.Length == 0)
            return false;

        bool hasBounds = false;
        Bounds localBounds = default;
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        foreach (Collider col in _doorColliders)
        {
            if (col == null)
                continue;

            Bounds worldBounds = col.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds)
            return false;

        float distanceToMinX = Mathf.Abs(hingeJoint.anchor.x - localBounds.min.x);
        float distanceToMaxX = Mathf.Abs(hingeJoint.anchor.x - localBounds.max.x);
        float freeEdgeX = distanceToMinX < distanceToMaxX ? localBounds.max.x : localBounds.min.x;
        localPoint = new Vector3(freeEdgeX, localBounds.center.y, localBounds.center.z);
        return true;
    }

    protected static Vector3 RotatePointAroundAxis(Vector3 point, Vector3 pivot, Vector3 axis, float angle)
    {
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);
        return pivot + rotation * (point - pivot);
    }

    protected void CancelPendingTapCloseSound()
    {
        _pendingTapCloseSound = false;
        _pendingTapCloseIgnoreOcclusion = false;
        _pendingTapCloseForcedSlam = false;
    }

    protected void FinalizePendingTapCloseSound()
    {
        PlaySound(_pendingTapCloseForcedSlam ? slamSound : closeSound, _pendingTapCloseIgnoreOcclusion);
        CancelPendingTapCloseSound();
        StopDoor();
    }

    protected void EnableAudioOcclusions(bool enable)
    {
        if (!enable)
        {
            ApplyAudioOcclusion(false);
            _audioOcclusionState = false;
            _audioOcclusionInitialized = true;
            return;
        }

        RefreshAudioOcclusionState(force: true);
    }

    private void RefreshAudioOcclusionState(bool force = false)
    {
        bool shouldBlock = !IsAnyDoorConsideredOpen();

        if (!force && _audioOcclusionInitialized && shouldBlock == _audioOcclusionState)
            return;

        _audioOcclusionInitialized = true;
        _audioOcclusionState = shouldBlock;
        ApplyAudioOcclusion(shouldBlock);
    }

    private bool IsAnyDoorConsideredOpen()
    {
        if (IsDoorConsideredOpen(this))
            return true;

        if (doorsSharingSameSoundOcclusion != null)
        {
            foreach (Door door in doorsSharingSameSoundOcclusion)
            {
                if (IsDoorConsideredOpen(door))
                    return true;
            }
        }

        if (twinDoor != null && twinDoor != this && IsDoorConsideredOpen(twinDoor))
            return true;

        return false;
    }

    protected virtual bool IsDoorConsideredOpen()
    {
        if (hingeJoint == null)
            return isOpen;

        float angleFromClose = Mathf.Abs(hingeJoint.angle - closeAngle);
        return angleFromClose > closeAnglePermissiveness;
    }

    protected virtual bool IsNearClosedForManualRelease()
    {
        return !IsDoorConsideredOpen();
    }

    private static bool IsDoorConsideredOpen(Door door)
    {
        if (door == null)
            return false;
        
        return door.IsDoorConsideredOpen();
    }

    private void ApplyAudioOcclusion(bool enable)
    {
        if (mAudioOccluder != null)
            mAudioOccluder.blockSound = enable;

        if (connectedWallOccluders == null)
            return;

        foreach (AudioOccluder occluder in connectedWallOccluders)
        {
            if (occluder != null)
                occluder.blockSound = enable;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHandleSpirimonzCollision(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryHandleSpirimonzCollision(collision.collider);
    }

    private void TryHandleSpirimonzCollision(Collider hitCollider)
    {
        if (hitCollider == null)
            return;

        Spirimonz spirimonz = hitCollider.GetComponentInParent<Spirimonz>();
        if (spirimonz == null)
            return;

        HandleSpirimonzContact(spirimonz, hitCollider);
    }

    private void RegisterSpirimonzCollider(Collider spirimonzCollider)
    {
        if (spirimonzCollider == null)
            return;

        if (_spirimonzColliders.Contains(spirimonzCollider))
            return;

        _spirimonzColliders.Add(spirimonzCollider);
    }

    private void RegisterSpirimonzColliders(Spirimonz spirimonz)
    {
        if (spirimonz == null)
            return;

        Collider[] colliders = spirimonz.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
            return;

        foreach (Collider col in colliders)
        {
            RegisterSpirimonzCollider(col);
        }
    }

    protected void RefreshSpirimonzCollisionIgnores()
    {
        EnsureDoorColliders();
        if (_spirimonzColliders.Count == 0 || _doorColliders == null || _doorColliders.Length == 0)
            return;

        bool shouldIgnore = GetAngleFromClose() > spirimonzOpenAngleThreshold;

        for (int i = _spirimonzColliders.Count - 1; i >= 0; i--)
        {
            Collider spirimonzCollider = _spirimonzColliders[i];
            if (spirimonzCollider == null)
            {
                _spirimonzColliders.RemoveAt(i);
                continue;
            }

            SetIgnoreForSpirimonz(spirimonzCollider, shouldIgnore);
        }
    }

    private void SetIgnoreForSpirimonz(Collider spirimonzCollider, bool ignore)
    {
        EnsureDoorColliders();
        if (spirimonzCollider == null || _doorColliders == null)
            return;

        foreach (Collider doorCollider in _doorColliders)
        {
            if (doorCollider == null)
                continue;

            Physics.IgnoreCollision(spirimonzCollider, doorCollider, ignore);
        }
    }

    private void EnsureDoorColliders()
    {
        if (_doorColliders != null && _doorColliders.Length > 0)
            return;

        _doorColliders = GetComponentsInChildren<Collider>(true);
    }
}
