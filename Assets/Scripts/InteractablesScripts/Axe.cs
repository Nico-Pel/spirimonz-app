using System.Collections.Generic;
using UnityEngine;

public class Axe : CatchableObject
{
    [Header("Swing Settings")]
    public float swingAnimationSpeed = 1f;
    public float swingHitEnableDelay = 0.1f;
    public float swingHitActiveDuration = 0.2f;
    public Collider[] swingHitColliders;

    [Header("Break Detection")]
    public bool useOverlapDetection = true;
    public int maxOverlapHits = 16;
    public bool useDistanceDetection = true;
    public Transform hitOrigin;
    public float hitRadius = 1.25f;
    public float whooshDelay = 0f;

    [Header("Break Sounds")]
    public SoundParameters whooshSoundParameters;

    private const string SwingEnableInvoke = "AxeSwingEnable";
    private const string SwingDisableInvoke = "AxeSwingDisable";
    private const string SwingWhooshInvoke = "AxeSwingWhoosh";

    private GamePlayer _player;
    private bool _swingWindowActive;
    private bool _hasHitBreakable;
    private Collider[] _overlapHits;

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        CacheSwingColliders();
        SetSwingHitActive(false);

        if (maxOverlapHits < 1)
            maxOverlapHits = 1;
        _overlapHits = new Collider[maxOverlapHits];

        if (hitOrigin == null)
            hitOrigin = FindHitTransform();
    }

    public override void OnDrop()
    {
        base.OnDrop();
        SetSwingHitActive(false);
    }

    public override void OnThrow()
    {
        base.OnThrow();
        SetSwingHitActive(false);
    }

    public override void SpecialActionInHandsOnClick()
    {
        if (!isGrabbed)
            return;

        if (_player == null)
            _player = (GamePlayer)Player.Instance;

        if (_player == null)
            return;

        _player.UseSlashAnimation(swingAnimationSpeed);
        StartSwingWindow();
    }

    private void StartSwingWindow()
    {
        _hasHitBreakable = false;
        SetSwingHitActive(false);

        CancelInvoke(SwingEnableInvoke);
        CancelInvoke(SwingDisableInvoke);
        CancelInvoke(SwingWhooshInvoke);

        if (whooshSoundParameters != null)
        {
            if (whooshDelay <= 0f)
            {
                whooshSoundParameters.PlaySound(transform.position);
            }
            else
            {
                Invoke(SwingWhooshInvoke, whooshDelay, () =>
                {
                    if (!_hasHitBreakable)
                        whooshSoundParameters.PlaySound(transform.position);
                });
            }
        }

        Invoke(SwingEnableInvoke, swingHitEnableDelay, () =>
        {
            SetSwingHitActive(true);
            Invoke(SwingDisableInvoke, swingHitActiveDuration, () => SetSwingHitActive(false));
        });
    }

    private void SetSwingHitActive(bool active)
    {
        _swingWindowActive = active;

        if (swingHitColliders == null || swingHitColliders.Length == 0)
            return;

        foreach (Collider col in swingHitColliders)
        {
            if (col == null)
                continue;

            col.enabled = active;
        }
    }

    private void CacheSwingColliders()
    {
        if (swingHitColliders != null && swingHitColliders.Length > 0)
            return;

        Transform hitTransform = FindHitTransform();
        if (hitTransform != null && hitTransform.TryGetComponent(out Collider hitCollider))
        {
            hitCollider.isTrigger = true;
            swingHitColliders = new[] { hitCollider };
            return;
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        if (allColliders == null || allColliders.Length == 0)
        {
            swingHitColliders = new Collider[0];
            return;
        }

        List<Collider> triggers = new List<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col != null && col.isTrigger)
                triggers.Add(col);
        }

        swingHitColliders = triggers.ToArray();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBreakTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBreakTarget(other);
    }

    private void FixedUpdate()
    {
        if (!_swingWindowActive || _hasHitBreakable)
            return;

        if (useOverlapDetection)
            TryBreakFromOverlap();

        if (!_hasHitBreakable && useDistanceDetection)
            TryBreakByDistance();
    }

    private void TryBreakTarget(Collider other)
    {
        if (!_swingWindowActive || _hasHitBreakable)
            return;

        if (TryBreakComponent(other.GetComponentInParent<CatchableBreakingObject>()))
            return;

        TryBreakComponent(other.GetComponentInParent<StaticBreakableObject>());
    }

    private bool TryBreakComponent(CatchableBreakingObject breakable)
    {
        if (breakable == null)
            return false;

        _hasHitBreakable = true;
        SetSwingHitActive(false);
        breakable.BreakByAxe(hitRadius);
        return true;
    }

    private bool TryBreakComponent(StaticBreakableObject breakable)
    {
        if (breakable == null)
            return false;

        _hasHitBreakable = true;
        SetSwingHitActive(false);
        breakable.Break(hitRadius);
        return true;
    }

    private void TryBreakFromOverlap()
    {
        if (swingHitColliders == null || swingHitColliders.Length == 0)
            return;

        for (int i = 0; i < swingHitColliders.Length; i++)
        {
            Collider swingCol = swingHitColliders[i];
            if (swingCol == null || !swingCol.enabled)
                continue;

            Bounds bounds = swingCol.bounds;
            int hitCount = Physics.OverlapBoxNonAlloc(
                bounds.center,
                bounds.extents,
                _overlapHits,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int h = 0; h < hitCount; h++)
            {
                Collider hit = _overlapHits[h];
                if (hit == null)
                    continue;

                TryBreakTarget(hit);
                if (_hasHitBreakable)
                    return;
            }
        }
    }

    private void TryBreakByDistance()
    {
        Vector3 origin = hitOrigin != null ? hitOrigin.position : transform.position;
        float radius = Mathf.Max(0.01f, hitRadius);
        float radiusSqr = radius * radius;

        CatchableBreakingObject[] catchableBreakables = FindObjectsOfType<CatchableBreakingObject>();
        for (int i = 0; i < catchableBreakables.Length; i++)
        {
            CatchableBreakingObject breakable = catchableBreakables[i];
            if (breakable == null)
                continue;

            if (IsBreakableWithinRadius(breakable.transform, origin, radiusSqr))
            {
                _hasHitBreakable = true;
                SetSwingHitActive(false);
                breakable.BreakByAxe(radius);
                return;
            }
        }

        StaticBreakableObject[] staticBreakables = FindObjectsOfType<StaticBreakableObject>();
        for (int i = 0; i < staticBreakables.Length; i++)
        {
            StaticBreakableObject breakable = staticBreakables[i];
            if (breakable == null)
                continue;

            if (IsBreakableWithinRadius(breakable.transform, origin, radiusSqr))
            {
                _hasHitBreakable = true;
                SetSwingHitActive(false);
                breakable.Break(radius);
                return;
            }
        }
    }

    private static bool IsBreakableWithinRadius(Transform target, Vector3 origin, float radiusSqr)
    {
        if (target == null)
            return false;

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool anyCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled)
                continue;

            anyCollider = true;
            Vector3 closest = col.ClosestPoint(origin);
            if ((closest - origin).sqrMagnitude <= radiusSqr)
                return true;
        }

        return !anyCollider && (target.position - origin).sqrMagnitude <= radiusSqr;
    }

    private Transform FindHitTransform()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t != null && t.name == "BladeHit")
                return t;
        }

        return null;
    }
}
