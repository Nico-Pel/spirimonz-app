using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThrowableFireObject : ThrowableObject
{
    public FireableElement linkedFireableElement;
    
    public bool turnOffFireOnBigRotation = false;
    public float rotationLimitBeforeTurningOff = 60f;

    public override void OnThrow()
    {
        linkedFireableElement.EnableFire(false);
    }

    private void Update()
    {
        if (turnOffFireOnBigRotation && isGrabbed == false)
        {
            if (Mathf.Abs(transform.localEulerAngles.normalized.z) > rotationLimitBeforeTurningOff ||
                Mathf.Abs(transform.localEulerAngles.normalized.x) > rotationLimitBeforeTurningOff)
            {
                if (linkedFireableElement.IsOnFire())
                {
                    linkedFireableElement.EnableFire(false);
                }
            }
        }
    }

    public override void OnGrab()
    {
        linkedFireableElement.canBeTurnedOn = false;
    }
    
    public override void SpecialActionInHandsOnClick()
    {
        Player.Instance.UseSlashAnimation();
        linkedFireableElement.canBeTurnedOn = true;

        this.Invoke(0.5f, () =>
        {
            linkedFireableElement.canBeTurnedOn = false;
        });
    }
}