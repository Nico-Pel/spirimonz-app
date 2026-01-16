using Unity.Collections;
using UnityEngine;

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

    void Update()
    {
        HandleDoor();
        DetectInteractable();
        HandleInput();
        UpdateCursorUI();
    }

    // =========================
    // Raycast & Detection
    // =========================
    void DetectInteractable()
    {
        Vector3 rayOrigin = Camera.main.transform.position + Camera.main.transform.forward * rayOffset;
        Ray ray = new Ray(rayOrigin, Camera.main.transform.forward);

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
            if (Input.GetMouseButtonDown(0))
            {
                objectInHands.SpecialActionInHandsOnClick();
            }
            
            // Drop
            if (Input.GetKeyDown(Player.Instance.fpsController.dropObject))
            {
                DropObject();
            }

            // Throw
            if (Input.GetKeyDown(Player.Instance.fpsController.throwObject))
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
            if (Player.Instance.inventoryManager.OccupedHands()) return;
            
            if (Input.GetKeyDown(Player.Instance.fpsController.grabObject))
            {
                if (targetedCatchable.canBeGrabByPlayer && !targetedCatchable.isGrabbed)
                {
                    //If the player has a Spirimonz in hands, unequip it
                    Player.Instance.inventoryManager.ReplaceSpirimonzByAnItem();
                    
                    //Grab item
                    GrabItem(targetedCatchable);
                }
            }
        }
        else if (_currentTarget is Spirimonz spirimonz && spirimonz.isOnTheMap == true && spirimonz.canBeTakenBackIntoHands)
        {
            // Grab uniquement si rien en main
            if (Input.GetKeyDown(Player.Instance.fpsController.grabObject))
            {
                Player.Instance.inventoryManager.SpirimonzGoBackToHands(spirimonz);
            }
        }
    }

    private void DropObject()
    {
        if (objectInHands == null) return;

        objectInHands.ChangeLayer(_objectInHandLayerIndex, 0);

        Vector3 dropPos = handObjectDropPosition.position;
                
        // Check si un mur est juste devant
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Player.Instance.fpsController.playerCamera.transform.forward, out RaycastHit hit, 0.65f))
        {
            dropPos = hit.point - transform.forward * 0.25f; // recule un peu pour pas clipper
        }
    
        objectInHands.Drop(dropPos, Vector3.zero);
        objectInHands = null;
    }

    private void ThrowObject()
    {
        if (objectInHands == null) return;
        
        Vector3 throwDir = Player.Instance.fpsController.playerCamera.transform.forward;

        Vector3 throwForce = throwDir * throwForceForward;
        Vector3 dropPos = handObjectDropPosition.position;
                
        // Check collision avant de lancer
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, throwDir, out RaycastHit hit, 0.75f))
        {
            dropPos = hit.point - transform.forward * 0.25f; // recule un peu pour pas clipper
            throwForce = Vector3.zero;
        }

        objectInHands.ChangeLayer(_objectInHandLayerIndex, 0);
        objectInHands.Drop(dropPos, throwForce);
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
        Player.Instance.inventoryManager.SetHandsStateNull();
        objectInHands.ChangeLayer(Player.Instance.inventoryManager.fpsMask);
        objectInHands.Grab(handObjectPosition);
    }
    
    // =========================
    // UI Handling
    // =========================
    bool _grabTextVisible;

    void UpdateCursorUI()
    {
        bool showCursor = _currentTarget != null;
        UIGame.Instance.EnableCursor(showCursor || _targetedDoor != null);

        bool showGrabText = _currentTarget is CatchableObject;
        UIGame.Instance.EnableGrabText(showGrabText);
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
        Transform cam = Player.Instance.fpsController.playerCamera.transform;
        Ray ray = new Ray(cam.position, cam.forward);

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
                    _grabDistance = Vector3.Distance(Player.Instance.fpsController.playerCamera.transform.position, _grabbedDoor.transform.position);
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
                Vector3 targetPos = cam.position + cam.forward * _grabDistance;
                rb.velocity = (targetPos - rb.position) * 30f;
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
}