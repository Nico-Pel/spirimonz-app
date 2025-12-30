using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ActivitySource))]

public class ThrowableObject : MonoBehaviour
{
    public Rigidbody rb;
    public ActivitySource activitySource;

    private void Awake()
    {
        if (activitySource == null)
        {
            activitySource = GetComponent<ActivitySource>();
        }
    }
}
