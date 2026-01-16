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
    public float rotationLimitBeforeTurningOff = 60f;

    public override void OnThrow()
    {
        if (linkedFlammableElement.turnOffOnThrow)
        {
            linkedFlammableElement.EnableFire(false);
        }
    }

    private void Update()
    {
        if (turnOffFireOnBigRotation && isGrabbed == false)
        {
            if (Mathf.Abs(transform.localEulerAngles.normalized.z) > rotationLimitBeforeTurningOff ||
                Mathf.Abs(transform.localEulerAngles.normalized.x) > rotationLimitBeforeTurningOff)
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
        linkedFlammableElement.canBeTurnedOn = false;
    }
    
    public override void SpecialActionInHandsOnClick()
    {
        Player.Instance.UseSlashAnimation();
        linkedFlammableElement.canBeTurnedOn = true;

        this.Invoke(0.5f, () =>
        {
            linkedFlammableElement.canBeTurnedOn = false;
        });
    }
}