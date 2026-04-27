using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[System.Serializable]
public class ActivitySourceUnityEvent : UnityEvent<ActivitySource> { }

public class SpmzActivityGhostSensor : GameBehaviour
{
    [Header("References")]
    public Spirimonz linkedSpirimonz;
    public AbilityGhostTrigger ghostTrigger;
    public Transform detectionSourceTransform;
    public Transform squashTarget;
    public Renderer materialsRenderer;
    public GameObject printPrefab;

    [Header("Activity Detection")]
    [Min(0.1f)] public float detectionRange = 5f;
    [Min(1f)] public float waitingDetectionRangeMultiplier = 1.5f;
    [Min(0.05f)] public float detectionInterval = 0.2f;
    [Min(0f)] public float maxPathRange = 7.5f;
    public List<int> detectionMaterialIndices = new List<int> { 1, 2, 3, 4, 5 };

    [Header("Detection Materials")]
    public Material detectionMat;
    public Material detectionFiveMat;
    [Min(0.05f)] public float blinkInterval = 0.15f;

    [Header("Detection Sounds")]
    public SoundParameters soundParametersDetection;
    public SoundParameters soundParametersDetectionFive;
    public SoundParameters soundParametersSplat;

    [Header("Ghost Stomp")]
    [Min(0.1f)] public float ghostTriggerRadius = 0.85f;
    public Vector3 squashedScale = new Vector3(1.2f, 0.5f, 1.2f);
    [Min(0.01f)] public float squashDuration = 0.12f;

    [Header("Ghost Trail")]
    [Min(0.1f)] public float printTrailDuration = 3f;
    [Range(0f, 1f)] public float huntSlowPercent = 0.5f;
    [Min(0.1f)] public float huntSlowDuration = 3f;
    [Min(0.1f)] public float printLifeDuration = 12f;
    [Min(0.05f)] public float printStepDistance = 0.6f;
    [Min(0f)] public float printLateralOffset = 0.15f;
    [Min(0.1f)] public float printRaycastHeight = 1f;
    [Min(0.1f)] public float printRaycastDistance = 4f;
    [Range(0f, 1f)] public float minGroundDot = 0.75f;
    [Min(0f)] public float minGroundSurfaceExtent = 0.2f;
    [Min(0f)] public float printSurfaceOffset = 0.01f;
    public LayerMask groundLayers = ~0;

    [Header("Events")]
    public ActivitySourceUnityEvent onActivityDetected;

    private readonly List<ActivitySource> _activitySources = new List<ActivitySource>();
    private ActivitySource _currentDetectedSource;
    private Material[] _baseMaterials;
    private Sequence _blinkSequence;
    private Tween _squashTween;
    private Collider _ghostTriggerCollider;
    private Vector3 _baseSquashScale = Vector3.one;
    private float _nextDetectionTime;
    private bool _nextPrintOnRight = true;

    private void Awake()
    {
        if (detectionSourceTransform == null)
            detectionSourceTransform = transform;

        if (squashTarget == null)
            squashTarget = linkedSpirimonz != null ? linkedSpirimonz.transform : transform;

        if (materialsRenderer != null)
            _baseMaterials = materialsRenderer.sharedMaterials;

        if (squashTarget != null)
            _baseSquashScale = squashTarget.localScale;

        RegisterExistingActivitySources();
        RegisterGhostTrigger();
    }

    private void OnEnable()
    {
        if (House.Instance != null)
            House.Instance.onNewActivitySourceAddedToGame.AddListener(AddActivitySource);

        RegisterGhostTrigger();
        _nextDetectionTime = Time.time;
        RefreshDetectionFeedback();
    }

    private void OnDisable()
    {
        if (House.Instance != null)
            House.Instance.onNewActivitySourceAddedToGame.RemoveListener(AddActivitySource);

        if (ghostTrigger != null)
            ghostTrigger.onGhostTriggeredWithGhost.RemoveListener(OnGhostTriggered);

        StopBlinking();

        if (_squashTween != null && _squashTween.IsActive())
            _squashTween.Kill();

        if (squashTarget != null)
            squashTarget.localScale = _baseSquashScale;

        RestoreBaseMaterials();
    }

