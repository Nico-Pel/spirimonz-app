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

    [Header("Mobile Look")]
    public bool enableMobileLook = true;
    public float mobileLookSensitivityX = 2.0f;
    public float mobileLookSensitivityY = 2.0f;
    public float mobileLookSensitivityMultiplier = 0.06666667f;
    public float mobileMinPitch = -35f;
    public float mobileMaxPitch = 60f;

    [Header("Mobile Sprint")]
    [Range(0.1f, 1f)] public float mobileSprintThreshold = 0.75f;

    [Header("Gravity")]
    public float gravity = -20f;
    private Vector3 velocity;
    private bool _mobileAnglesInitialized;
    private float _mobileYaw;
    private float _mobilePitch;
    private Player _player;

    private void Start()
    {
        _player = Player.Instance;
    }

    void Update()
    {
        if (_player != null && _player.IsLocked())
            return;

        HandleMovement();
        HandleMobileLook();
    }

    void HandleMovement()
{
    // 1️⃣ Input
    float h = 0f;
    float v = 0f;
    if (!MobileInput.Enabled)
    {
        h = Input.GetAxis("Horizontal"); // A/D ou flèches
        v = Input.GetAxis("Vertical");   // W/S ou flèches
    }
    Vector2 mobileMove = MobileInput.Move;
    h += mobileMove.x;
    v += mobileMove.y;
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
        bool mobileSprint = MobileInput.Enabled && mobileMove.sqrMagnitude >= (mobileSprintThreshold * mobileSprintThreshold);
        if ((!MobileInput.Enabled && Input.GetKey(KeyCode.LeftShift)) || MobileInput.SprintHeld || mobileSprint) // sprint
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

    private void HandleMobileLook()
    {
        if (_player != null && _player.IsCameraLocked())
            return;

        if (!enableMobileLook || !MobileInput.Enabled || camTransform == null)
            return;

        if (!_mobileAnglesInitialized)
        {
            Vector3 euler = camTransform.localEulerAngles;
            _mobileYaw = euler.y;
            _mobilePitch = NormalizePitch(euler.x);
            _mobileAnglesInitialized = true;
        }

        Vector2 look = MobileInput.GetLookDelta();
        if (look.sqrMagnitude < 0.00001f)
            return;

        _mobileYaw += look.x * mobileLookSensitivityX * mobileLookSensitivityMultiplier * 100f * Time.deltaTime;
        _mobilePitch -= look.y * mobileLookSensitivityY * mobileLookSensitivityMultiplier * 100f * Time.deltaTime;
        _mobilePitch = Mathf.Clamp(_mobilePitch, mobileMinPitch, mobileMaxPitch);

        camTransform.localRotation = Quaternion.Euler(_mobilePitch, _mobileYaw, 0f);
    }

    private float NormalizePitch(float x)
    {
        if (x > 180f) x -= 360f;
        return x;
    }
}
