using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorEvents : GameBehaviour
{
    public Animator animator;
    
    public void AllowHandsStateChange()
    {
        animator.SetBool("CanChangeState", true);
    }
    
    public void ForbidHandsStateChange()
    {
        animator.SetBool("CanChangeState", false);
    }
}