    private void RegisterExistingActivitySources()
    {
        _activitySources.Clear();

        ActivitySource[] found = FindObjectsByType<ActivitySource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            AddActivitySource(found[i]);

        if (House.Instance != null)
        {
            for (int i = 0; i < House.Instance.activitySourcesAddedToGame.Count; i++)
                AddActivitySource(House.Instance.activitySourcesAddedToGame[i]);
        }
    }

    private void RegisterGhostTrigger()
    {
        if (ghostTrigger == null)
            return;

        ghostTrigger.linkedSpirimonz = linkedSpirimonz;
        CacheGhostTriggerCollider();
        ghostTrigger.onGhostTriggeredWithGhost.RemoveListener(OnGhostTriggered);
        ghostTrigger.onGhostTriggeredWithGhost.AddListener(OnGhostTriggered);
    }

    private void CacheGhostTriggerCollider()
    {
        if (ghostTrigger == null)
            return;

        Collider[] colliders = ghostTrigger.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger)
            {
                _ghostTriggerCollider = colliders[i];
                return;
            }
        }

        SphereCollider sphereCollider = ghostTrigger.gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = ghostTriggerRadius;
        _ghostTriggerCollider = sphereCollider;
    }

    private void AddActivitySource(ActivitySource source)
    {
        if (source == null || _activitySources.Contains(source))
            return;

        _activitySources.Add(source);
    }

    private void Update()
    {
        if (!CanDetect())
        {
            if (_currentDetectedSource != null)
            {
                _currentDetectedSource = null;
                RefreshDetectionFeedback();
            }
            return;
        }

        if (Time.time < _nextDetectionTime)
            return;

        _nextDetectionTime = Time.time + Mathf.Max(0.05f, detectionInterval);
        UpdateDetectedActivity();
    }

    private bool CanDetect()
    {
        if (linkedSpirimonz == null)
            return enabled;

        if (!linkedSpirimonz.gameObject.activeInHierarchy)
            return false;

        if (!linkedSpirimonz.isOnTheMap && !linkedSpirimonz.powerActiveInHands)
            return false;

        if (linkedSpirimonz.spirimonzGameObject != null && !linkedSpirimonz.spirimonzGameObject.activeInHierarchy)
            return false;

        return true;
    }

    private void UpdateDetectedActivity()
    {
        ActivitySource bestSource = null;
        int bestValue = 0;
        float bestDistance = float.MaxValue;
        float range = GetCurrentDetectionRange();
        Vector3 origin = detectionSourceTransform != null ? detectionSourceTransform.position : transform.position;

        for (int i = _activitySources.Count - 1; i >= 0; i--)
        {
            ActivitySource source = _activitySources[i];
            if (source == null)
            {
                _activitySources.RemoveAt(i);
                continue;
            }

            if (source.activityValue <= 0)
                continue;

            float distance = Vector3.Distance(origin, source.transform.position);
            if (distance > range)
                continue;

            if (!IsReachable(source))
                continue;

            if (bestSource == null || source.activityValue > bestValue ||
                (source.activityValue == bestValue && distance < bestDistance))
            {
                bestSource = source;
                bestValue = source.activityValue;
                bestDistance = distance;
            }
        }

        ActivitySource previousSource = _currentDetectedSource;
        int previousValue = previousSource != null ? previousSource.activityValue : 0;
        _currentDetectedSource = bestSource;
        int currentValue = _currentDetectedSource != null ? _currentDetectedSource.activityValue : 0;

        RefreshDetectionFeedback();

        if (_currentDetectedSource != null && (previousSource != bestSource || currentValue != previousValue))
        {
            onActivityDetected?.Invoke(_currentDetectedSource);
            PlayDetectionSound(currentValue);
        }
    }

    private float GetCurrentDetectionRange()
    {
        float range = detectionRange;
        if (linkedSpirimonz != null && linkedSpirimonz.CurrentBehaviour() == Spirimonz.SpirimonzBehaviourState.Wait)
            range *= Mathf.Max(1f, waitingDetectionRangeMultiplier);

        return range;
    }

    private bool IsReachable(ActivitySource source)
    {
        if (linkedSpirimonz == null || !linkedSpirimonz.isOnTheMap || linkedSpirimonz.agent == null || maxPathRange <= 0f)
            return true;

        if (!linkedSpirimonz.agent.isOnNavMesh)
            return true;

        NavMeshPath path = new NavMeshPath();
        if (!linkedSpirimonz.agent.CalculatePath(source.transform.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete || path.corners == null || path.corners.Length == 0)
            return false;

        float totalDistance = 0f;
        Vector3 previous = linkedSpirimonz.transform.position;
        for (int i = 0; i < path.corners.Length; i++)
        {
            totalDistance += Vector3.Distance(previous, path.corners[i]);
            previous = path.corners[i];
        }

        return totalDistance <= maxPathRange;
    }

    private void RefreshDetectionFeedback()
    {
        if (materialsRenderer == null || detectionMaterialIndices == null || detectionMaterialIndices.Count == 0)
            return;

        RestoreBaseMaterials();

        int value = _currentDetectedSource != null ? Mathf.Clamp(_currentDetectedSource.activityValue, 0, detectionMaterialIndices.Count) : 0;
        if (value <= 0)
        {
            StopBlinking();
            return;
        }

        Material materialToUse = value >= 5 ? detectionFiveMat : detectionMat;
        if (materialToUse == null)
            return;

        Material[] materials = materialsRenderer.materials;
        for (int i = 0; i < value && i < detectionMaterialIndices.Count; i++)
        {
            int materialIndex = detectionMaterialIndices[i];
            if (materialIndex < 0 || materialIndex >= materials.Length)
                continue;

            materials[materialIndex] = materialToUse;
        }

        materialsRenderer.materials = materials;

        if (value >= 5)
            StartBlinking();
        else
            StopBlinking();
    }

    private void RestoreBaseMaterials()
    {
        if (materialsRenderer == null || _baseMaterials == null || _baseMaterials.Length == 0)
            return;

        materialsRenderer.materials = (Material[])_baseMaterials.Clone();
    }

    private void StartBlinking()
    {
        StopBlinking();

        if (materialsRenderer == null || detectionFiveMat == null)
            return;

        _blinkSequence = DOTween.Sequence().SetTarget(this).SetLoops(-1, LoopType.Restart);
        _blinkSequence.AppendCallback(() => SetFiveMaterialEmission(true));
        _blinkSequence.AppendInterval(blinkInterval);
        _blinkSequence.AppendCallback(() => SetFiveMaterialEmission(false));
        _blinkSequence.AppendInterval(blinkInterval);
    }

    private void StopBlinking()
    {
        if (_blinkSequence != null && _blinkSequence.IsActive())
            _blinkSequence.Kill();

        _blinkSequence = null;
        SetFiveMaterialEmission(false);
    }

    private void SetFiveMaterialEmission(bool enabled)
    {
        if (materialsRenderer == null)
            return;

        Material[] materials = materialsRenderer.materials;
        for (int i = 0; i < detectionMaterialIndices.Count; i++)
        {
            int materialIndex = detectionMaterialIndices[i];
            if (materialIndex < 0 || materialIndex >= materials.Length)
                continue;

            Material mat = materials[materialIndex];
            if (mat == null || mat != detectionFiveMat)
                continue;

            if (enabled)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", Color.white);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void PlayDetectionSound(int activityValue)
    {
        SoundParameters soundParameters = activityValue >= 5 ? soundParametersDetectionFive : soundParametersDetection;
        if (soundParameters != null)
            soundParameters.PlaySound(transform.position);
    }

    private void OnGhostTriggered(Ghost ghost)
    {
        PlaySquash();

        if (soundParametersSplat != null)
            soundParametersSplat.PlaySound(transform.position);

        if (ghost == null)
            return;

        StartCoroutine(SpawnGhostTrailRoutine(ghost));

        if (ghost.IsHunting(false))
            ghost.ApplyExternalHuntSlow(huntSlowPercent, huntSlowDuration);
    }

    private void PlaySquash()
    {
        if (squashTarget == null)
            return;

        if (_squashTween != null && _squashTween.IsActive())
            _squashTween.Kill();

        squashTarget.localScale = _baseSquashScale;
        _squashTween = squashTarget.DOScale(Vector3.Scale(_baseSquashScale, squashedScale), squashDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    private IEnumerator SpawnGhostTrailRoutine(Ghost ghost)
    {
        if (ghost == null || printPrefab == null)
            yield break;

        float endTime = Time.time + Mathf.Max(0.1f, printTrailDuration);
        Vector3 lastPrintPosition = ghost.transform.position;
        bool firstPrint = true;
        _nextPrintOnRight = true;

        while (ghost != null && Time.time <= endTime)
        {
            Vector3 currentPosition = ghost.transform.position;
            if (firstPrint || Vector3.Distance(lastPrintPosition, currentPosition) >= printStepDistance)
            {
                if (TrySpawnPrint(ghost, currentPosition))
                {
                    lastPrintPosition = currentPosition;
                    firstPrint = false;
                }
            }

            yield return null;
        }
    }

    private bool TrySpawnPrint(Ghost ghost, Vector3 position)
    {
        Vector3 forward = ghost != null ? ghost.transform.forward : transform.forward;
        Vector3 lateral = ghost != null ? ghost.transform.right : transform.right;
        lateral = Vector3.ProjectOnPlane(lateral, Vector3.up).normalized;
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.Cross(Vector3.up, forward).normalized;

        float side = _nextPrintOnRight ? 1f : -1f;
        Vector3 spawnPosition = position + lateral * (printLateralOffset * side);
        Vector3 rayOrigin = spawnPosition + Vector3.up * printRaycastHeight;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, printRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (Vector3.Dot(hit.normal, Vector3.up) < minGroundDot)
            return false;

        Bounds bounds = hit.collider.bounds;
        float minExtent = Mathf.Min(bounds.extents.x, bounds.extents.z);
        if (minExtent < minGroundSurfaceExtent)
            return false;

        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, hit.normal).normalized;
        if (projectedForward.sqrMagnitude < 0.0001f)
            projectedForward = Vector3.Cross(hit.normal, Vector3.right).normalized;

        Quaternion rotation = Quaternion.LookRotation(hit.normal, projectedForward);
        GameObject printObject = Instantiate(printPrefab, hit.point + hit.normal * printSurfaceOffset, rotation, House.Instance != null ? House.Instance.transform : null);

        if (printObject != null)
        {
            Vector3 localScale = printObject.transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (_nextPrintOnRight ? 1f : -1f);
            printObject.transform.localScale = localScale;
        }

        PrintSource printSource = printObject.GetComponentInChildren<PrintSource>();
        if (printSource == null || printSource.spriteRenderer == null || printSource.spriteRenderer.sprite == null)
            return false;

        House.Instance?.DeclareNewPrintSource(printSource);
        printSource.Activate(printLifeDuration, printSource.spriteRenderer.sprite);
        _nextPrintOnRight = !_nextPrintOnRight;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Transform source = detectionSourceTransform != null ? detectionSourceTransform : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(source.position, detectionRange);

        Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.9f);
        Gizmos.DrawWireSphere(source.position, detectionRange * Mathf.Max(1f, waitingDetectionRangeMultiplier));
    }
}
