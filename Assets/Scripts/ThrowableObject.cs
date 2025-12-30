using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ActivitySource))]

public class ThrowableObject : GameBehaviour
{
    public Rigidbody rb;
    public ActivitySource activitySource;

    public bool isGrabbed;

    private void Awake()
    {
        if (activitySource == null)
        {
            activitySource = GetComponent<ActivitySource>();
        }
    }
}
