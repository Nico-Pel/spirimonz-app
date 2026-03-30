using System;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class NPC : GameBehaviour, IInteractable
{
    public enum MovingType
    {
        none,
        walk
    }

    public MovingType movingType = MovingType.none;
    
    [Space]
    
    [Header("Dialogue")]
    public Dialogue dialogue;
    public Transform neck;
    public CTA cta;
    public Animator animator;

    [Header("Camera")]
    public Cinemachine.CinemachineVirtualCamera dialogueVCam;
    public bool dynamicCamera = true;
    public bool disableDialogueCameraInFPS = true;
    //public float cameraMoveDuration = 0.35f;

    [Header("Interaction")]
    public float maxNeckAngle = 60f;
    public bool mustBeInFront = true;
    public bool turnBodyToTalk = true;
    public float bodyTurnDuration = 0.25f;
    public bool useAnimationTalk = true;
    public bool lockRotationToY = true;

    [Header("Moving Settings")] 
    public NPCMovingPoint firstMovingPoint;
    public NPCMovingPoint lastSupposedMovingPoint;
    public NavMeshAgent agent;
    public float speed = 2f;
    public float minDistToNextPoint = 0.1f;
    public float maxDistToNextPoint = 2f;

    [Header("Cursor")]
    public Sprite specialCursor;
    public float cursorSize = 1f;
    [SerializeField] private bool interactionLocked;

    private Vector3 _neckRotationBase;
    private bool _canInteract = true;
    
    private NPCMovingPoint _currentMovingPoint;
    private NPCMovingPoint _lastMovingPoint;
    private float _distToNextPoint;

    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;

    public NPCMovingPoint GetLastMovingPoint() => _lastMovingPoint;

    private void Awake()
    {
        _neckRotationBase = neck.localEulerAngles;

        if (agent == null || firstMovingPoint == null)
        {
            movingType = MovingType.none;
            agent.enabled = false;
        }
        else
        {
            if (movingType == MovingType.walk)
            {
                animator.SetBool("Walking", true);

            }
        }
    }

    private void Start()
    {
        if (movingType != MovingType.none)
        {
            _distToNextPoint = Random.Range(minDistToNextPoint, maxDistToNextPoint);
            _currentMovingPoint = firstMovingPoint;
            _lastMovingPoint = lastSupposedMovingPoint != null ? lastSupposedMovingPoint : _currentMovingPoint;

            agent.speed = speed;
            agent.SetDestination(firstMovingPoint.transform.position);
        }
    }

    private void Update()
    {
        if (movingType != MovingType.none)
        {
            float dist = Vector3.Distance(transform.position, _currentMovingPoint.transform.position);
            if (dist < _distToNextPoint)
            {
                _distToNextPoint = Random.Range(minDistToNextPoint, maxDistToNextPoint);
                NPCMovingPoint reachedPoint = _currentMovingPoint;
                _currentMovingPoint = _currentMovingPoint.SelectNextMovingPoint(this);
                _lastMovingPoint = reachedPoint;
                agent.SetDestination(_currentMovingPoint.transform.position);
            }
        }
    }

    public void Interact(Player player, bool useDialogueCamera = true)
    {
        if (!_canInteract)
        {
            player.LockControls(false);
            return;
        }

        if (mustBeInFront && !IsPlayerInFront(player))
        {
            player.LockControls(false);
            return;
        }

        _canInteract = false;
        onDialogueStart?.Invoke();

        // Ensure player tracks the current NPC for proper dialogue end/reset
        if (player != null)
            player.currentNPC = this;

        // Verrouille les contrôles du joueur
        player.LockControls(true);

        // Ferme le CTA
        CloseCTA();

        // Animation de dialogue
        if(useAnimationTalk)
            animator.SetBool("Talking", true);

        // Calcul direction + angle
        Vector3 dirToPlayer = (player.head.position - neck.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        // Fonction locale pour démarrer dialogue + caméra
        void StartDialogueAndCamera()
        {
            if (agent.enabled)
            {
                agent.speed = 0;
                agent.isStopped = true;
            }
            // La tête regarde le joueur
            neck.DOLookAt(player.head.position - Vector3.up * 0.2f, 0.2f);

            // Démarre le dialogue UI
            UIGame.Instance.uiDialogue.StartDialogue(dialogue);

            // Positionne la caméra après que la tête soit orientée
            if (dialogueVCam != null && useDialogueCamera && ShouldUseDialogueCamera(player))
            {
                if (dynamicCamera)
                    PositionDialogueCamera(player);
                
                dialogueVCam.gameObject.SetActive(true);
            }
        }

        // Si l’angle est trop grand, tourner le corps d’abord
        if (angle > maxNeckAngle && turnBodyToTalk)
        {
            Vector3 flatDir = GetFlatDirection(dirToPlayer);
            Quaternion targetRotation = Quaternion.LookRotation(flatDir, Vector3.up);
            Vector3 targetEuler = new Vector3(0f, targetRotation.eulerAngles.y, 0f);
            transform.DORotate(targetEuler, bodyTurnDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(StartDialogueAndCamera);
        }
        else
        {
            StartDialogueAndCamera();
        }
    }

    private Vector3 GetFlatDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return transform.forward;
        return dir.normalized;
    }

    private void LateUpdate()
    {
        if (!lockRotationToY)
            return;

        Vector3 euler = transform.eulerAngles;
        if (Mathf.Abs(euler.x) > 0.001f || Mathf.Abs(euler.z) > 0.001f)
            transform.eulerAngles = new Vector3(0f, euler.y, 0f);
    }

    private void PositionDialogueCamera(Player player)
    {
        if (dialogueVCam == null) return;
    
        Vector3 directionToTarget = transform.position - player.characterController.transform.position;
        float dot = Vector3.Dot(player.characterController.transform.right, directionToTarget);

        bool useRightShoulder = dot > 0;
        Vector3 shoulderOffset = useRightShoulder ? player.head.right * 1 : player.head.right * -1;
        
        Vector3 camTargetPos = player.head.position + shoulderOffset + player.head.forward * -0.5f;
        dialogueVCam.transform.position = camTargetPos;

        if (math.abs(player.characterController.transform.position.y - transform.position.y) > 1f)
        {
            float dist = Vector3.Distance(player.characterController.transform.position, transform.position);
            Vector3 neckOffset = useRightShoulder 
                ? dialogueVCam.transform.right * (-dist / 3) 
                : dialogueVCam.transform.right * (dist / 3);

            dialogueVCam.transform.LookAt(neck.position + neckOffset, Vector3.up);
        }
        else
        {
            dialogueVCam.transform.LookAt(neck.position);
            float dist = Vector3.Distance(player.characterController.transform.position, transform.position);
            Vector3 neckOffset = useRightShoulder ? dialogueVCam.transform.right * (-dist / 3) : dialogueVCam.transform.right * (dist / 3);
            dialogueVCam.transform.LookAt(neck.position + neckOffset);
        }
    }

    public void OpenCTA(Player player)
    {
        // Ne montre le CTA que si le joueur est devant ou si l'interaction arrière est autorisée
        if (mustBeInFront && !IsPlayerInFront(player)) return;
        cta.SetCallToAction(true, player);
    }

    public void CloseCTA()
    {
        cta.SetCallToAction(false, null);
    }

    public void Reset(Player player = null)
    {
        if(useAnimationTalk)
            animator.SetBool("Talking", false);
        
        neck.DOLocalRotate(_neckRotationBase, 0.25f);

        // Désactive la caméra
        if (dialogueVCam != null)
            dialogueVCam.gameObject.SetActive(false);

        this.Invoke(0.25f, () => _canInteract = true);

        // Déverrouille le joueur
        player?.LockControls(false);
        
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.speed = speed;
        }
        
        onDialogueEnd?.Invoke();
    }

    public bool CanInteract(Player player)
    {
        if (!_canInteract)
        {
            return false;
        }

        if (mustBeInFront && !IsPlayerInFront(player))
        {
            return false;

        }
        
        return _canInteract;
    }

    private bool IsPlayerInFront(Player player)
    {
        Vector3 dir = (player.characterController.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        return angle <= maxNeckAngle;
    }

    private bool ShouldUseDialogueCamera(Player player)
    {
        if (dialogueVCam == null)
            return false;

        if (disableDialogueCameraInFPS && player is GamePlayer)
            return false;

        return true;
    }

    // =========================
    // IInteractable (FPS)
    // =========================
    public Sprite SpecialCursor
    {
        get => specialCursor;
        set => specialCursor = value;
    }

    public float CursorSize
    {
        get => cursorSize;
        set => cursorSize = value;
    }

    public bool InteractionLocked
    {
        get => interactionLocked || !_canInteract;
        set => interactionLocked = value;
    }

    public void OnInteractStart()
    {
        Player player = Player.Instance;
        if (player == null)
            return;

        if (InteractionLocked)
            return;

        Interact(player, useDialogueCamera: true);
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }
}
