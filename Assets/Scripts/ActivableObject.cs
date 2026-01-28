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
    
    [Header("Sound")]
    public AudioClip loopSound;
    public float volume = 1f;
    public float range = 10f;
    private SoundManager.SoundInstance _loopSound;

    public House house { get; set; }

    public void Initialize(House h)
    {
        house = h;
        
        if(activationType == ActivationSpecialType.electronicLight || activationType == ActivationSpecialType.electronicObject)
            if (!house.electricCurrentEnabled && isActivated)
                Deactivate();
    }
    
    protected virtual void Start()
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
        
        PlayLoopSound();
        isActivated = true;
    }
    
    private void PlayLoopSound()
    {
        if (loopSound == null) return;
        
        _loopSound = SoundManager.Instance.PlaySound(
            loopSound,
            transform.position,
            volume: volume,
            loop: true,
            range: range,
            sourceParent: transform
        );
    }

    public virtual void Deactivate()
    {
        if(_loopSound != null)
            _loopSound.Stop(false);
        
        isActivated = false;
    }
}