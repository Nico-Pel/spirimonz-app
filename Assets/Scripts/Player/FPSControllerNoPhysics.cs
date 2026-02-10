using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSControllerNoPhysics : Controller
{
    public FootstepsListener footstepsListener;

    [Header("Références")]
    public Camera playerCamera;
    public Light mLight;
    public GameObject mLightObject;

    [Header("Déplacement")]
    public float walkSpeed = 2.0f;
    public float crouchSpeed = 1f;
    public float sprintSpeed = 3.5f;
    public float acceleration = 10f;

    [Header("Souris")]
    public float mouseSensitivityX = 2.0f;
    public float mouseSensitivityY = 2.0f;
    public float maxLookAngle = 80f;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float crouchCameraHeight = 0.5f;
    public float crouchTransitionSpeed = 6f;
    public float delayBeforeActivatingHeadBob = 0.5f;
    
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainTime = 4f;   // Temps en sprint pour vider la stamina
    public float staminaRegenTime = 6f;   // Temps pour récupérer complètement

    private float currentStamina;
    private bool staminaDepleted;

    [Header("Gravité")]
    public float gravity = -9.81f;
    public float stickToGroundForce = -2f;

    [Header("Pentes & Obstacles")]
    public float maxGroundAngle = 45f;
    public float groundCheckDistance = 1.2f;
    public float slopeCheckOffset = 0.4f;
    public LayerMask groundLayers;

    [Header("Headbob")]
    public float bobAmplitude = 0.04f;
    public float stepDuration = 0.45f;
    public float sprintStepMultiplier = 0.7f;
    public float bobResetSpeed = 8f;

    [Header("Arms Bob")]
    public Transform armsTransform;
    public float armsBobAmplitudeY = 0.02f;
    public float armsBobAmplitudeX = 0.015f;
    public float armsBobRotation = 1.5f;
    public float armsResetSpeed = 10f;

    [Header("Arm Sway")]
    public float swayAmountX = 1.5f;
    public float swayAmountY = 2.5f;
    public float swaySmooth = 8f;

    [Header("Audio")] 
    public AudioClip torchSoundOn;
    public AudioClip torchSoundOff;
    public AudioClip noStaminaSound;    

    // --- Private ---
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMove;
    private Vector3 smoothedMove;

    private float xRotation;
    private bool isCrouching;
    private float standingHeight;
    private Vector3 cameraStandingPos;

    private bool canUseHeadBob;
    private bool pendingHeadBobInvoke;

    private float stepTimer;
    private Vector3 cameraStartLocalPos;

    private Vector3 armsStartLocalPos;
    private Quaternion armsStartLocalRot;

    private Player _player;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        cameraStartLocalPos = playerCamera.transform.localPosition;
        cameraStandingPos = cameraStartLocalPos;
        standingHeight = controller.height;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (armsTransform != null)
        {
            armsStartLocalPos = armsTransform.localPosition;
            armsStartLocalRot = armsTransform.localRotation;
        }

        // Headbob actif dès le début
        canUseHeadBob = true;

        // Init stamina
        currentStamina = maxStamina;
        staminaDepleted = false;

        _player = Player.Instance;
    }

    void Update()
    {
        if (_player.IsLocked()) return;
        
        HandleLook();
        HandleMove();
        HandleCrouch();
        HandleHeadbob();
        HandleArmSway();
        HandleLight();
    }

    // ---------------- LOOK ----------------
    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // ---------------- MOVE ----------------
    void HandleMove()
    {
        float x = (Input.GetKey(_player.inputManager.rightKey) ? 1f : 0f) - (Input.GetKey(_player.inputManager.leftKey) ? 1f : 0f);
        float z = (Input.GetKey(_player.inputManager.forwardKey) ? 1f : 0f) - (Input.GetKey(_player.inputManager.backwardKey) ? 1f : 0f);

        Vector3 input = new Vector3(x, 0, z).normalized;
        bool isMoving = input.magnitude > 0.1f;
        bool wantsToSprint = Input.GetKey(_player.inputManager.sprintKey);
        bool canSprint = wantsToSprint && isMoving && !staminaDepleted;

        // Mise à jour stamina
        HandleStamina(canSprint);

        float speed = canSprint ? sprintSpeed : walkSpeed;
        if (isCrouching) speed = crouchSpeed;

        Vector3 targetMove = transform.TransformDirection(input) * speed;
        currentMove = Vector3.Lerp(currentMove, targetMove, acceleration * Time.deltaTime);

        smoothedMove = Vector3.Lerp(smoothedMove, currentMove, Time.deltaTime * 8f);

        // Gravité
        if (controller.isGrounded)
            velocity.y = stickToGroundForce;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move((smoothedMove + velocity) * Time.deltaTime);
    }
    
    //Stamina to use Sprint
    void HandleStamina(bool isTryingToSprint)
    {
        if (isTryingToSprint && !staminaDepleted)
        {
            currentStamina -= (maxStamina / staminaDrainTime) * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                staminaDepleted = true;

                PlayNoStaminaSound();
            }
        }
        else
        {
            currentStamina += (maxStamina / staminaRegenTime) * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                staminaDepleted = false;
            }
        }
    }

    // ---------------- CROUCH ----------------
    void HandleCrouch()
    {
        if (Input.GetKeyDown(_player.inputManager.crouchKey))
        {
            isCrouching = !isCrouching;

            CancelInvoke(nameof(EnableHeadBob));
            pendingHeadBobInvoke = false;

            if (!isCrouching)
            {
                pendingHeadBobInvoke = true;
                Invoke(nameof(EnableHeadBob), delayBeforeActivatingHeadBob);
            }
            else
            {
                canUseHeadBob = false;
            }
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.center = new Vector3(0, controller.height / 2f, 0);

        float targetCamY = isCrouching ? crouchCameraHeight : cameraStandingPos.y;
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
        playerCamera.transform.localPosition = camPos;
    }

    void EnableHeadBob()
    {
        canUseHeadBob = true;
        pendingHeadBobInvoke = false;
    }

    // ---------------- HEADBOB ----------------
    void HandleHeadbob()
    {
        if (isCrouching) return;

        if (!canUseHeadBob || smoothedMove.magnitude < 0.1f || !controller.isGrounded)
        {
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                cameraStartLocalPos,
                Time.deltaTime * bobResetSpeed
            );
            stepTimer = 0f;
            return;
        }

        bool isSprinting = Input.GetKey(_player.inputManager.sprintKey) && !staminaDepleted;
        float stepTime = isSprinting ? stepDuration * sprintStepMultiplier : stepDuration;

        float previousStepTimer = stepTimer;
        stepTimer += Time.deltaTime / stepTime;

        if (previousStepTimer < 0.5f && stepTimer >= 0.5f)
        {
            float volume = isSprinting ? 1f : 0.5f;
            PlayFootstep(volume);
        }

        if (stepTimer > 1f) stepTimer -= 1f;

        float bob = Mathf.Sin(stepTimer * Mathf.PI * 2f) * bobAmplitude;
        playerCamera.transform.localPosition = cameraStartLocalPos + Vector3.up * bob;

        HandleArmsBob();
    }

    // ---------------- ARMS ----------------
    void HandleArmsBob()
    {
        if (armsTransform == null) return;

        float sin = Mathf.Sin(stepTimer * Mathf.PI * 2f);
        float cos = Mathf.Cos(stepTimer * Mathf.PI * 2f);

        Vector3 pos = armsStartLocalPos;
        pos.y += sin * armsBobAmplitudeY;
        pos.x += cos * armsBobAmplitudeX;

        Quaternion bobRot = Quaternion.Euler(sin * armsBobRotation, cos * armsBobRotation, 0f);

        Vector3 moveDelta = transform.InverseTransformDirection(smoothedMove);
        Quaternion moveSway = Quaternion.Euler(-moveDelta.z * 0.5f, moveDelta.x * 0.5f, 0f);

        Quaternion finalRot = armsStartLocalRot * bobRot * moveSway;

        armsTransform.localPosition = Vector3.Lerp(
            armsTransform.localPosition,
            pos,
            Time.deltaTime * armsResetSpeed
        );

        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            finalRot,
            Time.deltaTime * armsResetSpeed
        );
    }

    void HandleArmSway()
    {
        if (armsTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Quaternion sway = Quaternion.Euler(
            -mouseY * swayAmountX,
            mouseX * swayAmountY,
            0f
        );

        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            armsTransform.localRotation * sway,
            Time.deltaTime * swaySmooth
        );
    }

    // ---------------- LIGHT ----------------
    void HandleLight()
    {
        if (Input.GetKeyDown(_player.inputManager.turnLight) && mLight != null)
        {
            bool enable = !mLight.gameObject.activeSelf;
            mLight.gameObject.SetActive(enable);
            mLightObject.SetActive(enable);
            
            AudioClip clip = enable ? torchSoundOff : torchSoundOn;
            PlayTorchSound(clip);
        }
    }

    public void ForceLightState(bool enable)
    {
        if (mLight != null)
            mLight.gameObject.SetActive(enable);

        if (mLightObject != null)
            mLightObject.SetActive(enable);
    }

    // ---------------- SOUNDS ----------------
    public void PlayFootstep(float volumeMultiplier = 1f)
    {
        if (footstepsListener != null)
            footstepsListener.PlayFootstep(volumeMultiplier);
    }

    private void PlayTorchSound(AudioClip clip)
    {
        if(clip != null)
            SoundManager.Instance.PlaySound(clip, transform.position, 0.5f);
    }
    
    private void PlayNoStaminaSound()
    {
        if(noStaminaSound != null)
            SoundManager.Instance.PlaySound(noStaminaSound, transform.position, 1f);
    }
}