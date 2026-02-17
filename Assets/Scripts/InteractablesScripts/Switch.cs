using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Switch : ClickableObject
{
    public PrintSource[] printSources;
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

    [Header("Special settings")] 
    public bool isOnStateSwitch;
    
    private int _state = 0;
    
    public override void OnClick()
    {
        base.OnClick();
        if (activableObject != null)
        {        
            activableObject.Operate();
        }

        if (animator != null)
        {
            int newState = _state == 1 ? 0 : 1;
            animator.SetInteger("State", newState);
            _state = newState;
        }
        
        //Play sound
            
        AudioClip clip = _state == 1 ? TurnOnSound : TurnOffSound;
        if(clip != null)
            PlaySound(clip);

        //Reset
        if (isOnStateSwitch)
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
    
    public PrintSource GetRandomPrintSource()
    {
        if (printSources.Length == 0) return null;
        
        List<PrintSource> possiblePrintSources = new List<PrintSource>();
        foreach (PrintSource printSource in printSources)
        {
            if(printSource.IsActivated() == false)
                possiblePrintSources.Add(printSource);
        }
        
        if (possiblePrintSources.Count == 0) return null;
        return possiblePrintSources[Random.Range(0, possiblePrintSources.Count)];
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
