using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSControllerNoPhysics : GameBehaviour
{
    public FootstepsListener footstepsListener;
    
    [Header("Références")]
    public Camera playerCamera;
    public Light mLight;
    public GameObject mLightObject;

    [Header("Déplacement")]
    public float walkSpeed = 2.0f;
    public float sprintSpeed = 3.5f;
    public float acceleration = 10f;

    [Header("Souris")]
    public float mouseSensitivityX = 2.0f;
    public float mouseSensitivityY = 2.0f;
    public float maxLookAngle = 80f;

    [Header("Clavier (AZERTY)")]
    public KeyCode forwardKey = KeyCode.Z;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.Q;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode turnLight = KeyCode.T;
    public KeyCode grabObject = KeyCode.E;
    public KeyCode dropObject = KeyCode.D;
    public KeyCode throwObject = KeyCode.G;
    public KeyCode openJournal = KeyCode.J;
    public KeyCode exitMenus = KeyCode.Escape;
    
    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.C;
    public float crouchHeight = 1f;          // Hauteur du CharacterController accroupi
    public float crouchSpeedMultiplier = 0.5f; // Vitesse divisée en crouch
    public float crouchCameraHeight = 0.5f;  // Hauteur caméra accroupi
    public float crouchTransitionSpeed = 6f; // Vitesse de transition
    public float delayBeforeActivatingHeadBob = 0.5f; // ré-activer le head bob après s'être remis debout

    private bool isCrouching = false;
    private float standingHeight;
    private Vector3 cameraStandingPos;

    [Header("Gravité")]
    public float gravity = -9.81f;
    public float stickToGroundForce = -2f;
    
    [Header("Pentes & Obstacles")]
    [Tooltip("Angle max de pente franchissable")]
    public float maxGroundAngle = 45f;

    [Tooltip("Distance du raycast vers le sol")]
    public float groundCheckDistance = 1.2f;

    [Tooltip("Décalage vers l'avant pour tester la pente")]
    public float slopeCheckOffset = 0.4f;

    [Header("Headbob")]

    [Tooltip("Amplitude verticale (très faible !)")]
    public float bobAmplitude = 0.04f;
    
    [Tooltip("Durée d'un pas (plus petit = marche plus rapide)")]
    public float stepDuration = 0.45f;
    
    [Tooltip("Multiplicateur de vitesse en sprint")]
    public float sprintStepMultiplier = 0.7f;
    
    [Tooltip("Vitesse de retour à la position neutre")]
    public float bobResetSpeed = 8f;
    
    [Header("Stamina")]
    public float maxStamina = 100f;

    [Tooltip("Temps pour vider complètement la stamina (en secondes)")]
    public float staminaDrainTime = 4f;

    [Tooltip("Temps pour recharger complètement la stamina (en secondes)")]
    public float staminaRegenTime = 6f;
    
    [Header("Arms Bob")]
    public Transform armsTransform;

    public float armsBobAmplitudeY = 0.02f;
    public float armsBobAmplitudeX = 0.015f;
    public float armsBobRotation = 1.5f;
    public float armsResetSpeed = 10f;
    
    [Header("Arm Sway")]
    public float swayAmountX = 1.5f; // Rotation sway sur X (pitch)
    public float swayAmountY = 2.5f; // Rotation sway sur Y (yaw)
    public float swaySmooth = 8f;    // Lerp de la rotation

    private Vector3 armsStartLocalPos;
    private Quaternion armsStartLocalRot;
    
    [Header("Layers")]
    public LayerMask groundLayers;

    private Vector3 movementInput;

    private float currentStamina;
    private bool staminaDepleted = false;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMove;
    private float xRotation = 0f;

    private float bobTimer = 0f;
    private float stepTimer = 0f;
    private bool _canUseHeadBob;
    
    private Vector3 cameraStartLocalPos;
    private Vector3 lastPlayerForward;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        cameraStartLocalPos = playerCamera.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        currentStamina = maxStamina;
        
        standingHeight = controller.height;
        cameraStandingPos = playerCamera.transform.localPosition;

        UIGame uiGame = UIGame.Instance;
        uiGame.EnablePointer(true);
        uiGame.InitControlTexts(this);
        
        if (armsTransform != null)
        {
            armsStartLocalPos = armsTransform.localPosition;
            armsStartLocalRot = armsTransform.localRotation;
        }

        lastPlayerForward = transform.forward; // initialisation pour lag
    }

    void Update()
    {
        if (House.Instance.currentGhost.IsLocked()) return;
        
        //UI controls
        HandleJournal();
        HandleExitMenu();
        
        if (Player.Instance.AreControlsLocked()) return;
        
        //Player controls
        HandleLook();
        HandleArmSway();
        HandleMove();
        ApplyGravity();
        HandleHeadbob();
        HandleCrouch();
        HandleLight();
    }

    void HandleLight()
    {
        if (Input.GetKeyDown(turnLight) && mLight != null)
        {
            ChangeLightState();
        }
    }
    
    void HandleJournal()
    {
        if (Input.GetKeyDown(openJournal))
        {
            UIGame.Instance.EnableJournal(!UIGame.Instance.GetJournalState());
        }
    }
    
    void HandleExitMenu()
    {
        if (Input.GetKeyDown(exitMenus))
        {
            UIGame.Instance.ExitLastMenu();
        }
    }

    public void ChangeLightState()
    {
        bool enable = !mLight.gameObject.activeInHierarchy;
        mLight.gameObject.SetActive(enable);
        mLightObject.gameObject.SetActive(enable);
    }
    
    public void ForceLightState(bool enable)
    {
        mLight.gameObject.SetActive(enable);
        mLightObject.gameObject.SetActive(enable);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMove()
    {
        float inputX = 0f;
        float inputZ = 0f;

        if (Input.GetKey(forwardKey)) inputZ += 1f;
        if (Input.GetKey(backwardKey)) inputZ -= 1f;
        if (Input.GetKey(leftKey)) inputX -= 1f;
        if (Input.GetKey(rightKey)) inputX += 1f;

        movementInput = new Vector3(inputX, 0, inputZ).normalized;

        bool wantsToSprint = Input.GetKey(sprintKey);
        bool isMoving = movementInput.magnitude > 0.1f;
        bool canSprint = wantsToSprint && isMoving && !staminaDepleted;

        // Gestion stamina
        HandleStamina(canSprint);

        float baseSpeed = canSprint ? sprintSpeed : walkSpeed;
        float targetSpeed = isCrouching ? baseSpeed * crouchSpeedMultiplier : baseSpeed;
        Vector3 targetMove = transform.TransformDirection(movementInput) * targetSpeed;

        currentMove = Vector3.Lerp(currentMove, targetMove, acceleration * Time.deltaTime);

        if (IsSlopeTooSteep(currentMove))
        {
            currentMove = Vector3.zero;
        }

        controller.Move(currentMove * Time.deltaTime);
    }
    
    bool IsSlopeTooSteep(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.1f)
            return false;

        Vector3 origin = transform.position 
                         + Vector3.up * 0.1f 
                         + moveDirection.normalized * slopeCheckOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle > maxGroundAngle;
        }

        return false;
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            velocity.y = stickToGroundForce;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleCrouch()
    {
        // Toggle crouch
        if (Input.GetKeyDown(crouchKey))
            isCrouching = !isCrouching;

        if (isCrouching)
        {
            _canUseHeadBob = false;
        }
        else
        {
            this.Invoke(delayBeforeActivatingHeadBob, () =>
            {
                if (!isCrouching)
                {
                    _canUseHeadBob = true;
                }
            });
        }

        // Hauteur cible du CharacterController
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Ajustement du center du CharacterController
        controller.center = new Vector3(0, controller.height / 2f, 0);

        // Caméra
        float targetCamY = isCrouching ? crouchCameraHeight : cameraStandingPos.y;
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
        playerCamera.transform.localPosition = camPos;
    }

    private float lastStepTimer = 0f;

    void HandleHeadbob()
    {
        if (!_canUseHeadBob) return;

        if (movementInput.magnitude > 0.1f && controller.isGrounded)
        {
            float stepTime = Input.GetKey(sprintKey)
                ? stepDuration * sprintStepMultiplier
                : stepDuration;

            lastStepTimer = stepTimer;
            stepTimer += Time.deltaTime / stepTime;

            // Détecte le passage du demi-cycle (0.5) → on joue un pas
            if (lastStepTimer < 0.5f && stepTimer >= 0.5f)
            {
                float volumeMultiplier = Input.GetKeyDown(sprintKey) ? 2f : 1f;
                PlayFootstep(volumeMultiplier);
            }

            if (stepTimer >= 1f)
            {
                stepTimer -= 1f; // reset pour le cycle suivant
            }

            // Headbob caméra
            float bobOffset = Mathf.Sin(stepTimer * Mathf.PI * 2f) * bobAmplitude;
            playerCamera.transform.localPosition = cameraStartLocalPos + Vector3.up * bobOffset;

            // Headbob + arm lag
            HandleArmsBob();
        }
        else
        {
            stepTimer = 0f;
            lastStepTimer = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                cameraStartLocalPos,
                Time.deltaTime * bobResetSpeed
            );
        }
    }
    
    void HandleArmsBob()
    {
        if (armsTransform == null) return;

        // --- Headbob position ---
        float sin = Mathf.Sin(stepTimer * Mathf.PI * 2f);
        float cos = Mathf.Cos(stepTimer * Mathf.PI * 2f);

        Vector3 targetPos = armsStartLocalPos;
        targetPos.y += sin * armsBobAmplitudeY;
        targetPos.x += cos * armsBobAmplitudeX;

        armsTransform.localPosition = Vector3.Lerp(
            armsTransform.localPosition,
            targetPos,
            Time.deltaTime * armsResetSpeed
        );

        // --- Headbob rotation ---
        Quaternion bobRot = Quaternion.Euler(sin * armsBobRotation, cos * armsBobRotation, 0f);
        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            armsTransform.localRotation * bobRot,
            Time.deltaTime * armsResetSpeed
        );

        // --- Movement sway ---
        Vector3 moveDelta = transform.InverseTransformDirection(currentMove);
        Quaternion moveSway = Quaternion.Euler(
            -moveDelta.z * 0.5f,   // avant/recul
            moveDelta.x * 0.5f,    // gauche/droite
            0f
        );
        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            armsTransform.localRotation * moveSway,
            Time.deltaTime * swaySmooth
        );
    }
    
    void HandleArmSway()
    {
        if (armsTransform == null) return;

        // Récupérer les inputs souris
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Rotation cible pour le sway (inverse pour effet "lag")
        Quaternion targetRotation = Quaternion.Euler(
            -mouseY * swayAmountX,  // Pitch
            mouseX * swayAmountY,   // Yaw
            0f                      // Roll (optionnel)
        );

        // Lerp smooth vers la rotation target
        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            armsStartLocalRot * targetRotation,
            Time.deltaTime * swaySmooth
        );
    }
    
    void HandleStamina(bool isTryingToSprint)
    {
        // Consommation
        if (isTryingToSprint && !staminaDepleted)
        {
            currentStamina -= (maxStamina / staminaDrainTime) * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                staminaDepleted = true;
            }
        }
        // Recharge
        else
        {
            currentStamina += (maxStamina / staminaRegenTime) * Time.deltaTime;

            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                staminaDepleted = false; // sprint à nouveau autorisé
            }
        }
    }
    
    private void PlayFootstep(float volumeMultiplier = 1f)
    {
        if (!controller.isGrounded || movementInput.magnitude < 0.1f)
            return;

        if (footstepsListener != null)
        {
            footstepsListener.PlayFootstep(volumeMultiplier);
        }
    }
}