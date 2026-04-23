using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpmzCandleOracle : Spirimonz
{
    [Header("Stationary Settings")]
    [Min(0.05f)] public float candleCheckInterval = 0.2f;
    [Min(0.05f)] public float sourceRefreshInterval = 0.35f;

    [Header("Candle Detection")]
    public Transform candleDetectionOrigin;
    public Vector3 candleDetectionLocalOffset = new Vector3(0f, 0f, 0.5f);
    [Min(0.05f)] public float candleDetectionRadius = 0.75f;
    public LayerMask candleDetectionLayerMask = ~0;

    [Header("Activity Detection")]
    public Transform activityDetectionOrigin;
    [Min(0.1f)] public float activityDetectionRange = 4f;
    public List<ActivitySource> activitySources = new List<ActivitySource>();

    [Header("Animator")]
    public string detectingBoolName = "Detecting";
    public string detectionTriggerName = "Detection";

    [Header("Sounds")]
    public SoundParameters[] detectionSoundsByValue = new SoundParameters[5];
    public SoundParameters detectingLoopSound;
    [Min(0f)] public float detectingLoopResumeDelay = 2f;

    [Header("Emission")]
    public Renderer emissionRenderer;
    [Min(0)] public int emissionMaterialIndex = 0;
    public Texture baseEmissionTexture;
    public Texture[] detectedEmissionTexturesByValue = new Texture[5];
    public Color baseEmissionColor = new Color32(0x00, 0xFF, 0xE0, 0xFF);
    public Color activityFiveEmissionColor = new Color32(0x00, 0xFF, 0x0C, 0xFF);
    [Min(0.01f)] public float activityFiveBlinkSpeed = 3f;
    [Range(0f, 1f)] public float activityFiveBlinkMinIntensity = 0.65f;

    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private readonly Collider[] _candleHits = new Collider[16];
    private MaterialPropertyBlock _emissionPropertyBlock;

    private ActivitySource _currentDetectedSource;
    private int _currentDetectedValue;
    private bool _isCandleNearby;
    private float _nextCandleCheckTime;
    private float _nextSourceRefreshTime;
    private float _detectingLoopResumeTime;
    private bool _isRegisteredToHouse;
    private SoundManager.SoundInstance _detectingLoopSoundInstance;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();

        if (IsLocked())
            return;

        activitySources.Clear();
        RegisterExistingActivitySources();
        RegisterHouseListener();

        CacheBaseEmissionTexture();
        ApplyEmissionFeedback(baseEmissionTexture, baseEmissionColor);
        SetDetectingAnimator(false);
    }

    public override void ActionOnEnabled()
    {
        base.ActionOnEnabled();
        RegisterExistingActivitySources();
        RegisterHouseListener();
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        canBeTakenBackIntoHands = false;
        StopMovement();
        RefreshCandleDetection(force: true);
        RefreshBestActivitySource(forceTrigger: false);
    }

    public override void InteractionStarted()
    {
        onInteract?.Invoke();
        RotateTowardsPlayer();
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        if (!isOnTheMap)
            return true;

        if (Time.time >= _nextCandleCheckTime)
            RefreshCandleDetection(force: false);

        if (!_isCandleNearby)
        {
            ClearDetection();
            return true;
        }

        if ((_currentDetectedSource == null || _currentDetectedSource.activityValue <= 0) &&
            Time.time >= _nextSourceRefreshTime)
        {
            RefreshBestActivitySource(forceTrigger: false);
        }

        UpdateEmissionBlink();
        UpdateDetectingLoopSound();

        return true;
    }

    protected override void UpdateMovementBehaviour()
    {
        StopMovement();
    }

    protected override void OnDisable()
    {
        UnregisterAllActivitySources();
        UnregisterHouseListener();

        ClearDetection();
        SetDetectingAnimator(false);
        StopDetectingLoopSound();
        ApplyEmissionFeedback(baseEmissionTexture, baseEmissionColor);
        base.OnDisable();
    }

    private void RegisterExistingActivitySources()
    {
        ActivitySource[] existingSources = FindObjectsByType<ActivitySource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < existingSources.Length; i++)
            RegisterActivitySource(existingSources[i]);

        if (_house == null)
            return;

        for (int i = 0; i < _house.activitySourcesAddedToGame.Count; i++)
            RegisterActivitySource(_house.activitySourcesAddedToGame[i]);
    }

    private void RegisterActivitySource(ActivitySource source)
    {
        if (source == null)
            return;

        if (!activitySources.Contains(source))
            activitySources.Add(source);

        source.onActivityValueChanged -= HandleActivityValueChanged;
        source.onActivityValueChanged += HandleActivityValueChanged;
    }

    private void RegisterHouseListener()
    {
        if (_house == null || _isRegisteredToHouse)
            return;

        _house.onNewActivitySourceAddedToGame.AddListener(HandleNewActivitySource);
        _isRegisteredToHouse = true;
    }

    private void UnregisterHouseListener()
    {
        if (_house == null || !_isRegisteredToHouse)
            return;

        _house.onNewActivitySourceAddedToGame.RemoveListener(HandleNewActivitySource);
        _isRegisteredToHouse = false;
    }

    private void UnregisterAllActivitySources()
    {
        for (int i = 0; i < activitySources.Count; i++)
        {
            ActivitySource source = activitySources[i];
            if (source != null)
                source.onActivityValueChanged -= HandleActivityValueChanged;
        }
    }

    private void HandleNewActivitySource(ActivitySource newSource)
    {
        RegisterActivitySource(newSource);

        if (_isCandleNearby && newSource != null && newSource.activityValue > 0)
            TryDetectSource(newSource, newSource.activityValue, forceTrigger: false);
    }

    private void HandleActivityValueChanged(ActivitySource source, int previousValue, int newValue)
    {
        if (source == null)
            return;

        if (source == _currentDetectedSource)
        {
            if (newValue <= 0)
            {
                ClearDetection();
                return;
            }

            if (_isCandleNearby && isOnTheMap && newValue != _currentDetectedValue)
            {
                ApplyDetection(source, newValue, true);
            }

            return;
        }

        if (!_isCandleNearby || !isOnTheMap || newValue <= 0 || !IsSourceInRange(source))
            return;

        if (_currentDetectedSource != null && newValue <= _currentDetectedValue)
            return;

        TryDetectSource(source, newValue, forceTrigger: true);
    }

    private void RefreshCandleDetection(bool force)
    {
        _nextCandleCheckTime = Time.time + Mathf.Max(0.05f, candleCheckInterval);

        bool candleNearby = IsLitCandleNearby();
        if (!force && candleNearby == _isCandleNearby)
            return;

        _isCandleNearby = candleNearby;
        SetDetectingAnimator(_isCandleNearby);

        if (!_isCandleNearby)
        {
            ClearDetection();
            return;
        }

        RefreshBestActivitySource(forceTrigger: false);
    }

    private bool IsLitCandleNearby()
    {
        Vector3 origin = GetCandleDetectionOrigin();
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            candleDetectionRadius,
            _candleHits,
            candleDetectionLayerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _candleHits[i];
            if (hit == null)
                continue;

            if (IsHitLitCandle(hit))
                return true;
        }

        return false;
    }

    private bool IsHitLitCandle(Collider hit)
    {
        if (hit == null)
            return false;

        FlammableElement flammable = hit.GetComponentInParent<FlammableElement>();
        if (IsLitCandle(flammable))
            return true;

        CatchableFireObject fireObject = hit.GetComponentInParent<CatchableFireObject>();
        if (fireObject != null && IsLitCandle(fireObject.linkedFlammableElement))
            return true;

        Spirimonz nearbySpirimonz = hit.GetComponentInParent<Spirimonz>();
        if (nearbySpirimonz == null)
            return false;

        CatchableFireObject embeddedFireObject = nearbySpirimonz.GetComponentInChildren<CatchableFireObject>(true);
        if (embeddedFireObject != null && IsLitCandle(embeddedFireObject.linkedFlammableElement))
            return true;

        FlammableElement embeddedFlammable = nearbySpirimonz.GetComponentInChildren<FlammableElement>(true);
        return IsLitCandle(embeddedFlammable);
    }

    private static bool IsLitCandle(FlammableElement flammable)
    {
        return flammable != null &&
               flammable.type == FlammableElement.FlammableType.Candle &&
               flammable.IsOnFire();
    }

    private void RefreshBestActivitySource(bool forceTrigger)
    {
        _nextSourceRefreshTime = Time.time + Mathf.Max(0.05f, sourceRefreshInterval);

        if (!_isCandleNearby)
            return;

        ActivitySource bestSource = null;
        int bestValue = 0;

        for (int i = 0; i < activitySources.Count; i++)
        {
            ActivitySource source = activitySources[i];
            if (source == null || source.activityValue <= 0 || !IsSourceInRange(source))
                continue;

            if (source.activityValue > bestValue)
            {
                bestSource = source;
                bestValue = source.activityValue;
            }
        }

        if (bestSource == null)
        {
            ClearDetection();
            return;
        }

        if (_currentDetectedSource != null && _currentDetectedSource != bestSource && bestValue <= _currentDetectedValue)
            return;

        TryDetectSource(bestSource, bestValue, forceTrigger);
    }

    private void TryDetectSource(ActivitySource source, int detectedValue, bool forceTrigger)
    {
        if (source == null || detectedValue <= 0)
            return;

        if (!IsSourceInRange(source))
            return;

        bool shouldTrigger = forceTrigger ||
                             source != _currentDetectedSource ||
                             detectedValue != _currentDetectedValue;

        ApplyDetection(source, detectedValue, shouldTrigger);
    }

    private void ApplyDetection(ActivitySource source, int detectedValue, bool triggerFeedback)
    {
        _currentDetectedSource = source;
        _currentDetectedValue = Mathf.Clamp(detectedValue, 1, 5);

        if (triggerFeedback && animator != null && !string.IsNullOrEmpty(detectionTriggerName))
            animator.SetTrigger(detectionTriggerName);

        ApplyDetectionFeedback(_currentDetectedValue, triggerFeedback);

        if (triggerFeedback)
        {
            StopDetectingLoopSound();
            _detectingLoopResumeTime = Time.time + Mathf.Max(0f, detectingLoopResumeDelay);
        }
    }

    private void ApplyDetectionFeedback(int detectedValue, bool playSound)
    {
        Texture emissionTexture = GetDetectedEmissionTexture(detectedValue);
        Color emissionColor = detectedValue == 5 ? activityFiveEmissionColor : baseEmissionColor;
        ApplyEmissionFeedback(emissionTexture != null ? emissionTexture : baseEmissionTexture, emissionColor);

        if (!playSound)
            return;

        int soundIndex = Mathf.Clamp(detectedValue - 1, 0, detectionSoundsByValue.Length - 1);
        SoundParameters soundParameters = detectionSoundsByValue[soundIndex];
        if (soundParameters != null)
            soundParameters.PlaySound(transform.position);
    }

    private Texture GetDetectedEmissionTexture(int detectedValue)
    {
        int textureIndex = Mathf.Clamp(detectedValue - 1, 0, detectedEmissionTexturesByValue.Length - 1);
        return detectedEmissionTexturesByValue[textureIndex];
    }

    private void ClearDetection()
    {
        _currentDetectedSource = null;
        _currentDetectedValue = 0;
        ApplyEmissionFeedback(baseEmissionTexture, baseEmissionColor);

        if (!_isCandleNearby || !isOnTheMap)
            StopDetectingLoopSound();
    }

    private bool IsSourceInRange(ActivitySource source)
    {
        if (source == null)
            return false;

        Vector3 origin = activityDetectionOrigin != null ? activityDetectionOrigin.position : transform.position;
        return Vector3.Distance(origin, source.transform.position) <= activityDetectionRange;
    }

    private void CacheBaseEmissionTexture()
    {
        if (emissionRenderer == null || baseEmissionTexture != null)
            return;

        Material[] materials = emissionRenderer.sharedMaterials;
        if (emissionMaterialIndex < 0 || emissionMaterialIndex >= materials.Length)
            return;

        Material material = materials[emissionMaterialIndex];
        if (material != null && material.HasProperty(EmissionMapId))
            baseEmissionTexture = material.GetTexture(EmissionMapId);
    }

    private void ApplyEmissionFeedback(Texture emissionTexture, Color emissionColor)
    {
        if (emissionRenderer == null)
            return;

        EnsureEmissionPropertyBlock();
        if (_emissionPropertyBlock == null)
            return;

        emissionRenderer.GetPropertyBlock(_emissionPropertyBlock, emissionMaterialIndex);
        _emissionPropertyBlock.SetTexture(EmissionMapId, emissionTexture);
        _emissionPropertyBlock.SetColor(EmissionColorId, emissionColor);
        emissionRenderer.SetPropertyBlock(_emissionPropertyBlock, emissionMaterialIndex);
    }

    private void UpdateEmissionBlink()
    {
        if (emissionRenderer == null)
            return;

        if (_currentDetectedValue != 5)
            return;

        float blink = Mathf.Lerp(
            activityFiveBlinkMinIntensity,
            1f,
            (Mathf.Sin(Time.time * Mathf.Max(0.01f, activityFiveBlinkSpeed) * Mathf.PI * 2f) + 1f) * 0.5f
        );

        ApplyEmissionFeedback(
            GetDetectedEmissionTexture(_currentDetectedValue) ?? baseEmissionTexture,
            activityFiveEmissionColor * blink
        );
    }

    private void SetDetectingAnimator(bool active)
    {
        if (animator == null || string.IsNullOrEmpty(detectingBoolName))
            return;

        animator.SetBool(detectingBoolName, active);
    }

    private void UpdateDetectingLoopSound()
    {
        if (!_isCandleNearby || !isOnTheMap || detectingLoopSound == null)
        {
            StopDetectingLoopSound();
            return;
        }

        if (Time.time < _detectingLoopResumeTime)
            return;

        if (_detectingLoopSoundInstance != null && _detectingLoopSoundInstance.IsPlaying)
            return;

        _detectingLoopSoundInstance = detectingLoopSound.PlayManagedSound(transform.position);
    }

    private void StopDetectingLoopSound()
    {
        if (_detectingLoopSoundInstance == null)
            return;

        _detectingLoopSoundInstance.Stop(false);
        _detectingLoopSoundInstance = null;
    }

    private Vector3 GetCandleDetectionOrigin()
    {
        if (candleDetectionOrigin != null)
            return candleDetectionOrigin.position;

        return transform.TransformPoint(candleDetectionLocalOffset);
    }

    private void StopMovement()
    {
        if (agent == null)
            return;

        if (agent.enabled)
            agent.ResetPath();

        agent.speed = 0f;
        agent.velocity = Vector3.zero;
    }

    private void EnsureEmissionPropertyBlock()
    {
        if (_emissionPropertyBlock == null)
            _emissionPropertyBlock = new MaterialPropertyBlock();
    }

    private void RotateTowardsPlayer()
    {
        Transform target = null;

        if (_house != null && _house.currentPlayer != null)
        {
            target = _house.currentPlayer.head != null
                ? _house.currentPlayer.head
                : _house.currentPlayer.transform;
        }

        if (target == null && Camera.main != null)
            target = Camera.main.transform;

        if (target == null)
            return;

        Vector3 targetPosition = target.position;
        Vector3 flatDirection = targetPosition - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
        transform.DOKill();
        transform.DORotateQuaternion(targetRotation, 0.25f).SetEase(Ease.OutSine);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(GetCandleDetectionOrigin(), candleDetectionRadius);

        Vector3 activityOrigin = activityDetectionOrigin != null ? activityDetectionOrigin.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(activityOrigin, activityDetectionRange);
    }
}
