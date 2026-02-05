using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TPSController : Controller
{
    [Header("References")]
    public CharacterController controller;
    public Transform camTransform; // Cinemachine Target (camera pivot)
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float sprintSpeed = 6f;
    public float rotationSpeed = 10f; // smooth rotation

    [Header("Gravity")]
    public float gravity = -20f;
    private Vector3 velocity;

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
{
    // 1️⃣ Input
    float h = Input.GetAxis("Horizontal"); // A/D ou flèches
    float v = Input.GetAxis("Vertical");   // W/S ou flèches
    Vector3 inputDir = new Vector3(h, 0f, v).normalized;

    // 2️⃣ Vérifier si on bouge
    if (inputDir.magnitude >= 0.1f)
    {
        // 3️⃣ Mouvement relatif à la caméra
        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = camTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        // 4️⃣ Rotation smooth du player
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // 5️⃣ Vitesse
        float speed = runSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) // sprint
            speed = sprintSpeed;
        else
            speed = walkSpeed;

        Vector3 move = moveDir * speed;

        // 6️⃣ Gravité
        if (controller.isGrounded)
            velocity.y = -2f; // reste collé au sol
        else
            velocity.y += gravity * Time.deltaTime;

        // Appliquer la gravité verticale
        move += Vector3.up * velocity.y;

        // 7️⃣ Déplacer le CharacterController
        controller.Move(move * Time.deltaTime);

        // 8️⃣ Animator
        float normalizedSpeed = moveDir.magnitude * (speed / sprintSpeed);
        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
    }
    else
    {
        // Si le joueur ne bouge pas
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        // Appliquer quand même la gravité pour rester collé au sol
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
        }
        else
        {
            velocity.y = -2f;
        }
    }
}

}
