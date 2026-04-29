using UnityEngine;

public class SlidingDoor : Door
{
    public enum SlideAxis
    {
        X,
        Y,
        Z
    }

    [Header("Sliding Settings")]
    public SlideAxis slideAxis = SlideAxis.X;
    public bool useCurrentPositionAsClosed = true;
    public float closedPosition = 0f;
    public float openPosition = 1f;
    public float closePositionPermissiveness = 0.02f;

    [Header("Collision Overrides")]
    public bool ignoreWallCollisions = false;
    public string wallLayerName = "Wall";
    public bool includeInactiveWalls = true;

    private Vector3 _baseLocalPosition;
    private bool _hasSlideTarget;
    private float _slideTarget;
    private float _slideSpeed;
    private bool _defaultUseGravity;
    private float _holdStartRatio;

    protected override bool UsesHinge => false;

    protected override void Start()
    {
        base.Start();

        _baseLocalPosition = transform.localPosition;
        if (useCurrentPositionAsClosed)
            closedPosition = GetAxisValue(_baseLocalPosition);

        if (rb != null)
            _defaultUseGravity = rb.useGravity;

        _lastAngle = GetAxisValue(_baseLocalPosition);

        if (ignoreWallCollisions)
            IgnoreWallCollisions();
    }

    public override bool CanBeGrabbed()
    {
        return rb != null;
    }

