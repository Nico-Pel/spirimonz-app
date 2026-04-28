using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class FlammableElement : GameBehaviour
{
    public enum FlammableType
    {
        None,
        Candle,
        Chimney
    }

    public FlammableType type;
    public bool startOnFire;
    public bool turnOffOnThrow;
    public Room optionalLinkedRoom;
    
    public GameObject[] fireObjects;
    public ParticleSystem[] fireOffParticles;

    [Header("Ghost sound settings")]
    public AudioClip blowUpByGhostSoundClip;
    public float ghostVolume = 1f;
    public float ghostPitch = 1f;
    
    [Header("Loop Sound")]
    public SoundParameters activeLoopSound;

    [Header("Optional Linked Light")]
    public Light linkedLight;
    public bool useLinkedLightBlend = true;
    [Min(0f)] public float linkedLightBlendDuration = 0.35f;
    public bool blendLinkedLightOnInitialEnable = false;

    public bool canBeTurnedOn = true;
    public bool debugPouetFire = false;

    [Header("Room Heating")]
    public bool heatRoom = false;
    [Min(0f)] public float heatPerSecond = 0.5f;
    [Min(0f)] public float maxRoomHeatDelta = 8f;

    [Header("Cursed Settings")]
    public bool mightBeCursed = false;
    public Renderer cursedRenderer;
    public Color cursedColor = new Color(0.77263737f, 0.3820755f, 1f, 1f);
    public float cursedTransitionTime = 0.5f;

    [Header("Cursed Temperature Color")]
    public Gradient cursedTemperatureGradient;
    public Color cursedFreezingColor = new Color(0.77263737f, 0.3820755f, 1f, 1f);
    public float cursedHigherTemperature = 25f;
    public float cursedLowerTemperature = 1f;
    public float cursedColorLerpSpeed = 0.1f;
    public bool startCursedHotOnIgnite = true;
    public float cursedWarmupDuration = 20f;
    public Light[] cursedLights;
    public ParticleSystem[] cursedParticles;

    private bool _isOnFire;
    private Room _cachedRoom;
    private bool _isCursed;
    private Color _currentCursedColor;
    private Tween _cursedColorTween;
    private Tween _linkedLightTween;
    private bool _cursedWarmupActive;
    private float _cursedWarmupStartTime;
    private Color _cursedWarmupStartColor;
    private SoundManager.SoundInstance _activeLoopSoundInstance;
    private float _linkedLightBaseIntensity;
    private bool _linkedLightCached;
    private bool _linkedLightInitialized;
    private MobileLightOptimizedLight _linkedOptimizedLight;
    private Spirimonz _parentSpirimonz;

    public UnityEvent<bool> onChangeFireState;
    public UnityEvent onBecomeCursed;

    private bool _usedForAQuest;

    private void Start()
    {
        CacheLinkedLightIfNeeded();
        bool immediateInitialLightState = !blendLinkedLightOnInitialEnable;
        _linkedLightInitialized = !immediateInitialLightState;
        EnableFire(startOnFire, false);
        CacheCursedReferencesIfNeeded();
        EnsureCursedGradient();
        _linkedLightInitialized = true;
    }

    private void OnEnable()
    {
        if (_linkedLightInitialized)
            RefreshFireVisuals(immediate: false);
    }

    private void Update()
    {
        if (heatRoom && _isOnFire && heatPerSecond > 0f && maxRoomHeatDelta > 0f)
        {
            Room room = GetLinkedRoom();
            if (room != null)
            {
                float maxAllowed = room.GetStartTemperature() + maxRoomHeatDelta;
                maxAllowed = Mathf.Min(maxAllowed, room.maxTemperature);
                room.AddHeatingClamped(heatPerSecond * Time.deltaTime, maxAllowed);
            }
        }

        if (_isCursed)
            UpdateCursedTemperatureColors();
    }

    public void EnableFire(bool enable, bool useParticlesOff = true, bool useGhostSoundClip = false, bool forced = false)
    {
        if (enable == true && (canBeTurnedOn == false && forced == false)) return;
        
        _isOnFire = enable;

        if (enable)
            PrepareLinkedLightForActivation();
        
        foreach (GameObject fire in fireObjects)
        {
            if (fire != null)
                fire.SetActive(enable);
        }

        UpdateLinkedLight(enable, immediate: !_linkedLightInitialized);

        if (enable == false && useParticlesOff && fireOffParticles.Length > 0)
        {
            foreach (ParticleSystem particles in fireOffParticles)
            {
                if (particles != null)
                {
                    particles.Play();
                }
            }

            if (useGhostSoundClip && blowUpByGhostSoundClip != null)
            {
                SoundManager.Instance.PlaySound(blowUpByGhostSoundClip, transform.position, ghostVolume, ghostPitch, -1f, 15f);
            }
        }

        UpdateActiveLoopSound();

        if (enable && type != FlammableType.Candle && _usedForAQuest == false)
        {
            _usedForAQuest = true;
            House house = House.Instance;
            foreach (Quest quest in house.map.quests)
            {
                if(quest.type == Quest.QuestType.LightACandle)
                    GameManager.Instance.UpdateQuestProgress(quest, house.map.houseID, 1);
            }
        }
        
        onChangeFireState?.Invoke(_isOnFire);

        if (enable && _isCursed && type == FlammableType.Candle)
        {
            StartCursedWarmup();
        }
    }
    
    public bool IsOnFire()
    {
        return _isOnFire;
    }

    public void RefreshFireVisuals(bool immediate = false)
    {
        foreach (GameObject fire in fireObjects)
        {
            if (fire != null)
                fire.SetActive(_isOnFire);
        }

        UpdateLinkedLight(_isOnFire, immediate);

        if (_isCursed)
            SetCursedFxColors(_currentCursedColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (optionalLinkedRoom != null)
            return;

        if (other.TryGetComponent(out Room room))
        {
            _cachedRoom = room;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (optionalLinkedRoom != null)
            return;

        if (_cachedRoom != null && other.TryGetComponent(out Room room) && room == _cachedRoom)
        {
            _cachedRoom = null;
        }
    }

    public void TryActivateCursed()
    {
        if (!mightBeCursed || _isCursed)
            return;

        _isCursed = true;
        onBecomeCursed?.Invoke();
        CacheCursedReferencesIfNeeded();
        EnsureCursedGradient();

        if (cursedRenderer != null)
        {
            _cursedColorTween?.Kill();
            _cursedColorTween = cursedRenderer.material.DOColor(cursedColor, cursedTransitionTime);
        }

        _currentCursedColor = cursedTemperatureGradient.Evaluate(0f);
        SetCursedFxColors(_currentCursedColor);

        if (_isOnFire && type == FlammableType.Candle)
        {
            StartCursedWarmup();
        }
    }

    private Room GetLinkedRoom()
    {
        if (optionalLinkedRoom != null)
            return optionalLinkedRoom;

        if (_parentSpirimonz == null)
            _parentSpirimonz = GetComponentInParent<Spirimonz>();

        if (_parentSpirimonz != null && _parentSpirimonz.currentRoom != null)
        {
            _cachedRoom = _parentSpirimonz.currentRoom;
            return _cachedRoom;
        }

        if (_cachedRoom == null)
            _cachedRoom = GetComponentInParent<Room>();

        return _cachedRoom;
    }

    private void UpdateCursedTemperatureColors()
    {
        Room room = GetLinkedRoom();
        if (room == null)
            return;

        float currentTemperature = room.GetTemperatureCelsius();

        bool freezingEvidence = room.house != null &&
                                room.house.currentGhost != null &&
                                room.house.currentGhost.ghostParameters.FreezingTemperature;

        Color targetColor = cursedFreezingColor;
        if (!freezingEvidence || currentTemperature >= cursedLowerTemperature)
        {
            float minTemp = cursedLowerTemperature;
            float maxTemp = Mathf.Max(cursedHigherTemperature, minTemp + 0.01f);
            float t = 1f - Mathf.InverseLerp(minTemp, maxTemp, currentTemperature);
            targetColor = cursedTemperatureGradient.Evaluate(t);
        }

        if (_cursedWarmupActive)
        {
            float duration = Mathf.Max(0.01f, cursedWarmupDuration);
            float t = Mathf.Clamp01((Time.time - _cursedWarmupStartTime) / duration);
            _currentCursedColor = Color.Lerp(_cursedWarmupStartColor, targetColor, t);

            if (t >= 1f)
                _cursedWarmupActive = false;
        }
        else
        {
            _currentCursedColor = Color.Lerp(
                _currentCursedColor,
                targetColor,
                Time.deltaTime * cursedColorLerpSpeed
            );
        }

        SetCursedFxColors(_currentCursedColor);
    }

    private void SetCursedFxColors(Color colorToSet)
    {
        if (cursedLights != null)
        {
            foreach (Light light in cursedLights)
            {
                if (light != null)
                    light.color = colorToSet;
            }
        }

        if (cursedParticles != null)
        {
            foreach (ParticleSystem particle in cursedParticles)
            {
                if (particle == null) continue;
                var main = particle.main;
                main.startColor = colorToSet;
            }
        }
    }

    private void CacheCursedReferencesIfNeeded()
    {
        if (cursedRenderer == null)
        {
            cursedRenderer = GetComponent<Renderer>();
        }

        if ((cursedLights == null || cursedLights.Length == 0) && fireObjects != null)
        {
            List<Light> lights = new List<Light>();
            foreach (GameObject fire in fireObjects)
            {
                if (fire == null) continue;
                lights.AddRange(fire.GetComponentsInChildren<Light>(true));
            }
            cursedLights = lights.ToArray();
        }

        if ((cursedParticles == null || cursedParticles.Length == 0) && fireObjects != null)
        {
            List<ParticleSystem> particles = new List<ParticleSystem>();
            foreach (GameObject fire in fireObjects)
            {
                if (fire == null) continue;
                particles.AddRange(fire.GetComponentsInChildren<ParticleSystem>(true));
            }
            cursedParticles = particles.ToArray();
        }
    }

    private void EnsureCursedGradient()
    {
        if (cursedTemperatureGradient == null)
            cursedTemperatureGradient = new Gradient();

        if (cursedTemperatureGradient.colorKeys.Length > 0)
            return;

        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(new Color(1f, 0.10950938f, 0f, 1f), 0f),
            new GradientColorKey(new Color(1f, 0.48650524f, 0f, 1f), 11372f / 65535f),
            new GradientColorKey(new Color(1f, 0.80729437f, 0f, 1f), 39899f / 65535f),
            new GradientColorKey(new Color(0f, 0.8143573f, 1f, 1f), 51464f / 65535f),
            new GradientColorKey(new Color(0f, 1f, 0.896049f, 1f), 1f)
        };

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        cursedTemperatureGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void StartCursedWarmup()
    {
        if (!startCursedHotOnIgnite)
            return;

        float duration = Mathf.Max(0f, cursedWarmupDuration);
        if (duration <= 0f)
        {
            _cursedWarmupActive = false;
            return;
        }

        EnsureCursedGradient();
        _cursedWarmupStartColor = cursedTemperatureGradient.Evaluate(0f); // hot (red)
        _currentCursedColor = _cursedWarmupStartColor;
        _cursedWarmupStartTime = Time.time;
        _cursedWarmupActive = true;
        SetCursedFxColors(_currentCursedColor);
    }

    private void UpdateActiveLoopSound()
    {
        if (_isOnFire)
        {
            if ((_activeLoopSoundInstance == null || !_activeLoopSoundInstance.IsPlaying) && activeLoopSound != null)
                _activeLoopSoundInstance = activeLoopSound.PlayManagedSound(transform.position);

            return;
        }

        if (_activeLoopSoundInstance != null)
        {
            _activeLoopSoundInstance.Stop(false);
            _activeLoopSoundInstance = null;
        }
    }

    private void CacheLinkedLightIfNeeded()
    {
        if (linkedLight == null || _linkedLightCached)
            return;

        _linkedLightBaseIntensity = Mathf.Max(0f, linkedLight.intensity);
        _linkedOptimizedLight = linkedLight.GetComponent<MobileLightOptimizedLight>();
        _linkedLightCached = true;
    }

    private void PrepareLinkedLightForActivation()
    {
        CacheLinkedLightIfNeeded();
        if (linkedLight == null)
            return;

        float duration = Mathf.Max(0f, linkedLightBlendDuration);
        bool shouldBlend = _linkedLightInitialized && useLinkedLightBlend && duration > 0f;
        if (!shouldBlend)
            return;

        if (_linkedLightTween != null && _linkedLightTween.IsActive())
            _linkedLightTween.Kill();

        if (_linkedOptimizedLight != null)
            _linkedOptimizedLight.SetBaseEnabledState(true);

        linkedLight.enabled = true;
        linkedLight.intensity = 0f;
    }

    private void ApplyLinkedLightImmediate(bool enable)
    {
        CacheLinkedLightIfNeeded();
        if (linkedLight == null)
            return;

        if (_linkedLightTween != null && _linkedLightTween.IsActive())
            _linkedLightTween.Kill();

        if (_linkedOptimizedLight != null)
            _linkedOptimizedLight.SetBaseEnabledState(enable);

        linkedLight.enabled = enable;
        linkedLight.intensity = enable ? _linkedLightBaseIntensity : 0f;
    }

    private void UpdateLinkedLight(bool enable, bool immediate = false)
    {
        CacheLinkedLightIfNeeded();
        if (linkedLight == null)
            return;

        if (_linkedLightTween != null && _linkedLightTween.IsActive())
            _linkedLightTween.Kill();

        if (_linkedOptimizedLight != null)
            _linkedOptimizedLight.SetBaseEnabledState(enable);

        float duration = Mathf.Max(0f, linkedLightBlendDuration);
        if (immediate || !useLinkedLightBlend || duration <= 0f)
        {
            ApplyLinkedLightImmediate(enable);
            return;
        }

        if (enable)
        {
            linkedLight.enabled = true;
            linkedLight.intensity = 0f;
            _linkedLightTween = DOTween.To(
                () => linkedLight.intensity,
                value => linkedLight.intensity = value,
                _linkedLightBaseIntensity,
                duration
            );
            return;
        }

        linkedLight.enabled = true;
        _linkedLightTween = DOTween.To(
            () => linkedLight.intensity,
            value => linkedLight.intensity = value,
            0f,
            duration
        ).OnComplete(() =>
         {
             if (linkedLight != null)
                 linkedLight.enabled = false;
         });
    }

    private void OnDisable()
    {
        if (_activeLoopSoundInstance != null)
        {
            _activeLoopSoundInstance.Stop(false);
            _activeLoopSoundInstance = null;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(FlammableElement))]
public class FlammableElementEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UnityEditor.EditorGUILayout.Space();

        FlammableElement flammable = (FlammableElement)target;
        if (flammable == null)
            return;

        using (new UnityEditor.EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Force Fire ON"))
                flammable.EnableFire(true, useParticlesOff: false, forced: true);

            if (GUILayout.Button("Force Fire OFF"))
                flammable.EnableFire(false, useParticlesOff: false, forced: true);
        }

        if (!Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.HelpBox(
                "These test buttons are available in Play Mode only.",
                UnityEditor.MessageType.Info
            );
        }
    }
}
#endif
