using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivableWaterFiller : ActivableObject
{
    [ReadOnly] public float waterFillPercentage;
    public GameObject waterObject;
    public GameObject waterFillerObject;

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
    
    [Header("Fill Curve")]
    public AnimationCurve fillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public override void Activate()
    {
        base.Activate();
        waterFillerObject.SetActive(true);
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        waterFillerObject.SetActive(false);
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

        waterObject.SetActive(waterFillPercentage > 5f);

        float t = waterFillPercentage / 100f;
        float curvedT = fillCurve.Evaluate(t);

        waterObject.transform.localPosition = Vector3.Lerp(basePosition, endPosition, curvedT);
        waterObject.transform.localScale    = Vector3.Lerp(baseScale, endScale, curvedT);
    }

    private void OnWaterEmpty()
    {
        SoundManager.Instance.PlaySound(waterEmptySound, transform.position, waterEmptyVolume);
    }
}