    public override void StartGrab()
    {
        Grab();
        _holdStartRatio = GetOpenRatio();
        _holdMovedDuringGrab = false;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = true;
        }
    }

    public override void EndGrab()
    {
        Release();
        if (rb != null)
            rb.useGravity = _defaultUseGravity;
    }

    public override void ApplyGrabMovement(Vector3 targetPosition, float velocityMultiplier)
    {
        Vector3 localTarget = WorldToParentLocal(targetPosition);
        float axisValue = Mathf.Clamp(GetAxisValue(localTarget), GetMinAxis(), GetMaxAxis());
        Vector3 localClamped = SetAxisValue(_baseLocalPosition, axisValue);
        Vector3 worldTarget = ParentLocalToWorld(localClamped);

        if (rb != null)
        {
            rb.velocity = (worldTarget - rb.position) * velocityMultiplier;
        }
        else
        {
            transform.localPosition = localClamped;
        }
    }

    public override void ApplyGrabHorizontalDelta(float totalScreenDeltaX, float screenWidth)
    {
        if (screenWidth <= 0.001f)
            return;

        float effectiveDelta = ApplyHoldDeadZone(totalScreenDeltaX);
        float travelPixels = Mathf.Max(1f, screenWidth * holdScreenTravelFraction);
        float normalized = Mathf.Clamp(effectiveDelta / travelPixels, -1f, 1f);
        if (Mathf.Abs(normalized) <= 0.00001f)
            return;

        _holdMovedDuringGrab = true;

        CancelPendingTapCloseSound();

        float targetRatio = Mathf.Clamp01(_holdStartRatio + normalized);
        float targetAxis = Mathf.Lerp(closedPosition, openPosition, targetRatio);
        _slideTarget = targetAxis;
        _slideSpeed = Mathf.Abs(holdMoveSpeed);
        _hasSlideTarget = false;

        Vector3 newLocal = SetAxisValue(_baseLocalPosition, targetAxis);
        Vector3 newWorld = ParentLocalToWorld(newLocal);

        if (rb != null)
            rb.MovePosition(newWorld);
        else
            transform.localPosition = newLocal;

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
    }

    public override void Grab()
    {
        _lastAngle = GetAxisValue(transform.localPosition);
        _isGrabbed = true;
        InvokeRepeating(nameof(CheckAngle), checkDelay, checkDelay);
        _askedForGhostSlam = false;

        SetCursor(cursorGrab, cursorGrabSize);
    }

    public override void Release()
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

        if (isOpen && IsNearClosed())
            CloseDoor(autoCloseSpeed, ignoreAudioOcclusions: true);
        else
            StopDoor();

        _holdMovedDuringGrab = false;

        SetCursor(cursorHand, cursorHandSize);
    }

    public override void GhostDoorInteraction(float openPercentage, float moveSpeed, bool slam = false, bool openedBySpirimonz = false)
    {
        CancelPendingTapCloseSound();
        _ghostJustInteracted = true;
        _askedForGhostSlam = slam;
        Invoke(nameof(ResetGhostInteraction), 0.75f);

        float targetAxis = Mathf.Lerp(closedPosition, openPosition, Mathf.Clamp01(openPercentage));
        _slideTarget = targetAxis;
        _slideSpeed = Mathf.Abs(moveSpeed);
        _hasSlideTarget = true;

        if (openPercentage > 0f)
        {
            if (!isOpen)
                isOpen = true;

            EnableAudioOcclusions(false);

            if (openedBySpirimonz)
                _lastSpirimonzOpenRequestTime = Time.time;
        }

        if (!openedBySpirimonz)
            onGhostInteracted?.Invoke(this);
    }

    public override void CloseDoor(float closeSpeed, bool forcedSlam = false, bool ignoreAudioOcclusions = false)
    {
        CancelPendingTapCloseSound();
        isOpen = false;
        EnableAudioOcclusions(true);

        _slideTarget = closedPosition;
        _slideSpeed = Mathf.Abs(closeSpeed);
        _hasSlideTarget = true;

        PlaySound((forcedSlam || IsSlamDetected()) ? slamSound : closeSound, ignoreAudioOcclusions);
    }

    protected override void UpdateDoor()
    {
        if (_pendingTapCloseSound && IsNearClosed())
        {
            FinalizePendingTapCloseSound();
            return;
        }

        if (_isGrabbed)
            return;

        if (isOpen && !_ghostJustInteracted && IsNearClosed())
        {
            CloseDoor(autoCloseSpeed, _askedForGhostSlam, ignoreAudioOcclusions: false);
            return;
        }

        if (isOpen && !_ghostJustInteracted && Mathf.Abs(GetOpenVelocity()) < 0.02f)
            StopDoor();
    }

    protected override void FixedUpdateDoor()
    {
        float currentAxis = GetAxisValue(transform.localPosition);

        if (!_isGrabbed && _hasSlideTarget)
        {
            currentAxis = Mathf.MoveTowards(currentAxis, _slideTarget, _slideSpeed * Time.fixedDeltaTime);
            if (Mathf.Abs(currentAxis - _slideTarget) <= 0.0005f)
                _hasSlideTarget = false;
        }

        float clampedAxis = Mathf.Clamp(currentAxis, GetMinAxis(), GetMaxAxis());
        Vector3 newLocal = SetAxisValue(_baseLocalPosition, clampedAxis);
        Vector3 newWorld = ParentLocalToWorld(newLocal);

        if (rb != null)
        {
            rb.MovePosition(newWorld);
            rb.MoveRotation(_baseRotation);
        }
        else
        {
            transform.localPosition = newLocal;
            transform.rotation = _baseRotation;
        }

        RefreshSpirimonzCollisionIgnores();
    }

    protected override void CheckAngle()
    {
        float currentValue = GetAxisValue(transform.localPosition);

        if (Mathf.Abs(_lastAngle - currentValue) > slamAngleDetected)
        {
            Invoke(nameof(ResetSlam), slamDetectionDuration);
        }

        if (!isOpen && Mathf.Abs(currentValue - closedPosition) > closePositionPermissiveness)
        {
            isOpen = true;
            EnableAudioOcclusions(false);
        }

        _lastAngle = currentValue;
    }

    protected override bool IsDoorConsideredOpen()
    {
        float distance = Mathf.Abs(GetAxisValue(transform.localPosition) - closedPosition);
        return distance > closePositionPermissiveness;
    }

    public override float GetAngleFromClose()
    {
        return Mathf.Abs(GetAxisValue(transform.localPosition) - closedPosition);
    }

    public override float GetOpenRatio()
    {
        float current = GetAxisValue(transform.localPosition);
        return Mathf.InverseLerp(closedPosition, openPosition, current);
    }

    public override void OpenDoorFully()
    {
        if (InteractionLocked)
            return;

        CancelPendingTapCloseSound();
        _ghostJustInteracted = true;
        CancelInvoke(nameof(ResetGhostInteraction));
        Invoke(nameof(ResetGhostInteraction), clickOpenProtectionDuration);

        if (!isOpen)
            isOpen = true;

        EnableAudioOcclusions(false);
        _slideTarget = openPosition;
        _slideSpeed = Mathf.Abs(fullOpenSpeed * clickOpenSpeedMultiplier);
        _hasSlideTarget = true;
    }

    protected override void BeginTapClose()
    {
        CancelPendingTapCloseSound();

        isOpen = false;
        EnableAudioOcclusions(true);
        _slideTarget = closedPosition;
        _slideSpeed = Mathf.Abs(autoCloseSpeed * clickCloseSpeedMultiplier);
        _hasSlideTarget = true;
        _pendingTapCloseSound = true;
        _pendingTapCloseIgnoreOcclusion = true;
        _pendingTapCloseForcedSlam = false;
    }

    public override float GetOpenVelocity()
    {
        if (rb == null)
            return 0f;

        Vector3 localVel = WorldToParentLocalDirection(rb.velocity);
        return GetAxisValue(localVel);
    }

    protected override void StopDoor()
    {
        _hasSlideTarget = false;
        if (rb != null)
            rb.velocity = Vector3.zero;
    }

    private bool IsNearClosed()
    {
        return Mathf.Abs(GetAxisValue(transform.localPosition) - closedPosition) <= closePositionPermissiveness;
    }

    private float GetAxisValue(Vector3 local)
    {
        switch (slideAxis)
        {
            case SlideAxis.Y:
                return local.y;
            case SlideAxis.Z:
                return local.z;
            default:
                return local.x;
        }
    }

    private Vector3 SetAxisValue(Vector3 local, float value)
    {
        switch (slideAxis)
        {
            case SlideAxis.Y:
                local.y = value;
                break;
            case SlideAxis.Z:
                local.z = value;
                break;
            default:
                local.x = value;
                break;
        }

        return local;
    }

    private float GetMinAxis()
    {
        return Mathf.Min(closedPosition, openPosition);
    }

    private float GetMaxAxis()
    {
        return Mathf.Max(closedPosition, openPosition);
    }

    private Vector3 WorldToParentLocal(Vector3 worldPosition)
    {
        return transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPosition)
            : worldPosition;
    }

    private Vector3 ParentLocalToWorld(Vector3 localPosition)
    {
        return transform.parent != null
            ? transform.parent.TransformPoint(localPosition)
            : localPosition;
    }

    private Vector3 WorldToParentLocalDirection(Vector3 worldDirection)
    {
        return transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDirection)
            : worldDirection;
    }

    private void IgnoreWallCollisions()
    {
        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        if (wallLayer < 0)
        {
            Debug.LogWarning($"{name} : Layer '{wallLayerName}' not found. Wall collisions not ignored.");
            return;
        }

        Collider[] doorColliders = GetComponentsInChildren<Collider>(true);
        if (doorColliders == null || doorColliders.Length == 0)
            return;

        Collider[] allColliders = FindObjectsOfType<Collider>(includeInactiveWalls);
        if (allColliders == null || allColliders.Length == 0)
            return;

        foreach (Collider doorCol in doorColliders)
        {
            if (doorCol == null)
                continue;

            foreach (Collider other in allColliders)
            {
                if (other == null || other.gameObject.layer != wallLayer)
                    continue;

                Physics.IgnoreCollision(doorCol, other, true);
            }
        }
    }
}
