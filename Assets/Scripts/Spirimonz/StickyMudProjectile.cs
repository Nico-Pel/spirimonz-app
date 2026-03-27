using UnityEngine;

public class StickyMudProjectile : GameBehaviour
{
    [Header("Physics")]
    public Rigidbody rb;
    public Collider col;

    [Header("Sensors")]
    public UVRevealer uvRevealer;
    public RadiationDetector radiationDetector;
    public SoundParameters stickSoundParameters;

    [Header("Placement")]
    public float surfaceOffset = 0.01f;
    public bool requireSurfaceSize = true;
    public float minSurfaceSize = 0.4f;
    public LayerMask validSurfaceMask = ~0;
    public LayerMask invalidSurfaceMask = 0;
    public bool disableColliderOnStick = true;

    [Header("Room Detection")]
    public float roomDetectionRadius = 0.2f;
    public LayerMask roomLayerMask = ~0;
    public float roomProbeOffset = 0.05f;
    public float roomProbeRadius = 0.01f;

    [Header("Parenting")]
    public float minParentScale = 0.01f;
    public float parentScaleTolerance = 0.01f;

    [Header("FX")]
    public ParticleSystem burstFx;
    public float destroyDelay = 0.05f;

    private SpmzUsePower _owner;
    private float _energyCost;
    private bool _resolved;
    private Vector3 _lastVelocity;
    private Collider _lastHitCollider;
    private Room _roomFromTrigger;
    private Transform _followTransform;
    private Vector3 _followOffset;
    private Quaternion _followRotationOffset = Quaternion.identity;
    private bool _useFollowTransform;
    private Vector3 _desiredWorldScale = Vector3.one;

    public void Initialize(SpmzUsePower owner, float energyCost)
    {
        _owner = owner;
        _energyCost = Mathf.Max(0f, energyCost);

        GamePlayer player = Player.Instance as GamePlayer;
        if (player != null && player.currentRoom != null)
            _roomFromTrigger = player.currentRoom;
        else if (_owner != null && _owner.currentRoom != null)
            _roomFromTrigger = _owner.currentRoom;
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (col == null)
            col = GetComponent<Collider>();

        if (uvRevealer != null)
            uvRevealer.enabled = false;
    }

    private void FixedUpdate()
    {
        if (rb != null)
            _lastVelocity = rb.velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_resolved)
            return;

        if (collision.contactCount == 0)
        {
            FailAt(transform.position, -transform.forward);
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        HandleHit(contact.point, contact.normal, collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        Room room = other.GetComponentInParent<Room>();
        if (room != null)
        {
            _roomFromTrigger = room;
            return;
        }

        if (_resolved)
            return;

        // Ignore non-room triggers
    }

    private void HandleHit(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
    {
        if (_resolved)
            return;

        if (hitCollider == null)
        {
            FailAt(hitPoint, hitNormal);
            return;
        }

        if (hitCollider.isTrigger)
            return;

        _lastHitCollider = hitCollider;

        if (!IsSurfaceValid(hitCollider, hitNormal))
        {
            FailAt(hitPoint, hitNormal);
            return;
        }

        if (requireSurfaceSize && !IsSurfaceLargeEnough(hitCollider, hitNormal))
        {
            FailAt(hitPoint, hitNormal);
            return;
        }

        Room room = FindRoom(hitCollider, hitPoint, hitNormal);
        if (room == null)
        {
            FailAt(hitPoint, hitNormal);
            return;
        }

        StickToSurface(hitPoint, hitNormal, room);
    }

    private bool IsSurfaceValid(Collider hitCollider, Vector3 hitNormal)
    {
        if (IsLayerInMask(hitCollider.gameObject.layer, invalidSurfaceMask))
            return false;

        if (validSurfaceMask.value != 0 && !IsLayerInMask(hitCollider.gameObject.layer, validSurfaceMask))
            return false;

        if (hitCollider.GetComponentInParent<CatchableObject>() != null)
            return false;

        if (hitCollider.GetComponentInParent<Spirimonz>() != null)
            return false;

        if (hitCollider.GetComponentInParent<Player>() != null)
            return false;

        return true;
    }

    private bool IsSurfaceLargeEnough(Collider hitCollider, Vector3 hitNormal)
    {
        Vector3 size = hitCollider.bounds.size;
        Vector3 abs = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));

        if (abs.y >= abs.x && abs.y >= abs.z)
            return size.x >= minSurfaceSize && size.z >= minSurfaceSize;

        if (abs.x >= abs.y && abs.x >= abs.z)
            return size.y >= minSurfaceSize && size.z >= minSurfaceSize;

        return size.x >= minSurfaceSize && size.y >= minSurfaceSize;
    }

    private Room FindRoom(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        Room room = hitCollider.GetComponentInParent<Room>();
        if (room != null)
            return room;

        int mask = GetRoomOverlapMask();
        float offset = Mathf.Max(0f, roomProbeOffset);

        Vector3 probeA = hitPoint + hitNormal * offset;
        Vector3 probeB = hitPoint - hitNormal * offset;

        if (TryFindRoomAtPoint(probeA, mask, out Room roomAtA))
            return roomAtA;

        if (TryFindRoomAtPoint(probeB, mask, out Room roomAtB))
            return roomAtB;

        Collider[] hits = Physics.OverlapSphere(
            hitPoint,
            Mathf.Max(0.01f, roomDetectionRadius),
            mask,
            QueryTriggerInteraction.Collide);

        Room bestRoom = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null) continue;

