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

    [Header("Spirimonz Settings")] 
    [ReadOnly] public bool isOnTheMap;
    public bool canInteract = true;
    public bool canBeDroppedOnMap = true;
    [FormerlySerializedAs("canBetakenBackIntoHands")] public bool canBeTakenBackIntoHands = true;
    public bool powerActiveInHands = true;
    public bool lookAtPlayerWhileWaiting = true;
    public bool openDoorsOnItsWay = false;
    public bool lookForwardOnDropOnMap;
    public float lookAtDistanceFromPlayer = 10f;

    private bool _baseCanInteract;

    public InventoryManager.HandPoses handPosType = InventoryManager.HandPoses.PalmOfTheHand;
    public SpirimonzBehaviourState baseBehaviour = SpirimonzBehaviourState.Wait;
    public SpirimonzBehaviourState secondaryBehaviour = SpirimonzBehaviourState.FollowPlayer;
    public float speed = 2;
    public float followingDistance = 2f;
    
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

    private void Start()
    {
        InitSpirimonz();
    }

    public virtual void InitSpirimonz()
    {
        _baseCanInteract = canInteract;
        _currentBehaviour = SpirimonzBehaviourState.Wait;
    }

    public virtual void DroppedOnMap()
    {
        _currentBehaviour = baseBehaviour;
        
        animator.SetBool("IsOnMap", true);

        if (baseBehaviour == SpirimonzBehaviourState.Escape)
        {
            _escaping = true;
        }
    }

    public virtual void GoBackToHands(Transform handPos)
    {
        EnableSpirimonz(false);
        transform.parent = handPos;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
        
        animator.SetBool("IsOnMap", false);
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
        UpdateSpirimonzBehaviour();
        
        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", agent.speed);
        }
        
        UpdateMovementBehaviour();
    }

    private void UpdateMovementBehaviour()
    {
        if (isOnTheMap == false || _stopMoving)
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
        float dist = Vector3.Distance(transform.position, Player.Instance.transform.position);
        if (dist < lookAtDistanceFromPlayer)
        {
            transform.LookAt(House.Instance.currentPlayer.transform.position);
        }
        else
        {
            Vector3 forwardTarget = transform.forward * 5;
            transform.LookAt(new Vector3(forwardTarget.x, transform.position.y, forwardTarget.z));
        }
    }

    public virtual void UpdateSpecialMovement()
    {
        
    }

    public virtual void UpdateSpirimonzBehaviour()
    {
        if (isOnTheMap == false && powerActiveInHands == false) return;
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
        if (canInteract == false) return;
        
        InteractionStarted();
    }

    public virtual void InteractionStarted()
    {
        SwitchBehaviour();
    }
    
    public void SwitchBehaviour()
    {
        SpirimonzBehaviourState stateToUse = _currentBehaviour == baseBehaviour ? secondaryBehaviour : baseBehaviour;
        ChangeBehaviour(stateToUse);
    }

    public void ChangeBehaviour(SpirimonzBehaviourState newBehaviour)
    {
        _currentBehaviour = newBehaviour;
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
        agent.enabled = enable;
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
}