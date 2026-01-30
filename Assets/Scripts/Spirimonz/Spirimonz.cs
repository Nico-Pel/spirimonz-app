using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Spirimonz : GameBehaviour, IInteractable
{
    public enum SpirimonzBehaviourState
    {
        Wait,
        FollowPlayer,
        Roam,
        Escape,
        Special,
    }

    public GameObject spirimonzGameObject;
    public GameObject hidingGameObject;

    [Header("Spirimonz Settings")] 
    [ReadOnly] public bool isOnTheMap;
    public bool canInteract = true;
    public bool canBeDroppedOnMap = true;
    public bool canBeTakenBackIntoHands = true;
    public bool powerActiveInHands = true;
    public bool lookAtPlayerWhileWaiting = true;
    public bool openDoorsOnItsWay = false;
    public bool lookForwardOnDropOnMap;
    public float lookAtDistanceFromPlayer = 10f;
    public float forecastTimeBeforeAHunt = 1f;
    public float jumpForceMultiplier = 1f;

    private bool _baseCanInteract;

    public InventoryManager.HandPoses handPosType = InventoryManager.HandPoses.PalmOfTheHand;
    public SpirimonzBehaviourState baseBehaviour = SpirimonzBehaviourState.Wait;
    public SpirimonzBehaviourState secondaryBehaviour = SpirimonzBehaviourState.FollowPlayer;
    public float speed = 2;
    public float followingDistance = 2f;
    [SerializeField] private float lookAtSpeed = 5f;
    
    private SpirimonzBehaviourState _currentBehaviour;
    public Room currentRoom { get; set; }

    [Header("Spirimonz Settings : Escape")]
    public float targetedEscapeDistance = 1f;
    public float escapingSpeed = 5f;
    public int nbOfWayPointsToConsider = 3;

    private bool _escaping;
    private bool _stopMoving;
    private float _waitDoorTime = 1f;
    
    [Header("Spirimonz Components")]
    public NavMeshAgent agent;
    public Collider collider;
    public Animator animator;
    
    private IInteractable _interactableImplementation;
    
    private Transform _targetedTransform;
    
    private bool _hidingFromAGhost;

    private bool _isLocked;
    private bool _initialized;

    private void Start()
    {
        InitSpirimonz();
    }

    public virtual void InitSpirimonz()
    {
        if (_isLocked) return;
        
        _baseCanInteract = canInteract;
        _currentBehaviour = SpirimonzBehaviourState.Wait;
        hidingGameObject.SetActive(false);
        
        House.Instance.currentGhost.onGhostCallForAHunt.AddListener(StartDelayBeforeFeelingAHunt);
        House.Instance.currentGhost.onGhostStartToHunt.AddListener(OnHuntStart);
        House.Instance.currentGhost.onGhostStopToHunt.AddListener(OnHuntEnd);
        
        EnableSpirimonz(false);
        _initialized = true;
    }

    private void OnEnable()
    {
        if (_initialized)
        {
            SetSpiritHideMode(_hidingFromAGhost);

            // Si le Spirimonz était désactivé pendant le forecast, on applique la fuite maintenant
            if (_shouldFeelAHunt)
            {
                _shouldFeelAHunt = false;
                FeelAHunt();
            }
        }

        Player player = GetComponentInParent<Player>();
        animator.SetBool("Hands", player != null);

        ActionOnEnabled();
    }

    public virtual void ActionOnEnabled()
    {
        
    }

    private void SetSpiritHideMode(bool hide)
    {
        spirimonzGameObject.SetActive(!hide);
        hidingGameObject.SetActive(hide);
        agent.enabled = !hide && isOnTheMap;

        if (isOnTheMap && hide == false)
        {
            collider.enabled = true;
        }

        if (hide)
        {
            collider.enabled = false;
        }
    }

    private bool _shouldFeelAHunt;
    private void StartDelayBeforeFeelingAHunt()
    {
        float timeBeforeDisappearing = House.Instance.currentGhost.forecastTimeBeforeAHunt - forecastTimeBeforeAHunt;
        if (timeBeforeDisappearing <= 0)
            timeBeforeDisappearing = 0.1f;

        if (gameObject.activeSelf)
        {
            this.Invoke(timeBeforeDisappearing, FeelAHunt);
        }
        else
        {
            _shouldFeelAHunt = true;
        }
    }

    private void FeelAHunt()
    {
        _hidingFromAGhost = true;
        agent.speed = 0;
        agent.velocity = Vector3.zero;
        
        SetSpiritHideMode(true);
    }

    protected virtual void OnHuntStart()
    {
        
    }

    protected virtual void OnHuntEnd()
    {
        if (!Player.Instance.IsDead())
        {
            _shouldFeelAHunt = false;
            _hidingFromAGhost = false;
            SetSpiritHideMode(false);
        }
    }

    public virtual void DroppingOnMap()
    {
        animator.SetBool("Hands", false);
    }
    
    public virtual void DroppedOnMap()
    {
        _currentBehaviour = baseBehaviour;

        if (baseBehaviour == SpirimonzBehaviourState.Escape)
        {
            _escaping = true;
        }

        if (House.Instance.currentGhost.IsHunting())
        {
            collider.enabled = false;
        }
    }

    public virtual bool GoBackToHands(Transform handPos)
    {
        if (canBeTakenBackIntoHands == false)
        {
            animator.SetTrigger("Nop");
            return false;
        }
        
        EnableSpirimonz(false);
        transform.parent = handPos;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
        
        animator.SetBool("Hands", true);

        return true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>() && other.gameObject.layer == 8)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        if (other.TryGetComponent(out Room room))
        {
            SetCurrentRoom(room);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (openDoorsOnItsWay == true)
        {
            if (other.gameObject.TryGetComponent(out Door door) && openDoorsOnItsWay)
            {
                if (door.IsGrabbed()) return;
                
                if (other.gameObject.TryGetComponent(out Collider doorCollider))
                {
                    IgnoreCollider(other.collider, 3f);
                }
                TryToOpenDoor(door);
            }
        }
    }

    private void IgnoreCollider(Collider coll, float duration = -1)
    {
        Physics.IgnoreCollision(coll, collider, true);
        if (duration > 0)
        {
            this.Invoke(duration, () =>
            {
                Physics.IgnoreCollision(coll, collider, false);
            });
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>() && other.gameObject.layer == 8)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    protected virtual void SetCurrentRoom(Room room)
    {
        currentRoom = room;
    }

    private void TryToOpenDoor(Door door)
    {
        if (openDoorsOnItsWay == false) return;
        
        OpenDoor(door);

        /*Vector3 directionToDoor = (door.transform.position - transform.position).normalized;
        Vector3 moveDirection = agent.velocity.normalized;

        float dot = Vector3.Dot(moveDirection, directionToDoor);

        if (dot > 0.6f) // seuil à ajuster
        {
            OpenDoor(door);
        }*/
    }

    private void OpenDoor(Door door)
    {
        _stopMoving = true;
        agent.velocity = Vector3.zero;        
        this.Invoke(_waitDoorTime, () => _stopMoving = false);

        // 1️⃣ Récupère l'angle cible complet
        float fullOpen = door.openFullAngle;
        float currentAngle = door.hingeJoint.angle;

        // 2️⃣ Vérifie la distance restante
        float delta = Mathf.Abs(fullOpen - currentAngle);

        float targetPercentage;

        if (delta < 10f) 
        {
            // La porte est à peine ouverte → on force un peu plus
            // Par exemple, on vise entre 80 et 100%
            targetPercentage = Mathf.Lerp(
                currentAngle, fullOpen, Random.Range(0.8f, 1f)
            );
        }
        else
        {
            // Porte fermée ou partiellement ouverte → ouverture normale
            targetPercentage = Random.Range(0.8f, 1f);
        }

        // 3️⃣ Définir la vitesse raisonnable
        float speed = 50f;

        // 4️⃣ Interaction fantôme
        door.GhostDoorInteraction(targetPercentage, speed);
    }

    private void Update()
    {
        if (_isLocked) return;

        if (Input.GetMouseButtonDown(1) && !isOnTheMap)
        {
            OnClickInHands();
        }
        
        UpdateSpirimonzBehaviour();
        
        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", agent.speed);
        }
        
        UpdateMovementBehaviour();
    }

    private void UpdateMovementBehaviour()
    {
        if (_hidingFromAGhost || isOnTheMap == false || _stopMoving)
        {
            agent.speed = 0;
            return;
        }

        if (_stopMoving == true)
        {
            Wait();
            return;
        }
        
        switch (_currentBehaviour)
        {
            case SpirimonzBehaviourState.Wait:
                Wait();
                break;
            case SpirimonzBehaviourState.FollowPlayer:
                FollowingPlayer();
                break;
            case SpirimonzBehaviourState.Escape:
                Escaping();
                break;
            case SpirimonzBehaviourState.Special:
                UpdateSpecialMovement();
                break;
            default:
                break;
        }
    }

    private void Wait()
    {
        agent.speed = 0;
        if (lookAtPlayerWhileWaiting)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        Vector3 targetDir;
        float dist = Vector3.Distance(transform.position, Player.Instance.transform.position);

        if (dist < lookAtDistanceFromPlayer)
        {
            targetDir = House.Instance.currentPlayer.transform.position - transform.position;
        }
        else
        {
            Vector3 forwardTarget = transform.position + transform.forward * 5f;
            targetDir = forwardTarget - transform.position;
        }

        // Optionnel : lock l’axe Y
        targetDir.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            lookAtSpeed * Time.deltaTime
        );
    }

    public virtual void UpdateSpecialMovement()
    {
        
    }

    public virtual bool UpdateSpirimonzBehaviour()
    {
        if (_isLocked) return false;
        
        if ((isOnTheMap == false && powerActiveInHands == false) || _hidingFromAGhost)
            return false;

        return true;
    }

    private void FollowingPlayer()
    {
        Vector3 playerPos = House.Instance.currentPlayer.transform.position;
        float dist = Vector3.Distance(transform.position, playerPos);
        agent.speed = dist > followingDistance ? speed : 0;
        agent.SetDestination(playerPos);
        
        LookAtPlayer();
    }

    private void Escaping()
    {
        if (_escaping == false)
        {
            SwitchBehaviour();
        }
        
        if (_targetedTransform == null)
        {
            _targetedTransform =
                House.Instance.SelectRandomWaypointFurthestFromPosition(agent, nbOfWayPointsToConsider).transform;
        }

        if (_targetedTransform == null)
        {
            SwitchBehaviour();
            return;
        }
        
        float dist = Vector3.Distance(transform.position, _targetedTransform.position);
        if (dist > targetedEscapeDistance)
        {
            agent.speed = escapingSpeed;
            agent.SetDestination(_targetedTransform.position);
        }
        else
        {
            agent.speed = 0;
            EscapePointReached();
        }
    }

    public virtual void EscapePointReached()
    {
        
    }

    public void OnInteractStart()
    {
        if (canInteract == false)
        {
            animator.SetTrigger("Nop");
            return;
        }
        
        animator.SetTrigger("Click");
        
        InteractionStarted();
    }

    public virtual void InteractionStarted()
    {
        SwitchBehaviour();
    }
    
    public void SwitchBehaviour()
    {
        SpirimonzBehaviourState stateToUse = _currentBehaviour == baseBehaviour ? secondaryBehaviour : baseBehaviour;
        if (stateToUse == secondaryBehaviour)
        {
            animator.SetTrigger("Switch1");
        }
        else
        {
            animator.SetTrigger("Switch2");
        }
        ChangeBehaviour(stateToUse);
    }

    public void ChangeBehaviour(SpirimonzBehaviourState newBehaviour)
    {
        _currentBehaviour = newBehaviour;
        animator.SetBool("Stand", newBehaviour == SpirimonzBehaviourState.Wait);
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }

    public void EnableSpirimonz(bool enable)
    {
        gameObject.SetActive(enable);
        agent.enabled = enable && House.Instance.currentGhost.IsHunting() == false;
        collider.enabled = enable;
        isOnTheMap = enable;

        if (enable == true)
        {
            OnSpirimonzEnabled();
        }
        else
        {
            OnSpirimonzDisabled();
        }
    }

    public virtual void OnSpirimonzEnabled()
    {
        
    }
    
    public virtual void OnSpirimonzDisabled()
    {
        
    }

    public SpirimonzBehaviourState CurrentBehaviour()
    {
        return _currentBehaviour;
    }

    public void Lock()
    {
        _isLocked = true;
    }
    
    public bool IsLocked() => _isLocked;

    public virtual void OnClickInHands()
    {
        
    }
}