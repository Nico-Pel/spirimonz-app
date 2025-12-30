using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivitySource : MonoBehaviour
{
    [Range(0, 5)]
    public int activityValue = 0;
    
    private float _activityTimer;

    public void SetActivityValue(int newValue, float time)
    {
        if (newValue > activityValue)
        {
            activityValue = newValue;
        }

        _activityTimer += time;
    }

    private void Update()
    {
        if (_activityTimer > 0)
        {
            _activityTimer -= Time.deltaTime;
        }
        else if (_activityTimer < 0)
        {
            ResetActivitySource();
        }
    }

    private void ResetActivitySource()
    {
        _activityTimer = 0;
        activityValue = 0;
    }
}
