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
        //Raycast
        RaycastHit hit;

        if (Physics.Raycast(controller.playerCamera.transform.position, controller.playerCamera.transform.forward, out hit, 20, doorLayer))
        {
            _targetingDoor = true;
            if (Input.GetMouseButtonDown(0))
            {
                _targetedDoor = hit.collider.gameObject.transform.GetComponent<Door>();
            }
        }
        else
        {
            _targetingDoor = false;
        }

        if (_targetedDoor != null)
        {
            HingeJoint joint = _targetedDoor.hingeJoint;
            JointMotor motor = joint.motor;

            //Create drag point object for reference where players mouse is pointing
            if (_dragPointDoor == null)
            {
                _dragPointDoor = new GameObject("Ray door");
                _dragPointDoor.transform.parent = _targetedDoor.transform;
            }

            Ray ray = controller.playerCamera.ScreenPointToRay(Input.mousePosition);
            _dragPointDoor.transform.position =
                ray.GetPoint(Vector3.Distance(_targetedDoor.transform.position, transform.position));
            _dragPointDoor.transform.rotation = _targetedDoor.transform.rotation;


            float delta = Mathf.Pow(Vector3.Distance(_dragPointDoor.transform.position, _targetedDoor.transform.position), 3);

            //Deciding if it is left or right door
            if (_targetedDoor.GetComponent<MeshRenderer>().localBounds.center.x > _targetedDoor.transform.localPosition.x)
            {
                _doorValue = 1;
            }
            else
            {
                _doorValue = -1;
            }

            //Applying velocity to door motor
            float speedMultiplier = 60000;
            if (Mathf.Abs(_targetedDoor.transform.parent.forward.z) > 0.5f)
            {
                if (_dragPointDoor.transform.position.x > _targetedDoor.transform.position.x)
                {
                    motor.targetVelocity = delta * speedMultiplier * Time.deltaTime * _doorValue;
                }
                else
                {
                    motor.targetVelocity = delta * -speedMultiplier * Time.deltaTime * _doorValue;
                }
            }
            else
            {
                if (_dragPointDoor.transform.position.z > _targetedDoor.transform.position.z)
                {
                    motor.targetVelocity = delta * speedMultiplier * Time.deltaTime * _doorValue;
                }
                else
                {
                    motor.targetVelocity = delta * -speedMultiplier * Time.deltaTime * _doorValue;
                }
            }

            joint.motor = motor;

            if (Input.GetMouseButtonUp(0))
            {
                _targetedDoor = null;
                motor.targetVelocity = 0;
                joint.motor = motor;
                Destroy(_dragPointDoor);
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