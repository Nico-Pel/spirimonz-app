using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzNPC : MonoBehaviour
{
    public Animator animator;
    public bool useWaitAnimation;

    private void Start()
    {
        SetStartAnimation();
    }

    private void SetStartAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool("Wait", useWaitAnimation);
    }
}