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
    public float sphereRadius = 0.1f;

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
        Vector3 rayOrigin = _cam.transform.position + _cam.transform.forward * rayOffset;
        Ray ray = new Ray(rayOrigin, _cam.transform.forward);

        IInteractable newTarget = null;

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            targetingGround = (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
            _lastGroundPosTargeted = hit.point;
            newTarget = hit.collider.GetComponent<IInteractable>();
            
            if (newTarget != null && newTarget.InteractionLocked) 
                newTarget = null;
            
            if (newTarget != null && objectInHands != null && newTarget is CatchableObject)
            {
                newTarget = null;
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

    // =========================
    // Input Handling
    // =========================
    void HandleInput()
    {
        if (_currentTarget != null)
        {
            if (Input.GetMouseButtonDown(0))
                _currentTarget.OnInteractStart();

            if (Input.GetMouseButton(0))
                _currentTarget.OnInteractHold();

            if (Input.GetMouseButtonUp(0))
                _currentTarget.OnInteractEnd();
        }

        if (objectInHands != null)
        {
            if (Input.GetMouseButtonDown(1))
                objectInHands.SpecialActionInHandsOnClick();

            if (Input.GetKeyDown(_player.inputManager.dropObject))
                DropObject();

            if (Input.GetKeyDown(_player.inputManager.throwObject))
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

            if (Input.GetKeyDown(_player.inputManager.grabObject))
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
            if (Input.GetKeyDown(_player.inputManager.grabObject))
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
        if (Input.GetMouseButtonUp(0))
        {
            if (_targetedDoor != null)
            {
                _targetedDoor.Release();
                _targetedDoor = null;
            }
        }

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDoorsDistance, doorLayer))
        {
            if (_targetedDoor == null)
            {
                Door door =  hit.collider.GetComponent<Door>();
                if(door.InteractionLocked == false)
                    _targetedDoor = door;
            }
            
            if (Input.GetMouseButtonDown(0) && _targetedDoor != null)
            {
                Rigidbody rb = _targetedDoor.rb;
                HingeJoint hinge = _targetedDoor.hingeJoint;

                if (rb != null && hinge != null)
                {
                    _grabbedDoor = _targetedDoor;
                    _targetedDoor.Grab();
                    _grabDistance = Vector3.Distance(_cam.transform.position, _grabbedDoor.transform.position);
                    rb.useGravity = false;
                    rb.freezeRotation = false;
                }
            }
        }
        else if (!Input.GetMouseButton(0) && _targetedDoor != null && _grabbedDoor == null)
        {
            _targetedDoor = null;
        }

        if (_grabbedDoor != null)
        {
            Rigidbody rb = _grabbedDoor.GetComponent<Rigidbody>();
            Vector3 targetPos = _cam.transform.position + _cam.transform.forward * _grabDistance;
            rb.velocity = (targetPos - rb.position) * 30f;

            if (Input.GetMouseButtonUp(0))
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
        return targetingGround ? _lastGroundPosTargeted : Vector3.zero;
    }

    public bool HasTarget()
    {
        return _currentTarget != null;
    }
}
