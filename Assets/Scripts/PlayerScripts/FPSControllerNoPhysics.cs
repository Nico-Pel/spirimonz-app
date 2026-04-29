using System.Collections;
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
    public bool useRawMouseInput = true;
    public bool useSmoothDeltaTimeForLook = true;
    
    [Header("Look Safety")]
    [Min(0)] public int ignoreLookFramesOnFocus = 1;
    [Min(0f)] public float maxLookDeltaPerFrame = 8f;
    [Min(0f)] public float maxLookDeltaTime = 0.05f;

    [Header("Mobile Look")]
    public float mobileLookSensitivityX = 2.0f;
    public float mobileLookSensitivityY = 0.8f;
    public float mobileLookSensitivityMultiplier = 0.25f;
    [Range(0f, 1f)] public float mobileLookDeadZone = 0.25f;
    public float mobileLookBoostThreshold = 0.7f;
    public float mobileLookBoostMultiplier = 1.6f;
    public float mobileIdleLookMultiplier = 4f;
    public float mobileIdleLookLerpSpeed = 6f;

    [Header("Mobile Sprint")]
    [Range(0.1f, 1f)] public float mobileSprintThreshold = 0.75f;

    [Header("Debug / Testing")]
    public bool allowKeyboardMovementWhenMobile = true;

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
    private bool _inputMoving;
    private bool _debugHeadBobDisabled;
    private float _cameraCrouchY;
    private float _headBobOffsetY;

    private Vector3 armsStartLocalPos;
    private Quaternion armsStartLocalRot;
    private Vector2 _lastMobileLookScaled;
    private Vector2 _lastMouseInputRaw;
    private bool _lastMobileEnabled;
    private bool _isMoving;
    private float _mobileIdleMultiplier = 1f;
    private int _ignoreLookFrames;
    private CursorLockMode _lastCursorLockState;
    private Coroutine _smoothLookRoutine;

    private Player _player;

    void Start()
    {
        _player = Player.Instance;

        controller = GetComponent<CharacterController>();

        cameraStartLocalPos = _player.camera.transform.localPosition;
        cameraStandingPos = cameraStartLocalPos;
        standingHeight = controller.height;
        _cameraCrouchY = cameraStartLocalPos.y;
        _headBobOffsetY = 0f;

        _lastMobileEnabled = MobileInput.Enabled;
        ApplyCursorState(_lastMobileEnabled);
        _lastCursorLockState = Cursor.lockState;

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

        TrackCursorLockState();

        if (_player.IsLocked()) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.O))
        {
            _debugHeadBobDisabled = !_debugHeadBobDisabled;
            if (_debugHeadBobDisabled)
            {
                stepTimer = 0f;
                _headBobOffsetY = 0f;
                ResetArmsPose();
            }
        }
