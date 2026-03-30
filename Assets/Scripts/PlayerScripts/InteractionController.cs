using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InteractionController : GameBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 3f;
    public float interactionDoorsDistance = 5f;
    public LayerMask interactableLayer;
    public LayerMask groundLayer;
    public float rayOffset = 0.2f;
    private float sphereRadius = 0.05f;

    private int _objectInHandLayerIndex;

    [ReadOnly] public bool targetingGround;
    private Vector3 _lastGroundPosTargeted;

    [Header("Hand Settings")]
    public Transform handObjectPosition;
    public Transform handObjectDropPosition;
    public float throwForceForward = 5f;

    [ReadOnly] public CatchableObject objectInHands;

    [Header("Doors Settings")]
    [SerializeField] LayerMask doorLayer;

#if UNITY_EDITOR
    [Header("Debug")]
    public bool debugDropRaycast = false;
    [ReadOnly] [SerializeField] private Transform debugDropHitTransform;
#endif

    private Door _targetedDoor;
    private Door _grabbedDoor;
    private float _grabDistance;

    private IInteractable _currentTarget;
    private IInteractable _lastTarget;
    private CatchableObject _currentCatchable;
    private NPC _currentNpcTarget;

    private Camera _cam;
    private GamePlayer _player;
    private UIGame _uiGame;

    public UnityEvent<CatchableObject> OnGrabItem;
    public UnityEvent<CatchableObject> OnDropItem;

    // =========================
    // UI Cache
    // =========================
    bool _lastShowCursor;
    bool _lastShowGrab;
    Sprite _lastCursorSprite;
    float _lastCursorSize;

    void Awake()
    {
        _cam = Camera.main;
    }

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        _uiGame = UIGame.Instance;

        // fallback sécurité
        InvokeRepeating(nameof(RefreshCursorUI), 0f, 0.2f);
    }

    void Update()
    {
        if (IsInteractionBlockedByUI())
        {
            ClearInteractionTargets(true);
            return;
        }

        HandleDoor();
        DetectInteractable();
        HandleInput();
#if UNITY_EDITOR
        DebugDropRaycast();
#endif
    }

    // =========================
    // Raycast & Detection
    // =========================
    void DetectInteractable()
    {
        Vector3 rayOrigin;
        Ray ray;
        if (MobileInput.Enabled && MobileInput.HasPrimaryScreenPos && _cam != null)
        {
            ray = _cam.ScreenPointToRay(MobileInput.PrimaryScreenPos);
            rayOrigin = ray.origin + ray.direction * rayOffset;
            ray.origin = rayOrigin;
        }
        else
        {
            rayOrigin = _cam.transform.position + _cam.transform.forward * rayOffset;
            ray = new Ray(rayOrigin, _cam.transform.forward);
        }

        IInteractable newTarget = null;
        CatchableObject newCatchable = null;

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            targetingGround = (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
            _lastGroundPosTargeted = hit.point;
            newTarget = hit.collider.GetComponent<IInteractable>();
            if (newTarget == null)
                newTarget = hit.collider.GetComponentInParent<IInteractable>();

            if (newTarget != null && newTarget.InteractionLocked)
                newTarget = null;

            if (newTarget != null && objectInHands != null && newTarget is CatchableObject)
                newTarget = null;

            // Prioriser un Catchable centré à l'écran (si on ne porte rien).
            if (objectInHands == null)
            {
                RaycastHit[] hits = Physics.SphereCastAll(ray, sphereRadius, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore);
                CatchableObject bestCatchable = null;
                float bestScreenDist = Mathf.Infinity;
                float bestDistance = Mathf.Infinity;
                const float screenEpsilon = 0.000001f;

                for (int i = 0; i < hits.Length; i++)
                {
                    CatchableObject candidate = hits[i].collider.GetComponent<CatchableObject>();
                    if (candidate == null)
                        candidate = hits[i].collider.GetComponentInParent<CatchableObject>();
                    if (candidate == null)
                        continue;
                    if (candidate.InteractionLocked)
                        continue;

                    Vector3 center = hits[i].collider.bounds.center;
                    if (!IsVisibleFromCamera(rayOrigin, center, candidate))
                        continue;
                    Vector3 viewport = _cam.WorldToViewportPoint(center);
                    if (viewport.z < 0f)
                        continue;

                    float dx = viewport.x - 0.5f;
                    float dy = viewport.y - 0.5f;
                    float screenDist = (dx * dx) + (dy * dy);

                    if (screenDist < bestScreenDist - screenEpsilon ||
                        (Mathf.Abs(screenDist - bestScreenDist) <= screenEpsilon && hits[i].distance < bestDistance))
                    {
                        bestScreenDist = screenDist;
                        bestDistance = hits[i].distance;
                        bestCatchable = candidate;
                    }
                }

                if (bestCatchable != null)
                {
                    newCatchable = bestCatchable;
                    ClickableObject clickable = bestCatchable.GetComponent<ClickableObject>();
                    if (clickable != null && !clickable.InteractionLocked)
                        newTarget = clickable;
                    else
                        newTarget = bestCatchable;
                }
            }
        }
        else
        {
            targetingGround = false;
        }

        if (newCatchable == null)
            newCatchable = GetCatchableFromInteractable(newTarget);

        if (newTarget != _currentTarget || newCatchable != _currentCatchable)
        {
            _lastTarget = _currentTarget;
            _currentTarget = newTarget;
            _currentCatchable = newCatchable;
            UpdateNpcCTA(_lastTarget, _currentTarget);
            RefreshCursorUI();
        }
    }

    private bool IsVisibleFromCamera(Vector3 origin, Vector3 targetPoint, IInteractable candidate)
    {
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return true;

        Vector3 direction = toTarget / distance;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
            return true;

        float nearestDistance = Mathf.Infinity;
        RaycastHit nearestHit = default;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;
            if (_player != null && col.transform.IsChildOf(_player.transform))
                continue;

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                found = true;
            }
        }

        if (!found)
            return true;

        IInteractable hitInteractable = nearestHit.collider.GetComponentInParent<IInteractable>();
        if (hitInteractable == candidate)
            return true;

        Component hitComp = hitInteractable as Component;
        Component candComp = candidate as Component;
        if (hitComp == null || candComp == null)
            return false;

        Transform hitTransform = hitComp.transform;
        Transform candTransform = candComp.transform;
        return hitTransform == candTransform ||
               hitTransform.IsChildOf(candTransform) ||
               candTransform.IsChildOf(hitTransform);
    }

    // =========================
    // Input Handling
    // =========================
    void HandleInput()
    {
        bool allowInteract = TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract);
        bool allowInteractSpmz = TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteractSpmz);
        bool allowGrab = TutorialInputGate.IsAllowed(TutorialInputGate.AllowGrab);
        bool allowPickupSpmz = TutorialInputGate.IsAllowed(TutorialInputGate.AllowPickupSpmz);
        bool allowSecondary = TutorialInputGate.IsAllowed(TutorialInputGate.AllowSecondary);
        bool allowDrop = TutorialInputGate.IsAllowed(TutorialInputGate.AllowDrop);
        bool allowThrow = TutorialInputGate.IsAllowed(TutorialInputGate.AllowThrow);

        if (_currentTarget != null)
        {
            bool isSpmzTarget = _currentTarget is Spirimonz spmzTarget && spmzTarget.isOnTheMap;
            bool allowCurrentInteract = isSpmzTarget ? allowInteractSpmz : allowInteract;

            if (allowCurrentInteract && ((!MobileInput.Enabled && Input.GetMouseButtonDown(0)) || MobileInput.PrimaryDown))
                _currentTarget.OnInteractStart();

            if (allowCurrentInteract && ((!MobileInput.Enabled && Input.GetMouseButton(0)) || MobileInput.PrimaryHeld))
                _currentTarget.OnInteractHold();

            if (allowCurrentInteract && ((!MobileInput.Enabled && Input.GetMouseButtonUp(0)) || MobileInput.PrimaryUp))
                _currentTarget.OnInteractEnd();

            bool interactionKeyDown = !MobileInput.Enabled &&
                                      (_player.inputManager.GetGrabDown() || _player.inputManager.GetWorldInteractionDownRaw());
            if (allowCurrentInteract && interactionKeyDown && !(_currentTarget is CatchableObject))
                _currentTarget.OnInteractStart();
        }

        if (objectInHands != null)
        {
            if (allowSecondary && ((!MobileInput.Enabled && Input.GetMouseButtonDown(1)) || MobileInput.SecondaryDown))
                objectInHands.OnSecondaryUse();

            if (allowDrop && ((!MobileInput.Enabled && _player.inputManager.GetDropDown()) || MobileInput.DropDown))
                DropObject();

            if (allowThrow && ((!MobileInput.Enabled && _player.inputManager.GetThrowDown()) || MobileInput.ThrowDown))
            {
                if (objectInHands.canBeThrownByPlayer)
                    ThrowObject();
                else
                    DropObject();
            }
        }
        else if (_currentCatchable != null)
        {
            if (allowGrab && ((!MobileInput.Enabled && _player.inputManager.GetGrabDown()) || MobileInput.GrabDown))
            {
                if (_currentCatchable.canBeGrabByPlayer && !_currentCatchable.isGrabbed)
                {
                    if (_player.inventoryManager.OccupedHands())
                    {
                        Spirimonz selectedSpirimonz = _player.inventoryManager.selectedSpirimonz;
                        if (selectedSpirimonz != null && !selectedSpirimonz.isOnTheMap)
                            _player.inventoryManager.ReplaceSpirimonzByAnItem();
                        else
                            return;
                    }

                    GrabItem(_currentCatchable);
                }
            }
        }
        else if (_currentTarget is Spirimonz spirimonz && spirimonz.isOnTheMap)
        {
            if (allowPickupSpmz && ((!MobileInput.Enabled && _player.inputManager.GetGrabDown()) || MobileInput.GrabDown))
                _player.inventoryManager.SpirimonzGoBackToHands(spirimonz);
        }
    }

    private bool IsInteractionBlockedByUI()
    {
        if (_uiGame == null)
            return false;

        return _uiGame.tablet != null && _uiGame.tablet.gameObject.activeSelf;
    }

    private void ClearInteractionTargets(bool releaseDoor)
    {
        if (releaseDoor && _grabbedDoor != null)
        {
            Rigidbody rb = _grabbedDoor.GetComponent<Rigidbody>();
            _grabbedDoor.Release();
            if (rb != null)
                rb.useGravity = true;

            _grabbedDoor = null;
        }

        _targetedDoor = null;
        _currentTarget = null;
        _currentCatchable = null;
        ClearNpcCTA();
        RefreshCursorUI();
    }

    private void UpdateNpcCTA(IInteractable lastTarget, IInteractable newTarget)
    {
        NPC lastNpc = lastTarget as NPC;
        NPC newNpc = newTarget as NPC;

        if (lastNpc != null && lastNpc != newNpc)
            lastNpc.CloseCTA();

        if (newNpc != null && newNpc != _currentNpcTarget && _player != null)
            newNpc.OpenCTA(_player);

        _currentNpcTarget = newNpc;
    }

    private void ClearNpcCTA()
    {
        if (_currentNpcTarget != null)
            _currentNpcTarget.CloseCTA();
        _currentNpcTarget = null;
    }

    // =========================
    // UI Handling
    // =========================
    void RefreshCursorUI()
    {
        if (!TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract) &&
            !TutorialInputGate.IsAllowed(TutorialInputGate.AllowPickupSpmz))
        {
            _uiGame.SetBigPointerSprite(null, 1f);
            _uiGame.EnableBigPointer(false);
            _uiGame.EnableGrabText(false);
            _lastShowCursor = false;
            _lastShowGrab = false;
            _lastCursorSprite = null;
            _lastCursorSize = 1f;
            return;
        }

        // showCursor = target exist OR door is grabbed
        bool showCursor = (_currentTarget != null && _currentTarget.InteractionLocked == false) || (_targetedDoor != null && _targetedDoor.InteractionLocked == false);
        
        Sprite sprite = null;
        float size = 1f;

        if (_currentTarget != null && _currentTarget.SpecialCursor)
        {
            sprite = _currentTarget.SpecialCursor;
            size = _currentTarget.CursorSize;
        }
        else if (_targetedDoor != null && _targetedDoor.SpecialCursor != null)
        {
            sprite = _targetedDoor.SpecialCursor;
            size = _targetedDoor.CursorSize;
        }

        if (sprite != _lastCursorSprite || Mathf.Abs(size - _lastCursorSize) > 0.01f)
        {
            _uiGame.SetBigPointerSprite(sprite, size);
            _lastCursorSprite = sprite;
            _lastCursorSize = size;
        }

        if (showCursor != _lastShowCursor)
        {
            _uiGame.EnableBigPointer(showCursor);
            _lastShowCursor = showCursor;
        }

        bool allowGrab = TutorialInputGate.IsAllowed(TutorialInputGate.AllowGrab);
        bool showGrab = allowGrab && objectInHands == null && _currentCatchable != null && _currentCatchable.canBeGrabByPlayer;
        if (showGrab != _lastShowGrab)
        {
            _uiGame.EnableGrabText(showGrab);
            _lastShowGrab = showGrab;
        }
    }

    // =========================
    // Door Handling (INCHANGÉ)
    // =========================
    private void HandleDoor()
    {
        if (!TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract))
        {
            if (_grabbedDoor != null)
            {
                Rigidbody rb = _grabbedDoor.GetComponent<Rigidbody>();
                _grabbedDoor.Release();
                if (rb != null)
                    rb.useGravity = true;
                _grabbedDoor = null;
            }
            _targetedDoor = null;
            return;
        }
        bool primaryDown = (!MobileInput.Enabled && Input.GetMouseButtonDown(0)) || MobileInput.PrimaryDown;
        bool primaryHeld = (!MobileInput.Enabled && Input.GetMouseButton(0)) || MobileInput.PrimaryHeld;
        bool primaryUp = (!MobileInput.Enabled && Input.GetMouseButtonUp(0)) || MobileInput.PrimaryUp;

        if (primaryUp)
        {
            if (_targetedDoor != null)
            {
                _targetedDoor.Release();
                _targetedDoor = null;
            }
        }

        Ray ray = (MobileInput.Enabled && MobileInput.HasPrimaryScreenPos)
            ? _cam.ScreenPointToRay(MobileInput.PrimaryScreenPos)
            : new Ray(_cam.transform.position, _cam.transform.forward);

        if (_grabbedDoor == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDoorsDistance, doorLayer))
            {
                Door door = hit.collider.GetComponent<Door>();
                if (door != null && door.InteractionLocked == false)
                    _targetedDoor = door;
                else if (!primaryHeld)
                    _targetedDoor = null;

                if (primaryDown && _targetedDoor != null)
                {
                    Rigidbody rb = _targetedDoor.rb;
                    HingeJoint hinge = _targetedDoor.hingeJoint;

                    if (rb != null && hinge != null)
                    {
                        _grabbedDoor = _targetedDoor;
                        _targetedDoor.Grab();
                        _grabDistance = hit.distance;
                        rb.useGravity = false;
                        rb.freezeRotation = false;
                    }
                }
            }
            else if (!primaryHeld && _targetedDoor != null)
            {
                _targetedDoor = null;
            }
        }
        else
        {
            _targetedDoor = _grabbedDoor;
        }

        if (_grabbedDoor != null)
        {
            Rigidbody rb = _grabbedDoor.GetComponent<Rigidbody>();
            Ray dragRay = (MobileInput.Enabled && MobileInput.HasPrimaryScreenPos)
                ? _cam.ScreenPointToRay(MobileInput.PrimaryScreenPos)
                : new Ray(_cam.transform.position, _cam.transform.forward);
            Vector3 targetPos = dragRay.origin + dragRay.direction * _grabDistance;
            rb.velocity = (targetPos - rb.position) * 30f;

            if (primaryUp)
            {
                if(_targetedDoor != null)
                    _targetedDoor.Release();
                
                rb.useGravity = true;
                _grabbedDoor = null;
                _targetedDoor = null;
            }
        }
    }
    
    private void DropObject()
    {
        if (objectInHands == null) return;

        objectInHands.ChangeLayer(_objectInHandLayerIndex, 0);

        Vector3 desiredDropPos = handObjectDropPosition.position;
        Vector3 dropPos = GetSafeDropPosition(desiredDropPos);

        // Fallback si aucun collider valide (ou bounds inutilisable)
        if (dropPos == desiredDropPos && TryGetDropWallHit(out RaycastHit hit))
            dropPos = hit.point - transform.forward * 0.25f; // recule un peu pour pas clipper
    
        objectInHands.Drop(dropPos, Vector3.zero);
        _player.handAnimator.SetInteger("HandPos", 1);
        
        OnDropItem?.Invoke(objectInHands);
        objectInHands = null;
    }

    private void ThrowObject()
    {
        if (objectInHands == null) return;

        Vector3 forward = _player.GetForward();
        Vector3 throwForce = forward * throwForceForward;
        Vector3 dropPos = handObjectDropPosition.position;
                
        // Check collision avant de lancer
        if (DetectCollisionForward())
        {
            dropPos = _lastWallHitPos - transform.forward * 0.5f; // recule un peu pour pas clipper
            throwForce = Vector3.zero;
        }

        objectInHands.ChangeLayer(_objectInHandLayerIndex, 0);
        objectInHands.Drop(dropPos, throwForce);
        _player.handAnimator.SetInteger("HandPos", 1);
        
        OnDropItem?.Invoke(objectInHands);
        objectInHands = null;
    }

    private float _detectionWallDistance = 0.85f;
    private bool TryGetDropWallHit(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = _player != null && _player.camera != null
            ? _player.camera.transform.forward
            : transform.forward;
        float distance = _detectionWallDistance;

        return Physics.Raycast(origin, direction, out hit, distance, ~0, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetSafeDropPosition(Vector3 desiredDropPos)
    {
        if (objectInHands == null)
            return desiredDropPos;

        Vector3 startPos = objectInHands.transform.position;
        Vector3 toTarget = desiredDropPos - startPos;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
            return desiredDropPos;

        if (!TryGetObjectBounds(objectInHands, out Bounds bounds))
            return desiredDropPos;

        Vector3 direction = toTarget / distance;
        Vector3 centerOffset = bounds.center - objectInHands.transform.position;
        Vector3 startCenter = startPos + centerOffset;
        Quaternion orientation = objectInHands.transform.rotation;
        int mask = GetDropCollisionMask();
        const float skin = 0.02f;

        if (Physics.BoxCast(startCenter, bounds.extents, direction, out RaycastHit hit, orientation, distance, mask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - skin);
            Vector3 safeCenter = startCenter + direction * safeDistance;
            return safeCenter - centerOffset;
        }

        return desiredDropPos;
    }

    private bool TryGetObjectBounds(CatchableObject catchable, out Bounds bounds)
    {
        bounds = new Bounds(catchable.transform.position, Vector3.zero);
        bool hasBounds = false;

        Collider[] colliders = catchable.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds;
    }

    private int GetDropCollisionMask()
    {
        int mask = ~0;

        // Ignore player layer
        mask &= ~(1 << gameObject.layer);

        // Ignore FPS hand/object layer (object in hands is moved here)
        if (_player != null && _player.inventoryManager != null)
            mask &= ~_player.inventoryManager.fpsMask.value;

        return mask;
    }

#if UNITY_EDITOR
    private void DebugDropRaycast()
    {
        if (!debugDropRaycast || _player == null || _player.camera == null)
        {
            debugDropHitTransform = null;
            return;
        }

        if (objectInHands == null)
        {
            debugDropHitTransform = null;
            return;
        }

        Vector3 desiredDropPos = handObjectDropPosition.position;
        Vector3 startPos = objectInHands.transform.position;
        Vector3 toTarget = desiredDropPos - startPos;
        float distance = toTarget.magnitude;
        Vector3 direction = distance > 0.001f ? toTarget / distance : _player.camera.transform.forward;

        bool hit = false;
        RaycastHit hitInfo = new RaycastHit();

        if (distance > 0.001f && TryGetObjectBounds(objectInHands, out Bounds bounds))
        {
            Vector3 centerOffset = bounds.center - objectInHands.transform.position;
            Vector3 startCenter = startPos + centerOffset;
            int mask = GetDropCollisionMask();
            hit = Physics.BoxCast(startCenter, bounds.extents, direction, out hitInfo, objectInHands.transform.rotation, distance, mask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            hit = TryGetDropWallHit(out hitInfo);
        }

        debugDropHitTransform = hit ? hitInfo.transform : null;
        Debug.DrawRay(startPos, direction * distance, hit ? Color.red : Color.green);
    }
#endif

    private CatchableObject GetCatchableFromInteractable(IInteractable interactable)
    {
        if (interactable is CatchableObject catchable)
            return catchable;

        Component comp = interactable as Component;
        if (comp == null)
            return null;

        CatchableObject direct = comp.GetComponent<CatchableObject>();
        if (direct != null)
            return direct;

        return comp.GetComponentInParent<CatchableObject>();
    }

    public void GrabItem(CatchableObject catchableObject)
    {
        if (objectInHands != null)
        {
            objectInHands.Drop(transform.position, Vector3.zero);
        }
    
        objectInHands = catchableObject;
        _objectInHandLayerIndex = objectInHands.gameObject.layer;
        _player.inventoryManager.SetHandsStateNull();
        objectInHands.ChangeLayer(_player.inventoryManager.fpsMask);
        objectInHands.Grab(handObjectPosition);
    
        OnGrabItem?.Invoke(catchableObject);

        _currentTarget = null;
        _currentCatchable = null;
        RefreshCursorUI();
    }
    
    private Vector3 _lastWallHitPos;
    public bool DetectCollisionForward()
    {
        Vector3 origin = _player.camera.transform.position;
        Vector3 direction = _player.GetForward().normalized;
        float distance = 1f;

        // Crée le Ray
        Ray ray = new Ray(origin, direction);

        // Détecte le mur
        if (Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            _lastWallHitPos = hit.point;
            return true;
        }

        // Debug du ray
        //Debug.DrawRay(origin, direction * distance, Color.magenta, 1f); // couleur rouge, durée 1 sec

        return false;
    }

    public Vector3 GetLastGroundPos()
    {
        if (MobileInput.Enabled && MobileInput.HasPrimaryScreenPos && _cam != null)
        {
            Ray ray = _cam.ScreenPointToRay(MobileInput.PrimaryScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, groundLayer, QueryTriggerInteraction.Ignore))
                return hit.point;
        }

        return targetingGround ? _lastGroundPosTargeted : Vector3.zero;
    }

    public bool HasTarget()
    {
        return _currentTarget != null;
    }

    public bool IsDoorGrabbed()
    {
        return _grabbedDoor != null;
    }

    public bool IsDoorTargeted()
    {
        return _targetedDoor != null;
    }

    public bool TryGetDoorUnderScreenPoint(Vector2 screenPos, out Door door)
    {
        door = null;
        if (_cam == null)
            return false;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDoorsDistance, doorLayer))
        {
            Door d = hit.collider.GetComponent<Door>();
            if (d != null && !d.InteractionLocked)
            {
                door = d;
                return true;
            }
        }

        return false;
    }
}
