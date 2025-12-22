using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 3f; // Distance max pour interagir
    public LayerMask interactableLayer;    // Layer des objets interactifs

    private ClickableObject currentObject;

    void Update()
    {
        HandleRaycast();
        HandleInput();
    }

    void HandleRaycast()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast pour détecter un objet à cliquer
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                currentObject = hit.collider.GetComponent<ClickableObject>();
            }
        }

        // Si on ne maintient plus le clic, on peut libérer currentObject
        if (!Input.GetMouseButton(0))
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