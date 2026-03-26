using UnityEngine;

public class SpmzRoamDropper : Spirimonz
{
    [Header("Drop Settings")]
    public GameObject[] itemsToDrop = new GameObject[5];
    public float dropIntervalMin = 3f;
    public float dropIntervalMax = 8f;
    public float dropDelay = 0.3f;
    public string dropAnimationTrigger = "Drop";
    public bool avoidRepeat = true;
    public int maxRepeatRerolls = 5;
    public int maxDrops = 20;
    public float spawnForwardOffset = 0.4f;
    public float spawnUpOffset = 0.2f;
    public float dropImpulseForce = 1.2f;
    public float dropTorqueForce = 0.6f;

    private float _nextDropTime;
    private int _lastDropIndex = -1;
    private bool _pendingDrop;
    private int _dropsCount;

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        SetRoamRoom(null);
        ScheduleNextDrop();
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        if (IsInHidingMode() || !isOnTheMap)
            return true;

        if (!_pendingDrop && Time.time >= _nextDropTime)
            StartDropSequence();

        return true;
    }

    private void StartDropSequence()
    {
        if (maxDrops > 0 && _dropsCount >= maxDrops)
            return;

        _pendingDrop = true;

        if (animator != null && !string.IsNullOrEmpty(dropAnimationTrigger))
        {
            animator.SetTrigger(dropAnimationTrigger);
        }

        if (dropDelay <= 0f)
        {
            PerformDrop();
        }
        else
        {
            this.Invoke(dropDelay, PerformDrop);
        }
    }

    private void PerformDrop()
    {
        _pendingDrop = false;

        if (IsInHidingMode() || !isOnTheMap)
        {
            ScheduleNextDrop();
            return;
        }

        DropRandomItem();
        ScheduleNextDrop();
    }

    private void OnDisable()
    {
        _pendingDrop = false;
        CancelInvoke(nameof(PerformDrop));
    }

    private void ScheduleNextDrop()
    {
        float min = Mathf.Max(0f, dropIntervalMin);
        float max = Mathf.Max(min, dropIntervalMax);
        _nextDropTime = Time.time + Random.Range(min, max);
    }

    private void DropRandomItem()
    {
        if (maxDrops > 0 && _dropsCount >= maxDrops)
            return;

        if (itemsToDrop == null || itemsToDrop.Length == 0)
            return;

        int index = Random.Range(0, itemsToDrop.Length);
        if (avoidRepeat && itemsToDrop.Length > 1)
        {
            int tries = 0;
            while (index == _lastDropIndex && tries < maxRepeatRerolls)
            {
                index = Random.Range(0, itemsToDrop.Length);
                tries++;
            }
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

        _lastDropIndex = index;
    }
}
