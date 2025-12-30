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
    }

    private void HandleUICursor()
    {
        UIGame.Instance.EnableCursor(_targetedClickableObject != null || _targetedThrowableObject != null);
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