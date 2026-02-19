using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WorldPlayer : Player
{
    public ThirdPersonController tpsController;
    private float _reviveTime = 7f;

    public void PlayReviveAnimation()
    {
        LockControls(true, true);
        tpsController.animator.SetTrigger("StandUp");
        this.Invoke(_reviveTime, () => LockControls(false));
    }
}