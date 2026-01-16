using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : GameBehaviour
{
    public static SoundManager Instance { get; private set; }

    private AudioSource _ambientSource;

    private void Awake()
    {
        Instance = this;
    }

    // --- Sound Effects existants
    public SoundInstance PlaySound(
        AudioClip clip,
        Vector3 position,
        float volume = 1f,
        float pitch = 1f,
        float duration = -1f,
        float range = 15f,
        bool loop = false,
        Transform sourceParent = null
    )
    {
        if (clip == null)
            return null;

        GameObject go = sourceParent == null ? new GameObject($"Sound_{clip.name}") : sourceParent.gameObject;
        go.transform.position = position;

        AudioSource source = go.TryGetComponent(out AudioSource audioSource) ? audioSource : go.AddComponent<AudioSource>();
        
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;

        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = range;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.loop = loop;
        source.Play();

        // Gestion de la durée
        if (duration > 0f)
        {
            if (sourceParent == null)
                Destroy(go, duration);
            else
                Destroy(source, duration);
        }
        else if (!loop)
        {
            float effectivePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
            this.Invoke(clip.length / effectivePitch, () =>
            {
                if (sourceParent == null)
                    Destroy(go);
                else
                    Destroy(source);
            });
        }

        return new SoundInstance(source, go);
    }

    // --- Nouvelle fonction pour les sons d'ambiance
    public void PlayAmbient(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;

        // Crée la source si elle n'existe pas
        if (_ambientSource == null)
        {
            GameObject go = new GameObject("AmbientAudio");
            go.transform.SetParent(transform);
            _ambientSource = go.AddComponent<AudioSource>();
            _ambientSource.spatialBlend = 0f; // 2D
            _ambientSource.loop = loop;
        }

        _ambientSource.clip = clip;
        _ambientSource.volume = volume;
        _ambientSource.loop = loop;
        _ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (_ambientSource != null)
            _ambientSource.Stop();
    }

    public class SoundInstance
    {
        private AudioSource _source;
        private GameObject _gameObject;

        public SoundInstance(AudioSource source, GameObject gameObject)
        {
            _source = source;
            _gameObject = gameObject;
        }

        public void Stop(bool destroyGameObject = true)
        {
            if (_source == null) return;

            _source.Stop();
            if (destroyGameObject)
            {
                Object.Destroy(_gameObject);
            }
            else
            {
                Destroy(_source);
            }
            _source = null;
            _gameObject = null;
        }

        public bool IsPlaying => _source != null && _source.isPlaying;
    }
}