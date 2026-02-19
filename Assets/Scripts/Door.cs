using System;
using System.Collections.Generic;
using UnityEngine;
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
    public float checkDelay = 0.2f;
    public float slamAngleDetected = 20;
    public float slamDetectionDuration = 0.2f;
    public float closeAnglePermissiveness = 5f;

    [Header("Door Sounds")] 
    public float volume = 0.7f;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip slamSound;

    [Header("Audio Occlusion")] 
    public AudioOccluder mAudioOccluder;
    public AudioOccluder[] connectedWallOccluders;
    [FormerlySerializedAs("connectedDoors")] public Door[] doorsSharingSameSoundOcclusion;
    public Door twinDoor;

    [ReadOnly] public bool isOpen = false;
    [ReadOnly] public bool opensTowardNegative;
    [ReadOnly] public float closeAngle;
    [ReadOnly] public float openFullAngle;

    private float _lastAngle;
    private float _almostCloseAngle;
    private bool _askedForGhostSlam;
    private bool _ghostJustInteracted;
    private bool _isGrabbed;

    //private float _lockTimeAfterGhostInteraction = 1.5f;
    private Vector3 _basePosition;

    private void Start()
    {
        _basePosition = transform.position;
        
        if (hingeJoint == null)
        {
            Debug.LogError($"{name} : Missing HingeJoint");
            return;
        }

        closeAngle = hingeJoint.angle;

        JointLimits limits = hingeJoint.limits;
        float distToMin = Mathf.Abs(limits.min - closeAngle);
        float distToMax = Mathf.Abs(limits.max - closeAngle);

        opensTowardNegative = distToMin > distToMax;
        openFullAngle = opensTowardNegative ? limits.min : limits.max;

        _almostCloseAngle = Mathf.Abs(closeAngle) + closeAnglePermissiveness;
        
        SetCursor(cursorHand, cursorHandSize);
    }

    #region Grab / Release

    public void Grab()
    {
        _lastAngle = hingeJoint.angle;
        _isGrabbed = true;
        InvokeRepeating(nameof(CheckAngle), checkDelay, checkDelay);
        _askedForGhostSlam = false;
        
        SetCursor(cursorGrab, cursorGrabSize);
    }

    public void Release()
    {
        _isGrabbed = false;
        CancelInvoke(nameof(CheckAngle));

        if (isOpen && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
            CloseDoor(autoCloseSpeed, ignoreAudioOcclusions:true);
        else
            StopDoor();
        
        SetCursor(cursorHand, cursorHandSize);
    }

    #endregion

    #region Door Actions

    public void GhostDoorInteraction(float openPercentage, float moveSpeed, bool slam = false)
    {
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

        if (openPercentage > 0 && !isOpen)
        {
            isOpen = true;
            EnableAudioOcclusions(false);
            PlaySound(openSound, false);
        }

        ForcedHinge(targetAngle, moveSpeed);
    }

    public void CloseDoor(float closeSpeed, bool forcedSlam = false, bool ignoreAudioOcclusions = false)
    {
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

    private void HingeClose(float closeSpeed)
    {
        JointMotor motor = hingeJoint.motor;
        motor.force = 100f;

        float delta = closeAngle - hingeJoint.angle;
        motor.targetVelocity = Mathf.Sign(delta) * Mathf.Abs(closeSpeed);

        hingeJoint.motor = motor;
        hingeJoint.useMotor = true;
    }

    private void StopDoor()
    {
        hingeJoint.useMotor = false;
        rb.freezeRotation = true;
    }

    #endregion

    #region Update / Detection

    private void Update()
    {
        if (_isGrabbed)
            return;

        if (isOpen && !_ghostJustInteracted && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
            CloseDoor(autoCloseSpeed, _askedForGhostSlam, ignoreAudioOcclusions:false);
        else if (isOpen && !_ghostJustInteracted && Mathf.Abs(hingeJoint.velocity) < 2f)
            StopDoor();
    }

    private void FixedUpdate()
    {
        transform.position = _basePosition;
    }

    private void CheckAngle()
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
            PlaySound(openSound, true);
        }

        _lastAngle = currentAngle;
    }

    private bool IsSlamDetected() => IsInvoking(nameof(ResetSlam)) == false;

    private void ResetSlam() { }

    private void ResetGhostInteraction()
    {
        _ghostJustInteracted = false;
    }

    #endregion

    #region Utility

    public float GetTargetedAngle(float openPercentage)
    {
        return Mathf.Lerp(closeAngle, openFullAngle, Mathf.Clamp01(openPercentage));
    }

    private void PlaySound(AudioClip clip, bool ignoreOcclusion)
    {
        if (clip != null)
            SoundManager.Instance.PlaySound(clip, transform.position, volume: volume, ignoreAudioOcclusion:ignoreOcclusion);
    }

    public bool IsGrabbed() => _isGrabbed;

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

    public void SetCursor(Sprite sprite, float size = 1)
    {
        SpecialCursor = sprite;
        CursorSize = size;
    }
    
    public float GetOpenRatio()
    {
        float angle = Mathf.Abs(hingeJoint.angle);
        return Mathf.InverseLerp(
            Mathf.Abs(closeAngle),
            Mathf.Abs(openFullAngle),
            angle
        );
    }

    private void EnableAudioOcclusions(bool enable)
    {
        foreach (Door door in doorsSharingSameSoundOcclusion)
        {
            if (door != null && door.isOpen)
                return;
        }
        
        if (mAudioOccluder != null)
        {
            mAudioOccluder.blockSound = enable;
        }
        
        foreach (AudioOccluder occluder in connectedWallOccluders)
        {
            if(occluder != null)
                occluder.blockSound = enable;
        }
    }
}