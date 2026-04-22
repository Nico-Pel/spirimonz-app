using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivitySource : MonoBehaviour
{
    [Range(0, 5)]
    public int activityValue = 0;
    public int nbOfActivity { get; set; }

    public event Action<ActivitySource, int, int> onActivityValueChanged;
    
    private float _activityTimer;

    public void SetActivityValue(int newValue, float time)
    {
        if (newValue != 0)
            nbOfActivity++;
        
        int previousValue = activityValue;
        if (newValue > activityValue)
        {
            activityValue = newValue;
        }

        _activityTimer += time;

        if (activityValue != previousValue)
            onActivityValueChanged?.Invoke(this, previousValue, activityValue);
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
        int previousValue = activityValue;
        _activityTimer = 0;
        activityValue = 0;

        if (previousValue != activityValue)
            onActivityValueChanged?.Invoke(this, previousValue, activityValue);
    }
    
    public float GetActivityTimer() => _activityTimer;
}
