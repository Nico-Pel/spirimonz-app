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
        
        if (sourceParent != null)
        {
            AudioSource[] sources = sourceParent.GetComponentsInChildren<AudioSource>();
            foreach (var s in sources)
            {
                if (s.clip == clip && s.isPlaying)
                    return null; // on bloque
            }
        }

        GameObject go = new GameObject($"Sound_{clip.name}");
        if (sourceParent != null)
            go.transform.SetParent(sourceParent);

        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        
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
            {
                Destroy(go, duration);
            }
            else
            {
                Destroy(source, duration);
            }
        }
        else if (!loop)
        {
            float effectivePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
            this.Invoke(clip.length / effectivePitch, () =>
            {
                if (sourceParent == null)
                {
                    Destroy(go);
                }
                else
                {
                    Destroy(source);
                }
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

    private Coroutine _ambientFadeCoroutine;

    public void StopAmbient(float fadeDuration = 0f)
    {
        if (_ambientSource == null)
            return;

        if (_ambientFadeCoroutine != null)
            StopCoroutine(_ambientFadeCoroutine);

        if (fadeDuration <= 0f)
        {
            _ambientSource.Stop();
            return;
        }

        _ambientFadeCoroutine = StartCoroutine(FadeOutAmbient(fadeDuration));
    }
    
    private IEnumerator FadeOutAmbient(float duration)
    {
        float startVolume = _ambientSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            _ambientSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        _ambientSource.Stop();
        _ambientSource.volume = startVolume; // reset pour la prochaine fois
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