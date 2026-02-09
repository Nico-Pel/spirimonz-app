using UnityEngine;

public class TPSPlayerLook : MonoBehaviour
{
    [Header("References")]
    public Transform neck;
    public Transform cameraTransform;
    public Transform characterRoot; // forward du personnage

    [Header("Offsets")]
    public float verticalLookOffset = -0.2f;

    [Header("Limits")]
    public float maxUpAngle = 30f;
    public float maxDownAngle = 40f;
    public float maxLeftAngle = 60f;
    public float maxRightAngle = 60f;

    [Header("Fallback Settings")]
    public float maxAngleFromForward = 120f; // angle max où le cou suit la caméra
    public float rotationSpeed = 8f; // vitesse de rotation

    private Quaternion initialLocalRotation;

    void Start()
    {
        if (neck == null)
        {
            Debug.LogError("TPSPlayerLook: Neck is not assigned.");
            enabled = false;
            return;
        }

        if (characterRoot == null)
        {
            characterRoot = transform;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        initialLocalRotation = neck.localRotation;
    }

    void LateUpdate()
    {
        UpdateNeckRotation();
    }

    void UpdateNeckRotation()
    {
        // 1️⃣ Direction caméra + offset vertical
        Vector3 lookDir = cameraTransform.forward + Vector3.up * verticalLookOffset;
        lookDir.Normalize();

        // 2️⃣ Calcul angle entre forward personnage et direction caméra
        float angleFromForward = Vector3.Angle(characterRoot.forward, lookDir);

        // 3️⃣ Si angle > maxAngleFromForward, on regarde droit devant
        if (angleFromForward > maxAngleFromForward)
        {
            lookDir = characterRoot.forward + Vector3.up * verticalLookOffset;
        }

        // 4️⃣ Direction dans l'espace local du parent du cou
        Vector3 localLookDir = neck.parent.InverseTransformDirection(lookDir);

        // 5️⃣ Calcul des angles
        float yaw = Mathf.Atan2(localLookDir.x, localLookDir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(localLookDir.y) * Mathf.Rad2Deg;

        // 6️⃣ Clamp des angles
        yaw = Mathf.Clamp(yaw, -maxLeftAngle, maxRightAngle);
        pitch = Mathf.Clamp(pitch, -maxUpAngle, maxDownAngle);

        // 7️⃣ Rotation finale
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f) * initialLocalRotation;

        // 8️⃣ Lissage
        neck.localRotation = Quaternion.Slerp(neck.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
