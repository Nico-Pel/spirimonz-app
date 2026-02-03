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
    private GameObject _grabbedDoor; 
    private float _grabDistance;

    private IInteractable _currentTarget;
    
    private Camera _cam;
    private Player _player;
    
    public UnityEvent<CatchableObject> OnGrabItem;
    public UnityEvent<CatchableObject> OnDropItem;

    void Awake()
    {
        _cam = Camera.main;
    }

    private void Start()
    {
        _player = Player.Instance;
    }

    void Update()
    {
        HandleDoor();

        if (objectInHands == null)
        {
            DetectInteractable();
        }
        
        HandleInput();
        UpdateCursorUI();
    }

    // =========================
    // Raycast & Detection
    // =========================
    void DetectInteractable()
    {
        Vector3 rayOrigin = _cam.transform.position + _cam.transform.forward * rayOffset;
        Ray ray = new Ray(rayOrigin, _cam.transform.forward);

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            targetingGround = (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0; //Detect if player is targeting ground to drop a spirimonz
            _lastGroundPosTargeted = hit.point;
            _currentTarget = hit.collider.GetComponent<IInteractable>();
        }
        else
        {
            targetingGround = false;
            _currentTarget = null;
        }
    }

    // =========================
    // Input Handling
    // =========================
    void HandleInput()
    {
        // =====================
        // Interaction avec l'objet ciblé
        // =====================
        if (_currentTarget != null)
        {
            if (Input.GetMouseButtonDown(0))
                _currentTarget.OnInteractStart();

            if (Input.GetMouseButton(0))
                _currentTarget.OnInteractHold();

            if (Input.GetMouseButtonUp(0))
                _currentTarget.OnInteractEnd();
        }

        // =====================
        // Grab / Drop / Throw d'objet en main
        // =====================
        if (objectInHands != null)
        {
            if (Input.GetMouseButtonDown(1))
            {
                objectInHands.SpecialActionInHandsOnClick();
            }
            
            // Drop
            if (Input.GetKeyDown(_player.fpsController.dropObject))
            {
                DropObject();
            }

            // Throw
            if (Input.GetKeyDown(_player.fpsController.throwObject))
            {
                if (objectInHands != null)
                {
                    if (objectInHands.canBeThrownByPlayer)
                    {
                        ThrowObject();
                    }
                    else
                    {
                        DropObject();
                    }
                }
            }
        }
        else if (_currentTarget is CatchableObject targetedCatchable)
        {
            // Grab uniquement si rien en main ni en mode caméra
            if (_player.inventoryManager.OccupedHands()) return;
            
            if (Input.GetKeyDown(_player.fpsController.grabObject))
            {
                if (targetedCatchable.canBeGrabByPlayer && !targetedCatchable.isGrabbed)
                {
                    //If the player has a Spirimonz in hands, unequip it
                    _player.inventoryManager.ReplaceSpirimonzByAnItem();
                    
                    //Grab item
                    GrabItem(targetedCatchable);
                }
            }
        }
        else if (_currentTarget is Spirimonz spirimonz && spirimonz.isOnTheMap)
        {
            // Grab uniquement si rien en main
            if (Input.GetKeyDown(_player.fpsController.grabObject))
            {
                _player.inventoryManager.SpirimonzGoBackToHands(spirimonz);
            }
        }
    }

    private void DropObject()
    {
        if (objectInHands == null) return;

        objectInHands.ChangeLayer(_objectInHandLayerIndex, 0);

        Vector3 dropPos = handObjectDropPosition.position;
                
        // Check si un mur est juste devant
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, _player.fpsController.playerCamera.transform.forward, out RaycastHit hit, 0.65f))
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

    private Vector3 _lastWallHitPos;
    public bool DetectCollisionForward()
    {
        Player player = _player;
        Vector3 origin = player.fpsController.playerCamera.transform.position;
        Vector3 direction = player.GetForward().normalized;
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
    }
    
    // =========================
    // UI Handling
    // =========================
    bool _grabTextVisible;

    bool _lastShowCursor;
    bool _lastShowGrab;

    void UpdateCursorUI()
    {
        bool showCursor = _currentTarget != null || _targetedDoor != null;
        if (showCursor != _lastShowCursor)
        {
            UIGame.Instance.EnableBigPointer(showCursor);
            _lastShowCursor = showCursor;
        }

        bool showGrab = _currentTarget is CatchableObject c && c.canBeGrabByPlayer;
        if (showGrab != _lastShowGrab)
        {
            UIGame.Instance.EnableGrabText(showGrab);
            _lastShowGrab = showGrab;
        }
    }

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
        
        // Raycast pour détecter la porte
        RaycastHit hit;
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if (Physics.Raycast(ray, out hit, interactionDoorsDistance, doorLayer))
        {
            if(_targetedDoor == null)
                _targetedDoor = hit.collider.GetComponent<Door>();
            
            if (Input.GetMouseButtonDown(0))
            {
                Rigidbody rb = _targetedDoor.rb;
                HingeJoint hinge = _targetedDoor.hingeJoint;
                if (rb != null && hinge != null)
                {
                    _grabbedDoor = _targetedDoor.gameObject;
                    _targetedDoor.Grab();
                    _grabDistance = Vector3.Distance(_player.fpsController.playerCamera.transform.position, _grabbedDoor.transform.position);
                    rb.useGravity = false;          // on désactive la gravité pendant qu’on tire
                    rb.freezeRotation = false;      // on autorise le Hinge à tourner
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
            if (rb != null)
            {
                Vector3 targetPos = _cam.transform.position + _cam.transform.forward * _grabDistance;
                rb.velocity = (targetPos - rb.position) * 30f;
                //rb.position = Vector3.Lerp(rb.position, targetPos, Time.deltaTime * 10f);
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_targetedDoor != null)
                {
                    _targetedDoor.Release();
                    _targetedDoor = null;
                }
                
                rb.useGravity = true;
                _grabbedDoor = null;
            }
        }
    }

    public Vector3 GetLastGroundPos()
    {
        if (!targetingGround)
            return Vector3.zero;
        
        return _lastGroundPosTargeted;
    }

    public bool HasTarget()
    {
        return _currentTarget != null;
    }
}