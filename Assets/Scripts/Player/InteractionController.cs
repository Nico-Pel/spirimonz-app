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

    [ReadOnly] public bool targetingGround;
    private Vector3 _lastGroundPosTargeted;

    [Header("Hand Settings")]
    public Transform handObjectPosition;
    public Transform handObjectDropPosition;
    public float throwForceForward = 5f;

    [ReadOnly] public ThrowableObject objectInHands;
    
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
            // Drop
            if (Input.GetKeyDown(Player.Instance.fpsController.dropObject))
            {
                objectInHands.Drop(handObjectDropPosition, Vector3.zero);
                objectInHands = null;
            }

            // Throw
            if (Input.GetKeyDown(Player.Instance.fpsController.throwObject))
            {
                Vector3 throwForce = transform.forward * throwForceForward;
                objectInHands.Drop(handObjectDropPosition, throwForce);
                objectInHands = null;
            }
        }
        else if (_currentTarget is ThrowableObject targetThrowable)
        {
            // Grab uniquement si rien en main
            if (Input.GetKeyDown(Player.Instance.fpsController.grabObject))
            {
                objectInHands = targetThrowable;

                if (!objectInHands.isGrabbed)
                {
                    //If the player has a spirimonz in hands, unequip it
                    Player.Instance.inventoryManager.ReplaceSpirimonzByAnItem();
                    
                    //Grab item
                    objectInHands.Grab(handObjectPosition);
                }
            }
        }
        else if (_currentTarget is Spirimonz spirimonz && spirimonz.isOnTheMap == true)
        {
            // Grab uniquement si rien en main
            if (Input.GetKeyDown(Player.Instance.fpsController.grabObject))
            {
                Player.Instance.inventoryManager.SpirimonzGoBackToHands(spirimonz);
            }
        }
    }
    
    // =========================
    // UI Handling
    // =========================
    void UpdateCursorUI()
    {
        bool showCursor = _currentTarget != null;
        UIGame.Instance.EnableCursor(showCursor || _targetedDoor != null);
        bool showGrabText = _currentTarget is ThrowableObject;
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
        Ray ray = Player.Instance.fpsController.playerCamera.ScreenPointToRay(Input.mousePosition);

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
                Vector3 targetPos = Player.Instance.fpsController.playerCamera.transform.position + ray.direction * _grabDistance;
                rb.velocity = (targetPos - rb.position) * 20f;
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