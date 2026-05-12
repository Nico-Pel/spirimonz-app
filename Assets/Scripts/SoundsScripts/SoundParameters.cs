using System;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundParameters : GameBehaviour
{
    [SerializeField] private bool playerOnEnabled;
    [SerializeField] private bool usePlayerPos;
    [SerializeField] public bool isUISound;
    
    [SerializeField] private AudioClip[] possibleClips;
    [SerializeField] public float volume = 1f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    [SerializeField] private float duration = -1f;
    [SerializeField] private float range = 15f;
    [SerializeField] private bool loop = false;
    [SerializeField] private Transform sourceParent = null;
    [SerializeField] private bool ignoreAudioOcclusion = false;
    
    private int _lastPlayedClipIndex = -1;

    private void OnEnable()
    {
        if(playerOnEnabled)
            PlaySound();
    }

    public void PlaySound(Vector3 position, float forcedVolume = -1)
    {
        PlayManagedSound(position, forcedVolume);
    }

    public void PlaySound(Vector3 position, float forcedVolume, float forcedPitch)
    {
        PlayManagedSound(position, forcedVolume, forcedPitch);
    }

    public SoundManager.SoundInstance PlayManagedSound(Vector3 position, float forcedVolume = -1)
    {
        return PlayManagedSound(position, forcedVolume, float.NaN);
    }

    public SoundManager.SoundInstance PlayManagedSound(Vector3 position, float forcedVolume, float forcedPitch)
    {
        AudioClip clip = GetRandomClip();
        if (clip == null)
            return null;
        if (SoundManager.Instance == null)
            return null;
        if (isUISound && SoundManager.Instance.ShouldBlockUiSound())
            return null;

        if (usePlayerPos)
            position = GetPlayerPosition(position);

        float volumeToUse = volume;
        
        if (forcedVolume >= 0)
        {
            volumeToUse = forcedVolume;
        }

        if (isUISound && SoundManager.Instance != null)
            volumeToUse *= SoundManager.Instance.uiVolumeMultiplier;

        float pitch = float.IsNaN(forcedPitch) ? Random.Range(pitchMin, pitchMax) : forcedPitch;
        return SoundManager.Instance.PlaySound(clip, position, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);
    }
    
    public void PlaySound()
    {
        PlayManagedSound();
    }

    public SoundManager.SoundInstance PlayManagedSound()
    {
        AudioClip clip = GetRandomClip();
        if (clip == null)
            return null;
        if (SoundManager.Instance == null)
            return null;
        if (isUISound && SoundManager.Instance.ShouldBlockUiSound())
            return null;

        float volumeToUse = volume;

        if (isUISound && SoundManager.Instance != null)
            volumeToUse *= SoundManager.Instance.uiVolumeMultiplier;

        float pitch = Random.Range(pitchMin, pitchMax);
        Vector3 positionToUse = usePlayerPos ? GetPlayerPosition(transform.position) : transform.position;
        SoundManager.SoundInstance soundInstance = SoundManager.Instance.PlaySound(clip, position: positionToUse, volumeToUse, pitch, duration, range, loop, sourceParent, ignoreAudioOcclusion);

        return soundInstance;
    }

    public float GetPitchMin()
    {
        return pitchMin;
    }

    public float GetPitchMax()
    {
        return pitchMax;
    }

    public void SetPitchRange(float min, float max)
    {
        pitchMin = Mathf.Max(0.01f, min);
        pitchMax = Mathf.Max(pitchMin, max);

#if UNITY_EDITOR
        if (Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
    }

    private AudioClip GetRandomClip()
    {
        if (possibleClips == null || possibleClips.Length == 0)
            return null;

        int validClipCount = 0;
        for (int i = 0; i < possibleClips.Length; i++)
        {
            if (possibleClips[i] != null)
                validClipCount++;
        }

        if (validClipCount == 0)
            return null;

        int selectedValidClipOrder = Random.Range(0, validClipCount);
        if (validClipCount > 1 && selectedValidClipOrder == _lastPlayedClipIndex)
            selectedValidClipOrder = (selectedValidClipOrder + 1) % validClipCount;

        int currentValidClipOrder = 0;
        for (int i = 0; i < possibleClips.Length; i++)
        {
            if (possibleClips[i] == null)
                continue;

            if (currentValidClipOrder == selectedValidClipOrder)
            {
                _lastPlayedClipIndex = currentValidClipOrder;
                return possibleClips[i];
            }

            currentValidClipOrder++;
        }

        return null;
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
