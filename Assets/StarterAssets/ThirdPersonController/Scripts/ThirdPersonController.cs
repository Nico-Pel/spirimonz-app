using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Player Settings")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    [Range(0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    [Header("Jump & Gravity")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15f;
    public float JumpTimeout = 0.5f;
    public float FallTimeout = 0.15f;

    [Header("Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;
    
    [Header("Camera Sensitivity")]
    public float mouseSensitivity = 1.5f;
    public float mobileLookSensitivityMultiplier = 0.08f;
    public float mobileLookVerticalMultiplier = 0.6f;

    [Header("Mobile Sprint")]
    [Range(0.1f, 1f)] public float mobileSprintThreshold = 0.75f;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70f;
    public float BottomClamp = -30f;
    public float CameraAngleOverride = 0f;
    public bool LockCameraPosition = false;
    
    [Header("Footsteps")]
    public FootstepsListener footstepsListener;
    private float _footstepCooldownWalk = 0.35f;
    private float _footstepCooldownRun  = 0.30f;

    private float _lastFootstepTime = -999f;

    [Header("Audio")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0f, 1f)] public float FootstepAudioVolume = 0.5f;

    // Private
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    [FormerlySerializedAs("_animator")] public Animator animator;
    private CharacterController _controller;
    private InputManager _inputManager;

    private Player _player;

    private GameObject _mainCamera;

    private void Start()
    {
        _player = Player.Instance;
        _mainCamera = _player.camera.gameObject;
        _inputManager = InputManager.Instance;

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _controller = GetComponent<CharacterController>();

        if (_inputManager == null)
            Debug.LogError("InputManager manquant sur le Player !");

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity();

        if (_player.IsLocked())
        {
            animator.SetFloat("Speed", 0);
            return;
        }
        
        Move();
        UpdateFootsteps();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        animator.SetBool("Grounded", Grounded);
    }

    private void CameraRotation()
    {
        if (LockCameraPosition || _player.IsCameraLocked()) return;

        float mouseX = 0f;
        float mouseY = 0f;
        if (!MobileInput.Enabled)
        {
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }
        else
        {
            Vector2 look = MobileInput.GetLookDelta();
            mouseX = look.x * mouseSensitivity * mobileLookSensitivityMultiplier * 100f * Time.deltaTime;
            mouseY = look.y * mouseSensitivity * mobileLookSensitivityMultiplier * mobileLookVerticalMultiplier * 100f * Time.deltaTime;
        }

        _cinemachineTargetYaw += mouseX;
        _cinemachineTargetPitch -= mouseY;
        _cinemachineTargetPitch = Mathf.Clamp(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                                                                      _cinemachineTargetYaw, 0f);
    }

    private void Move()
    {
        Vector2 moveInput = Vector2.zero;
        if (!MobileInput.Enabled)
        {
            if (Input.GetKey(_inputManager.forwardKey)) moveInput.y += 1f;
            if (Input.GetKey(_inputManager.backwardKey)) moveInput.y -= 1f;
            if (Input.GetKey(_inputManager.leftKey)) moveInput.x -= 1f;
            if (Input.GetKey(_inputManager.rightKey)) moveInput.x += 1f;
        }
        else
        {
            moveInput = MobileInput.Move;
        }

        bool mobileSprint = MobileInput.Enabled && moveInput.sqrMagnitude >= (mobileSprintThreshold * mobileSprintThreshold);
        bool sprintInput = (!MobileInput.Enabled && Input.GetKey(_inputManager.sprintKey)) || MobileInput.SprintHeld || mobileSprint;

        float targetSpeed = sprintInput ? SprintSpeed : MoveSpeed;
        if (moveInput == Vector2.zero) targetSpeed = 0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (moveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                                                   RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

        animator.SetFloat("Speed", _animationBlend);
        animator.SetFloat("MotionSpeed", inputMagnitude);
    }

    private void JumpAndGravity()
    {
        bool jumpInput = /*Input.GetKeyDown(_inputManager.jumpKey)*/ false;

        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            animator.SetBool("Jump", false);
            animator.SetBool("FreeFall", false);

            if (_verticalVelocity < 0f) _verticalVelocity = -2f;

            if (jumpInput && _jumpTimeoutDelta <= 0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                animator.SetBool("Jump", true);
            }

            if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0f) _fallTimeoutDelta -= Time.deltaTime;
            else
                animator.SetBool("FreeFall", true);
        }

        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        Color color = Grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
        Gizmos.color = color;
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }
    
// =======================
// FOOTSTEPS
// =======================

    /// <summary>
    /// Called by Animation Event
    /// </summary>
    public void OnFootstep()
    {
        if (!Grounded)
            return;

        if (footstepsListener == null)
            return;

        if (_speed < 0.1f)
            return;

        float speed01 = Mathf.InverseLerp(0f, SprintSpeed, _speed);
        bool isRunning = speed01 > 0.55f;

        float cooldown = isRunning ? _footstepCooldownRun : _footstepCooldownWalk;

        if (Time.time - _lastFootstepTime < cooldown)
            return;

        _lastFootstepTime = Time.time;

        float volumeMultiplier = Mathf.Lerp(0.7f, 1.2f, speed01);
        footstepsListener.PlayFootstep(volumeMultiplier);
    }
    
    private float lastLeftStep = -1f;
    private float lastRightStep = -1f;

    private void UpdateFootsteps()
    {
        if (!Grounded || _speed < 0.1f || footstepsListener == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = state.length; 
        float speed = animator.speed * state.speedMultiplier; // prend le blendTree en compte
        float currentTime = state.normalizedTime * clipLength / speed; // temps réel écoulé dans le clip

        // Frames converties en secondes
        float leftStepTime  = 12f / 37f * clipLength;
        float rightStepTime = 30f / 37f * clipLength;

        if (currentTime >= leftStepTime && lastLeftStep < leftStepTime)
            footstepsListener.PlayFootstep();

        if (currentTime >= rightStepTime && lastRightStep < rightStepTime)
            footstepsListener.PlayFootstep();

        lastLeftStep  = currentTime;
        lastRightStep = currentTime;
    }
}
