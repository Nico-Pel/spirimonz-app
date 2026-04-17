using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Spirimonz : GameBehaviour, IInteractable
{
    public SpirimonzSettings settings;
    
    public enum SpirimonzBehaviourState
    {
        Wait,
        FollowPlayer,
        Roam,
        Escape,
        Special,
    }
    
    [Header("Hiding Object Settings")] 
    public GameObject spirimonzGameObject;
    public GameObject hidingGameObject;
    private Vector3 _baseHidingOrbPos;
    [SerializeField] private bool useDifferentHidingOrbPosForHands;
    [SerializeField] private Vector3 _handHidingOrbPos;

    [Header("Spirimonz Settings")] 
    [ReadOnly] public bool isOnTheMap;
    public bool canInteract = true;
    public bool canBeDroppedOnMap = true;
    public bool canBeTakenBackIntoHands = true;
    public bool powerActiveInHands = true;
    public bool useSecondaryButton = false;
    public bool lookAtPlayerWhileWaiting = true;
    public bool lookAtPlayerOnInteract = false;
    public float lookAtPlayerOnInteractDuration = 0.25f;
    public float interactionCooldown = 0f;
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
    [SerializeField] protected float lookAtSpeed = 5f;
    
    protected SpirimonzBehaviourState _currentBehaviour;
    public Room currentRoom { get; set; }

    [Header("Spirimonz Settings : Roam")]
    public bool canChangeRoamRoom;
    public float minTimeToChangeRoamRoom;
    public float maxTimeToChangeRoamRoom;
    [Range(0f, 1f)] public float chancesToChangeRoamRoom;
    public float minTimeToChangeRoamWaypoint;
    public float maxTimeToChangeRoamWaypoint;
    public float minDistToReachRoamWaypoint;
    public float maxDistToReachRoamWaypoint;
    public float roamPauseOnInteractDuration = 3f;

    private Room _currentRoamRoom;
    private WayPoint _currentRoamWaypoint;
    private bool _canChangeRoamRoomAllowed;
    private bool _roamSettingsInitialized;
    private float _currentRoamReachDistance;
    private bool _isWaitingForNextRoamWaypoint;
    private float _nextRoamWaypointTime;
    private float _lastRoamWaypointChangeTime;
    private float _lastRoamMovementTime;
    private Vector3 _lastRoamPosition;
    private const string ROAM_ROOM_CHANGE_INVOKE = "RoamRoomChangeCooldown";
    private const string FEEL_HUNT_INVOKE = "FeelHuntInvoke";

    [Header("Spirimonz Settings : Escape")]
    public float targetedEscapeDistance = 1f;
    public float escapingSpeed = 5f;
    public int nbOfWayPointsToConsider = 3;

    [Header("Spirimonz Settings : Room Detection")]
    public bool enableRoomValidation = true;
    [Min(0.05f)] public float roomValidationInterval = 0.5f;
    [Min(0.01f)] public float roomDetectionRadius = 0.2f;
    [Min(0.001f)] public float roomProbeRadius = 0.05f;
    public LayerMask roomLayerMask;

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
    protected House _house;
    private bool _pauseRoamAfterInteract;
    private float _pauseRoamEndTime;
    private Coroutine _lookAtInteractCoroutine;
    private float _nextRoomValidationTime;

    public UnityEvent onInteract;
    public UnityEvent onSecondaryButtonUsed;
    public UnityEvent onDisable;
    public UnityEvent onDroppedOnMap;
    public UnityEvent onGoingBackToHands;
    public UnityEvent<Room> onSetRoom;

    protected virtual void Start()
    {
        _baseHidingOrbPos = hidingGameObject.transform.localPosition;
        _house = House.Instance;
        InitSpirimonz();
    }

    public virtual void InitSpirimonz()
    {
        if (_isLocked) return;

        _baseCanInteract = canInteract;
        _currentBehaviour = SpirimonzBehaviourState.Wait;
        InitializeRoamSettings();
        hidingGameObject.SetActive(false);
        
        _house.currentGhost.onGhostCallForAHunt.AddListener(StartDelayBeforeFeelingAHunt);
        _house.currentGhost.onGhostStartToHunt.AddListener(OnHuntStart);
        _house.currentGhost.onGhostStopToHunt.AddListener(OnHuntEnd);
        
        EnableSpirimonz(false);
        _initialized = true;
    }

    protected virtual void OnEnable()
    {
        if (_initialized)
        {
            SetSpiritHideMode(_hidingFromAGhost);

            // Si le Spirimonz était désactivé pendant le forecast, on applique la fuite maintenant
            if (_shouldFeelAHunt)
            {
                _shouldFeelAHunt = false;
                if (IsGhostHuntStillPending())
                {
                    FeelAHunt();
                }
                else
                {
                    _hidingFromAGhost = false;
                    SetSpiritHideMode(false);
                }
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
        animator.SetBool("Hands", !isOnTheMap);

        hidingGameObject.SetActive(hide);
        agent.enabled = !hide && isOnTheMap;

        if (isOnTheMap && hide == false)
        {
            collider.enabled = true;
        }

        if (hide)
        {
            collider.enabled = false;
            if (isOnTheMap)
            {
                hidingGameObject.transform.localPosition = _baseHidingOrbPos;
            }
            else
            {
                hidingGameObject.transform.localPosition = useDifferentHidingOrbPosForHands ? _handHidingOrbPos : _baseHidingOrbPos;
            }
        }
        else
        {
            RefreshAnimatorState();
        }
    }

    private void RefreshAnimatorState()
    {
        if (animator == null)
            return;

        animator.SetBool("Hands", !isOnTheMap);
        animator.SetBool("Wait", isOnTheMap && _currentBehaviour == SpirimonzBehaviourState.Wait);
    }

    private bool _shouldFeelAHunt;
    private void StartDelayBeforeFeelingAHunt()
    {
        float timeBeforeDisappearing = _house.currentGhost.forecastTimeBeforeAHunt - forecastTimeBeforeAHunt;
        if (timeBeforeDisappearing <= 0)
            timeBeforeDisappearing = 0.1f;

        if (gameObject.activeSelf)
        {
            this.Invoke(FEEL_HUNT_INVOKE, timeBeforeDisappearing, FeelAHunt);
        }
        else
        {
            _shouldFeelAHunt = true;
        }
    }

    private void FeelAHunt()
    {
        if (!IsGhostHuntStillPending())
            return;

        _hidingFromAGhost = true;
        agent.speed = 0;
        agent.velocity = Vector3.zero;
        
        SetSpiritHideMode(true);
    }

    private bool IsGhostHuntStillPending()
    {
        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.currentGhost == null)
            return false;

        return _house.currentGhost.IsHunting(true);
    }

    protected virtual void OnHuntStart()
    {
        
    }

    protected virtual void OnHuntEnd()
    {
        if (!Player.Instance.IsDead())
        {
            CancelInvoke(FEEL_HUNT_INVOKE);
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

        if (_house.currentGhost.IsHunting())
        {
            collider.enabled = false;
        }

        isOnTheMap = true;

        if (baseBehaviour == SpirimonzBehaviourState.Wait)
        {
            animator.SetBool("Wait", true);
        }
        
        onDroppedOnMap?.Invoke();
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

        onGoingBackToHands?.Invoke();
        
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

        OnColliderTriggeredEnter(other);
    }

    protected virtual void OnColliderTriggeredEnter(Collider other)
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (openDoorsOnItsWay == true)
        {
            if (other.gameObject.TryGetComponent(out Door door) && openDoorsOnItsWay)
            {
                if (door.IsGrabbed()) return;

                door.HandleSpirimonzContact(this, collider);
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

        OnColliderTriggeredExit(other);
    }
    
    protected virtual void OnColliderTriggeredExit(Collider other)
    {
        
    }

    public virtual void SetCurrentRoom(Room room)
    {
        currentRoom = room;
        onSetRoom?.Invoke(room);
    }

    private void TryToOpenDoor(Door door)
    {
        if (openDoorsOnItsWay == false) return;
        
        if (door.GetOpenRatio() > 0.7f)
            return;
        
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
        float currentRatio = door.GetOpenRatio();

        // ⛔ Refuse toute interaction qui réduirait l'ouverture
        if (currentRatio > 0.05f && door.GetOpenVelocity() < 0f)
            return;

        _stopMoving = true;
        agent.velocity = Vector3.zero;        
        this.Invoke(_waitDoorTime, () => _stopMoving = false);

        float targetPercentage = Random.Range(
            Mathf.Max(currentRatio, 0.8f),
            1f
        );

        float speed = 50f;
        door.GhostDoorInteraction(targetPercentage, speed, openedBySpirimonz: true);
    }

    private void Update()
    {
        if (_isLocked) return;

        if (TutorialInputGate.IsAllowed(TutorialInputGate.AllowSecondary) &&
            ((!MobileInput.Enabled && Input.GetMouseButtonDown(1)) || MobileInput.SecondaryDown) && !isOnTheMap)
        {
            OnClickInHands();
        }
        
        ValidateCurrentRoom();
        UpdateSpirimonzBehaviour();
        
        if (animator != null)
        {
            float moveSpeed = agent != null ? agent.velocity.magnitude : 0f;
            if (moveSpeed < 0.02f)
                moveSpeed = 0f;
            animator.SetFloat("MoveSpeed", moveSpeed);
        }
        
        UpdateMovementBehaviour();
    }

    private void ValidateCurrentRoom()
    {
        if (!enableRoomValidation || !isOnTheMap)
            return;

        if (Time.time < _nextRoomValidationTime)
            return;

        _nextRoomValidationTime = Time.time + Mathf.Max(0.05f, roomValidationInterval);

        Room detected = FindRoomAtPosition(transform.position);
        if (detected != null && detected != currentRoom)
        {
            SetCurrentRoom(detected);
        }
    }

    private Room FindRoomAtPosition(Vector3 position)
    {
        int mask = GetRoomOverlapMask();
        float radius = Mathf.Max(0.01f, roomDetectionRadius);
        Collider[] hits = Physics.OverlapSphere(position, radius, mask, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            Room bestRoom = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null) continue;

                Room candidate = hit.GetComponentInParent<Room>();
                if (candidate == null) continue;

                Vector3 closest = hit.ClosestPoint(position);
                float dist = (closest - position).sqrMagnitude;
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestRoom = candidate;
                }
            }

            if (bestRoom != null)
                return bestRoom;
        }

        if (TryFindRoomAtPoint(position, mask, out Room roomAtPoint))
            return roomAtPoint;

        return currentRoom;
    }

    private bool TryFindRoomAtPoint(Vector3 point, int mask, out Room room)
    {
        room = null;
        float radius = Mathf.Max(0.001f, roomProbeRadius);
        Collider[] hits = Physics.OverlapSphere(point, radius, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null) continue;

            Room candidate = hit.GetComponentInParent<Room>();
            if (candidate == null) continue;

            room = candidate;
            return true;
        }

        return false;
    }

    private int GetRoomOverlapMask()
    {
        if (roomLayerMask.value != 0)
            return roomLayerMask.value;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast >= 0)
            return 1 << ignoreRaycast;

        return Physics.AllLayers;
    }

    protected virtual void UpdateMovementBehaviour()
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
            case SpirimonzBehaviourState.Roam:
                Roaming();
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

    protected void LookAtPlayer()
    {
        Vector3 targetDir = _house.currentPlayer.transform.position - transform.position;

        // Lock X et Z pour ne jamais se pencher
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.001f) return; // éviter NaN si trop proche

        Quaternion targetRotation = Quaternion.LookRotation(targetDir);

        // Garder seulement la rotation Y
        Vector3 euler = transform.rotation.eulerAngles;
        euler.y = targetRotation.eulerAngles.y;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(euler),
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
        if (isOnTheMap == false) return;
        
        Vector3 playerPos = _house.currentPlayer.transform.position;
        float dist = Vector3.Distance(transform.position, playerPos);
        agent.speed = dist > followingDistance ? speed : 0;
        agent.SetDestination(playerPos);
        
        LookAtPlayer();
    }

    private void InitializeRoamSettings()
    {
        if (_roamSettingsInitialized) return;

        _roamSettingsInitialized = true;
        _canChangeRoamRoomAllowed = canChangeRoamRoom;
        if (!_canChangeRoamRoomAllowed)
        {
            canChangeRoamRoom = false;
            CancelInvoke(ROAM_ROOM_CHANGE_INVOKE);
        }
    }

    private void StartRoamRoomChangeCooldown()
    {
        if (!_canChangeRoamRoomAllowed)
        {
            canChangeRoamRoom = false;
            return;
        }

        canChangeRoamRoom = false;

        float min = Mathf.Max(0f, minTimeToChangeRoamRoom);
        float max = Mathf.Max(min, maxTimeToChangeRoamRoom);
        float delay = Random.Range(min, max);

        this.Invoke(ROAM_ROOM_CHANGE_INVOKE, delay, () => canChangeRoamRoom = true);
    }

    public void SetRoamRoom(Room room)
    {
        if (_house == null)
            _house = House.Instance;

        InitializeRoamSettings();

        Room roomToUse = room != null ? room : SelectRandomHouseRoom();
        _currentRoamRoom = roomToUse;

        if (_currentRoamRoom != null && _house != null)
        {
            _currentRoamWaypoint = _house.SelectRandomWayPointFromARoom(_currentRoamRoom);
            RefreshRoamReachDistance();
            MarkRoamWaypointChanged();
        }
        else
        {
            _currentRoamWaypoint = null;
            _currentRoamReachDistance = 0f;
        }

        if (canChangeRoamRoom)
        {
            StartRoamRoomChangeCooldown();
        }
        else if (!_canChangeRoamRoomAllowed)
        {
            canChangeRoamRoom = false;
        }
    }

    private void Roaming()
    {
        if (isOnTheMap == false) return;

        if (_house == null)
            _house = House.Instance;

        InitializeRoamSettings();

        if (_pauseRoamAfterInteract)
        {
            if (Time.time < _pauseRoamEndTime)
            {
                agent.speed = 0;
                return;
            }

            _pauseRoamAfterInteract = false;
        }

        if (_currentRoamRoom == null)
        {
            SetRoamRoom(currentRoom);
        }

        if (_currentRoamWaypoint == null)
        {
            _currentRoamWaypoint = _house != null && _currentRoamRoom != null
                ? _house.SelectRandomWayPointFromARoom(_currentRoamRoom)
                : null;
            if (_currentRoamWaypoint != null)
            {
                RefreshRoamReachDistance();
                MarkRoamWaypointChanged();
            }
        }

        if (_currentRoamWaypoint == null)
        {
            agent.speed = 0;
            return;
        }

        agent.speed = speed;
        agent.SetDestination(_currentRoamWaypoint.transform.position);

        UpdateRoamMovementTracking();

        if (ShouldForceRoamWaypointChange())
        {
            ForceChangeRoamWaypoint();
            return;
        }

        if (_isWaitingForNextRoamWaypoint)
        {
            if (Time.time >= _nextRoamWaypointTime)
            {
                _isWaitingForNextRoamWaypoint = false;
                SelectNextRoamWaypoint();
            }

            return;
        }

        if (HasReachedRoamWaypoint())
        {
            OnRoamWaypointReached();
            StartRoamWaypointChangeDelay();
        }
    }

    private bool HasReachedRoamWaypoint()
    {
        if (_currentRoamWaypoint == null)
            return true;

        if (agent.pathPending)
            return false;

        if (_currentRoamReachDistance <= 0f)
        {
            RefreshRoamReachDistance();
        }

        float reachDistance = Mathf.Max(_currentRoamReachDistance, 0.01f);
        float dist = Vector3.Distance(transform.position, _currentRoamWaypoint.transform.position);
        return dist <= reachDistance;
    }

    private void SelectNextRoamWaypoint()
    {
        if (_currentRoamRoom == null)
        {
            SetRoamRoom(null);
            return;
        }

        bool canRollForChange = _canChangeRoamRoomAllowed && canChangeRoamRoom;
        float roll = Random.value;

        if (canRollForChange && roll <= Mathf.Clamp01(chancesToChangeRoamRoom))
        {
            Room nextRoom = SelectNeighborRoom(_currentRoamRoom);
            if (nextRoom != null)
            {
                SetRoamRoom(nextRoom);
                return;
            }
        }

        if (_house != null)
        {
            _currentRoamWaypoint = _house.SelectRandomWayPointFromARoom(_currentRoamRoom);
            RefreshRoamReachDistance();
            MarkRoamWaypointChanged();
        }
    }

    private void StartRoamWaypointChangeDelay()
    {
        float minDelay = Mathf.Max(0f, GetRoamWaypointChangeDelayMin());
        float maxDelay = Mathf.Max(minDelay, GetRoamWaypointChangeDelayMax());

        if (maxDelay <= 0f)
        {
            SelectNextRoamWaypoint();
            return;
        }

        _isWaitingForNextRoamWaypoint = true;
        _nextRoamWaypointTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    private void ForceChangeRoamWaypoint()
    {
        _isWaitingForNextRoamWaypoint = false;
        SelectNextRoamWaypoint();
    }

    private void MarkRoamWaypointChanged()
    {
        _lastRoamWaypointChangeTime = Time.time;
        _lastRoamMovementTime = Time.time;
        _lastRoamPosition = transform.position;
    }

    private void UpdateRoamMovementTracking()
    {
        float moveThreshold = Mathf.Max(0.001f, GetRoamStuckMoveDistance());
        float sqrThreshold = moveThreshold * moveThreshold;
        Vector3 currentPos = transform.position;
        if ((currentPos - _lastRoamPosition).sqrMagnitude >= sqrThreshold)
        {
            _lastRoamPosition = currentPos;
            _lastRoamMovementTime = Time.time;
        }
    }

    private bool ShouldForceRoamWaypointChange()
    {
        float forceAfter = GetRoamForceChangeAfterTime();
        if (forceAfter > 0f && Time.time - _lastRoamWaypointChangeTime >= forceAfter)
            return true;

        if (!_isWaitingForNextRoamWaypoint)
        {
            float stuckTime = GetRoamStuckTime();
            if (stuckTime > 0f && Time.time - _lastRoamMovementTime >= stuckTime)
                return true;

            if (agent != null && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return true;
        }

        return false;
    }

    private void RefreshRoamReachDistance()
    {
        _currentRoamReachDistance = GetRoamReachDistance();
    }

    protected virtual float GetRoamWaypointChangeDelayMin()
    {
        return minTimeToChangeRoamWaypoint;
    }

    protected virtual float GetRoamWaypointChangeDelayMax()
    {
        return maxTimeToChangeRoamWaypoint;
    }

    protected virtual float GetRoamForceChangeAfterTime()
    {
        return 0f;
    }

    protected virtual float GetRoamStuckTime()
    {
        return 0f;
    }

    protected virtual float GetRoamStuckMoveDistance()
    {
        return 0.05f;
    }

    protected virtual float GetRoamReachDistance()
    {
        float stoppingDistance = agent != null ? agent.stoppingDistance : 0f;
        if (minDistToReachRoamWaypoint <= 0f && maxDistToReachRoamWaypoint <= 0f)
            return Mathf.Max(stoppingDistance, 0.2f);

        float min = Mathf.Max(minDistToReachRoamWaypoint, stoppingDistance, 0.01f);
        float max = Mathf.Max(maxDistToReachRoamWaypoint, min);
        return Random.Range(min, max);
    }

    protected virtual void OnRoamWaypointReached()
    {
    }

    private Room SelectNeighborRoom(Room sourceRoom)
    {
        if (sourceRoom == null || sourceRoom.neighborRooms == null || sourceRoom.neighborRooms.Length == 0)
            return null;

        List<Room> selectableRooms = new List<Room>();
        foreach (Room room in sourceRoom.neighborRooms)
        {
            if (room != null)
                selectableRooms.Add(room);
        }

        if (selectableRooms.Count == 0)
            return null;

        return selectableRooms[Random.Range(0, selectableRooms.Count)];
    }

    private Room SelectRandomHouseRoom()
    {
        if (_house == null || _house.rooms == null || _house.rooms.Length == 0)
            return null;

        List<Room> selectableRooms = new List<Room>();
        foreach (Room room in _house.rooms)
        {
            if (room != null)
                selectableRooms.Add(room);
        }

        if (selectableRooms.Count == 0)
            return null;

        return selectableRooms[Random.Range(0, selectableRooms.Count)];
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
                _house.SelectRandomWaypointFurthestFromPosition(agent, nbOfWayPointsToConsider).transform;
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

    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
        if (!isOnTheMap) return;
        
        if (canInteract == false)
        {
            animator.SetTrigger("Nop");
            return;
        }

        if (interactionCooldown > 0)
        {
            canInteract = false;
            this.Invoke(interactionCooldown, () => canInteract = true);
        }
        
        animator.SetTrigger("Click");
        
        InteractionStarted();
    }

    public virtual void InteractionStarted()
    {
        onInteract?.Invoke();
        HandleLookAtPlayerOnInteract();
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
        animator.SetBool("Wait", newBehaviour == SpirimonzBehaviourState.Wait);
    }

    protected void HandleLookAtPlayerOnInteract()
    {
        if (!lookAtPlayerOnInteract || _house == null || _house.currentPlayer == null)
            return;

        if (_lookAtInteractCoroutine != null)
            StopCoroutine(_lookAtInteractCoroutine);

        _lookAtInteractCoroutine = StartCoroutine(LookAtPlayerOnInteractRoutine());

        if (_currentBehaviour == SpirimonzBehaviourState.Roam)
        {
            _pauseRoamAfterInteract = true;
            _pauseRoamEndTime = Time.time + Mathf.Max(0f, roamPauseOnInteractDuration);
        }
    }

    private IEnumerator LookAtPlayerOnInteractRoutine()
    {
        float duration = Mathf.Max(0.01f, lookAtPlayerOnInteractDuration);
        Vector3 dir = _house.currentPlayer.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            _lookAtInteractCoroutine = null;
            yield break;
        }

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 targetEuler = startRotation.eulerAngles;
        targetEuler.y = targetRotation.eulerAngles.y;
        Quaternion endRotation = Quaternion.Euler(targetEuler);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, lerp);
            yield return null;
        }

        transform.rotation = endRotation;
        _lookAtInteractCoroutine = null;
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }

    public bool InteractionLocked { get; set; }

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

    public void Lock()
    {
        _isLocked = true;
    }
    
    public bool IsLocked() => _isLocked;

    public virtual void OnClickInHands()
    {
        FireTrigger[] fireTriggers = GetComponentsInChildren<FireTrigger>(true);
        for (int i = 0; i < fireTriggers.Length; i++)
        {
            FireTrigger fireTrigger = fireTriggers[i];
            if (fireTrigger != null)
                fireTrigger.ArmHeldTransmission();
        }

        onSecondaryButtonUsed?.Invoke();
    }
    
    public bool IsInHidingMode() => _hidingFromAGhost;

    protected virtual void OnDisable()
    {
        onDisable?.Invoke();
    }
}
