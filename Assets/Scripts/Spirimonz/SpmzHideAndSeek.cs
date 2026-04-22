using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class SpmzHideAndSeek : Spirimonz
{
    [Header("Hide & Seek")]
    [Range(0f, 1f)] public float closestSpotsToAvoidRatio = 0.5f;
    [Min(1)] public int minFarSpotsCount = 1;
    [Min(0.1f)] public float hidingSpotArrivalDistance = 1f;
    [Min(0.05f)] public float hideSnapDuration = 0.3f;
    public Ease hideSnapEase = Ease.OutSine;
    [Min(0.1f)] public float navMeshSampleMaxDistance = 2f;
    [Min(0f)] public float maxAllowedNavMeshOffset = 1.5f;
    [Min(0f)] public float hidingSpotTiltPermissiveness = 1f;
    public SoundParameters hiddenLoopSound;
    [Min(0.1f)] public float hiddenSoundMinDelay = 5f;
    [Min(0.1f)] public float hiddenSoundMaxDelay = 15f;

    [Header("Radiation Detection")]
    [Range(0f, 1f)] public float radiationDetectionChance = 0.5f;
    [Min(0.1f)] public float radiationFeedbackDuration = 10f;
    public Renderer radiationFeedbackRenderer;
    [Min(0)] public int radiationFeedbackMaterialIndex;
    public Material baseRadiationMaterial;
    public Material detectedRadiationMaterial;
    public ParticleSystem radiationFeedbackParticles;
    public Color baseRadiationParticleColor = new Color32(0xA6, 0xCA, 0xD9, 0xFF);
    public Color detectedRadiationParticleColor = new Color32(0xA6, 0xD9, 0xA7, 0xFF);
    [Min(0.01f)] public float radiationParticleBlendDuration = 0.5f;
    public SoundParameters radiationDetectedSound;

    [Header("Detector References")]
    public AbilityGhostTrigger ghostTrigger;
    [Min(0.1f)] public float ghostTriggerRadius = 0.85f;
    public RadiationDetector radiationDetector;
    public ArticlesDetector articlesDetector;
    public SoundParameters ghostTriggeredSound;
    public SoundParameters foundSound;

    [Header("Movables")]
    [FormerlySerializedAs("movableAutoOpenDistance")]
    [Min(0.1f)] public float fallbackMovableAutoOpenDistance = 1.5f;

    [Header("Hidden Collider")]
    [Min(0.01f)] public float hiddenCapsuleRadius = 0.2f;
    [Min(0.01f)] public float hiddenCapsuleHeight = 0.4f;
    public Vector3 hiddenCapsuleCenter = new Vector3(0f, 0.2f, 0f);

    private HidingSpot _currentHidingSpot;
    private HidingSpot _targetHidingSpot;
    private Vector3 _targetApproachPosition;
    private readonly List<MovableObject> _openedMovables = new List<MovableObject>();
    private bool _isHidingOnSpot;
    private bool _isSnappingToSpot;
    private float _radiationFeedbackEndTime;
    private SoundManager.SoundInstance _radiationDetectedSoundInstance;
    private Material[] _baseSharedMaterials;
    private Tween _radiationParticleColorTween;
    private Color _currentRadiationParticleColor;
    private Collider _ghostTriggerCollider;
    private Vector3 _baseColliderCenter;
    private float _baseCapsuleRadius;
    private float _baseCapsuleHeight;
    private float _baseSphereRadius;
    private Vector3 _baseBoxSize;
    private bool _cachedMainColliderState;
    private const string HIDDEN_SOUND_INVOKE = "SpmzHideAndSeek.HiddenSound";

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();

        CacheBaseMaterials();
        CacheGhostTriggerCollider();
        _currentRadiationParticleColor = baseRadiationParticleColor;
        SetRadiationParticleColor(_currentRadiationParticleColor);

        if (ghostTrigger != null)
        {
            ghostTrigger.linkedSpirimonz = this;
            ghostTrigger.onGhostTriggered.RemoveListener(OnGhostTriggered);
            ghostTrigger.onGhostTriggered.AddListener(OnGhostTriggered);
            ghostTrigger.enabled = false;
            if (_ghostTriggerCollider != null)
                _ghostTriggerCollider.enabled = false;
        }

        if (radiationDetector != null)
        {
            radiationDetector.linkedSpirimonz = this;
            radiationDetector.enabled = false;
            radiationDetector.EndDetection();
        }

        if (articlesDetector != null)
        {
            articlesDetector.linkedSpirimonz = this;
            articlesDetector.SetDetectionEnabled(false);
        }

        SetHidingState(false);
        SetEscapeSystemsEnabled(false);
        InteractionLocked = true;
    }

    public override void ActionOnEnabled()
    {
        base.ActionOnEnabled();
        IgnorePlayerCollisions();
        ApplyRadiationFeedback(Time.time < _radiationFeedbackEndTime);
        SetEscapeSystemsEnabled(ShouldUseEscapeSystems());
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        IgnorePlayerCollisions();
        BeginEscapeToNextSpot(_currentHidingSpot);
    }

    public override bool GoBackToHands(Transform handPos)
    {
        bool success = base.GoBackToHands(handPos);
        if (!success)
            return false;

        transform.DOKill();
        _currentHidingSpot = null;
        _targetHidingSpot = null;
        _targetApproachPosition = Vector3.zero;
        _isSnappingToSpot = false;
        _isHidingOnSpot = false;
        CloseOpenedMovables();
        SetHidingColliderState(false);
        SetEscapeSystemsEnabled(false);
        CancelHiddenSoundLoop();
        ApplyRadiationFeedback(false);
        return true;
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        bool canContinue = base.UpdateSpirimonzBehaviour();
        if (!canContinue)
            return false;

        if (_radiationFeedbackEndTime > 0f && Time.time >= _radiationFeedbackEndTime)
        {
            _radiationFeedbackEndTime = 0f;
            ApplyRadiationFeedback(false);
        }

        return true;
    }

    protected override void UpdateMovementBehaviour()
    {
        if (IsInHidingMode())
        {
            if (agent != null)
                agent.speed = 0f;
            return;
        }

        if (_isSnappingToSpot)
        {
            if (agent != null)
                agent.speed = 0f;
            return;
        }

        if (_isHidingOnSpot)
        {
            if (agent != null)
                agent.speed = 0f;
            return;
        }

        if (!isOnTheMap || agent == null)
            return;

        if (_currentBehaviour == SpirimonzBehaviourState.Escape)
        {
            UpdateEscapeBehaviour();
            return;
        }

        base.UpdateMovementBehaviour();
    }

    public override void InteractionStarted()
    {
        onInteract?.Invoke();
        HandleLookAtPlayerOnInteract();

        if (!_isHidingOnSpot)
            return;

        foundSound?.PlaySound(transform.position);
        StartExitHideSequence();
    }

    protected override void OnHuntStart()
    {
        base.OnHuntStart();
        SetEscapeSystemsEnabled(false);
    }

    protected override void OnHuntEnd()
    {
        base.OnHuntEnd();
        SetEscapeSystemsEnabled(ShouldUseEscapeSystems());
    }

    protected override void OnDisable()
    {
        SetEscapeSystemsEnabled(false);
        CloseOpenedMovables();
        CancelHiddenSoundLoop();
        ApplyRadiationFeedback(false);
        KillRadiationParticleColorTween();
        SetRadiationParticleColor(baseRadiationParticleColor);
        base.OnDisable();
    }

    private void UpdateEscapeBehaviour()
    {
        if (_targetHidingSpot == null)
        {
            BeginEscapeToNextSpot(_currentHidingSpot);
            return;
        }

        Vector3 targetPosition = _targetHidingSpot.GetWorldPosition();
        Vector3 approachPosition = _targetApproachPosition;
        float distance = Vector3.Distance(transform.position, approachPosition);

        float movableDistance = HasLinkedMovables(_targetHidingSpot)
            ? _targetHidingSpot.movableInteractionDistance
            : fallbackMovableAutoOpenDistance;

        bool shouldOpenMovables = HasLinkedMovables(_targetHidingSpot) && distance <= movableDistance;
        if (shouldOpenMovables)
            OpenMovables(_targetHidingSpot);
        else
            CloseOpenedMovables();

        if (distance <= hidingSpotArrivalDistance)
        {
            StartHideSequence();
            return;
        }

        agent.enabled = true;
        agent.speed = escapingSpeed;
        agent.SetDestination(approachPosition);
    }

    private void BeginEscapeToNextSpot(HidingSpot excludedSpot)
    {
        HidingSpot nextSpot = ChooseNextHidingSpot(excludedSpot);
        if (nextSpot == null)
        {
            ChangeBehaviour(SpirimonzBehaviourState.Wait);
            InteractionLocked = false;
            SetHidingState(true);
            return;
        }

        transform.DOKill();
        _currentHidingSpot = null;
        _targetHidingSpot = nextSpot;
        _targetApproachPosition = ResolveApproachPosition(nextSpot);
        _isSnappingToSpot = false;
        SetHidingColliderState(false);
        SetHidingState(false);
        CloseOpenedMovables();

        if (!agent.enabled)
            agent.enabled = true;

        ChangeBehaviour(SpirimonzBehaviourState.Escape);
        InteractionLocked = true;
        SetEscapeSystemsEnabled(true);
    }

    private HidingSpot ChooseNextHidingSpot(HidingSpot excludedSpot)
    {
        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.hidingSpots == null || _house.hidingSpots.Count == 0)
            return null;

        Player player = _house.currentPlayer;
        if (player == null)
            return null;

        List<HidingSpot> validSpots = new List<HidingSpot>();
        for (int i = 0; i < _house.hidingSpots.Count; i++)
        {
            HidingSpot spot = _house.hidingSpots[i];
            if (spot == null || spot == excludedSpot)
                continue;

            if (!IsHidingSpotReachable(spot, out _))
                continue;

            validSpots.Add(spot);
        }

        if (validSpots.Count == 0)
        {
            if (excludedSpot != null && IsHidingSpotReachable(excludedSpot, out _))
                return excludedSpot;

            if (_house != null)
                Debug.LogWarning($"{name}: no valid HidingSpot found in '{_house.name}'.", this);
            else
                Debug.LogWarning($"{name}: no valid HidingSpot found.", this);

            return null;
        }

        validSpots.Sort((a, b) =>
            GetPlayerTravelDistance(player, a).CompareTo(GetPlayerTravelDistance(player, b)));

        int spotsToSkip = Mathf.FloorToInt(validSpots.Count * Mathf.Clamp01(closestSpotsToAvoidRatio));
        spotsToSkip = Mathf.Clamp(spotsToSkip, 0, Mathf.Max(0, validSpots.Count - minFarSpotsCount));

        int selectableCount = Mathf.Max(1, validSpots.Count - spotsToSkip);
        int selectedIndex = Random.Range(spotsToSkip, spotsToSkip + selectableCount);
        return validSpots[Mathf.Clamp(selectedIndex, 0, validSpots.Count - 1)];
    }

    private bool IsHidingSpotReachable(HidingSpot hidingSpot, out Vector3 approachPosition)
    {
        approachPosition = Vector3.zero;

        if (hidingSpot == null)
            return false;

        if (IsHidingSpotTilted(hidingSpot))
            return false;

        Vector3 targetPosition = hidingSpot.GetApproachPosition();
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
        {
            Debug.LogError($"{name}: HidingSpot '{hidingSpot.name}' is not reachable from the NavMesh.", hidingSpot);
            return false;
        }

        float offset = Vector3.Distance(targetPosition, hit.position);
        if (offset > maxAllowedNavMeshOffset)
        {
            Debug.LogError(
                $"{name}: HidingSpot '{hidingSpot.name}' is too far from the NavMesh ({offset:0.00}m).",
                hidingSpot
            );
            return false;
        }

        approachPosition = hit.position;
        return true;
    }

    private bool IsHidingSpotTilted(HidingSpot hidingSpot)
    {
        Quaternion worldRotation = hidingSpot.GetWorldRotation();
        Vector3 euler = worldRotation.eulerAngles;
        float xTilt = Mathf.DeltaAngle(0f, euler.x);
        float zTilt = Mathf.DeltaAngle(0f, euler.z);
        float maxTilt = Mathf.Max(0f, hidingSpotTiltPermissiveness);

        bool isTilted = Mathf.Abs(xTilt) > maxTilt || Mathf.Abs(zTilt) > maxTilt;
        if (isTilted)
        {
            Debug.LogWarning(
                $"{name}: HidingSpot '{hidingSpot.name}' ignored because it is tilted (X:{xTilt:0.0} / Z:{zTilt:0.0}).",
                hidingSpot
            );
        }

        return isTilted;
    }

    private float GetPlayerTravelDistance(Player player, HidingSpot hidingSpot)
    {
        if (player == null || hidingSpot == null)
            return float.MaxValue;

        Vector3 targetPosition = hidingSpot.GetApproachPosition();
        if (IsHidingSpotReachable(hidingSpot, out Vector3 approachPosition))
            targetPosition = approachPosition;

        float pathDistance = PathDistance(player.transform.position, targetPosition, navMeshSampleMaxDistance);
        if (pathDistance >= 0f)
            return pathDistance;

        return Vector3.Distance(player.transform.position, targetPosition);
    }

    private Vector3 ResolveApproachPosition(HidingSpot hidingSpot)
    {
        if (IsHidingSpotReachable(hidingSpot, out Vector3 approachPosition))
            return approachPosition;

        return hidingSpot != null ? hidingSpot.GetApproachPosition() : transform.position;
    }

    private void StartHideSequence()
    {
        if (_isSnappingToSpot || _targetHidingSpot == null)
            return;

        StartCoroutine(HideAtSpotRoutine(_targetHidingSpot));
    }

    private void StartExitHideSequence()
    {
        if (_isSnappingToSpot)
            return;

        StartCoroutine(ExitHideRoutine(_currentHidingSpot));
    }

    private IEnumerator HideAtSpotRoutine(HidingSpot spot)
    {
        _isSnappingToSpot = true;
        SetEscapeSystemsEnabled(false);
        SetHidingColliderState(true);

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        Vector3 targetPosition = spot.GetWorldPosition();
        Quaternion targetRotation = spot.GetWorldRotation();

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOMove(targetPosition, hideSnapDuration).SetEase(hideSnapEase));
        sequence.Join(transform.DORotateQuaternion(targetRotation, hideSnapDuration).SetEase(hideSnapEase));
        yield return sequence.WaitForCompletion();

        _currentHidingSpot = spot;
        _targetHidingSpot = null;
        _targetApproachPosition = Vector3.zero;
        ChangeBehaviour(SpirimonzBehaviourState.Wait);
        SetHidingState(true);
        CloseOpenedMovables();
        _isSnappingToSpot = false;
        InteractionLocked = false;
    }

    private IEnumerator ExitHideRoutine(HidingSpot spot)
    {
        _isSnappingToSpot = true;
        InteractionLocked = true;
        SetHidingState(false);
        SetHidingColliderState(true);
        CloseOpenedMovables();

        transform.DOKill();

        if (spot != null)
        {
            Vector3 exitPosition = spot.GetAnchorPosition();
            Quaternion exitRotation = spot.GetAnchorRotation();

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMove(exitPosition, hideSnapDuration).SetEase(hideSnapEase));
            sequence.Join(transform.DORotateQuaternion(exitRotation, hideSnapDuration).SetEase(hideSnapEase));
            yield return sequence.WaitForCompletion();
        }

        SetHidingColliderState(false);
        _isSnappingToSpot = false;
        BeginEscapeToNextSpot(spot);
    }

    private void SetHidingState(bool hiding)
    {
        _isHidingOnSpot = hiding;
        canInteract = hiding;
        SetHidingColliderState(hiding);
        UpdateHiddenSoundLoop();

        if (animator != null)
            animator.SetBool("Hiding", hiding);
    }

    private void UpdateHiddenSoundLoop()
    {
        CancelHiddenSoundLoop();

        if (!_isHidingOnSpot || !isOnTheMap || hiddenLoopSound == null)
            return;

        float minDelay = Mathf.Max(0.1f, hiddenSoundMinDelay);
        float maxDelay = Mathf.Max(minDelay, hiddenSoundMaxDelay);
        float delay = Random.Range(minDelay, maxDelay);
        this.Invoke(HIDDEN_SOUND_INVOKE, delay, PlayHiddenSound);
    }

    private void CancelHiddenSoundLoop()
    {
        CancelInvoke(HIDDEN_SOUND_INVOKE);
    }

    private void PlayHiddenSound()
    {
        if (_isHidingOnSpot && isOnTheMap && hiddenLoopSound != null)
            hiddenLoopSound.PlaySound(transform.position);

        UpdateHiddenSoundLoop();
    }

    private void SetEscapeSystemsEnabled(bool enable)
    {
        if (ghostTrigger != null)
        {
            ghostTrigger.enabled = enable;
            if (_ghostTriggerCollider != null)
                _ghostTriggerCollider.enabled = enable;
        }

        if (radiationDetector != null)
        {
            radiationDetector.enabled = enable;
            if (!enable)
                radiationDetector.EndDetection();
        }

        if (articlesDetector != null)
            articlesDetector.SetDetectionEnabled(enable);
    }

    private bool ShouldUseEscapeSystems()
    {
        return isOnTheMap &&
               _currentBehaviour == SpirimonzBehaviourState.Escape &&
               !_isHidingOnSpot &&
               !_isSnappingToSpot &&
               !IsInHidingMode();
    }

    private void OnGhostTriggered()
    {
        if (!ShouldUseEscapeSystems())
            return;

        ghostTriggeredSound?.PlaySound(transform.position);

        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.currentGhost == null || _house.currentGhost.ghostParameters == null)
            return;

        if (!_house.currentGhost.ghostParameters.Radioactivity)
            return;

        if (Random.value > radiationDetectionChance)
            return;

        _radiationFeedbackEndTime = Time.time + radiationFeedbackDuration;
        ApplyRadiationFeedback(true);
        PlayRadiationDetectedSound();
    }

    private void CacheBaseMaterials()
    {
        if (radiationFeedbackRenderer == null)
            return;

        _baseSharedMaterials = radiationFeedbackRenderer.sharedMaterials;
        if ((baseRadiationMaterial == null || baseRadiationMaterial == detectedRadiationMaterial) &&
            _baseSharedMaterials != null &&
            radiationFeedbackMaterialIndex >= 0 &&
            radiationFeedbackMaterialIndex < _baseSharedMaterials.Length)
        {
            baseRadiationMaterial = _baseSharedMaterials[radiationFeedbackMaterialIndex];
        }
    }

    private void SetHidingColliderState(bool hiding)
    {
        if (collider == null)
            return;

        CacheMainColliderState();

        if (collider is CapsuleCollider capsule)
        {
            capsule.center = hiding ? hiddenCapsuleCenter : _baseColliderCenter;
            capsule.radius = hiding ? hiddenCapsuleRadius : _baseCapsuleRadius;
            capsule.height = hiding ? hiddenCapsuleHeight : _baseCapsuleHeight;
            return;
        }

        if (collider is SphereCollider sphere)
        {
            sphere.center = hiding ? hiddenCapsuleCenter : _baseColliderCenter;
            sphere.radius = hiding ? hiddenCapsuleRadius : _baseSphereRadius;
            return;
        }

        if (collider is BoxCollider box)
        {
            box.center = hiding ? hiddenCapsuleCenter : _baseColliderCenter;
            box.size = hiding
                ? new Vector3(hiddenCapsuleRadius * 2f, hiddenCapsuleHeight, hiddenCapsuleRadius * 2f)
                : _baseBoxSize;
        }
    }

    private void CacheMainColliderState()
    {
        if (_cachedMainColliderState || collider == null)
            return;

        if (collider is CapsuleCollider capsule)
        {
            _baseColliderCenter = capsule.center;
            _baseCapsuleRadius = capsule.radius;
            _baseCapsuleHeight = capsule.height;
            _cachedMainColliderState = true;
            return;
        }

        if (collider is SphereCollider sphere)
        {
            _baseColliderCenter = sphere.center;
            _baseSphereRadius = sphere.radius;
            _cachedMainColliderState = true;
            return;
        }

        if (collider is BoxCollider box)
        {
            _baseColliderCenter = box.center;
            _baseBoxSize = box.size;
            _cachedMainColliderState = true;
        }
    }

    private void CacheGhostTriggerCollider()
    {
        if (ghostTrigger == null)
            return;

        Collider[] colliders = ghostTrigger.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (candidate != null && candidate.isTrigger)
            {
                _ghostTriggerCollider = candidate;
                return;
            }
        }

        SphereCollider sphereCollider = ghostTrigger.gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = ghostTriggerRadius;
        sphereCollider.center = Vector3.zero;
        _ghostTriggerCollider = sphereCollider;
    }

    private void ApplyRadiationFeedback(bool active)
    {
        if (animator != null)
            animator.SetBool("Radioactivity", active);

        ApplyRadiationParticleFeedback(active);

        if (!active)
            StopRadiationDetectedSound();

        if (radiationFeedbackRenderer == null || detectedRadiationMaterial == null)
            return;

        Material[] materials = radiationFeedbackRenderer.materials;
        if (radiationFeedbackMaterialIndex < 0 || radiationFeedbackMaterialIndex >= materials.Length)
            return;

        Material baseMaterial = baseRadiationMaterial != null ? baseRadiationMaterial : materials[radiationFeedbackMaterialIndex];
        materials[radiationFeedbackMaterialIndex] = active ? detectedRadiationMaterial : baseMaterial;
        radiationFeedbackRenderer.materials = materials;
    }

    private void ApplyRadiationParticleFeedback(bool active)
    {
        if (radiationFeedbackParticles == null)
            return;

        Color targetColor = active ? detectedRadiationParticleColor : baseRadiationParticleColor;
        KillRadiationParticleColorTween();

        _radiationParticleColorTween = DOTween.To(
                () => _currentRadiationParticleColor,
                color =>
                {
                    _currentRadiationParticleColor = color;
                    SetRadiationParticleColor(color);
                },
                targetColor,
                Mathf.Max(0.01f, radiationParticleBlendDuration)
            )
            .SetEase(Ease.Linear)
            .SetLink(gameObject);
    }

    private void SetRadiationParticleColor(Color color)
    {
        if (radiationFeedbackParticles == null)
            return;

        ParticleSystem.MainModule main = radiationFeedbackParticles.main;
        main.startColor = color;
    }

    private void KillRadiationParticleColorTween()
    {
        if (_radiationParticleColorTween == null)
            return;

        if (_radiationParticleColorTween.IsActive())
            _radiationParticleColorTween.Kill();

        _radiationParticleColorTween = null;
    }

    private void PlayRadiationDetectedSound()
    {
        if (radiationDetectedSound == null)
            return;

        if (_radiationDetectedSoundInstance != null && _radiationDetectedSoundInstance.IsPlaying)
            return;

        _radiationDetectedSoundInstance = radiationDetectedSound.PlayManagedSound(transform.position);
    }

    private void StopRadiationDetectedSound()
    {
        if (_radiationDetectedSoundInstance == null)
            return;

        _radiationDetectedSoundInstance.Stop(false);
        _radiationDetectedSoundInstance = null;
    }

    private void OpenMovable(MovableObject movable)
    {
        if (movable == null)
            return;

        movable.SetActivatedState(true);
        if (!_openedMovables.Contains(movable))
            _openedMovables.Add(movable);
    }

    private void OpenMovables(HidingSpot hidingSpot)
    {
        if (hidingSpot == null || hidingSpot.linkedMovables == null)
            return;

        for (int i = 0; i < hidingSpot.linkedMovables.Length; i++)
        {
            MovableObject movable = hidingSpot.linkedMovables[i];
            if (movable == null)
                continue;

            OpenMovable(movable);
        }
    }

    private void CloseOpenedMovables()
    {
        if (_openedMovables.Count == 0)
            return;

        for (int i = 0; i < _openedMovables.Count; i++)
        {
            MovableObject movable = _openedMovables[i];
            if (movable == null)
                continue;

            movable.SetActivatedState(false);
        }

        _openedMovables.Clear();
    }

    private bool HasLinkedMovables(HidingSpot hidingSpot)
    {
        if (hidingSpot == null || hidingSpot.linkedMovables == null)
            return false;

        for (int i = 0; i < hidingSpot.linkedMovables.Length; i++)
        {
            if (hidingSpot.linkedMovables[i] != null)
                return true;
        }

        return false;
    }

    private void IgnorePlayerCollisions()
    {
        Player player = Player.Instance;
        if (player == null || collider == null)
            return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null)
                continue;

            Physics.IgnoreCollision(collider, playerCollider, true);
        }
    }
}
