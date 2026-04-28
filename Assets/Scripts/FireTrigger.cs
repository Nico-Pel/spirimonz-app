using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FireTrigger : MonoBehaviour
{
    [Tooltip("Having a linked flammable object is not an obligation")]
    public FlammableElement linkedFlammableObject;

    public bool canGiveFire = true;
    public bool canCurseFlammables = false;
    public float heldActivationDuration = 0.75f;
    public float heldCandleScanRadiusMultiplier = 1.75f;
    public float heldSpirimonzScanRadiusMultiplier = 3f;
    public float heldSpirimonzForwardScanOffset = 1.25f;
    public float heldSpirimonzNonCandleRange = 3f;
    [Range(-1f, 1f)] public float heldSpirimonzNonCandleMinDot = 0.15f;

    private Collider _triggerCollider;
    private CatchableObject _linkedCatchableObject;
    private Spirimonz _linkedSpirimonz;
    private float _heldActivationUntil;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        _linkedCatchableObject = GetComponentInParent<CatchableObject>();
        _linkedSpirimonz = GetComponentInParent<Spirimonz>();
    }

    private void Update()
    {
        if (!canGiveFire || _triggerCollider == null || !_triggerCollider.enabled)
            return;

        ScanNearbyFlammables();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        TryAffectFlammable(other, "OnTriggerEnter");
    }

    private void OnTriggerStay(Collider other)
    {
        TryAffectFlammable(other, "OnTriggerStay");
    }

    private void TryAffectFlammable(Collider other, string sourceContext, Vector3? scanCenter = null, float scanRadius = -1f)
    {
        if (!CanTransmitFire())
            return;

        if (linkedFlammableObject != null && !linkedFlammableObject.IsOnFire())
            return;

        if (!other.TryGetComponent(out FlammableElement otherFire))
            otherFire = other.GetComponentInParent<FlammableElement>();

        if (otherFire == null)
            return;

        if (canCurseFlammables)
            otherFire.TryActivateCursed();

        if (!otherFire.IsOnFire())
        {
            LogPouetFire(otherFire, sourceContext, scanCenter, scanRadius, "collider-trigger");
            otherFire.EnableFire(true);
        }
    }

    private void ScanNearbyFlammables()
    {
        Bounds bounds = _triggerCollider.bounds;
        Vector3 scanCenter = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float heldScanRadiusMultiplier = GetHeldScanRadiusMultiplier();
        bool useHeldScanRadius = heldScanRadiusMultiplier > 1f;
        if (useHeldScanRadius)
            radius *= heldScanRadiusMultiplier;

        if (ShouldUseHeldSpirimonzForwardOffset())
            scanCenter += GetHeldSpirimonzForward() * Mathf.Max(0f, heldSpirimonzForwardScanOffset);

        if (radius <= 0f)
            return;

        Collider[] hits = Physics.OverlapSphere(scanCenter, radius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit == _triggerCollider)
                continue;

            TryAffectFlammable(hit, "ScanNearbyFlammables.OverlapSphere", scanCenter, radius);
        }

        if (useHeldScanRadius)
            ScanNearbyFlammableElements(scanCenter, radius);
    }

    public void ArmHeldTransmission(float duration = -1f)
    {
        float finalDuration = duration >= 0f ? duration : heldActivationDuration;
        _heldActivationUntil = Mathf.Max(_heldActivationUntil, Time.time + Mathf.Max(0f, finalDuration));
        ScanNearbyFlammables();
    }

    private bool CanTransmitFire()
    {
        if (!canGiveFire)
            return false;

        if (linkedFlammableObject != null && !linkedFlammableObject.IsOnFire())
            return false;

        if (linkedFlammableObject != null && linkedFlammableObject.type == FlammableElement.FlammableType.Candle)
        {
            // A lit candle placed in the world should transmit fire normally.
            // The temporary held activation gate only applies while the candle is in hands.
            if (_linkedCatchableObject == null || !_linkedCatchableObject.isGrabbed)
                return true;

            return Time.time <= _heldActivationUntil;
        }

        return true;
    }

    private bool IsHeldAndActivationRequired()
    {
        return linkedFlammableObject != null &&
               linkedFlammableObject.type == FlammableElement.FlammableType.Candle &&
               _linkedCatchableObject != null &&
               _linkedCatchableObject.isGrabbed;
    }

    private bool ShouldUseHeldScanRadius()
    {
        if (_linkedCatchableObject != null && _linkedCatchableObject.isGrabbed)
            return true;

        return _linkedSpirimonz != null && !_linkedSpirimonz.isOnTheMap;
    }

    private float GetHeldScanRadiusMultiplier()
    {
        if (_linkedCatchableObject != null && _linkedCatchableObject.isGrabbed)
            return Mathf.Max(1f, heldCandleScanRadiusMultiplier);

        if (_linkedSpirimonz != null && !_linkedSpirimonz.isOnTheMap)
            return Mathf.Max(1f, heldSpirimonzScanRadiusMultiplier);

        return 1f;
    }

    private bool ShouldUseHeldSpirimonzForwardOffset()
    {
        return _linkedSpirimonz != null && !_linkedSpirimonz.isOnTheMap;
    }

    private Vector3 GetHeldSpirimonzForward()
    {
        Transform sourceTransform = _linkedSpirimonz != null ? _linkedSpirimonz.transform : transform;
        Vector3 forward = sourceTransform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private void ScanNearbyFlammableElements(Vector3 center, float radius)
    {
        if (!CanTransmitFire())
            return;

        FlammableElement[] flammables = FindObjectsOfType<FlammableElement>();
        for (int i = 0; i < flammables.Length; i++)
        {
            FlammableElement flammable = flammables[i];
            if (flammable == null)
                continue;

            if (linkedFlammableObject != null && flammable == linkedFlammableObject)
                continue;

            if (ShouldIgniteHeldSpirimonzNonCandle(flammable))
            {
                IgniteFlammable(flammable, "ScanNearbyFlammableElements.HeldSpirimonzNonCandle", center, radius);
                continue;
            }

            // When a Spirimonz is held in hands, non-candle ignition should only use
            // the dedicated short-range forward rule above, not the generic proximity scan.
            if (_linkedSpirimonz != null && !_linkedSpirimonz.isOnTheMap && flammable.type != FlammableElement.FlammableType.Candle)
                continue;

            bool usedCollider;
            int activeColliderCount;
            float distanceToClosest;
            if (!IsFlammableCloseEnough(flammable, center, radius, out usedCollider, out activeColliderCount, out distanceToClosest))
                continue;

            string detail = usedCollider
                ? $"proximity-collider closest={distanceToClosest:F2} activeColliders={activeColliderCount}"
                : $"proximity-transform fallbackDistance={distanceToClosest:F2} activeColliders={activeColliderCount}";
            IgniteFlammable(flammable, "ScanNearbyFlammableElements.Proximity", center, radius, detail);
        }
    }

    private void IgniteFlammable(FlammableElement flammable, string sourceContext, Vector3 center, float radius, string extraDetail = null)
    {
        if (flammable == null)
            return;

        if (canCurseFlammables)
            flammable.TryActivateCursed();

        if (!flammable.IsOnFire())
        {
            LogPouetFire(flammable, sourceContext, center, radius, extraDetail);
            flammable.EnableFire(true);
        }
    }

    private bool ShouldIgniteHeldSpirimonzNonCandle(FlammableElement flammable)
    {
        if (_linkedSpirimonz == null || _linkedSpirimonz.isOnTheMap || flammable == null)
            return false;

        if (flammable.type == FlammableElement.FlammableType.Candle)
            return false;

        Vector3 origin = _linkedSpirimonz.transform.position;
        Vector3 target = flammable.transform.position;
        Vector3 toTarget = target - origin;
        toTarget.y = 0f;

        float maxRange = Mathf.Max(0.1f, heldSpirimonzNonCandleRange);
        if (toTarget.sqrMagnitude > maxRange * maxRange)
            return false;

        Vector3 forward = GetHeldSpirimonzForward();
        if (Vector3.Dot(forward, toTarget.normalized) < heldSpirimonzNonCandleMinDot)
            return false;

        return true;
    }

    private static bool IsFlammableCloseEnough(FlammableElement flammable, Vector3 center, float radius, out bool usedCollider, out int activeColliderCount, out float distanceToClosest)
    {
        usedCollider = false;
        activeColliderCount = 0;
        distanceToClosest = float.MaxValue;

        Collider[] colliders = flammable.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled)
                continue;

            activeColliderCount++;
            Vector3 closest = col.ClosestPoint(center);
            float distance = Vector3.Distance(center, closest);
            distanceToClosest = Mathf.Min(distanceToClosest, distance);
            if (distance <= radius)
            {
                usedCollider = true;
                return true;
            }
        }

        distanceToClosest = Vector3.Distance(center, flammable.transform.position);
        return distanceToClosest <= radius;
    }

    private void LogPouetFire(FlammableElement flammable, string sourceContext, Vector3? scanCenter, float scanRadius, string extraDetail)
    {
        if (flammable == null || !flammable.debugPouetFire)
            return;

        Vector3 sourcePos = transform.position;
        Vector3 targetPos = flammable.transform.position;
        float distance = Vector3.Distance(sourcePos, targetPos);
        bool heldCatchable = _linkedCatchableObject != null && _linkedCatchableObject.isGrabbed;
        bool heldSpirimonz = _linkedSpirimonz != null && !_linkedSpirimonz.isOnTheMap;
        string linkedName = linkedFlammableObject != null ? linkedFlammableObject.name : "none";
        string centerText = scanCenter.HasValue ? scanCenter.Value.ToString("F2") : "n/a";
        string radiusText = scanRadius >= 0f ? scanRadius.ToString("F2") : "n/a";

        Debug.Log(
            $"[PouetFire] target={flammable.name} type={flammable.type} sourceTrigger={name} linkedFlammable={linkedName} " +
            $"context={sourceContext} sourcePos={sourcePos:F2} targetPos={targetPos:F2} sourceToTarget={distance:F2} " +
            $"scanCenter={centerText} scanRadius={radiusText} heldCatchable={heldCatchable} heldSpirimonz={heldSpirimonz} " +
            $"triggerColliderEnabled={(_triggerCollider != null && _triggerCollider.enabled)} extra={extraDetail}",
            flammable
        );
    }
}
