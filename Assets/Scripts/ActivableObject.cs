using System;
using UnityEngine;

public class ActivableObject : MonoBehaviour
{
    public enum ActivationSpecialType
    {
        none,
        electronicLight,
        electronicObject,
        fire,
        water
    }
    
    public ActivationSpecialType activationType;
    
    public bool isActivated;
    public bool isLocked;

    public House house { get; set; }

    public void Initialize(House h)
    {
        house = h;
        
        if(activationType == ActivationSpecialType.electronicLight || activationType == ActivationSpecialType.electronicObject)
            if (!house.electricCurrentEnabled && isActivated)
                Deactivate();
    }
    
    private void Start()
    {
        if (isActivated)
        {
            Activate();
        }
    }

    public void Operate()
    {
        if (!isActivated)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    public virtual void Activate()
    {
        if (isLocked) return;
        
        //This electronic object can't be used if electric current is not enabled
        if(activationType == ActivationSpecialType.electronicLight || activationType == ActivationSpecialType.electronicObject)
            if (house != null && !house.electricCurrentEnabled)
                return;
        
        isActivated = true;
    }

    public virtual void Deactivate()
    {
        isActivated = false;
    }
}