using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundParameters : GameBehaviour
{
    [SerializeField] private bool playerOnEnabled;
    
    [SerializeField] private AudioClip[] possibleClips;
    [SerializeField] public float volume = 1f;
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;
    [SerializeField] private float duration = -1f;
    [SerializeField] private float range = 15f;
    [SerializeField] private bool loop = false;
    [SerializeField] private Transform sourceParent = null;
    [SerializeField] private bool ignoreAudioOcclusion = false;

    private void OnEnable()
    {
        if(playerOnEnabled)
            PlaySound();
    }

    public void PlaySound(Vector3 position, float forcedVolume = -1)
    {
        float volumeToUse = volume;
        
        if (forcedVolume >= 0)
        {
            volumeToUse = forcedVolume;
        }
        
        AudioClip clip = possibleClips[Random.Range(0, possibleClips.Length)];
        float pitch = Random.Range(pitchMin, pitchMax);
        SoundManager.Instance.PlaySound(clip, position, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);
    }
    
    public void PlaySound()
    {
        float volumeToUse = volume;
        
        AudioClip clip = possibleClips[Random.Range(0, possibleClips.Length)];
        float pitch = Random.Range(pitchMin, pitchMax);
        SoundManager.Instance.PlaySound(clip, position: transform.position, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);
    }
}