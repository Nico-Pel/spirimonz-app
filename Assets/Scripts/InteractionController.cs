using Unity.Collections;
using UnityEngine;

public class InteractionController : GameBehaviour
{
    public FPSControllerNoPhysics controller;

    [Header("Raycast Settings")]
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;
    public float rayOffset = 0.2f;
    public float sphereRadius = 0.1f;

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
        DetectInteractable();
        HandleInput();
        UpdateCursorUI();
        HandleDoor();
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
            _currentTarget = hit.collider.GetComponent<IInteractable>();
        }
        else
        {
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
            if (Input.GetKeyDown(controller.dropObject))
            {
                objectInHands.Drop(handObjectDropPosition, Vector3.zero);
                objectInHands = null;
            }

            // Throw
            if (Input.GetKeyDown(controller.throwObject))
            {
                Vector3 throwForce = transform.forward * throwForceForward;
                objectInHands.Drop(handObjectDropPosition, throwForce);
                objectInHands = null;
            }
        }
        else if (_currentTarget is ThrowableObject targetThrowable)
        {
            // Grab uniquement si rien en main
            if (Input.GetKeyDown(controller.grabObject))
            {
                objectInHands = targetThrowable;
                objectInHands.Grab(handObjectPosition);
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
        // Raycast pour détecter la porte
        RaycastHit hit;
        Ray ray = controller.playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 5f, doorLayer))
        {
            if(_targetedDoor == null)
                _targetedDoor = hit.collider.GetComponent<Door>();
            
            if (Input.GetMouseButtonDown(0))
            {
                Rigidbody rb = _targetedDoor.rb;
                HingeJoint hinge = _targetedDoor.hingeJoint;
                if (rb != null && hinge != null)
                {
                    _grabbedDoor = hit.collider.gameObject;
                    _grabDistance = Vector3.Distance(controller.playerCamera.transform.position, _grabbedDoor.transform.position);
                    rb.useGravity = false;          // on désactive la gravité pendant qu’on tire
                    rb.freezeRotation = false;      // on autorise le Hinge à tourner
                }
            }
        }

        if (_grabbedDoor != null)
        {
            Rigidbody rb = _grabbedDoor.GetComponent<Rigidbody>();
            if (rb == null) return;

            // Calcul du point cible devant la caméra
            Vector3 targetPos = controller.playerCamera.transform.position + ray.direction * _grabDistance;

            // Appliquer la vitesse pour suivre la souris
            rb.velocity = (targetPos - rb.position) * 10f;

            // Relâchement du clic
            if (Input.GetMouseButtonUp(0))
            {
                rb.useGravity = true;
                _grabbedDoor = null;
                _targetedDoor = null;
            }
        }
    }
}