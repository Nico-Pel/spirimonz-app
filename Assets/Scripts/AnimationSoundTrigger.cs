using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSoundTrigger : GameBehaviour
{
    public AudioClip clip;
    public float volume = 1f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    public float range = 20f;

    public void PlaySound()
    {
        SoundManager.Instance.PlaySound(clip, transform.position, volume, pitchMin, pitchMax, range);
    }
}
