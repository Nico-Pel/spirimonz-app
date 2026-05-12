using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVRevealer : GameBehaviour
{
    private const string ACTIVE_DURATION_INVOKE = "UVRevealer.ActiveDuration";

    [Header("Reveal")]
    public float range = 3f;
    public float chargeSpeed = 0.25f;
    public Transform source;
    public float optionalActivationDuration = 3f;

    [Header("Sound")]
    public SoundParameters activeLoopSound;

    [Header("Ghost Prints")]
    public bool makeGhostLeavePrintsWhenDetected = false;
    public GameObject ghostPrintPrefab;
    public float ghostPrintTrailDuration = 3f;
    public float ghostPrintLifeDuration = 12f;
    public float ghostPrintStepDistance = 0.6f;
    public float ghostPrintLateralOffset = 0.15f;
    public float ghostPrintRaycastHeight = 1f;
    public float ghostPrintRaycastDistance = 4f;
    public float ghostPrintSurfaceOffset = 0.01f;
    public float minGroundSurfaceExtent = 0.2f;
    [Range(0f, 1f)] public float minGroundDot = 0.75f;
    public LayerMask ghostPrintGroundLayers = ~0;

    private List<PrintSource> _printSources;
    private House _house;
    private Coroutine _ghostTrailCoroutine;
    private bool _nextPrintOnRight = true;
    private SoundManager.SoundInstance _activeLoopSoundInstance;

    private void Start()
    {
        _printSources = new List<PrintSource>(
            FindObjectsOfType<PrintSource>()
        );

        _house = House.Instance;
        if (_house == null)
            return;

        foreach (PrintSource ps in _house.printSourcesAddedToGame)
        {
            if(_printSources.Contains(ps) == false)
                _printSources.Add(ps);
        }
        
        _house.onNewPrintSourceAddedToGame.AddListener(AddNewPrintSourceToList);
    }

    private void AddNewPrintSourceToList(PrintSource newSource)
    {
        if(_printSources.Contains(newSource) == false)
            _printSources.Add(newSource);
    }

    private void Update()
    {
        Transform revealSource = source != null ? source : transform;

        foreach (PrintSource ps in _printSources)
        {
            if (ps == null) continue;
            
            float dist = Vector3.Distance(revealSource.position, ps.transform.position);
            if (dist <= range)
            {
                ps.ChargingColor(chargeSpeed * Time.deltaTime);
            }
        }

        TryTriggerGhostPrintTrail();
    }

    private void OnEnable()
    {
        PlayLoopSound();
    }

    private void OnDisable()
    {
        if (_ghostTrailCoroutine != null)
        {
            StopCoroutine(_ghostTrailCoroutine);
            _ghostTrailCoroutine = null;
        }

        StopLoopSound();
    }

    private void TryTriggerGhostPrintTrail()
    {
        if (!makeGhostLeavePrintsWhenDetected || ghostPrintPrefab == null || _ghostTrailCoroutine != null)
            return;

        Ghost ghost = _house != null ? _house.currentGhost : null;
        if (ghost == null)
            return;

        Transform revealSource = source != null ? source : transform;
        if (Vector3.Distance(revealSource.position, ghost.transform.position) > range)
            return;

        _ghostTrailCoroutine = StartCoroutine(SpawnGhostTrailRoutine(ghost));
    }

    private IEnumerator SpawnGhostTrailRoutine(Ghost ghost)
    {
        float endTime = Time.time + Mathf.Max(0.1f, ghostPrintTrailDuration);
        Vector3 lastPrintPosition = ghost.transform.position;
        bool firstPrint = true;
        _nextPrintOnRight = true;

        while (ghost != null && Time.time <= endTime)
        {
            Vector3 currentPosition = ghost.transform.position;
            if (firstPrint || Vector3.Distance(lastPrintPosition, currentPosition) >= ghostPrintStepDistance)
            {
                if (TrySpawnGhostPrint(ghost, currentPosition))
                {
                    lastPrintPosition = currentPosition;
                    firstPrint = false;
                }
            }

            yield return null;
        }

        _ghostTrailCoroutine = null;
    }

    private bool TrySpawnGhostPrint(Ghost ghost, Vector3 position)
    {
        if (ghost == null || ghostPrintPrefab == null)
            return false;

        Vector3 forward = ghost.transform.forward;
        Vector3 lateral = Vector3.ProjectOnPlane(ghost.transform.right, Vector3.up).normalized;
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.Cross(Vector3.up, forward).normalized;

        float side = _nextPrintOnRight ? 1f : -1f;
        Vector3 spawnPosition = position + lateral * (ghostPrintLateralOffset * side);
        Vector3 rayOrigin = spawnPosition + Vector3.up * ghostPrintRaycastHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, ghostPrintRaycastDistance, ghostPrintGroundLayers, QueryTriggerInteraction.Ignore))
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
        GameObject printObject = Instantiate(
            ghostPrintPrefab,
            hit.point + hit.normal * ghostPrintSurfaceOffset,
            rotation,
            House.Instance != null ? House.Instance.transform : null);

        if (printObject != null)
        {
            Vector3 localScale = printObject.transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (_nextPrintOnRight ? 1f : -1f);
            printObject.transform.localScale = localScale;
        }

        PrintSource printSource = printObject.GetComponentInChildren<PrintSource>();
        if (printSource == null || printSource.spriteRenderer == null || printSource.spriteRenderer.sprite == null)
        {
            if (printObject != null)
                Destroy(printObject);
            return false;
        }

        House.Instance?.DeclareNewPrintSource(printSource);
        printSource.Activate(ghostPrintLifeDuration, printSource.spriteRenderer.sprite);
        _nextPrintOnRight = !_nextPrintOnRight;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (source == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(source.position, range);
    }

    public void ActiveDuration()
    {
        if (enabled) return;
        
        enabled = true;
        Invoke(ACTIVE_DURATION_INVOKE, optionalActivationDuration, () => enabled = false);
    }

    private void PlayLoopSound()
    {
        if (activeLoopSound == null)
            return;

        if (_activeLoopSoundInstance != null && _activeLoopSoundInstance.IsPlaying)
            return;

        _activeLoopSoundInstance = activeLoopSound.PlayManagedSound(transform.position);
    }

    private void StopLoopSound()
    {
        if (_activeLoopSoundInstance == null)
            return;

        _activeLoopSoundInstance.Stop(false);
        _activeLoopSoundInstance = null;
    }
}
