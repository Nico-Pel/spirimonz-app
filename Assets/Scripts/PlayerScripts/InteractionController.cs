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

    private Door _targetedDoor;
    private Door _grabbedDoor;
    private float _grabDistance;

    private IInteractable _currentTarget;
    private IInteractable _lastTarget;

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
        HandleDoor();
        DetectInteractable();
        HandleInput();
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

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            targetingGround = (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
            _lastGroundPosTargeted = hit.point;
            newTarget = hit.collider.GetComponent<IInteractable>();

            if (newTarget != null && newTarget.InteractionLocked)
                newTarget = null;

            if (newTarget != null && objectInHands != null && newTarget is CatchableObject)
                newTarget = null;

            // Prioriser un Catchable centré à l'écran (si on ne porte rien).
            if (objectInHands == null)
            {
                RaycastHit[] hits = Physics.SphereCastAll(ray, sphereRadius, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore);
                IInteractable bestCatchable = null;
                float bestScreenDist = Mathf.Infinity;
                float bestDistance = Mathf.Infinity;
                const float screenEpsilon = 0.000001f;

                for (int i = 0; i < hits.Length; i++)
                {
                    IInteractable candidate = hits[i].collider.GetComponent<IInteractable>();
                    if (candidate == null)
                        continue;
                    if (candidate.InteractionLocked)
                        continue;
                    if (candidate is not CatchableObject)
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
                    newTarget = bestCatchable;
            }
        }
        else
        {
            targetingGround = false;
        }

        if (newTarget != _currentTarget)
        {
            _lastTarget = _currentTarget;
            _currentTarget = newTarget;
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
        return hitInteractable == candidate;
    }

    // =========================
    // Input Handling
    // =========================
    void HandleInput()
    {
        if (_currentTarget != null)
        {
            if ((!MobileInput.Enabled && Input.GetMouseButtonDown(0)) || MobileInput.PrimaryDown)
                _currentTarget.OnInteractStart();

            if ((!MobileInput.Enabled && Input.GetMouseButton(0)) || MobileInput.PrimaryHeld)
                _currentTarget.OnInteractHold();

            if ((!MobileInput.Enabled && Input.GetMouseButtonUp(0)) || MobileInput.PrimaryUp)
                _currentTarget.OnInteractEnd();
        }

        if (objectInHands != null)
        {
            if ((!MobileInput.Enabled && Input.GetMouseButtonDown(1)) || MobileInput.SecondaryDown)
                objectInHands.SpecialActionInHandsOnClick();

            if ((!MobileInput.Enabled && _player.inputManager.GetDropDown()) || MobileInput.DropDown)
                DropObject();

            if ((!MobileInput.Enabled && _player.inputManager.GetThrowDown()) || MobileInput.ThrowDown)
            {
                if (objectInHands.canBeThrownByPlayer)
                    ThrowObject();
                else
                    DropObject();
            }
        }
        else if (_currentTarget is CatchableObject targetedCatchable)
        {
            if (_player.inventoryManager.OccupedHands()) return;

            if ((!MobileInput.Enabled && _player.inputManager.GetGrabDown()) || MobileInput.GrabDown)
            {
                if (targetedCatchable.canBeGrabByPlayer && !targetedCatchable.isGrabbed)
                {
                    _player.inventoryManager.ReplaceSpirimonzByAnItem();
                    GrabItem(targetedCatchable);
                }
            }
        }
        else if (_currentTarget is Spirimonz spirimonz && spirimonz.isOnTheMap)
        {
            if ((!MobileInput.Enabled && _player.inputManager.GetGrabDown()) || MobileInput.GrabDown)
                _player.inventoryManager.SpirimonzGoBackToHands(spirimonz);
        }
    }

    // =========================
    // UI Handling
    // =========================
    void RefreshCursorUI()
    {
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

        bool showGrab = _currentTarget is CatchableObject c && c.canBeGrabByPlayer;
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

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDoorsDistance, doorLayer))
        {
            if (_targetedDoor == null)
            {
                Door door =  hit.collider.GetComponent<Door>();
                if(door.InteractionLocked == false)
                    _targetedDoor = door;
            }
            
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
        else if (!primaryHeld && _targetedDoor != null && _grabbedDoor == null)
        {
            _targetedDoor = null;
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

        Vector3 dropPos = handObjectDropPosition.position;
                
        // Check si un mur est juste devant
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, _player.camera.transform.forward, out RaycastHit hit, 0.65f))
        {
            dropPos = hit.point - transform.forward * 0.25f; // recule un peu pour pas clipper
        }
    
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
