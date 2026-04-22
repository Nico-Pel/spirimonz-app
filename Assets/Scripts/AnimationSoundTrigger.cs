using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSoundTrigger : GameBehaviour
{
    public SoundParameters soundParameters;

    [Space]
    
    public AudioClip clip;
    public float volume = 1f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    public float range = 20f;

    public void PlaySound()
    {
        if(clip != null)
            SoundManager.Instance.PlaySound(clip, transform.position, volume, pitchMin, pitchMax, range);
        
        if(soundParameters != null)
            soundParameters.PlaySound();
    }
}
