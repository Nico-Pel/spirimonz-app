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
        if (linkedFlammableElement.turnOffOnThrow && linkedFlammableElement.IsOnFire())
        {
            linkedFlammableElement.EnableFire(false);
        }
    }

    public override void OnDrop()
    {
        base.OnDrop();
        _dropTime = Time.time;
    }

    private void Update()
    {
        bool isProtected =
            Time.time - _dropTime < rotationProtectionDuration;
        
        if (rb.isKinematic == false && turnOffFireOnBigRotation && !isGrabbed)
        {
            if (isProtected)
                return;

            float rotZ = Mathf.Abs(transform.localEulerAngles.normalized.z);
            float rotX = Mathf.Abs(transform.localEulerAngles.normalized.x);

            if (rotZ < rotationZMinBeforeTurningOff ||
                rotX > rotationXMaxBeforeTurningOff)
            {
                if (linkedFlammableElement.IsOnFire())
                {
                    linkedFlammableElement.EnableFire(false);
                }
            }
        }
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