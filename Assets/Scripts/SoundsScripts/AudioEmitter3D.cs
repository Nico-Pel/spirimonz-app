using UnityEngine;

public class AudioEmitter3D : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioOcclusion audioOcclusion;

    public void Init(
        AudioClip clip,
        float volume,
        float pitch,
        float range,
        bool loop,
        bool ignoreAudioOcclusion = false
    )
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;

        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = range;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = loop;
        
        audioOcclusion.enabled = !ignoreAudioOcclusion;
    }

    public void Play()
    {
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }
}