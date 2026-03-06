using System;
using UnityEngine;
using DG.Tweening;

public class NPC : GameBehaviour
{
    [Header("Dialogue")]
    public Dialogue dialogue;
    public Transform neck;
    public CTA cta;
    public Animator animator;

    [Header("Camera")]
    public Cinemachine.CinemachineVirtualCamera dialogueVCam;
    public bool dynamicCamera = true;
    public float cameraMoveDuration = 0.35f;

    [Header("Interaction")]
    public float maxNeckAngle = 60f;
    public bool mustBeInFront = true;
    public float bodyTurnDuration = 0.25f;

    private Vector3 _neckRotationBase;
    private bool _canInteract = true;

    private void Awake()
    {
        _neckRotationBase = neck.localEulerAngles;
    }

    public void Interact(Player player)
    {
        if (!_canInteract) return;
        if (mustBeInFront && !IsPlayerInFront(player)) return;

        _canInteract = false;

        // Verrouille les contrôles du joueur
        player.LockControls(true);

        // Ferme le CTA
        CloseCTA();

        // Animation de dialogue
        animator.SetBool("Talking", true);

        // Calcul direction + angle
        Vector3 dirToPlayer = (player.head.position - neck.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        // Fonction locale pour démarrer dialogue + caméra
        void StartDialogueAndCamera()
        {
            // La tête regarde le joueur
            neck.DOLookAt(player.head.position, 0.2f);

            // Démarre le dialogue UI
            UIGame.Instance.uiDialogue.StartDialogue(dialogue);

            // Positionne la caméra après que la tête soit orientée
            if (dialogueVCam != null)
            {
                if (dynamicCamera)
                    PositionDialogueCamera(player);
                
                dialogueVCam.gameObject.SetActive(true);
            }
        }

        // Si l’angle est trop grand, tourner le corps d’abord
        if (angle > maxNeckAngle)
        {
            transform.DORotateQuaternion(Quaternion.LookRotation(dirToPlayer), bodyTurnDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(StartDialogueAndCamera);
        }
        else
        {
            StartDialogueAndCamera();
        }
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

        dialogueVCam.transform.LookAt(neck.position);
        Vector3 neckOffset = useRightShoulder ? dialogueVCam.transform.right * -1 : dialogueVCam.transform.right * 1;
        dialogueVCam.transform.LookAt(neck.position + neckOffset);
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
        animator.SetBool("Talking", false);
        neck.DOLocalRotate(_neckRotationBase, 0.25f);

        // Désactive la caméra
        if (dialogueVCam != null)
            dialogueVCam.gameObject.SetActive(false);

        this.Invoke(0.25f, () => _canInteract = true);

        // Déverrouille le joueur
        player?.LockControls(false);
    }

    public bool CanInteract() => _canInteract;

    private bool IsPlayerInFront(Player player)
    {
        Vector3 dir = (player.characterController.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        return angle <= maxNeckAngle;
    }
}