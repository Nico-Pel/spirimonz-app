using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : GameBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }
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

        // 🔁 Gestion de la durée
        if (duration > 0f)
        {
            if (sourceParent != null)
            {
                Destroy(go, duration);
            }
            else
            {
                Destroy(source);
            }
        }
        else if (!loop)
        {
            // Pas de loop et durée indéterminée → jouer une fois
            float effectivePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
            this.Invoke(clip.length / effectivePitch, () =>
            {
                if (sourceParent != null)
                {
                    Destroy(go, duration);
                }
                else
                {
                    Destroy(source);
                }
            });
        }
        // else : loop + durée indéterminée → stop manuel uniquement

        return new SoundInstance(source, go);
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