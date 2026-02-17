using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivableWaterFiller : ActivableObject
{
    [ReadOnly] public float waterFillPercentage;
    public GameObject waterObject;

    [Header("Settings")] 
    public float percentageFillForSeconds = 5f;
    public float percentageEmptyForSeconds = 10f;

    [Space] 
    public Vector3 baseScale;
    public Vector3 basePosition;
    
    [Space] 
    public Vector3 endScale;
    public Vector3 endPosition;
    
    [Space] 
    [Header("Sounds")] 
    public AudioClip waterEmptySound;
    public float waterEmptyVolume = 1f;
    
    [Header("Fill Curves")]
    public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleCurve    = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public override void Activate()
    {
        base.Activate();
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
    }

    private void Update()
    {
        if (isActivated && AlmostEquals(waterFillPercentage, 100)) return;
        if (!isActivated && AlmostEquals(waterFillPercentage, 0)) return;

        float valueToAdd = isActivated ? percentageFillForSeconds : -percentageEmptyForSeconds;

        float previousPercentage = waterFillPercentage;

        waterFillPercentage += valueToAdd * Time.deltaTime;
        waterFillPercentage = Mathf.Clamp(waterFillPercentage, 0f, 100f);

        if (previousPercentage > 0f && AlmostEquals(waterFillPercentage, 0))
        {
            OnWaterEmpty();
        }

        waterObject.SetActive(waterFillPercentage > 0.1f);

        float t = waterFillPercentage / 100f;

        float positionT = positionCurve.Evaluate(t);
        float scaleT    = scaleCurve.Evaluate(t);

        waterObject.transform.localPosition = Vector3.Lerp(basePosition, endPosition, positionT);
        waterObject.transform.localScale    = Vector3.Lerp(baseScale, endScale, scaleT);
    }

    private void OnWaterEmpty()
    {
        SoundManager.Instance.PlaySound(waterEmptySound, transform.position, waterEmptyVolume);
    }
}