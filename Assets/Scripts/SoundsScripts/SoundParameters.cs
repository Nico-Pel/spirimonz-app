using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundParameters : GameBehaviour
{
    [SerializeField] private bool playerOnEnabled;
    [SerializeField] private bool usePlayerPos;
    [SerializeField] public bool isUISound;
    
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
        if (possibleClips == null || possibleClips.Length == 0)
            return;
        if (SoundManager.Instance == null)
            return;
        if (isUISound && SoundManager.Instance.ShouldBlockUiSound())
            return;

        if (usePlayerPos)
            position = GetPlayerPosition(position);

        float volumeToUse = volume;
        
        if (forcedVolume >= 0)
        {
            volumeToUse = forcedVolume;
        }

        if (isUISound && SoundManager.Instance != null)
            volumeToUse *= SoundManager.Instance.uiVolumeMultiplier;
        
        AudioClip clip = possibleClips[Random.Range(0, possibleClips.Length)];
        float pitch = Random.Range(pitchMin, pitchMax);
        SoundManager.Instance.PlaySound(clip, position, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);
    }
    
    public void PlaySound()
    {
        Debug.Log("POUET1 " + name);
        if (possibleClips == null || possibleClips.Length == 0)
            return;
        if (SoundManager.Instance == null)
            return;
        if (isUISound && SoundManager.Instance.ShouldBlockUiSound())
            return;

        float volumeToUse = volume;

        if (isUISound && SoundManager.Instance != null)
            volumeToUse *= SoundManager.Instance.uiVolumeMultiplier;
        
        AudioClip clip = possibleClips[Random.Range(0, possibleClips.Length)];
        float pitch = Random.Range(pitchMin, pitchMax);
        Vector3 positionToUse = usePlayerPos ? GetPlayerPosition(transform.position) : transform.position;
        SoundManager.Instance.PlaySound(clip, position: positionToUse, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);
        
        Debug.Log("POUET2 " + name);
    }

    private Vector3 GetPlayerPosition(Vector3 fallback)
    {
        Player player = Player.Instance;
        if (player == null)
            return fallback;

        if (player.head != null)
            return player.head.position;

        return player.transform.position;
    }
}
