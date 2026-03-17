using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSControllerNoPhysics : Controller
{
    public FootstepsListener footstepsListener;

    [Header("Références")]
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

    [Header("Mobile Look")]
    public float mobileLookSensitivityX = 2.0f;
    public float mobileLookSensitivityY = 0.8f;
    public float mobileLookSensitivityMultiplier = 0.25f;
    public float mobileIdleLookMultiplier = 2f;
    public float mobileIdleLookLerpSpeed = 6f;

    [Header("Mobile Sprint")]
    [Range(0.1f, 1f)] public float mobileSprintThreshold = 0.75f;

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
    private Vector2 _lastMobileLookScaled;
    private bool _lastMobileEnabled;
    private bool _isMoving;
    private float _mobileIdleMultiplier = 1f;

    private Player _player;

    void Start()
    {
        _player = Player.Instance;

        controller = GetComponent<CharacterController>();

        cameraStartLocalPos = _player.camera.transform.localPosition;
        cameraStandingPos = cameraStartLocalPos;
        standingHeight = controller.height;

        _lastMobileEnabled = MobileInput.Enabled;
        ApplyCursorState(_lastMobileEnabled);

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
    }

    void Update()
    {
        if (_lastMobileEnabled != MobileInput.Enabled)
        {
            _lastMobileEnabled = MobileInput.Enabled;
            ApplyCursorState(_lastMobileEnabled);
        }

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
        if (_player.IsCameraLocked()) return;
        
        float mouseX = 0f;
        float mouseY = 0f;
        if (!MobileInput.Enabled)
        {
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * 100f * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * 100f * Time.deltaTime;
        }
        float idleMultiplier = 1f;
        if (MobileInput.Enabled)
        {
            float targetMultiplier = _isMoving ? 1f : mobileIdleLookMultiplier;
            _mobileIdleMultiplier = Mathf.Lerp(_mobileIdleMultiplier, targetMultiplier, mobileIdleLookLerpSpeed * Time.deltaTime);
            idleMultiplier = _mobileIdleMultiplier;
        }
        else
        {
            _mobileIdleMultiplier = 1f;
        }

        Vector2 mobileLook = MobileInput.GetLookDelta();
        _lastMobileLookScaled = new Vector2(
            mobileLook.x * mobileLookSensitivityX * mobileLookSensitivityMultiplier * idleMultiplier * 100f * Time.deltaTime,
            mobileLook.y * mobileLookSensitivityY * mobileLookSensitivityMultiplier * idleMultiplier * 100f * Time.deltaTime
        );

        mouseX += _lastMobileLookScaled.x;
        mouseY += _lastMobileLookScaled.y;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        _player.camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // ---------------- MOVE ----------------
    void HandleMove()
    {
        if (_player.IsLocked()) return;
        
        float x = 0f;
        float z = 0f;
        if (!MobileInput.Enabled)
        {
            x = (Input.GetKey(_player.inputManager.rightKey) ? 1f : 0f) - (Input.GetKey(_player.inputManager.leftKey) ? 1f : 0f);
            z = (Input.GetKey(_player.inputManager.forwardKey) ? 1f : 0f) - (Input.GetKey(_player.inputManager.backwardKey) ? 1f : 0f);
        }
        Vector2 mobileMove = MobileInput.Move;
        x += mobileMove.x;
        z += mobileMove.y;
        x = Mathf.Clamp(x, -1f, 1f);
        z = Mathf.Clamp(z, -1f, 1f);

        Vector3 input = new Vector3(x, 0, z).normalized;
        bool isMoving = input.magnitude > 0.1f;
        bool mobileSprint = MobileInput.Enabled && mobileMove.sqrMagnitude >= (mobileSprintThreshold * mobileSprintThreshold);
        bool wantsToSprint = (!MobileInput.Enabled && Input.GetKey(_player.inputManager.sprintKey)) || MobileInput.SprintHeld || mobileSprint;
        bool canSprint = wantsToSprint && isMoving && !staminaDepleted;

        // Mise à jour stamina
        HandleStamina(canSprint);

        float speed = canSprint ? sprintSpeed : walkSpeed;
        if (isCrouching) speed = crouchSpeed;

        Vector3 targetMove = transform.TransformDirection(input) * speed;
        currentMove = Vector3.Lerp(currentMove, targetMove, acceleration * Time.deltaTime);

        smoothedMove = Vector3.Lerp(smoothedMove, currentMove, Time.deltaTime * 8f);
        _isMoving = smoothedMove.sqrMagnitude > 0.0001f;

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
        if (_player.IsLocked()) return;
        
        if ((!MobileInput.Enabled && Input.GetKeyDown(_player.inputManager.crouchKey)) || MobileInput.CrouchDown)
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
        Vector3 camPos = _player.camera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
        _player.camera.transform.localPosition = camPos;
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
            _player.camera.transform.localPosition = Vector3.Lerp(
                _player.camera.transform.localPosition,
                cameraStartLocalPos,
                Time.deltaTime * bobResetSpeed
            );
            stepTimer = 0f;
            return;
        }

        bool isSprinting = ((!MobileInput.Enabled && Input.GetKey(_player.inputManager.sprintKey)) || MobileInput.SprintHeld) && !staminaDepleted;
        float stepTime = isSprinting ? stepDuration * sprintStepMultiplier : stepDuration;

        float previousStepTimer = stepTimer;
        stepTimer += Time.deltaTime / stepTime;

        if (previousStepTimer < 0.5f && stepTimer >= 0.5f)
        {
            float speed = smoothedMove.magnitude;

            // ⚡ On ne joue le pas que si on bouge assez vite
            if (speed > 0.05f)
            {
                float volume = isSprinting ? 1f : 0.5f;
                PlayFootstep(volume);
            }
        }

        if (stepTimer > 1f) stepTimer -= 1f;

        float bob = Mathf.Sin(stepTimer * Mathf.PI * 2f) * bobAmplitude;
        _player.camera.transform.localPosition = cameraStartLocalPos + Vector3.up * bob;

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
        if (armsTransform == null || _player.IsCameraLocked()) return;

        float mouseX = _lastMobileLookScaled.x;
        float mouseY = _lastMobileLookScaled.y;
        if (!MobileInput.Enabled)
        {
            mouseX += Input.GetAxis("Mouse X");
            mouseY += Input.GetAxis("Mouse Y");
        }

        // Rotation cible basée sur la rotation de départ + sway
        Quaternion swayRot = Quaternion.Euler(
            -mouseY * swayAmountX,
            mouseX * swayAmountY,
            0f
        );

        Quaternion targetRot = armsStartLocalRot * swayRot;

        // Interpolation vers la rotation cible
        armsTransform.localRotation = Quaternion.Slerp(
            armsTransform.localRotation,
            targetRot,
            Time.deltaTime * swaySmooth
        );
    }

    // ---------------- LIGHT ----------------
    void HandleLight()
    {
        if (_player.IsLocked()) return;
        
        if (((!MobileInput.Enabled && Input.GetKeyDown(_player.inputManager.turnLight)) || MobileInput.ToggleLightDown) && mLight != null)
        {
            bool enable = !mLight.gameObject.activeSelf;

            if (enable)
            {
                GamePlayer gamePlayer = _player as GamePlayer;
                if (gamePlayer != null)
                    gamePlayer.AlertTheHuntingGhost();
            }
            
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

    private void ApplyCursorState(bool mobileEnabled)
    {
        if (mobileEnabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !Application.isMobilePlatform;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
