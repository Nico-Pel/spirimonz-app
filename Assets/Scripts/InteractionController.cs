using Unity.Collections;
using UnityEngine;

public class InteractionController : GameBehaviour
{
    public FPSControllerNoPhysics controller;
    
    [Header("Raycast Settings")]
    public float interactionDistance = 3f; // Distance max pour interagir
    public LayerMask interactableLayer;    // Layer des objets interactifs
    public float rayOffset = 0.2f;         // Décalage devant la caméra
    public float sphereRadius = 0.1f;      // Rayon du SphereCast

    [Header("Hand object Settings")]
    [ReadOnly] public ThrowableObject objectInHands;
    public Transform handObjectPosition;
    public Transform handObjectDropPosition;
    public float throwForceForward = 5;
    //public float throwForceUp = 3;

    [Header("Doors Settings")] 
    [SerializeField] LayerMask doorLayer;
    private bool _targetingDoor;
    private Door _targetedDoor;
    private GameObject _dragPointDoor;
    private int _doorValue = 0;
    
    private ClickableObject _targetedClickableObject;
    private ThrowableObject _targetedThrowableObject;
    
    private GameObject _grabbedDoor;
    private float _grabDistance;

    void Update()
    {
        HandleRaycast();
        HandleInput();

        if (Input.GetKeyDown(controller.grabObject) && _targetedThrowableObject && objectInHands == null)
        {
            GrabObject();
        }

        if (Input.GetKeyDown(controller.dropObject) && objectInHands != null)
        {
            DropObject(false);
        }
        
        if (Input.GetKeyDown(controller.throwObject) && objectInHands != null)
        {
            DropObject(true);
        }
        
        HandleUICursor();
        HandleUIText();
        HandleDoor();
    }

    private void HandleDoor()
    {
        // Raycast pour détecter la porte
        RaycastHit hit;
        Ray ray = controller.playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 5f, doorLayer))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                HingeJoint hinge = hit.collider.GetComponent<HingeJoint>();
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
            }
        }
    }

    private void HandleUICursor()
    {
        UIGame.Instance.EnableCursor(_targetedClickableObject != null || _targetedThrowableObject != null || _targetedDoor != null || (_targetedDoor == null && _targetingDoor));
    }
    
    private void HandleUIText()
    {
        UIGame.Instance.EnableGrabText(_targetedThrowableObject != null);
    }

    void HandleRaycast()
    {
        Vector3 rayOrigin = Camera.main.transform.position + Camera.main.transform.forward * rayOffset;
        Ray ray = new Ray(rayOrigin, Camera.main.transform.forward);

        // SphereCast pour plus de tolérance
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out ClickableObject clickableObject))
            {
                _targetedClickableObject = clickableObject;
            }
            else
            {
                _targetedClickableObject = null;
            }
            
            if (objectInHands == null && hit.collider.TryGetComponent(out ThrowableObject throwableObject) && throwableObject.canBeGrabByPlayer && throwableObject.isGrabbed == false)
            {
                _targetedThrowableObject = throwableObject;
            }
            else
            {
                _targetedThrowableObject = null;
            }
        }
        else
        {
            _targetedClickableObject = null;
            _targetedThrowableObject = null;
            _targetingDoor = false;
        }
    }

    void HandleInput()
    {
        if (_targetedClickableObject == null) return;

        // Clic pressé
        if (Input.GetMouseButtonDown(0) && _targetedClickableObject.canClick)
        {
            _targetedClickableObject.OnClick();
        }

        // Maintien du clic
        if (Input.GetMouseButton(0) && _targetedClickableObject.canHold)
        {
            _targetedClickableObject.OnHold();
        }

        // Relâchement du clic
        if (Input.GetMouseButtonUp(0) && _targetedClickableObject.canRelease)
        {
            _targetedClickableObject.OnRelease();
        }
    }

    void GrabObject()
    {
        if (objectInHands != null) return;
        
        objectInHands = _targetedThrowableObject;
        
        objectInHands.isGrabbed = true;
        objectInHands.transform.parent = handObjectPosition.transform;
        objectInHands.rb.isKinematic = true;
        objectInHands.transform.position = handObjectPosition.position;
        objectInHands.transform.localEulerAngles = Vector3.zero;
    }

    void DropObject(bool throwObject = false)
    {
        if (objectInHands == null) return;

        objectInHands.transform.parent = House.Instance.transform;
        objectInHands.transform.position = handObjectDropPosition.position;
        objectInHands.rb.isKinematic = false;
        objectInHands.isGrabbed = false;

        if (throwObject)
        {
            objectInHands.rb.AddForce(transform.forward * throwForceForward /*+ Vector3.up * throwForceUp*/, ForceMode.Impulse);
        }
        
        objectInHands = null;
    }
}