using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class CatchableKatana : CatchableObject
{
    [Header("Slash Settings")]
    public float slashAnimationSpeed = 1f;
    public float slashHitEnableDelay = 0.1f;
    public float slashHitActiveDuration = 0.2f;
    public Collider[] slashHitColliders;

    [Header("Ghost Hit Detection")]
    public LayerMask ghostHitMask;
    public bool useOverlapDetection = true;
    public int maxOverlapHits = 8;
    public bool useDistanceDetection = true;
    public Transform ghostHitOrigin;
    public float ghostHitRadius = 1.2f;
    public float stopHuntDelay = 0.2f;
    public float whooshDelay = 0f;

    [Header("Blade Break")]
    public GameObject blade;
    public GameObject bladeFractures;
    public bool forbidInteractionOnBreak = true;
    public SoundParameters breakingSoundParameters;
    public SoundParameters whooshSoundParameters;
    public SoundParameters curseSoundParameters;
    public float ghostAngerMultiplierOnHit = 1f;
    public float breakImpactForceForSound = 10f;

    [Header("Breaking Forces")]
    public float fractureExplosionForce = 2.5f;
    public float fractureExplosionRadius = 1f;
    public float fractureExplosionUpwards = 0.2f;
    public float fractureRandomTorque = 1.2f;
    public float lockingFracturesDelay = 3f;

    private const string SlashEnableInvoke = "KatanaSlashEnable";
    private const string SlashDisableInvoke = "KatanaSlashDisable";
    private const string SlashWhooshInvoke = "KatanaSlashWhoosh";

    private GamePlayer _player;
    private bool _slashWindowActive;
    private bool _hasHitGhost;
    private bool _bladeBroken;
    private Collider[] _overlapHits;
    private int _ghostHitMaskValue;

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        CacheSlashColliders();
        SetSlashHitActive(false);

        if (maxOverlapHits < 1)
            maxOverlapHits = 1;
        _overlapHits = new Collider[maxOverlapHits];

        if (ghostHitMask.value != 0)
        {
            _ghostHitMaskValue = ghostHitMask.value;
        }
        else
        {
            int ghostLayer = LayerMask.NameToLayer("Ghost");
            _ghostHitMaskValue = ghostLayer >= 0 ? (1 << ghostLayer) : ~0;
        }

        if (ghostHitOrigin == null)
            ghostHitOrigin = FindBladeHitTransform();
    }

    public override void OnDrop()
    {
        base.OnDrop();
        SetSlashHitActive(false);
    }

    public override void OnThrow()
    {
        base.OnThrow();
        SetSlashHitActive(false);
    }

    public override void SpecialActionInHandsOnClick()
    {
        if (!isGrabbed)
            return;

        if (_bladeBroken)
            return;

        if (_player == null)
            _player = (GamePlayer)Player.Instance;

        if (_player == null)
            return;

        _player.UseSlashAnimation(slashAnimationSpeed);
        StartSlashWindow();
    }

    private void StartSlashWindow()
    {
        _hasHitGhost = false;
        SetSlashHitActive(false);

        CancelInvoke(SlashEnableInvoke);
        CancelInvoke(SlashDisableInvoke);
        CancelInvoke(SlashWhooshInvoke);

        if (!_hasHitGhost && whooshSoundParameters != null)
        {
            if (whooshDelay <= 0f)
            {
                whooshSoundParameters.PlaySound(transform.position);
            }
            else
            {
                Invoke(SlashWhooshInvoke, whooshDelay, () =>
                {
                    if (!_hasHitGhost)
                        whooshSoundParameters.PlaySound(transform.position);
                });
            }
        }

        Invoke(SlashEnableInvoke, slashHitEnableDelay, () =>
        {
            SetSlashHitActive(true);

            Invoke(SlashDisableInvoke, slashHitActiveDuration, () => SetSlashHitActive(false));
        });
    }

    private void SetSlashHitActive(bool active)
    {
        _slashWindowActive = active;

        if (slashHitColliders == null || slashHitColliders.Length == 0)
            return;

        foreach (Collider col in slashHitColliders)
        {
            if (col == null)
                continue;

            col.enabled = active;
        }
    }

    private void CacheSlashColliders()
    {
        if (slashHitColliders != null && slashHitColliders.Length > 0)
            return;

        Transform bladeHit = FindBladeHitTransform();
        if (bladeHit != null && bladeHit.TryGetComponent(out Collider bladeCollider))
        {
            bladeCollider.isTrigger = true;
            slashHitColliders = new[] { bladeCollider };
            return;
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        if (allColliders == null || allColliders.Length == 0)
        {
            slashHitColliders = new Collider[0];
            return;
        }

        List<Collider> triggers = new List<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col != null && col.isTrigger)
                triggers.Add(col);
        }

        slashHitColliders = triggers.ToArray();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitGhost(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHitGhost(other);
    }

    private void FixedUpdate()
    {
        if (!_slashWindowActive || _hasHitGhost || _bladeBroken)
            return;

        if (useOverlapDetection)
            TryHitGhostFromOverlap();

        if (!_hasHitGhost && useDistanceDetection)
            TryHitGhostByDistance();
    }

    private void TryHitGhost(Collider other)
    {
        if (!_slashWindowActive || _hasHitGhost || _bladeBroken)
            return;

        Ghost ghost = other.GetComponentInParent<Ghost>();
        if (ghost == null)
            return;

        if (!ghost.IsHunting(includeWillHunt: false))
            return;

        _hasHitGhost = true;
        HandleGhostHit(ghost);
    }

    private void TryHitGhostFromOverlap()
    {
        if (slashHitColliders == null || slashHitColliders.Length == 0)
            return;

        for (int i = 0; i < slashHitColliders.Length; i++)
        {
            Collider slashCol = slashHitColliders[i];
            if (slashCol == null || !slashCol.enabled)
                continue;

            Bounds bounds = slashCol.bounds;
            int hitCount = Physics.OverlapBoxNonAlloc(
                bounds.center,
                bounds.extents,
                _overlapHits,
                Quaternion.identity,
                _ghostHitMaskValue,
                QueryTriggerInteraction.Collide);

            for (int h = 0; h < hitCount; h++)
            {
                Collider hit = _overlapHits[h];
                if (hit == null)
                    continue;

                Ghost ghost = hit.GetComponentInParent<Ghost>();
                if (ghost == null)
                    continue;

                if (!ghost.IsHunting(includeWillHunt: false))
                    continue;

                _hasHitGhost = true;
                HandleGhostHit(ghost);
                return;
            }
        }
    }

    private void TryHitGhostByDistance()
    {
        Ghost ghost = House.Instance != null ? House.Instance.currentGhost : null;
        if (ghost == null)
            return;

        if (!ghost.IsHunting(includeWillHunt: false))
            return;

        Vector3 hitPos = ghostHitOrigin != null ? ghostHitOrigin.position : transform.position;
        float radius = Mathf.Max(0.01f, ghostHitRadius);
        float radiusSqr = radius * radius;

        Collider[] ghostColliders = ghost.GetComponentsInChildren<Collider>(true);
        bool anyCollider = false;
        if (ghostColliders != null && ghostColliders.Length > 0)
        {
            for (int i = 0; i < ghostColliders.Length; i++)
            {
                Collider ghostCol = ghostColliders[i];
                if (ghostCol == null || !ghostCol.enabled)
                    continue;

                anyCollider = true;
                Vector3 closest = ghostCol.ClosestPoint(hitPos);
                if ((closest - hitPos).sqrMagnitude <= radiusSqr)
                {
                    _hasHitGhost = true;
                    HandleGhostHit(ghost);
                    return;
                }
            }
        }

        if (!anyCollider)
        {
            if ((ghost.transform.position - hitPos).sqrMagnitude <= radiusSqr)
            {
                _hasHitGhost = true;
                HandleGhostHit(ghost);
            }
        }
    }

    private void HandleGhostHit(Ghost ghost)
    {
        if (ghost == null)
            return;

        ghost.StopHuntByKatana(playApparitionFx: true, delayBeforeStop: stopHuntDelay);

        if (ghostAngerMultiplierOnHit > 0f)
            ghost.MultiplyAnger(ghostAngerMultiplierOnHit);

        if (curseSoundParameters != null)
            curseSoundParameters.PlaySound(transform.position);

        BreakBlade();
    }

    private void BreakBlade()
    {
        if (_bladeBroken)
            return;

        _bladeBroken = true;
        SetSlashHitActive(false);

        if (forbidInteractionOnBreak)
        {
            canBeGrabByPlayer = false;
            canBeThrownByGhost = false;
            canBeThrownByPlayer = false;
            InteractionLocked = true;
        }

        if (blade != null && bladeFractures != null)
            bladeFractures.transform.SetPositionAndRotation(blade.transform.position, blade.transform.rotation);

        if (blade != null)
            blade.SetActive(false);
        if (bladeFractures != null)
            bladeFractures.SetActive(true);

        PlayBreakingSound(breakImpactForceForSound);
        List<Transform> fracturePieces = DetachFracturePieces();
        ApplyFractureForces(breakImpactForceForSound, fracturePieces);
        ScheduleFractureCleanup(fracturePieces);
    }

    private void PlayBreakingSound(float impactForce)
    {
        if (breakingSoundParameters == null)
            return;

        float volumeMultiplier = Mathf.Clamp01(impactForce / 10f);
        float volumeToUse = breakingSoundParameters.volume * volumeMultiplier;
        breakingSoundParameters.PlaySound(transform.position, volumeToUse);
    }

    private void ApplyFractureForces(float impactForce, List<Transform> fracturePieces)
    {
        if (bladeFractures == null || fracturePieces == null || fracturePieces.Count == 0)
            return;

        float impactScale = Mathf.Clamp01(impactForce / 10f);
        float explosionForce = fractureExplosionForce * Mathf.Lerp(0.75f, 1.5f, impactScale);
        float torqueForce = fractureRandomTorque * Mathf.Lerp(0.75f, 1.5f, impactScale);
        Vector3 explosionOrigin = bladeFractures.transform.position;

        for (int i = 0; i < fracturePieces.Count; i++)
        {
            Transform piece = fracturePieces[i];
            if (piece == null || !piece.TryGetComponent(out Rigidbody pieceRb))
                continue;

            pieceRb.isKinematic = false;
            if (explosionForce > 0f && fractureExplosionRadius > 0f)
            {
                Vector3 jitteredOrigin = explosionOrigin + Random.insideUnitSphere * 0.05f;
                pieceRb.AddExplosionForce(explosionForce, jitteredOrigin, fractureExplosionRadius, fractureExplosionUpwards, ForceMode.Impulse);
            }

            if (torqueForce > 0f)
                pieceRb.AddTorque(Random.onUnitSphere * torqueForce, ForceMode.Impulse);
        }
    }

    private List<Transform> DetachFracturePieces()
    {
        List<Transform> detached = new List<Transform>();
        if (bladeFractures == null)
            return detached;

        if (bladeFractures.TryGetComponent(out Rigidbody rootRb))
        {
            rootRb.isKinematic = true;
            rootRb.detectCollisions = false;
        }

        HashSet<Transform> uniqueTransforms = new HashSet<Transform>();
        Rigidbody[] rigidbodies = bladeFractures.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb == null)
                continue;
            if (rb.transform == bladeFractures.transform)
                continue;
            if (uniqueTransforms.Add(rb.transform))
                detached.Add(rb.transform);
        }

        if (detached.Count == 0)
        {
            foreach (Transform child in bladeFractures.transform)
            {
                if (child == null)
                    continue;
                if (uniqueTransforms.Add(child))
                    detached.Add(child);
            }
        }

        foreach (Transform part in detached)
        {
            if (part == null)
                continue;
            part.parent = null;
        }

        return detached;
    }

    private void ScheduleFractureCleanup(List<Transform> fracturePieces)
    {
        if (fracturePieces == null || fracturePieces.Count == 0)
            return;

        foreach (Transform part in fracturePieces)
        {
            if (part == null)
                continue;

            Transform localPart = part;
            Invoke(lockingFracturesDelay, () =>
            {
                if (localPart == null)
                    return;

                if (localPart.TryGetComponent(out Rigidbody partRb) && partRb.velocity.magnitude < 0.5f)
                {
                    partRb.isKinematic = true;
                }
                else
                {
                    localPart.DOScale(0.01f, 2f).OnComplete(() =>
                    {
                        if (localPart != null)
                            Destroy(localPart.gameObject);
                    });
                }
            });
        }
    }

    private Transform FindBladeHitTransform()
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
