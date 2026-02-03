using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TPSController : Controller
{
    [Header("References")]
    public WorldPlayer player;
    public Transform cameraTransform;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float sprintSpeed = 6f;
    public float rotationSpeed = 720f; // degrés / seconde max

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStickForce = -5f;

    [Header("Camera")]
    public bool freeCam = true;
    public float mouseSensitivity = 20f; // tu peux garder 20
    public float minYAngle = -30f;
    public float maxYAngle = 60f;

    private CharacterController controller;
    private Vector3 velocity;

    private float cameraYaw;
    private float cameraPitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleCamera();
        HandleMovement();
        HandleInteraction();
    }

    // ========================= CAMERA =========================
    void HandleCamera()
    {
        if (!cameraTransform) return;

        // Cursor lock / unlock
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Rotation caméra uniquement si clic gauche
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            cameraYaw += mouseX;
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minYAngle, maxYAngle);

            cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        }
    }

    // ========================= MOVEMENT =========================
    void HandleMovement()
    {
        InputManager input = player.inputManager;

        float h = 0f;
        float v = 0f;

        if (Input.GetKey(input.forwardKey)) v += 1f;
        if (Input.GetKey(input.backwardKey)) v -= 1f;
        if (Input.GetKey(input.rightKey)) h += 1f;
        if (Input.GetKey(input.leftKey)) h -= 1f;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        // Direction caméra projetée sur le sol
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        float targetSpeed = walkSpeed;

        if (inputDir.magnitude > 0.1f)
        {
            targetSpeed = runSpeed;
            if (Input.GetKey(input.sprintKey))
                targetSpeed = sprintSpeed;

            // Rotation fluide vers la direction du mouvement
            RotateCharacterSmooth(moveDir);
        }

        Vector3 horizontalMove = moveDir * targetSpeed;

        // Gravité
        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = groundedStickForce;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 finalMove = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        UpdateAnimator(horizontalMove.magnitude, targetSpeed);
    }

    void RotateCharacterSmooth(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(moveDir);

        // RotateTowards : vitesse max de rotation en degrés/sec
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    void UpdateAnimator(float currentSpeed, float maxSpeed)
    {
        if (!animator) return;

        float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
    }

    // ========================= INTERACTION =========================
    void HandleInteraction()
    {
        if (!Input.GetKeyDown(player.inputManager.grabObject)) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteractStart();
            }
        }
    }
}
