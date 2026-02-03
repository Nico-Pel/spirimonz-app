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
    public float rotationSpeed = 720f; // Player rotation max deg/sec
    
    [Header("Player Rotation Lag")]
    public float rotationFollowDelay = 0.1f; // plus petit = suit plus vite, plus grand = plus lent


    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStickForce = -5f;

    [Header("Camera")]
    public bool freeCam = true;
    public float mouseSensitivity = 20f; // souris rapide OK
    public float maxMouseDeltaPerFrame = 5f; // limite rotation brutale
    public float minYAngle = -30f;
    public float maxYAngle = 60f;
    
    [Header("Auto Align Camera")]
    public float autoAlignDelay = 1.0f;       // secondes avant recentrage
    public float autoAlignSpeed = 2.5f;       // vitesse de recentrage

    private float timeSinceLastMouseInput = 0f;

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

    InputManager input = player.inputManager;

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

    // Vérifier si le clic gauche est maintenu
    bool isMouseHeld = Input.GetMouseButton(0);

    // Récupérer l'input clavier pour savoir si le joueur recule ou straf
    float h = 0f;
    float v = 0f;
    if (Input.GetKey(input.forwardKey)) v += 1f;
    if (Input.GetKey(input.backwardKey)) v -= 1f;
    if (Input.GetKey(input.rightKey)) h += 1f;
    if (Input.GetKey(input.leftKey)) h -= 1f;

    Vector3 inputDir = new Vector3(h, 0f, v).normalized;
    bool isMoving = inputDir.magnitude > 0.1f;
    bool isMovingBackward = inputDir.z < 0f; // reculer

    if (isMouseHeld)
    {
        // Reset timer quand la souris est utilisée
        timeSinceLastMouseInput = 0f;

        float rawMouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float rawMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Limiter la rotation par frame pour éviter les jumps
        float mouseX = Mathf.Clamp(rawMouseX, -maxMouseDeltaPerFrame, maxMouseDeltaPerFrame);
        float mouseY = Mathf.Clamp(rawMouseY, -maxMouseDeltaPerFrame, maxMouseDeltaPerFrame);

        cameraYaw += mouseX;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minYAngle, maxYAngle);

        cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }
    else
    {
        // Timer pour recentrage automatique uniquement si le joueur n'avance pas
        if (!isMoving || !isMovingBackward)
        {
            timeSinceLastMouseInput += Time.deltaTime;

            if (timeSinceLastMouseInput >= autoAlignDelay)
            {
                // Recentrage derrière le player
                float targetYaw = transform.eulerAngles.y;
                cameraYaw = Mathf.LerpAngle(cameraYaw, targetYaw, autoAlignSpeed * Time.deltaTime);
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            }
        }
        else
        {
            // Si on recule/straf, on incrémente quand même le timer mais ne recentre pas
            timeSinceLastMouseInput += Time.deltaTime;
        }
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

            // rotation player fluide
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

    // ========================= PLAYER ROTATION =========================
    void RotateCharacterSmooth(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f) return; // ne tourne pas si pas de move

        Quaternion targetRot = Quaternion.LookRotation(moveDir);

        // Lag configurable : plus rotationFollowDelay grand, plus le retard
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime / Mathf.Max(rotationFollowDelay, 0.001f)
        );
    }

    // ========================= ANIMATOR =========================
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
