using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundReader : MonoBehaviour
{
    public AudioClip[] possibleClips;
    
    public Transform soundPos;
    public bool useHadParent;

    public float volume = 1f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    public float range = 15f;
    public float duration = -1f;
    public bool loop;
    public bool ignoreOcclusion;

    public void PlayerSound()
    {
        AudioClip clip = possibleClips[Random.Range(0, possibleClips.Length)];

        if (clip == null) return;
        
        SoundManager.Instance.PlaySound(clip, 
            soundPos.position, 
            volume, 
            Random.Range(pitchMin, pitchMax), 
            this.duration, 
            range, 
            this.loop, 
            sourceParent : useHadParent ? soundPos : null, 
            ignoreOcclusion);
    }
}