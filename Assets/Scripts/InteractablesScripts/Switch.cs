using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Switch : ClickableObject
{
    public ActivableObject activableObject;
    public Animator animator;
    public bool isLocked;
    
    [Header("Sounds")] 
    public AudioClip TurnOnSound;
    public AudioClip TurnOffSound;
    public float volume = 0.5f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    
    private SoundManager.SoundInstance _soundInstance;

    [FormerlySerializedAs("isOnStateSwitch")] [Header("Special settings")] 
    public bool isOneStateSwitch;
    
    private int _state = 0;
    private bool _forceOffUntilRelease;
    private bool _isHolding;
    private bool _holdStartedWithActive;
    
    public override void OnClick()
    {
        base.OnClick();
        
        int newState = _state == 1 ? 0 : 1;
        SwitchState(newState);
    }
    
    public override void OnHold()
    {
        base.OnHold();
        if (canClick)
            return;

        if (_isHolding == false)
        {
            _isHolding = true;
            _holdStartedWithActive = activableObject != null && activableObject.isActivated;
            _forceOffUntilRelease = _holdStartedWithActive;

            if (_forceOffUntilRelease)
            {
                if (_state != 0)
                    SwitchState(0);
                return;
            }
        }

        if (_forceOffUntilRelease)
            return;

        if (_state != 1)
            SwitchState(1);
    }

    public override void OnRelease()
    {
        base.OnRelease();
        if (_state != 0)
            SwitchState(0);
        _forceOffUntilRelease = false;
        _isHolding = false;
        _holdStartedWithActive = false;
    }

    private void SwitchState(int state)
    {
        if (isOneStateSwitch && activableObject.isActivated) return;
        
        if (activableObject != null)
        {
            if (canClick)
            {
                activableObject.Operate();
            }
            else
            {
                if (state == 1)
                {
                    activableObject.Activate();
                }
                else
                {
                    activableObject.Deactivate();
                }
            }
        }

        if (animator != null)
        {
            animator.SetInteger("State", state);
            _state = state;
        }
        
        //Play sound
            
        AudioClip clip = _state == 1 ? TurnOnSound : TurnOffSound;
        if(clip != null)
            PlaySound(clip);

        //Reset
        if (isOneStateSwitch)
        {
            this.Invoke(0.1f, () =>
            {
                _state = 0;
                animator.SetInteger("State", _state);
            });
        }
    }

    public void LockObject()
    {
        isLocked = true;
        if (activableObject != null)
        {
            activableObject.Deactivate();
        }
    }
    
    private void PlaySound(AudioClip sound)
    {
        if (_soundInstance != null)
        {
            _soundInstance.Stop();
        }
        
        _soundInstance =
        SoundManager.Instance?.PlaySound(sound, activitySource.transform.position, volume, Random.Range(pitchMin, pitchMax));
    }
}