            Room candidate = hit.GetComponentInParent<Room>();
            if (candidate == null) continue;

            Vector3 closest = hit.ClosestPoint(hitPoint);
            float dist = (closest - hitPoint).sqrMagnitude;
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestRoom = candidate;
            }
        }

        if (bestRoom != null)
            return bestRoom;

        return _roomFromTrigger;
    }

    private bool TryFindRoomAtPoint(Vector3 point, int mask, out Room room)
    {
        room = null;
        float radius = Mathf.Max(0.001f, roomProbeRadius);
        Collider[] hits = Physics.OverlapSphere(point, radius, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null) continue;

            Room candidate = hit.GetComponentInParent<Room>();
            if (candidate == null) continue;

            room = candidate;
            return true;
        }

        return false;
    }

    private int GetRoomOverlapMask()
    {
        if (roomLayerMask.value != 0)
            return roomLayerMask.value;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast >= 0)
            return 1 << ignoreRaycast;

        return Physics.AllLayers;
    }

    private void StickToSurface(Vector3 hitPoint, Vector3 hitNormal, Room room)
    {
        _resolved = true;
        _useFollowTransform = false;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (disableColliderOnStick)
        {
            if (col != null)
                col.enabled = false;
        }

        Vector3 desiredPosition = hitPoint + hitNormal * surfaceOffset;
        Quaternion rotation = ComputeStickRotation(hitNormal);
        Vector3 worldScale = transform.lossyScale;
        _desiredWorldScale = worldScale;
        transform.SetPositionAndRotation(desiredPosition, rotation);

        Transform hitTransform = GetHitTransform();
        if (hitTransform != null)
        {
            if (IsInvalidScale(hitTransform.lossyScale))
            {
                _useFollowTransform = true;
                _followTransform = hitTransform;
                _followOffset = Quaternion.Inverse(hitTransform.rotation) * (desiredPosition - hitTransform.position);
                _followRotationOffset = Quaternion.Inverse(hitTransform.rotation) * rotation;
                transform.SetParent(null, true);
                transform.localScale = _desiredWorldScale;
            }
            else
            {
                transform.SetParent(hitTransform, true);
                SetWorldScale(worldScale);
            }
        }

        if (stickSoundParameters != null)
            stickSoundParameters.PlaySound(hitPoint);

        if (uvRevealer != null)
            uvRevealer.enabled = true;

        if (radiationDetector != null)
        {
            radiationDetector.useSound = true;
            radiationDetector.SetCurrentRoom(room);
            if (radiationDetector.IsDetectingRadiation())
                radiationDetector.PlaySoundManuallyIfNeeded();
        }
    }

    private Transform GetParentForHit()
    {
        if (_lastHitCollider == null)
            return null;

        if (_lastHitCollider.attachedRigidbody != null)
            return _lastHitCollider.attachedRigidbody.transform;

        return _lastHitCollider.transform;
    }

    private Transform GetHitTransform()
    {
        return GetParentForHit();
    }

    private bool IsInvalidScale(Vector3 scale)
    {
        if (Mathf.Abs(scale.x) < minParentScale ||
            Mathf.Abs(scale.y) < minParentScale ||
            Mathf.Abs(scale.z) < minParentScale)
            return true;

        if (Mathf.Abs(scale.x - 1f) > parentScaleTolerance ||
            Mathf.Abs(scale.y - 1f) > parentScaleTolerance ||
            Mathf.Abs(scale.z - 1f) > parentScaleTolerance)
            return true;

        return false;
    }

    private void SetWorldScale(Vector3 worldScale)
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            transform.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        transform.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z)
        );
    }

    private float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) < 0.0001f)
            return value;
        return value / divisor;
    }

    private void LateUpdate()
    {
        if (!_useFollowTransform || _followTransform == null)
            return;

        transform.position = _followTransform.position + _followTransform.rotation * _followOffset;
        transform.rotation = _followTransform.rotation * _followRotationOffset;
        transform.localScale = _desiredWorldScale;
    }

    private Quaternion ComputeStickRotation(Vector3 hitNormal)
    {
        Vector3 forward = _lastVelocity;
        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        forward = Vector3.ProjectOnPlane(forward, hitNormal);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.Cross(hitNormal, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.Cross(hitNormal, Vector3.right);

        return Quaternion.LookRotation(forward.normalized, hitNormal);
    }

    private void FailAt(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_resolved)
            return;

        _resolved = true;

        if (_owner != null)
            _owner.RestoreEnergy(_energyCost);

        if (stickSoundParameters != null)
            stickSoundParameters.PlaySound(hitPoint);

        PlayBurstFx(hitPoint, hitNormal);
        Destroy(gameObject, Mathf.Max(0f, destroyDelay));
    }

    private void PlayBurstFx(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (burstFx == null)
            return;

        ParticleSystem fxInstance = burstFx;
        if (burstFx.transform.IsChildOf(transform))
            fxInstance.transform.SetParent(null, true);

        fxInstance.transform.position = hitPoint;
        fxInstance.transform.rotation = Quaternion.LookRotation(hitNormal);
        fxInstance.Play();

        ParticleSystem.MainModule main = fxInstance.main;
        float lifetime = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            lifetime += main.startLifetime.constantMax;
        else
            lifetime += main.startLifetime.constant;

        Destroy(fxInstance.gameObject, Mathf.Max(0.1f, lifetime));
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