#endif
        
        HandleLook();
        HandleMove();
        HandleCrouch();
        HandleHeadbob();
        ApplyCameraLocalPosition();
        HandleArmSway();
        HandleLight();
    }

    // ---------------- LOOK ----------------
    void HandleLook()
    {
        if (_player.IsCameraLocked() && !_player.IsDead()) return;
        if (!TutorialInputGate.IsAllowed(TutorialInputGate.AllowLook))
        {
            _lastMobileLookScaled = Vector2.zero;
            _lastMouseInputRaw = Vector2.zero;
            return;
        }
        if (_ignoreLookFrames > 0)
        {
            _ignoreLookFrames--;
            if (!MobileInput.Enabled)
            {
                if (useRawMouseInput)
                {
                    Input.GetAxisRaw("Mouse X");
                    Input.GetAxisRaw("Mouse Y");
                }
                else
                {
                    Input.GetAxis("Mouse X");
                    Input.GetAxis("Mouse Y");
                }
            }
            _lastMobileLookScaled = Vector2.zero;
            _lastMouseInputRaw = Vector2.zero;
            return;
        }

        float mouseX = 0f;
        float mouseY = 0f;
        _lastMouseInputRaw = Vector2.zero;
        float fpsSensitivityMultiplier = (_player != null && _player.inputManager != null)
            ? _player.inputManager.fpsLookSensitivityMultiplier
            : 1f;
        if (!MobileInput.Enabled)
        {
            const float pcSensitivityScale = 0.5f;
            float rawX = useRawMouseInput ? Input.GetAxisRaw("Mouse X") : Input.GetAxis("Mouse X");
            float rawY = useRawMouseInput ? Input.GetAxisRaw("Mouse Y") : Input.GetAxis("Mouse Y");
            _lastMouseInputRaw = new Vector2(rawX, rawY);

            if (useRawMouseInput)
            {
                const float legacyFps = 60f;
                float legacyScale = (pcSensitivityScale * 100f) / legacyFps;
                mouseX = rawX * mouseSensitivityX * fpsSensitivityMultiplier * legacyScale;
                mouseY = rawY * mouseSensitivityY * fpsSensitivityMultiplier * legacyScale;
            }
            else
            {
                float lookDeltaTime = useSmoothDeltaTimeForLook ? Time.smoothDeltaTime : Time.deltaTime;
                if (maxLookDeltaTime > 0f)
                    lookDeltaTime = Mathf.Min(lookDeltaTime, maxLookDeltaTime);

                mouseX = rawX * mouseSensitivityX * fpsSensitivityMultiplier * pcSensitivityScale * 100f * lookDeltaTime;
                mouseY = rawY * mouseSensitivityY * fpsSensitivityMultiplier * pcSensitivityScale * 100f * lookDeltaTime;
            }
        }
        float idleMultiplier = 1f;
        if (MobileInput.Enabled)
        {
            float targetMultiplier = mobileIdleLookMultiplier / 3f;
            _mobileIdleMultiplier = targetMultiplier;
            idleMultiplier = _mobileIdleMultiplier;
        }
        else
        {
            _mobileIdleMultiplier = 1f;
        }

        Vector2 mobileLook = MobileInput.GetLookDelta();
        if (MobileInput.Enabled && mobileLook.magnitude < mobileLookDeadZone)
            mobileLook = Vector2.zero;

        float lookBoost = 1f;
        if (MobileInput.Enabled)
        {
            float mag = Mathf.Clamp01(mobileLook.magnitude);
            if (mag > mobileLookBoostThreshold)
            {
                float t = (mag - mobileLookBoostThreshold) / Mathf.Max(0.0001f, 1f - mobileLookBoostThreshold);
                lookBoost = Mathf.Lerp(1f, mobileLookBoostMultiplier, t);
            }
        }

        float mobileLookSensitivity = mobileLookSensitivityX * fpsSensitivityMultiplier;
        float mobileDeltaTime = useSmoothDeltaTimeForLook ? Time.smoothDeltaTime : Time.deltaTime;
        _lastMobileLookScaled = new Vector2(
            mobileLook.x * mobileLookSensitivity * mobileLookSensitivityMultiplier * idleMultiplier * lookBoost * 100f * mobileDeltaTime,
            mobileLook.y * mobileLookSensitivity * mobileLookSensitivityMultiplier * idleMultiplier * lookBoost * 100f * mobileDeltaTime
        );

        mouseX += _lastMobileLookScaled.x;
        mouseY += _lastMobileLookScaled.y;

        if (maxLookDeltaPerFrame > 0f)
        {
            mouseX = Mathf.Clamp(mouseX, -maxLookDeltaPerFrame, maxLookDeltaPerFrame);
            mouseY = Mathf.Clamp(mouseY, -maxLookDeltaPerFrame, maxLookDeltaPerFrame);
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        _player.camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void SmoothLookAt(Transform target, float duration = 0.3f)
    {
        if (target == null || _player == null || _player.camera == null)
            return;

        if (_smoothLookRoutine != null)
            StopCoroutine(_smoothLookRoutine);

        _smoothLookRoutine = StartCoroutine(SmoothLookAtRoutine(target.position, duration));
    }

    private IEnumerator SmoothLookAtRoutine(Vector3 targetPos, float duration)
    {
        Vector3 camPos = _player.camera.transform.position;
        Vector3 dir = targetPos - camPos;
        if (dir.sqrMagnitude < 0.0001f)
            yield break;

        dir.Normalize();

        float startYaw = transform.eulerAngles.y;
        float startPitch = xRotation;

        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        float targetYaw = flatDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flatDir).eulerAngles.y
            : startYaw;

        float targetPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, -maxLookAngle, maxLookAngle);

        float time = 0f;
        float clampedDuration = Mathf.Max(0.01f, duration);

        while (time < clampedDuration)
        {
            float t = time / clampedDuration;
            float yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
            float pitch = Mathf.LerpAngle(startPitch, targetPitch, t);
            ApplyLookAngles(yaw, pitch);
            time += Time.deltaTime;
            yield return null;
        }

        ApplyLookAngles(targetYaw, targetPitch);
        _smoothLookRoutine = null;
    }

    private void ApplyLookAngles(float yaw, float pitch)
    {
        xRotation = pitch;

        Vector3 camEuler = _player.camera.transform.localEulerAngles;
        _player.camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, camEuler.z);

        Vector3 bodyEuler = transform.eulerAngles;
        bodyEuler.y = yaw;
        transform.eulerAngles = bodyEuler;
    }

    // ---------------- MOVE ----------------
    void HandleMove()
    {
        if (_player.IsLocked()) return;

        bool allowMovement = TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement);

        float x = 0f;
        float z = 0f;
        if (!MobileInput.Enabled || allowKeyboardMovementWhenMobile)
        {
            x = (_player.inputManager.GetMoveRight() ? 1f : 0f) - (_player.inputManager.GetMoveLeft() ? 1f : 0f);
            z = (_player.inputManager.GetMoveForward() ? 1f : 0f) - (_player.inputManager.GetMoveBackward() ? 1f : 0f);
        }
        Vector2 mobileMove = MobileInput.Move;
        if (!allowMovement)
        {
            x = 0f;
            z = 0f;
            mobileMove = Vector2.zero;
        }
        x += mobileMove.x;
        z += mobileMove.y;
        x = Mathf.Clamp(x, -1f, 1f);
        z = Mathf.Clamp(z, -1f, 1f);

        Vector2 rawInput = new Vector2(x, z);
        float rawMagnitude = rawInput.magnitude;
        Vector3 input = rawMagnitude > 0.0001f ? new Vector3(x, 0, z).normalized : Vector3.zero;
        bool isMoving = rawMagnitude > 0.1f;
        _inputMoving = isMoving;
        bool mobileSprint = MobileInput.Enabled && mobileMove.sqrMagnitude >= (mobileSprintThreshold * mobileSprintThreshold);
        bool wantsToSprint = (!MobileInput.Enabled && _player.inputManager.GetSprint()) || MobileInput.SprintHeld || mobileSprint;
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

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        _isMoving = horizontalVelocity.sqrMagnitude > 0.01f;
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
        
        if ((!MobileInput.Enabled && _player.inputManager.GetCrouchDown()) || MobileInput.CrouchDown)
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
        _cameraCrouchY = Mathf.Lerp(_cameraCrouchY, targetCamY, Time.deltaTime * crouchTransitionSpeed);
    }

    void EnableHeadBob()
    {
        canUseHeadBob = true;
        pendingHeadBobInvoke = false;
    }

    // ---------------- HEADBOB ----------------
    void HandleHeadbob()
    {
        if (isCrouching)
        {
            stepTimer = 0f;
            _headBobOffsetY = Mathf.Lerp(_headBobOffsetY, 0f, Time.deltaTime * bobResetSpeed);
            return;
        }

        if (_debugHeadBobDisabled)
        {
            stepTimer = 0f;
            _headBobOffsetY = Mathf.Lerp(_headBobOffsetY, 0f, Time.deltaTime * bobResetSpeed);
            ResetArmsPose();
            return;
        }

        if (!canUseHeadBob || !_inputMoving || !controller.isGrounded)
        {
            stepTimer = 0f;
            _headBobOffsetY = Mathf.Lerp(_headBobOffsetY, 0f, Time.deltaTime * bobResetSpeed);
            return;
        }

        bool isSprinting = ((!MobileInput.Enabled && _player.inputManager.GetSprint()) || MobileInput.SprintHeld) && !staminaDepleted;
        float stepTime = isSprinting ? stepDuration * sprintStepMultiplier : stepDuration;

        float previousStepTimer = stepTimer;
        float bobDeltaTime = Time.smoothDeltaTime;
        stepTimer += bobDeltaTime / stepTime;

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

        float amplitude = bobAmplitude;
        if (isSprinting)
            amplitude *= sprintStepMultiplier;

        float bob = Mathf.Sin(stepTimer * Mathf.PI * 2f) * amplitude;
        _headBobOffsetY = Mathf.Lerp(_headBobOffsetY, bob, Time.deltaTime * bobResetSpeed);

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

    private void ResetArmsPose()
    {
        if (armsTransform == null) return;
        armsTransform.localPosition = armsStartLocalPos;
        armsTransform.localRotation = armsStartLocalRot;
    }

    private void ApplyCameraLocalPosition()
    {
        if (_player == null || _player.camera == null)
            return;

        Vector3 basePos = cameraStartLocalPos;
        basePos.y = _cameraCrouchY;
        _player.camera.transform.localPosition = basePos + Vector3.up * _headBobOffsetY;
    }

    void HandleArmSway()
    {
        if (armsTransform == null || _player.IsCameraLocked()) return;

        float mouseX = _lastMobileLookScaled.x;
        float mouseY = _lastMobileLookScaled.y;
        if (!MobileInput.Enabled)
        {
            mouseX += _lastMouseInputRaw.x;
            mouseY += _lastMouseInputRaw.y;
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
        
        if (((!MobileInput.Enabled && _player.inputManager.GetTurnLightDown()) || MobileInput.ToggleLightDown) && mLight != null)
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

        if (!mobileEnabled && ignoreLookFramesOnFocus > 0)
            _ignoreLookFrames = Mathf.Max(_ignoreLookFrames, ignoreLookFramesOnFocus);
    }

    private void TrackCursorLockState()
    {
        CursorLockMode current = Cursor.lockState;
        if (current == _lastCursorLockState)
            return;

        _lastCursorLockState = current;
        if (!MobileInput.Enabled && current == CursorLockMode.Locked && ignoreLookFramesOnFocus > 0)
            _ignoreLookFrames = Mathf.Max(_ignoreLookFrames, ignoreLookFramesOnFocus);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && ignoreLookFramesOnFocus > 0)
            _ignoreLookFrames = Mathf.Max(_ignoreLookFrames, ignoreLookFramesOnFocus);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && ignoreLookFramesOnFocus > 0)
            _ignoreLookFrames = Mathf.Max(_ignoreLookFrames, ignoreLookFramesOnFocus);
    }

}
