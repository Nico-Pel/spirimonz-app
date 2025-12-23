using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : ClickableObject
{
    public ActivableObject activableObject;
    public Animator animator;
    public bool isLocked;

    private int _state = 0;

    public override void OnClick()
    {
        base.OnClick();
        if (activableObject != null && !isLocked)
        {
            activableObject.Operate();
        }

        if (animator != null)
        {
            int newState = _state == 1 ? 0 : 1;
            animator.SetInteger("State", newState);
            _state = newState;
        }
    }

    public void LockObject()
    {
        isLocked = true;
        if (activableObject != null)
        {
            activableObject.Deactivate();
        }
    }
}
