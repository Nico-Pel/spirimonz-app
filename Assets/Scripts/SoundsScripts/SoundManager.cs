using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class SoundManager : GameBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [SerializeField]
    private AudioEmitter3D audioEmitterPrefab;
    
    public AudioClip ambientSound;
    public float ambientSoundVolume = 0.2f;

    private AudioSource _ambientSource;
    private Ghost _ghost;

    private void Awake()
    {
        Instance = this;
        
        if (ambientSound != null)
        {
            PlayAmbient(ambientSound, ambientSoundVolume, true);
        }
    }
    
    public SoundInstance PlaySound(
        AudioClip clip,
        Vector3 position,
        float volume = 1f,
        float pitch = 1f,
        float duration = -1f,
        float range = 15f,
        bool loop = false,
        Transform sourceParent = null,
        bool ignoreAudioOcclusion = false
    )
    {
        if (clip == null || audioEmitterPrefab == null)
            return null;

        if (sourceParent != null)
        {
            AudioSource[] sources = sourceParent.GetComponentsInChildren<AudioSource>();
            foreach (var s in sources)
            {
                if (s.clip == clip && s.isPlaying)
                    return null;
            }
        }

        AudioEmitter3D emitter = Instantiate(
            audioEmitterPrefab,
            position,
            Quaternion.identity
        );

        emitter.name = $"Sound_{clip.name}";

        if (sourceParent != null)
            emitter.transform.SetParent(sourceParent);

        emitter.Init(
            clip,
            volume,
            pitch,
            range,
            loop,
            ignoreAudioOcclusion
        );

        emitter.Play();

        AudioSource source = emitter.audioSource;
        GameObject go = emitter.gameObject;

        // --- Gestion durée / destruction
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
                {
                    //Debug.Log("Source : " + source.gameObject.name, source.gameObject);
                    Destroy(source.gameObject);
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
                Debug.Log("Source : " + _source.gameObject.name, _source.gameObject);
                Destroy(_source.gameObject);
            }
            _source = null;
            _gameObject = null;
        }

        public bool IsPlaying => _source != null && _source.isPlaying;
    }
}