using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : GameBehaviour
{
    public static Player Instance { get; private set; }

    public Camera camera;

    public Transform head;

    public CharacterController characterController;
    [ReadOnly] public InputManager inputManager;
    [ReadOnly] public InventoryManager inventoryManager;

    [Header("NPC Interactions")] 
    public bool detectNPC;
    [SerializeField] float interactionDistance = 3f;
    [SerializeField] float interactionAngle = 45f;
    [SerializeField] float interactionAngleIgnoreDistance = 0.6f;
    [SerializeField] LayerMask npcLayer;

    [ReadOnly] public NPC currentNPC;
    
    [Space]
    
    [ReadOnly] private bool lockControls;
    [ReadOnly] private bool lockCamera;
    protected bool _isDead;
    
    public bool IsLocked() => lockControls;
    public bool IsCameraLocked() => lockCamera;
    public bool IsDead() => _isDead;

    private bool _canStartDialogue = true;

    private void Awake()
    {
        Instance = this;
        
        if(GameManager.Instance != null)
            GameManager.Instance.player = this;
    }

    protected virtual void Start()
    {
        inventoryManager = InventoryManager.Instance;
        inputManager = InputManager.Instance;
    }

    public void LockControls(bool enable, bool movementsOnly = false)
    {
        if (_isDead)
            enable = true;
            
        lockControls = enable;
        
        if (movementsOnly == false)
            lockCamera = enable;
    }

    protected virtual void Update()
    {
        if (_canStartDialogue && detectNPC && IsLocked() == false)
        {
            DetectNPC();

            bool allowInteract = TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract);
            bool mobileNpcInteractDown = false;
            if (MobileInput.Enabled && currentNPC != null)
                mobileNpcInteractDown = MobileInput.GrabDown || MobileInput.PrimaryDown || MobileInput.ConsumeGrabDown() || MobileInput.ConsumePrimaryDown();

            if (allowInteract && currentNPC != null && ((!MobileInput.Enabled && inputManager.GetWorldInteractionDown()) || mobileNpcInteractDown))
            {
                _canStartDialogue = false;
                if (currentNPC.CanInteract(this))
                {
                    LockControls(true);
                    currentNPC.Interact(this);
                }
            }
        }
    }

    public void EndDialogue()
    {
        Player player = Player.Instance;

        if (currentNPC != null)
        {
            currentNPC.Reset(player);
            currentNPC = null;
        }
        else
        {
            // Safety: ensure controls are unlocked if dialogue ended without a tracked NPC
            LockControls(false);
        }
        
        this.Invoke(0.5f, () => _canStartDialogue = true);
    }
    
    void DetectNPC()
    {
        Collider[] hits = Physics.OverlapSphere(characterController.transform.position + Vector3.up, interactionDistance, npcLayer);

        if (hits.Length == 0 && currentNPC != null)
        {
            currentNPC.CloseCTA();
            currentNPC = null;
            return;
        }

        foreach (var hit in hits)
        {
            NPC npc = hit.GetComponent<NPC>();
            if (npc == null) continue;

            Vector3 dirToNPC = (npc.transform.position - characterController.transform.position).normalized;

            float angle = Vector3.Angle(characterController.transform.forward, dirToNPC);
            float distToNpc = Vector3.Distance(characterController.transform.position, npc.transform.position);
            float ignoreDistance = npc.interactionAngleIgnoreDistance > 0f ? npc.interactionAngleIgnoreDistance : interactionAngleIgnoreDistance;
            bool withinAngle = angle <= interactionAngle || (ignoreDistance > 0f && distToNpc <= ignoreDistance);

            if (withinAngle && npc.CanInteract(this))
            {
                if (currentNPC == null)
                {
                    currentNPC = npc;
                    npc.OpenCTA(this);
                }
                break;
            }
            else if (currentNPC == npc)
            {
                npc.CloseCTA();
                currentNPC = null;
            }
        }
    }

    public void SetPosition(Vector3 newPos)
    {
        if (characterController == null) return;
        
        characterController.enabled = false;
        characterController.transform.position = newPos;
        characterController.enabled = true;
    }
    
    public void SetRotation(Quaternion newRot)
    {
        //characterController.enabled = false;
        characterController.transform.rotation = newRot;
        //characterController.enabled = true;
    }

    public Vector3 GetPosition()
    {
        return characterController.transform.position;
    }

    public Quaternion GetRotation()
    {
        return characterController.transform.rotation;
    }

    public virtual void ReceiveArticle(Article article, bool useSound = false)
    {
        inventoryManager.AddArticle(article, useSound);
    }
}
