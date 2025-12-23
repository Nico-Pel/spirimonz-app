using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 3f; // Distance max pour interagir
    public LayerMask interactableLayer;    // Layer des objets interactifs
    public float rayOffset = 0.2f;         // Décalage devant la caméra
    public float sphereRadius = 0.1f;      // Rayon du SphereCast

    private ClickableObject currentObject;

    void Update()
    {
        HandleRaycast();
        HandleInput();
    }

    void HandleRaycast()
    {
        Vector3 rayOrigin = Camera.main.transform.position + Camera.main.transform.forward * rayOffset;
        Ray ray = new Ray(rayOrigin, Camera.main.transform.forward);

        // SphereCast pour plus de tolérance
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
        {
            currentObject = hit.collider.GetComponent<ClickableObject>();
        }
        else
        {
            currentObject = null;
        }
    }

    void HandleInput()
    {
        if (currentObject == null) return;

        // Clic pressé
        if (Input.GetMouseButtonDown(0) && currentObject.canClick)
        {
            currentObject.OnClick();
        }

        // Maintien du clic
        if (Input.GetMouseButton(0) && currentObject.canHold)
        {
            currentObject.OnHold();
        }

        // Relâchement du clic
        if (Input.GetMouseButtonUp(0) && currentObject.canRelease)
        {
            currentObject.OnRelease();
        }
    }
}