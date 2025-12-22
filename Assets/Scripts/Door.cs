using UnityEngine;

public class Door : ClickableObject
{
    [Header("Door Options")]
    public float openAngle = 90f;      // Angle maximal d'ouverture
    public float openSpeed = 2f;       // Vitesse de rotation
    public bool isOpen = false;        // État actuel de la porte

    private Quaternion closedRotation; // Rotation initiale de la porte
    private Quaternion targetRotation; // Rotation cible
    private float openDirection = 1f;  // 1 = droite, -1 = gauche

    void Start()
    {
        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        // Interpolation vers la rotation cible pour un mouvement fluide
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);
    }

    public override void OnClick()
    {
        // Détection du côté de la souris par rapport à la porte
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 doorToMouse = mouseWorldPos - transform.position;

        // Produit vectoriel pour déterminer de quel côté ouvrir
        float side = Vector3.Cross(transform.forward, doorToMouse).y;
        openDirection = side >= 0 ? 1f : -1f;

        // Bascule l'état
        isOpen = !isOpen;

        // Définition de la rotation cible
        targetRotation = isOpen
            ? closedRotation * Quaternion.Euler(0f, openAngle * openDirection, 0f)
            : closedRotation;

        Debug.Log($"{name} clicked! Opening {(isOpen ? "opened" : "closed")}");
    }

    /// <summary>
    /// Convertit la position de la souris à l'écran en point dans le monde sur le plan de la porte
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane doorPlane = new Plane(transform.up, transform.position); // Plan parallèle au sol passant par la porte
        if (doorPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return transform.position; // fallback
    }
}