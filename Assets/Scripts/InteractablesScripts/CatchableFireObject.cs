using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class CatchableFireObject : CatchableObject
{
    [FormerlySerializedAs("linkedFireableElement")] public FlammableElement linkedFlammableElement;
    
    public bool turnOffFireOnBigRotation = false;
    public float rotationZMinBeforeTurningOff = 0.1f;
    public float rotationXMaxBeforeTurningOff = 0.9f;

    private float _dropTime;
    public float rotationProtectionDuration = 2f;

    private GamePlayer _player;

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        if (canBeGrabByPlayer == false)
        {
            rb.isKinematic = true;
        }
        
        //linkedFlammableElement.onChangeFireState.AddListener(SetThrowable);
    }

    private void SetThrowable(bool fireOn)
    {
        canBeThrownByPlayer = !fireOn;
    }

    [Space]
    public bool useRotationDebug;

    public override void OnThrow()
    {
        base.OnThrow();

        if (linkedFlammableElement.turnOffOnThrow && linkedFlammableElement.IsOnFire())
        {
            linkedFlammableElement.EnableFire(false);
        }
    }

    public override void OnDrop()
    {
        base.OnDrop();
        _dropTime = Time.time;
        
        this.Invoke(1.5f, () =>
        {
            if(linkedFlammableElement.IsOnFire() == false)
                linkedFlammableElement.canBeTurnedOn = true;
        });
    }

    private void Update()
    {
        bool isProtected =
            Time.time - _dropTime < rotationProtectionDuration;
        
        if (rb.isKinematic == false && turnOffFireOnBigRotation && !isGrabbed)
        {
            if (isProtected)
                return;

            float maxTiltAngle = GetMaxTiltAngleBeforeTurningOff();
            float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

            if (useRotationDebug)
                Debug.Log($"[CatchableFireObject] Tilt={tiltAngle:F2}°, Threshold={maxTiltAngle:F2}°", this);

            if (tiltAngle >= maxTiltAngle)
                TurnOffFire();
        }
    }

    private float GetMaxTiltAngleBeforeTurningOff()
    {
        float zThreshold = NormalizeRotationThreshold(rotationZMinBeforeTurningOff);
        float xThreshold = NormalizeRotationThreshold(rotationXMaxBeforeTurningOff);

        // Use the larger threshold to avoid false positives with legacy small values.
        float maxThreshold = Mathf.Max(zThreshold, xThreshold);
        return Mathf.Max(1f, maxThreshold);
    }

    private static float NormalizeRotationThreshold(float value)
    {
        float absValue = Mathf.Abs(value);
        if (absValue <= 1f)
            return absValue * 90f; // legacy normalized (0..1) -> degrees

        return absValue;
    }

    private void TurnOffFire()
    {
        if (linkedFlammableElement.IsOnFire())
            linkedFlammableElement.EnableFire(false);
    }

    public override void OnGrab()
    {
        base.OnGrab();
        _dropTime = float.PositiveInfinity; // toujours protégé en main
        linkedFlammableElement.canBeTurnedOn = false;
    }

    
    public override void SpecialActionInHandsOnClick()
    {
        _player.UseSlashAnimation();
        linkedFlammableElement.canBeTurnedOn = true;

        this.Invoke(0.5f, () =>
        {
            linkedFlammableElement.canBeTurnedOn = false;
        });
    }
}
