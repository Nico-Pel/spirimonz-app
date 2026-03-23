using System;
using UnityEngine;
using UnityEngine.Events;
public class ActivableObject : GameBehaviour
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
    
    public bool canChangeRoom = false;
    public Room defaultRoom;
    
    [Header("Sound")]
    public AudioClip loopSound;
    public float volume = 1f;
    public float range = 10f;
    public float pitch = 1f;
    private SoundManager.SoundInstance _loopSound;
    
    public UnityEvent OnActivated;
    public UnityEvent OnDeactivated;

    [Header("Automatic disable")]
    public bool useAutomaticDisable = false;
    public float automaticDisableTime = 0.5f;

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

        if (defaultRoom == null)
            canChangeRoom = false;
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
        
        OnActivated?.Invoke();

        if (useAutomaticDisable)
        {
            if(isActivated)
                Invoke(nameof(Deactivate), automaticDisableTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (canChangeRoom == true)
        {
            Room room = other.gameObject.GetComponent<Room>();
            if (room)
            {
                ClickableObject clickableObject = GetComponent<ClickableObject>();
                if (clickableObject)
                    defaultRoom.clickableObjects.Remove(clickableObject);

                defaultRoom.activableObjects.Remove(this);

                defaultRoom = room;
                if (clickableObject)
                    defaultRoom.clickableObjects.Add(clickableObject);
            
                defaultRoom.activableObjects.Add(this);
            }
        }
    }

    private void PlayLoopSound()
    {
        if (loopSound == null) return;
        
        CancelInvoke();
        
        _loopSound = SoundManager.Instance.PlaySound(
            loopSound,
            transform.position,
            volume: volume,
            loop: true,
            range: range,
            sourceParent: transform,
            pitch : pitch
        );
    }

    public virtual void Deactivate()
    {
        if(_loopSound != null)
            _loopSound.Stop(false);
        
        isActivated = false;
        
        OnDeactivated?.Invoke();
    }
}