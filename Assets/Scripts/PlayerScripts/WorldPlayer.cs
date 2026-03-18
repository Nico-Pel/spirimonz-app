using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WorldPlayer : Player
{
    public ThirdPersonController tpsController;
    private float _reviveTime = 7f;
    public float reviveLookAtDuration = 0.6f;
    public Transform reviveLookAtTargetOverride;

    public void PlayReviveAnimation()
    {
        LockControls(false);
        LockControls(true, true);
        tpsController.animator.SetTrigger("StandUp");
        TriggerReviveLookAt();
        this.Invoke(_reviveTime, () => LockControls(false));
    }

    private void TriggerReviveLookAt()
    {
        if (tpsController == null)
            return;

        Transform target = reviveLookAtTargetOverride;
        if (target == null)
        {
            TPSPlayerLook look = GetComponentInChildren<TPSPlayerLook>();
            if (look != null && look.neck != null)
                target = look.neck;
            else if (head != null)
                target = head;
        }

        if (target != null)
            tpsController.TweenLookAt(target, reviveLookAtDuration);
    }
}
