using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Spirimonz : GameBehaviour, IInteractable
{
    public enum SpirimonzBehaviourState
    {
        Wait,
        FollowPlayer,
        Roam,
        Escape,
    }

    [Header("Spirimonz Settings")] 
    public bool canInteract = true;
    
    public SpirimonzBehaviourState baseBehaviour = SpirimonzBehaviourState.Wait;
    public SpirimonzBehaviourState secondaryBehaviour = SpirimonzBehaviourState.FollowPlayer;
    public float speed = 2;
    public float followingDistance = 2f;
    
    private SpirimonzBehaviourState _currentBehaviour;
    private Room _currentRoom;

    [Header("Spirimonz Settings : Escape")]
    public float targetedEscapeDistance = 1f;
    public float escapingSpeed = 5f;
    public int nbOfWayPointsToConsider = 3;

    private bool _escaping;
    
    [Header("Spirimonz Components")]
    public NavMeshAgent agent;
    public Animator animator;
    private IInteractable _interactableImplementation;
    
    private Transform _targetedTransform;

    private void Start()
    {
        InitSpirimonz();
    }

    public void InitSpirimonz()
    {
        _currentBehaviour = baseBehaviour;

        if (baseBehaviour == SpirimonzBehaviourState.Escape)
        {
            _escaping = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room room))
        {
            _currentRoom = room;
        }
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("Movespeed", agent.speed);
        }
        
        switch (_currentBehaviour)
        {
            case SpirimonzBehaviourState.Wait:
                agent.speed = 0;
                break;
            case SpirimonzBehaviourState.FollowPlayer:
                FollowingPlayer();
                break;
            case SpirimonzBehaviourState.Escape:
                Escaping();
                break;
            default:
                break;
        }
    }

    private void FollowingPlayer()
    {
        Vector3 playerPos = House.Instance.currentPlayer.transform.position;
        float dist = Vector3.Distance(transform.position, playerPos);
        agent.speed = dist > followingDistance ? speed : 0;
        agent.SetDestination(playerPos);
        
        transform.LookAt(playerPos);
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
                House.Instance.SelectRandomWaypointFurthestFromPosition(transform.position, nbOfWayPointsToConsider).transform;
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

    protected virtual void EscapePointReached()
    {
        
    }

    public void OnInteractStart()
    {
        if (canInteract == false) return;
        
        SwitchBehaviour();
    }
    
    private void SwitchBehaviour()
    {
        SpirimonzBehaviourState stateToUse = _currentBehaviour == baseBehaviour ? secondaryBehaviour : baseBehaviour;
        ChangeBehaviour(stateToUse);
    }

    private void ChangeBehaviour(SpirimonzBehaviourState newBehaviour)
    {
        _currentBehaviour = newBehaviour;
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }
}
