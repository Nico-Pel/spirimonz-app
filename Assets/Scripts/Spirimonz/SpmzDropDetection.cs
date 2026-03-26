using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Spirimonz))]
public class SpmzDropDetection : MonoBehaviour
{
    [Header("Spirimonz")]
    public Spirimonz spirimonz;

    [Header("Detection Settings")]
    public float detectionRange = 5f;
    public Transform detectorSourceTransform;
    public List<ActivitySource> activitySources = new List<ActivitySource>();

    [Header("Drop Settings")]
    public GameObject[] itemsToDrop = new GameObject[5];
    public float spawnForwardOffset = 0.4f;
    public float spawnUpOffset = 0.2f;
    public float dropImpulseForce = 1.2f;
    public float dropTorqueForce = 0.6f;
    public int maxDrops = 20;

    private ActivitySource _currentActivitySourceDetected;
    private int _currentDetectedValue;
    private int _dropsCount;

    private void Awake()
    {
        if (spirimonz == null)
            spirimonz = GetComponent<Spirimonz>();
    }

    private void Start()
    {
        activitySources.AddRange(FindObjectsOfType<ActivitySource>());
        House.Instance.onNewActivitySourceAddedToGame.AddListener(AddNewActivitySourceToList);
    }

    private void AddNewActivitySourceToList(ActivitySource newSource)
    {
        if(activitySources.Contains(newSource) == false)
            activitySources.Add(newSource);
    }

    private void Update()
    {
        if (spirimonz == null || spirimonz.IsLocked())
            return;

        if (ShouldSkipDetection())
        {
            ResetDetection();
            return;
        }

        ActivitySource bestSource = FindBestSourceInRange();

        if (bestSource == null || bestSource.activityValue <= 0)
        {
            ResetDetection();
            return;
        }

        if (bestSource != _currentActivitySourceDetected || bestSource.activityValue != _currentDetectedValue)
        {
            HandleDetection(bestSource);
        }
    }

    private bool ShouldSkipDetection()
    {
        if (spirimonz.IsInHidingMode())
            return true;

        if (spirimonz.isOnTheMap == false && spirimonz.powerActiveInHands == false)
            return true;

        return false;
    }

    private void ResetDetection()
    {
        _currentActivitySourceDetected = null;
        _currentDetectedValue = 0;
    }

    private ActivitySource FindBestSourceInRange()
    {
        Transform sourceTransform = detectorSourceTransform != null ? detectorSourceTransform : transform;

        ActivitySource bestSource = null;
        int bestValue = 0;
        float bestDistance = float.MaxValue;

        foreach (ActivitySource activitySource in activitySources)
        {
            if (activitySource == null) continue;

            int value = activitySource.activityValue;
            if (value <= 0) continue;

            float dist = Vector3.Distance(sourceTransform.position, activitySource.transform.position);
            if (dist > detectionRange) continue;

            if (bestSource == null || value > bestValue || (value == bestValue && dist < bestDistance))
            {
                bestSource = activitySource;
                bestValue = value;
                bestDistance = dist;
            }
        }

        return bestSource;
    }

    private void HandleDetection(ActivitySource activitySource)
    {
        _currentActivitySourceDetected = activitySource;
        _currentDetectedValue = activitySource.activityValue;

        DropItemForActivity(_currentDetectedValue);

        if (spirimonz != null && spirimonz.animator != null)
        {
            spirimonz.animator.SetTrigger("Detection");
        }
    }

    private void DropItemForActivity(int activityValue)
    {
        if (maxDrops > 0 && _dropsCount >= maxDrops)
            return;

        if (itemsToDrop == null || itemsToDrop.Length == 0)
            return;

        if (activityValue <= 0)
            return;

        int index = activityValue - 1;
        if (index < 0 || index >= itemsToDrop.Length)
        {
            Debug.LogWarning($"{name}: itemsToDrop ne contient pas d'index {index} (activityValue={activityValue}).");
            return;
        }

        GameObject prefab = itemsToDrop[index];
        if (prefab == null)
            return;

        Vector3 spawnPos = transform.position
                           + transform.forward * spawnForwardOffset
                           + Vector3.up * spawnUpOffset;

        Vector3 forceDir = (transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 force = forceDir * dropImpulseForce;
        Vector3 torque = Random.onUnitSphere * dropTorqueForce;
        Transform parent = House.Instance != null ? House.Instance.transform : null;
        GameObject spawned = SpmzDropUtility.SpawnDrop(prefab, spawnPos, Quaternion.identity, parent, force, torque);
        if (spawned != null)
            _dropsCount++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = detectorSourceTransform != null ? detectorSourceTransform.position : transform.position;
        Gizmos.DrawWireSphere(center, detectionRange);
    }
}
