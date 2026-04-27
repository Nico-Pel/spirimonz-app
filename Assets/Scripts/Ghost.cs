using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Ghost : GameBehaviour
{
    public enum GhostShape
    {
        normal,
        big,
        small
    }
    
   [ReadOnly] public GhostParameters ghostParameters;
   
   public GhostShape ghostShape;

#if UNITY_EDITOR
    [Space]
    [ReadOnly] public GhostActivities forcedGhostActivity = GhostActivities.Nothing;
    [ReadOnly] public bool tripleActivityDebug;
#endif

    [Header("Tutorial Override")]
    public bool tutorialOverrideEnabled;
    public bool tutorialBlockAllActivities;
    public bool tutorialForceRoom;
    public Room tutorialForcedRoom;
    public bool tutorialRestrictActivities;
    public List<GhostActivities> tutorialAllowedActivities = new List<GhostActivities>();
    public bool tutorialForceActivity;
    public GhostActivities tutorialForcedActivity = GhostActivities.Nothing;
    public bool tutorialAllowHunt = true;
    public bool tutorialAllowRoomChange = true;

    public enum GhostState
    {
        hideState,
        eventState,
        standingState,
        huntingState
    }
    
    public enum GhostActivities
    {
        ObjectInteraction,
        ChangeLightState,
        ChangeElectronicObjectState,
        TriggerEvent,
        Hunt,
        PlayWithDoor,
        BlowOutAFlammable,
        Nothing
    }
    
    public enum PrintTypes
    {
        Paw,
        Finger
    }
    
    [Space]
        
    [Header("Ghost Stats : Angriness")] 
    public float angerPercentage = 0f;

    public GhostState currentState;

    public House house;
    public Room favoriteRoom;
    public Room currentRoom;
    
    public Animator animator;
    public ParticleSystem fxApparition;

    public bool isBlinkingGhost;
    public bool levitates;

    [Header("Sounds")]
    public float ghostPitch = 1f;
    public AudioClip apparitionSound;
    public AudioClip huntingSound;
    public AudioClip killSound;
    
    [FormerlySerializedAs("sprintDurationMin")] [Header("Ghost Prints")]
    public float printDurationMin = 6f;
    [FormerlySerializedAs("sprintDurationMax")] public float printDurationMax = 15f;
    public Sprite[] pawSprites;
    public Sprite fingerSprite;

    [Header("Spirit Orbs")] 
    public GameObject ghostOrbsPrefab;

    [Header("Capture Scene")]
    [Min(0f)] public float captureLoseFadeDelay = 0.275f;

    private float _waitDoorTime = 0.5f;
    private bool _stopMoving = false;
    
    [Header("Ghost Stats : Hunting")]
    public float forecastTimeBeforeAHunt = 5f;
    public float startHuntingStandingTime = 4;
    public float delayBeforeLosingPlayerTargeting = 4f;
    public float huntTimeVariation = 5f;

    [ReadOnly] public float playerSeenDuration;
    
    [ReadOnly] public float forcedStartTargetingTime = 1f;
    [ReadOnly] public float angerThresholdStep = 10;
    [ReadOnly] public float forcedTargetingTimeIncreasePerStep = 2.5f;
    private float _baseForcedStartTargetingTime;

    private bool _forcedStartTargeting = false;
    private bool _targetingPlayer;
    private bool _losingPlayer;
    private bool _isLocked;

    private float _averageHuntTime;
    private float _baseAverageActivityTime;

    [Header("Ghost Stats : Waypoints Hunting")]

    [ReadOnly] public float currentHuntTime;
    [ReadOnly] public WayPoint currentWayPoint;
    public List<WayPoint> huntingWayPoints = new List<WayPoint>();
    [ReadOnly] public float currentHuntingWayPointDistanceTargeted;
    public float chancesPercentageToIgnoreAWayPoint = 40f;
    public float detectPlayerActivityRange = 20f;

    [Header("Ghost Stats : Throwing")] 
    public float throwDetectionRange = 5;
    public float throwTorqueMax = 90;
    public LayerMask throwableMask;
    
    public LayerMask blockingThrowMask; // Wall, Ground, Ceiling
    public float visibilityHeightOffset = 1.2f; // hauteur du raycast
    public LayerMask interactionOcclusionMask; // Wall, Ceiling (optional)
    public int maxInteractionSelectionTries = 5;

    [Header("Ghost Stats : Doors Playing")]
    public float doorDetectionRange = 8;
    public float closeForce = 10;
    public float slamForce = 100;
    public float openForceMin = 15;
    public float openForceMax = 25;
    public float openAngleMin = 0.5f;
    public float openAngleMax = 1;
    public LayerMask doorMask;

    [Header("Ghost Stats : Activities")] 
    public float averageActivityTime = 60f;
    public float activityTimeVariation = 10f;
    public float chancesToRoamInAnotherRoom = 25f;

    [Header("Ghost Stats : Anti Exploit")]
    public bool antiExploitEnabled = true;
    public float stuckTimeBeforePhase = 1f;
    public float stuckMoveDistance = 0.05f;
    public float stuckVelocityThreshold = 0.05f;
    public float phaseDuration = 1f;
    public float phaseMoveSpeedMultiplier = 1f;
    public float phaseReattachRadius = 2f;
    public float pathCheckInterval = 0.2f;

    private float openingDoorSpeed = 35f;

    [Header("Ghost Stats : Blinking")] 
    public float averageVisibleTime = 1;
    public float visibleTimeVariation = 0.5f;
    
    public float averageInvisibleTime = 1;
    public float invisibleTimeVariation = 0.5f;

    [Header("Ghost Components")] 
    public Renderer[] renderers;
    public NavMeshAgent agent;
    public GameObject ghostModel;
    public GhostVision vision;

    public UnityEvent onGhostStartToHunt;
    public UnityEvent onGhostCallForAHunt;
    public UnityEvent onGhostStopToHunt;
    
    private bool _canHunt = true;

    private SoundManager.SoundInstance _huntingSound;
    private GamePlayer _player;
    
    int agentContacts = 0;
    
    private const string ACTIVITY_INVOKE = "Ghost.TriggerActivity";
    private const string ORBS_INVOKE = "Ghost.SpiritOrbs";
    private const string PASSIVE_ANGER_INVOKE = "Ghost.PassiveAnger";
    private const int OVERLAP_BUFFER_SIZE = 128;
    private static GhostActivities[] _activityValues;

    private readonly Collider[] _overlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];
    private readonly List<CatchableObject> _catchableBuffer = new List<CatchableObject>();
    private readonly List<FlammableElement> _flammableBuffer = new List<FlammableElement>();
    private readonly List<Door> _doorBuffer = new List<Door>();

    private bool _isPhasing;
    private int _phaseAttempts;
    private float _phaseEndTime;
    private float _lastHuntMovementTime;
    private Vector3 _lastHuntPosition;
    private float _lastPathCheckTime;
    private bool _cachedPathValid = true;
    private NavMeshPathStatus _cachedPathStatus = NavMeshPathStatus.PathComplete;
    private NavMeshPath _playerPath;
    private bool _agentUpdatePositionBase = true;
    private bool _agentUpdateRotationBase = true;
    private readonly Dictionary<Room, float> _nonFavoriteOvercoolTimers = new Dictionary<Room, float>();
    private float _favoriteRoomColdestTemperatureReached = float.MaxValue;
    private bool _forceReturnToFavoriteRoom;
    private float _externalHuntSlowPercent;
    private float _externalHuntSlowEndTime;

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        _baseForcedStartTargetingTime = forcedStartTargetingTime;
        _baseAverageActivityTime = averageActivityTime;
        _playerPath = new NavMeshPath();
    }

    public void Initialize(House h)
    {
        house = h;

        if (house.map.ghosts.Length > 0)
        {
            ghostParameters = house.selectedGhostParameter;
        }
        
        favoriteRoom = house.hauntableRooms[Random.Range(0, house.hauntableRooms.Length)];
        
        _averageHuntTime = ghostParameters.averageHuntTime;
        
        # if UNITY_EDITOR
        if (h.useDebugs && h.forcedFavoriteRoomID >= 0 && house.hauntableRooms.Length > h.forcedFavoriteRoomID && house.hauntableRooms[h.forcedFavoriteRoomID] != null)
        {
            favoriteRoom = house.hauntableRooms[h.forcedFavoriteRoomID];
        }

        if (h.useDebugs && h.forcedGhostActivity != GhostActivities.Nothing)
        {
            forcedGhostActivity = h.forcedGhostActivity;
        }

        if (h.useDebugs && h.useHuntTimeMultiplierDebug)
        {
            _averageHuntTime *= h.huntTimeMultiplierDebug;
        }
        # endif

        agent.enabled = false;
        transform.position = house.SelectRandomWayPointFromARoom(favoriteRoom).transform.position;
        currentRoom = favoriteRoom;
        agent.enabled = true;
        if (favoriteRoom != null)
            _favoriteRoomColdestTemperatureReached = favoriteRoom.GetTemperatureCelsius();
        

        if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Trickster)
        {
            averageActivityTime *= 0.75f;
        }

        angerPercentage = Mathf.Max(0f, ghostParameters.startingAnger);
        SchedulePassiveAnger();

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        
        # if UNITY_EDITOR
        if (h.useDebugs && h.tripleActivityDebug)
        {
            tripleActivityDebug = true;
            nextActivityTime = nextActivityTime / 3;
        }
        #endif
        
        //Fist activity if called later
        this.Invoke(ACTIVITY_INVOKE, nextActivityTime * 2, TriggerActivity);
        
        ghostModel.SetActive(false);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritOrbs))
        {
            float delayBeforeNextGhostOrbs = Random.Range(ghostParameters.nextOrbsDelayMin, ghostParameters.nextOrbsDelayMax);
            this.Invoke(ORBS_INVOKE, delayBeforeNextGhostOrbs, CreateSpiritOrbs);
        }
    }

    private void CreateSpiritOrbs()
    {
        if (currentRoom == favoriteRoom)
        {
            GameObject newGhostOrbs = Instantiate(ghostOrbsPrefab, transform.position, ghostOrbsPrefab.transform.rotation, house.transform);
            EnsureGhostOrbsMarker(newGhostOrbs);
            this.Invoke(5, () =>
            {
                Destroy(newGhostOrbs);
            });
        }
        
        float delayBeforeNextGhostOrbs = Random.Range(ghostParameters.nextOrbsDelayMin, ghostParameters.nextOrbsDelayMax);
        this.Invoke(delayBeforeNextGhostOrbs, () =>
        {
            CreateSpiritOrbs();
        });
    }

    private static void EnsureGhostOrbsMarker(GameObject orbsRoot)
    {
        if (orbsRoot == null)
            return;

        GhostOrbsParticles existing = orbsRoot.GetComponentInChildren<GhostOrbsParticles>(true);
        if (existing != null)
            return;

        ParticleSystem ps = orbsRoot.GetComponentInChildren<ParticleSystem>(true);
        GhostOrbsParticles marker;
        if (ps != null)
        {
            marker = ps.gameObject.AddComponent<GhostOrbsParticles>();
            marker.aimPoint = ps.transform;
        }
        else
        {
            marker = orbsRoot.AddComponent<GhostOrbsParticles>();
            marker.aimPoint = orbsRoot.transform;
        }
    }

    private void SchedulePassiveAnger()
    {
        if (ghostParameters == null)
            return;

        if (ghostParameters.passiveAngerIncreaseAmount <= 0f)
            return;

        float minDelay = Mathf.Max(0f, ghostParameters.passiveAngerIncreaseMinDelay);
        float maxDelay = Mathf.Max(minDelay, ghostParameters.passiveAngerIncreaseMaxDelay);

        if (maxDelay <= 0f)
            return;

        float delay = Random.Range(minDelay, maxDelay);
        this.Invoke(PASSIVE_ANGER_INVOKE, delay, ApplyPassiveAnger);
    }

    private void ApplyPassiveAnger()
    {
        if (_isLocked)
            return;

        ImproveAnger(ghostParameters.passiveAngerIncreaseAmount);
        SchedulePassiveAnger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isLocked) return;

        if (other.GetComponent<NavMeshAgent>())
        {
            agentContacts++;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        
        if (other.TryGetComponent(out Room newRoom))
        {
            if (tutorialOverrideEnabled)
            {
                if (tutorialForceRoom && tutorialForcedRoom != null)
                    currentRoom = tutorialForcedRoom;
                else if (!tutorialAllowRoomChange && favoriteRoom != null)
                    currentRoom = favoriteRoom;
                else
                    currentRoom = newRoom;
            }
            else
            {
                currentRoom = newRoom;
            }

            if (_forceReturnToFavoriteRoom && favoriteRoom != null && currentRoom == favoriteRoom)
                _forceReturnToFavoriteRoom = false;
        }
        else if (other.TryGetComponent(out Player touchedPlayer))
        {
            if (currentState == GhostState.huntingState)
            {
                UIGame.Instance.BlinkOverlay(0.2f);
                this.Invoke(0.2f, StopHunting);
                Kill();
            }
            if (currentState == GhostState.hideState)
            {
                ImproveAnger(ghostParameters.angerToAddByTriggeringPlayer);
            }
        }
        else if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.Radioactivity) && other.TryGetComponent(out RadiationDetector radiationDetector))
        {
            //Debug.Log("TRIGGER RADIATIONS DETECTOR");
            if (ghostParameters.ShouldDetectRadiationOnTrigger())
            {
                //Debug.Log("TRIGGER RADIATIONS");
                radiationDetector.TriggerDetection(ghostParameters.radiationDurationOnTrigger);
            }
        }
        else if (other.TryGetComponent(out Door door))
        {
            // Ignore la porte si elle a un twinDoor déjà assez ouverte
            if (door.twinDoor != null && door.twinDoor.GetOpenRatio() >= 0.7f)
                return;

            if (currentState != GhostState.huntingState)
                return;

            Vector3 directionToDoor = (door.transform.position - transform.position).normalized;
            Vector3 moveDirection = agent.velocity.normalized;

            float dot = Vector3.Dot(moveDirection, directionToDoor);

            if (dot > 0.15f)
            {
                _stopMoving = true;
                agent.velocity = Vector3.zero;

                float waitTime = ComputeDoorWaitTime(door, _waitDoorTime);

                if (waitTime > 0f)
                {
                    StartCoroutine(WaitAndOpenDoor(door));
                }
                else
                {
                    door.GhostDoorInteraction(1f, openingDoorSpeed * ghostParameters.openingDoorSpeedMultiplier);
                    
                    _stopMoving = false;
                    agent.isStopped = false;
                }

                ActivateActivitySource(door.activitySource);
            }
        }
        else if (!levitates && ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritPrints) && currentState == GhostState.hideState && other.TryGetComponent(out PrintTrigger printTrigger))
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= ghostParameters.chancesToPutPrintOnPrintTriggers)
            {
                PrintSource printSourceToUse = printTrigger.GetRandomPrintSource();
                if (printSourceToUse != null)
                {
                    ActivatePrint(printSourceToUse, PrintTypes.Paw);
                }
            }
        }
    }
    
    float ComputeDoorWaitTime(Door door, float maxWaitTime)
    {
        float openRatio = door.GetOpenRatio();

        // Si twinDoor ouvert → on diminue l'attente
        if (door.twinDoor != null)
            openRatio = Mathf.Max(openRatio, door.twinDoor.GetOpenRatio());

        if (openRatio >= 0.7f)
            return 0f;

        if (!door.isOpen)
            return maxWaitTime;

        float normalized = openRatio / 0.7f;
        return maxWaitTime * (1f - normalized);
    }
    
    private Coroutine _doorRoutine;

    private IEnumerator WaitAndOpenDoor(Door door)
    {
        if (door == null)
            yield break;

        if (door.IsGrabbed())
            door.Release();

        door.InteractionLocked = true;

        _stopMoving = true;
        agent.isStopped = true;

        const float OPEN_THRESHOLD = 0.7f;
        float safetyTimer = 2.5f; // ⏱️ sécurité
        float timer = 0f;

        while (door != null && door.GetOpenRatio() < OPEN_THRESHOLD)
        {
            door.GhostDoorInteraction(
                1f,
                openingDoorSpeed * ghostParameters.openingDoorSpeedMultiplier,
                false
            );

            timer += Time.deltaTime;
            if (timer >= safetyTimer)
                break;

            yield return null;
        }

        // 🔓 LIBÉRATION GARANTIE
        _stopMoving = false;
        agent.isStopped = false;

        if (door != null)
            door.InteractionLocked = false;
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            agentContacts--;
            if (agentContacts <= 0)
            {
                agentContacts = 0;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }
    }

    public void TryToTriggerAHunt(float chances, float delayMin = 0.1f, float delayMax = 0.1f)
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= chances)
        {
            float delay = Random.Range(delayMin, delayMax);
            this.Invoke(delay, () =>
            {
                TriggerHunting(true);
            });
        }
    }

    private bool _willHunt;
    private void TriggerHunting(bool forceHunting = false)
    {
        if (_canHunt == false && forceHunting == false) return;
        if (_willHunt) return; // Prevent double scheduling

        _willHunt = true;
        _canHunt = false;

        Invoke(nameof(StandingBeforeHunting), forecastTimeBeforeAHunt);
        onGhostCallForAHunt?.Invoke();
    }

    private void StandingBeforeHunting()
    {
        if (_isLocked || _willHunt == false) return; //If willHunt is false, hunt has been canceled
        
        fxApparition.Play();
        SoundManager.Instance.PlaySound(apparitionSound, transform.position, 1f, ghostPitch, -1f, 25f);
        
        onGhostStartToHunt?.Invoke();
        currentState = GhostState.standingState;
        agent.velocity = Vector3.zero;
        agent.isStopped = true;
        agent.ResetPath();
        
        //float startingHuntDelay = DivideByPercentage(startHuntingStandingTime, angerPercentage);
        this.Invoke(startHuntingStandingTime, StartHunting);
        
        ghostModel.SetActive(true);
        SetVisibleRenderer(true);
        animator.SetTrigger("Apparition");
    }

    private void StartHunting()
    {
        ResetWaypoints();
        InitWayPoints();
        _huntingSound = SoundManager.Instance.PlaySound(huntingSound, transform.position, 1f, ghostPitch, -1f, 20f, true, this.transform);
        currentState = GhostState.huntingState;
        ResetAntiExploitState();
        
        //Start blinking
        SetVisibleRenderer(true);

        //Le fantôme va forcément aller vers le joueur durant cette période.
        _forcedStartTargeting = true;
        this.Invoke(forcedStartTargetingTime, () => _forcedStartTargeting = false);
        _targetingPlayer = true;

        currentHuntTime = Random.Range(_averageHuntTime - huntTimeVariation, _averageHuntTime + huntTimeVariation);
        //Debug.Log("Starting a HUNT for: " + currentHuntTime + " seconds");
    }

    private void Update()
    {
        UpdateFavoriteRoomTemperatureRules();

        if (currentState == GhostState.standingState)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.speed = 0;
            LookAtPlayer();
        }
        else if (currentState == GhostState.huntingState)
        {
            if (agent.isStopped && !_stopMoving)
                agent.isStopped = false;

            bool canSeePlayer = vision.CanSeePlayer(house.currentPlayer);
            if (canSeePlayer)
                playerSeenDuration += Time.deltaTime;
            UpdateHuntMovementTracking();

            currentHuntTime -= Time.deltaTime;
            if (currentHuntTime <= 0)
            {
                StopHunting();
            }

            if (!_forcedStartTargeting && !canSeePlayer)
            {
                if (_targetingPlayer && _losingPlayer == false)
                {
                    _losingPlayer = true;
                    this.Invoke("LosingTarget", delayBeforeLosingPlayerTargeting, () =>
                    {
                        if (vision.CanSeePlayer(house.currentPlayer) == false)
                        {
                            LosePlayer();
                        }
                    });
                }
            }
            else if(_forcedStartTargeting || canSeePlayer)
            {
                PlayerFound();
            }
            
            SetHuntingDestination();

            if (HandleHuntAntiExploit(canSeePlayer))
                return;
            
            if (_stopMoving)
            {
                agent.speed = 0;
                animator.SetBool("Walk", false);
            }
            else
            {
                float speed = currentState == GhostState.huntingState && canSeePlayer ? ghostParameters.targetingSpeedBase : ghostParameters.normalSpeedBase;
                if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Aquatic && currentRoom == favoriteRoom)
                {
                    speed *= 1.5f;
                }
                speed *= GetExternalHuntSpeedMultiplier();
                agent.speed = speed;
                animator.SetBool("Walk", true);
                animator.SetBool("Targeting", canSeePlayer);
            }
            
            animator.SetFloat("MoveSpeed", agent.speed);
        }
        else
        {
            if (agent.isStopped && !_stopMoving)
                agent.isStopped = false;

            SetHidingDestination();
        }
        
        # if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (currentState == GhostState.hideState)
            {
                TriggerHunting(true);
            }
            else if (currentState == GhostState.huntingState)
            {
                StopHunting();
            }
        }
        #endif
    }

    private void LosePlayer()
    {
        ResetWaypoints();
        _targetingPlayer = false;
        
        //Consider that Ghost don't know where is the player but know where he moved before losing him
        ForceNewWaypoint(_player.currentRoom);
    }

    public void PlayerFound()
    {
        if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Draconic && !_targetingPlayer &&
            !_forcedStartTargeting)
        {
            currentHuntTime += 5f;
        }
                    
        _targetingPlayer = true;
        _losingPlayer = false;
        this.CancelInvoke("LosingTarget");
    }
    
    private void LookAtPlayer()
    {
        Vector3 targetDir;
        float dist = Vector3.Distance(transform.position, Player.Instance.transform.position);

        if (dist < 10f)
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
            5f * Time.deltaTime
        );
    }

    private void SetHuntingDestination()
    {
        agent.destination = _targetingPlayer ? house.currentPlayer.transform.position : currentWayPoint.transform.position;

        if (_targetingPlayer == false)
        {
            float dist = Vector3.Distance(transform.position, currentWayPoint.transform.position);

            if (dist < currentHuntingWayPointDistanceTargeted)
            {
                huntingWayPoints.Remove(currentWayPoint);
                SelectNewHuntingWaypoint();
            }
        }
    }

    private bool HandleHuntAntiExploit(bool canSeePlayer)
    {
        if (!antiExploitEnabled || _stopMoving)
            return false;

        if (_isPhasing)
        {
            UpdatePhaseMovement();
            return true;
        }

        if (!canSeePlayer || !_targetingPlayer)
        {
            ResetAntiExploitState(keepPathCache: true);
            return false;
        }

        if (IsPathToPlayerValid())
        {
            ResetAntiExploitState(keepPathCache: true);
            return false;
        }

        if (!IsAgentStuck())
            return false;

        if (_phaseAttempts > 0)
        {
            StopHunting();
            return true;
        }

        StartPhase();
        return true;
    }

    private void UpdateHuntMovementTracking()
    {
        Vector3 currentPos = transform.position;
        float moveThreshold = Mathf.Max(0.001f, stuckMoveDistance);
        float sqrMoveThreshold = moveThreshold * moveThreshold;

        if ((currentPos - _lastHuntPosition).sqrMagnitude >= sqrMoveThreshold ||
            (agent != null && agent.velocity.sqrMagnitude >= stuckVelocityThreshold * stuckVelocityThreshold))
        {
            _lastHuntPosition = currentPos;
            _lastHuntMovementTime = Time.time;
        }
    }

    private bool IsAgentStuck()
    {
        if (stuckTimeBeforePhase <= 0f)
            return true;

        return Time.time - _lastHuntMovementTime >= stuckTimeBeforePhase;
    }

    private bool IsPathToPlayerValid()
    {
        if (agent == null || !agent.isOnNavMesh || house == null || house.currentPlayer == null)
        {
            _cachedPathValid = false;
            _cachedPathStatus = NavMeshPathStatus.PathInvalid;
            return false;
        }

        if (Time.time - _lastPathCheckTime < pathCheckInterval)
            return _cachedPathValid;

        _lastPathCheckTime = Time.time;
        if (_playerPath == null)
            _playerPath = new NavMeshPath();

        bool calculated = agent.CalculatePath(house.currentPlayer.transform.position, _playerPath);
        _cachedPathStatus = _playerPath.status;
        _cachedPathValid = calculated && _playerPath.status == NavMeshPathStatus.PathComplete;
        return _cachedPathValid;
    }

    private void StartPhase()
    {
        _phaseAttempts++;
        _isPhasing = true;
        _phaseEndTime = Time.time + Mathf.Max(0.05f, phaseDuration);

        if (agent != null)
        {
            _agentUpdatePositionBase = agent.updatePosition;
            _agentUpdateRotationBase = agent.updateRotation;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void UpdatePhaseMovement()
    {
        if (house == null || house.currentPlayer == null)
        {
            EndPhase();
            return;
        }

        float baseSpeed = ghostParameters != null ? ghostParameters.targetingSpeedBase : 1f;
        float moveSpeed = Mathf.Max(0.1f, baseSpeed * phaseMoveSpeedMultiplier);

        Vector3 targetPos = house.currentPlayer.transform.position;
        targetPos.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (agent != null)
        {
            agent.nextPosition = transform.position;
        }

        animator.SetBool("Walk", true);
        animator.SetFloat("MoveSpeed", moveSpeed);
        animator.SetBool("Targeting", true);

        if (Time.time >= _phaseEndTime)
        {
            EndPhase();
        }
    }

    private void EndPhase()
    {
        _isPhasing = false;

        if (agent != null)
        {
            agent.updatePosition = _agentUpdatePositionBase;
            agent.updateRotation = _agentUpdateRotationBase;
            agent.isStopped = false;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, phaseReattachRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                StopHunting();
                return;
            }
        }

        _lastHuntPosition = transform.position;
        _lastHuntMovementTime = Time.time;
    }

    private void ResetAntiExploitState(bool keepPathCache = false)
    {
        _isPhasing = false;
        _phaseAttempts = 0;
        _phaseEndTime = 0f;
        _lastHuntMovementTime = Time.time;
        _lastHuntPosition = transform.position;

        if (!keepPathCache)
        {
            _cachedPathValid = true;
            _cachedPathStatus = NavMeshPathStatus.PathComplete;
            _lastPathCheckTime = 0f;
        }

        if (agent != null)
        {
            agent.updatePosition = _agentUpdatePositionBase;
            agent.updateRotation = _agentUpdateRotationBase;
        }
    }

    private void SetHidingDestination()
    {
        if (currentWayPoint != null)
        {
            agent.destination = currentWayPoint.transform.position;
            
            float dist = Vector3.Distance(transform.position, currentWayPoint.transform.position);
            if (dist < 1)
            {
                //if ghost come back after a hunt, its speed become slow again
                if (currentState != GhostState.huntingState || currentState != GhostState.standingState)
                {
                    agent.speed = ghostParameters.hidingSpeedBase;
                }
            
                SelectNewHidingWaypoint();
            }
        }
        else
        {
            SelectNewHidingWaypoint();
        }
    }
    
    public void CancelHuntCompletely()
    {
        _willHunt = false;
        ResetExternalHuntSlow();

        CancelInvoke(nameof(StandingBeforeHunting));
        CancelInvoke(nameof(StartHunting));

        currentState = GhostState.hideState;
        
        _canHunt = false;
        this.Invoke(ghostParameters.minimumPeaceTime, () =>
        {
            _canHunt = true;
        });

        onGhostStopToHunt?.Invoke();
        ResetAntiExploitState();
    }

    public void ForceStartHunt()
    {
        TriggerHunting(true);
    }

    private void StopHunting()
    {
        _willHunt = false;
        ResetExternalHuntSlow();
        CancelInvoke(nameof(StandingBeforeHunting));
        
        if (huntingSound != null && _huntingSound != null)
        {
            _huntingSound.Stop(false);
        }

        currentState = GhostState.hideState;
        ghostModel.SetActive(false);

        //Ghost go back to its room
        agent.speed = ghostParameters.normalSpeedBase;
        
        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        
        CancelInvoke(ACTIVITY_INVOKE);
        this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.Radioactivity))
        {
            currentRoom.StartRadiation(ghostParameters.radiationDurationAfterAttack);
        }
        
        onGhostStopToHunt?.Invoke();
        
        this.Invoke(ghostParameters.minimumPeaceTime, () =>
        {
            _canHunt = true;
        });

        SetVisibleRenderer(false);
        ResetAntiExploitState();
    }

    public void StopHuntByKatana(bool playApparitionFx = true, float delayBeforeStop = 0.2f)
    {
        if (playApparitionFx && fxApparition != null)
            fxApparition.Play();

        if (delayBeforeStop > 0f)
        {
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }

            CancelInvoke(nameof(StopHuntByKatanaInternal));
            this.Invoke(nameof(StopHuntByKatanaInternal), delayBeforeStop, StopHuntByKatanaInternal);
            return;
        }

        StopHuntByKatanaInternal();
    }

    private void StopHuntByKatanaInternal()
    {
        if (IsHunting(includeWillHunt: true))
            StopHunting();
        else
            CancelHuntCompletely();
    }

    private void Kill()
    {
        if (_isLocked) return;
#if UNITY_EDITOR
        if (house.useDebugs && house.playerCantDie) return;
#endif
        if (TutorialManager.IsTutorialActive && TutorialManager.Instance != null &&
            TutorialManager.Instance.TryHandleTutorialHuntFailure(_player))
            return;

        _player.Die();
        house.ExpelPlayerFromHouse();
    }

    private void SetVisibleRenderer(bool enable)
    {
        if (currentState == GhostState.standingState)
        {
            enable = true;
        }
        else if (currentState == GhostState.huntingState)
        {
            if (isBlinkingGhost)
            {
                float averageChangeTime = enable == true ? averageVisibleTime : averageInvisibleTime;
                float changeTimeVariation = enable == true ? visibleTimeVariation : invisibleTimeVariation;
                float nextChange = Random.Range(averageChangeTime - changeTimeVariation, averageChangeTime + changeTimeVariation);
                this.Invoke(nextChange, () => SetVisibleRenderer(!enable));
            }
        }
        else
        {
            //Stop the loop
            enable = false;
        }

        foreach (Renderer r in renderers)
        {
            r.enabled = enable;
        }
    }


    private void TriggerActivity()
    {
        if (_isLocked) return;
        
        /*if(house.useDebugs)
            Debug.Log("Activity Triggered");*/

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);

        if (tutorialOverrideEnabled && tutorialBlockAllActivities)
        {
            CancelInvoke(ACTIVITY_INVOKE);
            this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
            return;
        }

        if (tutorialOverrideEnabled && tutorialRestrictActivities &&
            (tutorialAllowedActivities == null || tutorialAllowedActivities.Count == 0))
        {
            CancelInvoke(ACTIVITY_INVOKE);
            this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
            return;
        }
        
        //Do not trigger activity during a hunt, re-roll timer
        if (currentState == GhostState.huntingState)
        {
            CancelInvoke(ACTIVITY_INVOKE);
            this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
            return;
        }
        
        GhostActivities randomActivity = GetRandomActivity();

# if UNITY_EDITOR
        if (house.useDebugs && forcedGhostActivity != GhostActivities.Nothing)
        {
            randomActivity = forcedGhostActivity;
        }
#endif
        
        //Le fantôme trigger plus vite le prochain event s'il est enervé
        nextActivityTime = DivideByPercentage(nextActivityTime, angerPercentage);

        if (currentState != GhostState.hideState)
        {
            // Interactions uniquement en hide state
            CancelInvoke(ACTIVITY_INVOKE);
            this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
            return;
        }
        
        switch (randomActivity)
        {
            case GhostActivities.ObjectInteraction:
                ThrowObject();
                //Debug.Log("Activity triggered : Throw Object");
                break;

            case GhostActivities.ChangeLightState:
                TriggerElectronicLightActivity();
                //Debug.Log("Activity triggered : Elec Light " + Time.time);
                break;

            case GhostActivities.ChangeElectronicObjectState:
                TriggerActivatorObjectActivity();
                //Debug.Log("Activity triggered : Electronic Object " + Time.time);
                break;
            
            case GhostActivities.Hunt:
                //Can't attack if not enough angry
                //Can't attack if Earthbound ghost and not in its favorite room
                if (TutorialAllowsHunt() && _canHunt && angerPercentage >= ghostParameters.minimumAngerToHunt && 
                    (ghostParameters.ghostTypeData.ghostType != GhostTypeData.GhostType.Earthbound || 
                     (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Earthbound && currentRoom == favoriteRoom)))
                {
                    TriggerHunting();
                    //Debug.Log("Activity triggered : Hunt " + Time.time);
                    return;
                }
                else
                {
                    CancelInvoke(ACTIVITY_INVOKE);
                    this.Invoke(ACTIVITY_INVOKE, 0.1f, TriggerActivity);
                    return;
                    //Debug.Log("Activity triggered : Reroll activity " + Time.time);
                    return;
                }
                break;
            
            case GhostActivities.PlayWithDoor:
                PlayWithDoor();
                //Debug.Log("Activity triggered : Play With Door " + Time.time);
                break;
            
            case GhostActivities.BlowOutAFlammable:
                if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.BlowUpFlammables))
                {
                    BlowUpARandomFlammable();
                }
                else
                {
                    CancelInvoke(ACTIVITY_INVOKE);
                    this.Invoke(ACTIVITY_INVOKE, 0.1f, TriggerActivity);
                    return;
                    return;
                }
                break;

            default:
                if (ghostParameters.ShouldInteractWithClickableInsteadOfNothing())
                {
                    InteractWithAStandardClickable();
                }
                else
                {
                    //Debug.Log("Activity triggered : Nothing " + Time.time);
                    nextActivityTime *= 0.5f;
                }
                break;
        }

        # if UNITY_EDITOR
        if (house.useDebugs && tripleActivityDebug)
        {
            nextActivityTime = nextActivityTime / 3;
        }
        #endif
        
        CancelInvoke(ACTIVITY_INVOKE);
        this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
    }

    private GhostActivities GetRandomActivity()
    {
        if (tutorialOverrideEnabled)
        {
            if (tutorialForceActivity && tutorialForcedActivity != GhostActivities.Nothing)
                return tutorialForcedActivity;

            if (tutorialRestrictActivities && tutorialAllowedActivities != null && tutorialAllowedActivities.Count > 0)
                return tutorialAllowedActivities[Random.Range(0, tutorialAllowedActivities.Count)];
        }

        if (_activityValues == null || _activityValues.Length == 0)
            _activityValues = (GhostActivities[])Enum.GetValues(typeof(GhostActivities));

        return _activityValues[Random.Range(0, _activityValues.Length)];
    }

    private void InteractWithAStandardClickable()
    {
        if (TryGetClickableWithLineOfSight(true, out ClickableObject clickable))
        {
            ActivateActivitySource(clickable.activitySource);
            clickable.OnClick();
        }
    }

    private bool TryGetClickableWithLineOfSight(bool ignoreSwitch, out ClickableObject clickable)
    {
        clickable = null;
        if (currentRoom == null || currentRoom.clickableObjects == null || currentRoom.clickableObjects.Count == 0)
            return false;

        List<ClickableObject> candidates = new List<ClickableObject>();
        foreach (ClickableObject co in currentRoom.clickableObjects)
        {
            if (co == null) continue;
            if (ignoreSwitch && co.TryGetComponent(out Switch _)) continue;
            candidates.Add(co);
        }

        if (candidates.Count == 0)
            return false;

        int tries = Mathf.Max(1, maxInteractionSelectionTries);
        for (int i = 0; i < tries && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            ClickableObject candidate = candidates[index];
            candidates.RemoveAt(index);

            if (HasLineOfSightToClickable(candidate))
            {
                clickable = candidate;
                return true;
            }
        }

        return false;
    }

    private void BlowUpARandomFlammable()
    {
        if(house.useDebugs)
            Debug.Log("Blow up flammable triggered");
        
        FlammableElement flammableToBlowUp = GetRandomFlammableElement(true);
        if (flammableToBlowUp != null)
        {
            flammableToBlowUp.EnableFire(false, true, true);
        }
    }

    private void PlayWithDoor()
    {
        Door selectedDoor = GetRandomDoor();
        //No door found
        if (selectedDoor == null)
        {
            //Debug.Log("No door find");
            return;
        }
        else
        {
            //Debug.Log("Door activity! : ", selectedDoor);
        }

        if (selectedDoor.isOpen)
        {
            if(ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Misty) return;
            
            float roll = Random.Range(0f, 100f);
            if (roll <= ghostParameters.slamChances)
            {
                //Slam
                selectedDoor.GhostDoorInteraction(0, slamForce, true);
            }
            else
            {
                //Close
                selectedDoor.GhostDoorInteraction(0, closeForce);
            }
        }
        else
        {
            if(ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Grumpy) return;
            
            float openForce = Random.Range(openForceMin, openForceMax);
            float openAngle = Random.Range(openAngleMin, openAngleMax);
            selectedDoor.GhostDoorInteraction(openAngle, openForce);
        }
        
        ActivateActivitySource(selectedDoor.activitySource);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritPrints))
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= ghostParameters.chancesToPutPrintOnDoors)
            {
                PrintSource printSourceToUse = selectedDoor.GetRandomPrintSource();
                if (printSourceToUse != null)
                {
                    ActivatePrint(printSourceToUse, PrintTypes.Paw);
                }
            }
        }
    }

    private void ActivatePrint(PrintSource printSource, PrintTypes printType)
    {
        float duration = Random.Range(printDurationMin, printDurationMax);
        Sprite spriteToUse =
            printType == PrintTypes.Paw ? pawSprites[Random.Range(0, pawSprites.Length)] : fingerSprite;
        printSource.Activate(duration, spriteToUse);
    }

    private void TriggerElectronicLightActivity()
    {
        Switch switchLightObject = currentRoom.SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType.electronicLight);
        
        //No object found, throw an object instead
        if (switchLightObject == null)
        {
            ThrowObject();
            return;
        }
        
        if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Demonic || ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Totemic)
        {
            if (!switchLightObject.activableObject.isActivated)
            {
                ThrowObject(); //Demonics and Totemics can't turn on lights, throw on object instead
                return;
            }
        }
        else if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Luminous || ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Voltaic)
        {
            if (switchLightObject.activableObject.isActivated)
            {
                ThrowObject(); //Luminous and Voltaics can't turn off lights, throw on object instead
                return;
            }
        }
        
        switchLightObject.OnClick();
        ActivateActivitySource(switchLightObject.activitySource);
        
        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritPrints))
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= ghostParameters.chancesToPutPrintOnSwitch)
            {
                PrintSource printSourceToUse = switchLightObject.GetRandomPrintSource();
                if (printSourceToUse != null)
                {
                    ActivatePrint(printSourceToUse, PrintTypes.Finger);
                }
            }
        }
    }
    
    private void TriggerActivatorObjectActivity()
    {
        Switch electronicObject = currentRoom.SelectRandomSwitchObject(ActivableObject.ActivationSpecialType.electronicLight);
        
        //No object found, throw an object instead
        if (electronicObject == null)
        {
            ThrowObject();
            return;
        }

        if (electronicObject.activableObject != null)
        {
            if (electronicObject.activableObject.activationType == ActivableObject.ActivationSpecialType.electronicObject)
            {
                if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Totemic)
                {
                    if (electronicObject.activableObject.isActivated == false)
                    {
                        ThrowObject(); //Totemics can't turn on electronic objects, throw on object instead
                        return;
                    }
                }
                else if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Voltaic)
                {
                    if (electronicObject.activableObject.isActivated)
                    {
                        ThrowObject(); //Voltaics can't turn off electronic objects, throw on object instead
                        return;
                    }
                }
            }else if (electronicObject.activableObject.activationType == ActivableObject.ActivationSpecialType.water)
            {
                if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Blazing)
                {
                    if (electronicObject.activableObject.isActivated == false)
                    {
                        ThrowObject(); //Blazings can't turn on water objects, throw on object instead
                        return;
                    }
                }
                else if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Aquatic)
                {
                    if (electronicObject.activableObject.isActivated)
                    {
                        ThrowObject(); //Aquatics can't turn off water objects, throw on object instead
                        return;
                    }
                }
            }
        }
        
        electronicObject.OnClick();
        ActivateActivitySource(electronicObject.activitySource);
    }

    private void InitWayPoints()
    {
        huntingWayPoints.AddRange(house.wayPoints);
    }

    private void ResetWaypoints()
    {
        huntingWayPoints.Clear();
        huntingWayPoints.AddRange(house.wayPoints);
    }

    private void SelectNewHuntingWaypoint()
    {
        currentWayPoint = SelectNearestWayPoint();
        currentHuntingWayPointDistanceTargeted = Random.Range(0.1f, 5f);

        float chancesToIgnoreAPoint = Random.Range(0f, 100f);
        if (chancesToIgnoreAPoint <= chancesPercentageToIgnoreAWayPoint)
        {
            huntingWayPoints.Remove(currentWayPoint);
            SelectNewHuntingWaypoint();
        }
    }
    
    private void SelectNewHidingWaypoint()
    {
        Room roomToGo = currentRoom != null ? currentRoom : favoriteRoom;

        if (tutorialOverrideEnabled && !tutorialAllowRoomChange)
        {
            roomToGo = tutorialForceRoom && tutorialForcedRoom != null ? tutorialForcedRoom : favoriteRoom;
        }
        else if (tutorialOverrideEnabled && tutorialForceRoom && tutorialForcedRoom != null)
        {
            roomToGo = tutorialForcedRoom;
        }
        else if (_forceReturnToFavoriteRoom && favoriteRoom != null && currentRoom != favoriteRoom)
        {
            roomToGo = favoriteRoom;
        }
        else
        {
            bool shouldReturnToFavoriteRoom =
                currentRoom != null &&
                currentRoom != favoriteRoom &&
                favoriteRoom != null &&
                Random.Range(0f, 100f) <= ghostParameters.chancesToReturnToFavoriteRoom;

            if (shouldReturnToFavoriteRoom)
            {
                roomToGo = favoriteRoom;
            }
            else
            {
                Room sourceRoom = currentRoom != null ? currentRoom : favoriteRoom;
                float chances = Random.Range(0f, 100f);
                if (sourceRoom != null &&
                    sourceRoom.neighborRooms != null &&
                    sourceRoom.neighborRooms.Length > 0 &&
                    chances <= chancesToRoamInAnotherRoom)
                {
                    List<Room> validNeighbors = new List<Room>();
                    for (int i = 0; i < sourceRoom.neighborRooms.Length; i++)
                    {
                        Room neighbor = sourceRoom.neighborRooms[i];
                        if (neighbor != null)
                            validNeighbors.Add(neighbor);
                    }

                    if (validNeighbors.Count > 0)
                        roomToGo = validNeighbors[Random.Range(0, validNeighbors.Count)];
                }
            }
        }

        currentWayPoint = house.SelectRandomWayPointFromARoom(roomToGo);
    }

    private WayPoint SelectNearestWayPoint()
    {
        if(huntingWayPoints.Count <= 2 * house.rooms.Length)
            ResetWaypoints();
        
        WayPoint selectedWaypoint = huntingWayPoints[0];
        float bestDist = 1000;
        
        foreach (WayPoint w in huntingWayPoints)
        {
            float dist = PathDistanceForAnAgent(agent, w.transform.position, 0.1f);
            if (dist < bestDist)
            {
                bestDist = dist;
                selectedWaypoint = w;
            }
        }

        return selectedWaypoint;
    }

    public void ForceNewWaypoint(Room room)
    {
        currentHuntingWayPointDistanceTargeted = Random.Range(0.1f, 5f);
        currentWayPoint = house.SelectRandomWayPointFromARoom(room);
    }

    private int OverlapSphereAll(Vector3 position, float radius, int layerMask, out Collider[] results)
    {
        int count = Physics.OverlapSphereNonAlloc(position, radius, _overlapBuffer, layerMask);
        if (count == _overlapBuffer.Length)
        {
            results = Physics.OverlapSphere(position, radius, layerMask);
            return results.Length;
        }

        results = _overlapBuffer;
        return count;
    }

    private int OverlapSphereAll(Vector3 position, float radius, out Collider[] results)
    {
        int count = Physics.OverlapSphereNonAlloc(position, radius, _overlapBuffer);
        if (count == _overlapBuffer.Length)
        {
            results = Physics.OverlapSphere(position, radius);
            return results.Length;
        }

        results = _overlapBuffer;
        return count;
    }

    private CatchableObject GetRandomCatchableObject()
    {
        Collider[] hits;
        int hitCount = OverlapSphereAll(transform.position, throwDetectionRange, throwableMask, out hits);

        _catchableBuffer.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (!hit.TryGetComponent(out CatchableObject catchable))
                continue;

            if (!IsCatchableValid(catchable))
                continue;

            if (!HasLineOfSightToCatchable(catchable))
                continue;

            _catchableBuffer.Add(catchable);
        }

        if (_catchableBuffer.Count == 0)
            return null;

        return GetHighestPriorityRandom(_catchableBuffer);
    }
    
    private bool IsCatchableValid(CatchableObject catchable)
    {
        if (catchable.isGrabbed)
            return false;

        if (!catchable.canBeThrownByGhost)
            return false;

        float heightDiff = catchable.transform.position.y - transform.position.y;
        if (Mathf.Abs(heightDiff) > 3f)
            return false;

        return true;
    }
    
    private bool HasLineOfSightToCatchable(CatchableObject catchable)
    {
        Vector3 origin = transform.position + Vector3.up * visibilityHeightOffset;
        Vector3 target = catchable.transform.position + Vector3.up * 0.5f;

        Vector3 direction = (target - origin);
        float distance = direction.magnitude;

        direction.Normalize();

        int mask = GetInteractionOcclusionMask();
        if (mask != 0 && Physics.Raycast(origin, direction, out RaycastHit hit, distance, mask))
        {
            // Something blocks the view before reaching the object
            return false;
        }

        return true;
    }

    private bool HasLineOfSightToClickable(ClickableObject clickable)
    {
        if (clickable == null)
            return false;

        Vector3 origin = GetInteractionOrigin();
        Vector3 target = GetInteractionTargetPoint(clickable.transform);

        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        int mask = GetInteractionOcclusionMask();
        if (mask != 0 && Physics.Raycast(origin, direction, distance, mask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private Vector3 GetInteractionOrigin()
    {
        if (vision != null && vision.ghostHead != null)
            return vision.ghostHead.position;

        return transform.position + Vector3.up * visibilityHeightOffset;
    }

    private Vector3 GetInteractionTargetPoint(Transform target)
    {
        if (target == null)
            return transform.position;

        Collider col = target.GetComponent<Collider>();
        if (col == null)
            col = target.GetComponentInChildren<Collider>();

        if (col != null)
            return col.bounds.center;

        return target.position;
    }

    private int GetInteractionOcclusionMask()
    {
        if (interactionOcclusionMask.value != 0)
            return interactionOcclusionMask.value;

        return blockingThrowMask.value;
    }
    
    private CatchableObject GetHighestPriorityRandom(List<CatchableObject> catchables)
    {
        int highestPriority = int.MinValue;

        foreach (var c in catchables)
        {
            if (c.priority > highestPriority)
                highestPriority = c.priority;
        }

        List<CatchableObject> best = new List<CatchableObject>();

        foreach (var c in catchables)
        {
            if (c.priority == highestPriority)
                best.Add(c);
        }

        return best[Random.Range(0, best.Count)];
    }
    
    private FlammableElement GetRandomFlammableElement(bool enabledStateWanted)
    {
        Collider[] hits;
        int hitCount = OverlapSphereAll(transform.position, throwDetectionRange, out hits);

        _flammableBuffer.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (hit.TryGetComponent(out FlammableElement flammable))
            {
                if (flammable.optionalLinkedRoom != null && flammable.optionalLinkedRoom != currentRoom)
                {
                    continue;
                }
                //Ignore objects from another stair
                else if (flammable.optionalLinkedRoom == null &&
                         flammable.transform.position.y - transform.position.y > 3 ||
                         flammable.transform.position.y - transform.position.y < -3)
                {
                    continue;
                }
                
                if(flammable.IsOnFire() == enabledStateWanted)
                    _flammableBuffer.Add(flammable);
            }
        }

        if (_flammableBuffer.Count == 0) return null;

        return _flammableBuffer[Random.Range(0, _flammableBuffer.Count)];
    }
    
    private Door GetRandomDoor()
    {
        Collider[] hits;
        int hitCount = OverlapSphereAll(transform.position, doorDetectionRange, doorMask, out hits);

        _doorBuffer.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (hit.TryGetComponent(out Door door))
            {
                //Ignore if the door is too far (from another floor?)
                if (IsNearFromMyAgent(agent, door.transform, doorDetectionRange * 1.2f, 2) == false) continue;
                
                if(!door.IsGrabbed())
                    _doorBuffer.Add(door);
            }
        }

        if (_doorBuffer.Count == 0) return null;

        return _doorBuffer[Random.Range(0, _doorBuffer.Count)];
    }

    public void ThrowObject(CatchableObject objectToThrow = null)
    {
        if (objectToThrow == null) objectToThrow = GetRandomCatchableObject();

        if (objectToThrow == null) return; //No object to throw found
        
        if (objectToThrow is Fruit fruit && ghostParameters.ShouldEatFruit() && fruit.canBeEaten)
        {
            fruit.EatFruit(this);
            return;
        }

        Vector3 randomForce = new Vector3(
            Random.Range(-ghostParameters.throwForceMax, ghostParameters.throwForceMax),
            Random.Range(ghostParameters.throwForceMin, ghostParameters.throwForceMax) * 2,
            Random.Range(-ghostParameters.throwForceMax, ghostParameters.throwForceMax)
        );

        Vector3 randomTorque = new Vector3(
            Random.Range(-throwTorqueMax, throwTorqueMax),
            Random.Range(-throwTorqueMax, throwTorqueMax),
            Random.Range(-throwTorqueMax, throwTorqueMax)
        );

        if (objectToThrow is CatchableBook book)
            book.OnGhostThrow();

        objectToThrow.ApplyForce(randomForce, randomTorque);
        ActivateActivitySource(objectToThrow.activitySource);
    }

    public void ImproveAnger(float percentageToAdd)
    {
        angerPercentage += percentageToAdd;

        if (angerPercentage <= 100f)
            return;

        float surplusOfAnger = angerPercentage - 100f;

        int steps = Mathf.FloorToInt(surplusOfAnger / angerThresholdStep);

        forcedStartTargetingTime =
            _baseForcedStartTargetingTime +
            steps * forcedTargetingTimeIncreasePerStep;
    }

    public void MultiplyAnger(float value)
    {
        float newAnger = angerPercentage * value;
        ImproveAnger(newAnger - angerPercentage);
    }

    public void TryToImproveAnger(float angerValue, Transform source, float distMax = 10f, bool usePathDistance = false)
    {
        float dist = 0;
        if (usePathDistance)
        {
            dist = PathDistanceForAnAgent(agent, source.position);
        }
        else
        {
            dist = Vector3.Distance(transform.position, source.position);
        }

        if (dist <= distMax)
        {
            ImproveAnger(angerValue);
        }
    }

    public void DecreaseAngriness(float percentageToDecrease)
    {
        if (angerPercentage < 0)
        {
            angerPercentage = 0;
        }
    }

    private void ActivateActivitySource(ActivitySource activitySource)
    {
        //Activate paranormal activity
        int randomValue = ghostParameters.GetRandomActivityValue();
        float randomTime = ghostParameters.GetRandomActivityTime();
        
        activitySource.SetActivityValue(randomValue, randomTime);
        
        //Activate refreshment
        float refreshment = ghostParameters.GetRandomRefreshment();
        if (ghostParameters.FreezingTemperature && currentRoom != favoriteRoom)
            currentRoom.AddTemperatureDeltaClamped(refreshment, currentRoom.minNormalTemperature);
        else
            currentRoom.AddTemperatureDelta(refreshment);

        ApplyFavoriteRoomIndirectCooling(refreshment);
        HandlePostActivityRoomDecision();
    }

    private bool TutorialAllowsHunt()
    {
        return !tutorialOverrideEnabled || tutorialAllowHunt;
    }

    public void ApplyTutorialOverride(bool enabled, bool blockAllActivities, bool forceRoom, Room forcedRoom,
        bool restrictActivities, List<GhostActivities> allowedActivities, bool forceActivity, GhostActivities forcedActivity,
        bool allowHunt, bool allowRoomChange)
    {
        tutorialOverrideEnabled = enabled;
        tutorialBlockAllActivities = blockAllActivities;
        tutorialForceRoom = forceRoom;
        tutorialForcedRoom = forcedRoom;
        tutorialRestrictActivities = restrictActivities;
        tutorialForceActivity = forceActivity;
        tutorialForcedActivity = forcedActivity;
        tutorialAllowHunt = allowHunt;
        tutorialAllowRoomChange = allowRoomChange;

        tutorialAllowedActivities.Clear();
        if (allowedActivities != null && allowedActivities.Count > 0)
            tutorialAllowedActivities.AddRange(allowedActivities);

        if (tutorialOverrideEnabled && tutorialForceRoom && tutorialForcedRoom != null)
            ForceTutorialRoom(tutorialForcedRoom);
    }

    public void ClearTutorialOverride()
    {
        tutorialOverrideEnabled = false;
        tutorialBlockAllActivities = false;
        tutorialForceRoom = false;
        tutorialForcedRoom = null;
        tutorialRestrictActivities = false;
        tutorialForceActivity = false;
        tutorialForcedActivity = GhostActivities.Nothing;
        tutorialAllowHunt = true;
        tutorialAllowRoomChange = true;
        tutorialAllowedActivities.Clear();
    }

    private void ApplyFavoriteRoomIndirectCooling(float refreshment)
    {
        if (favoriteRoom == null || currentRoom == null || currentRoom == favoriteRoom || ghostParameters == null)
            return;

        float coolingPercent = ghostParameters.favoriteRoomIndirectCoolingPercent;
        if (ghostParameters.ghostTypeData != null &&
            ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Glacial)
        {
            coolingPercent = ghostParameters.glacialFavoriteRoomIndirectCoolingPercent;
        }

        coolingPercent = Mathf.Clamp01(coolingPercent);
        if (coolingPercent <= 0f)
            return;

        favoriteRoom.AddTemperatureDelta(refreshment * coolingPercent);
    }

    private void UpdateFavoriteRoomTemperatureRules()
    {
        if (!ShouldMaintainFavoriteRoomCold())
            return;

        float favoriteTemperature = favoriteRoom.GetTemperatureCelsius();
        _favoriteRoomColdestTemperatureReached = Mathf.Min(_favoriteRoomColdestTemperatureReached, favoriteTemperature);

        float favoriteRoomMaxRewarm = _favoriteRoomColdestTemperatureReached +
                                      Mathf.Max(0f, ghostParameters.favoriteRoomRewarmMargin);
        favoriteRoom.ClampTemperatureTargetMax(favoriteRoomMaxRewarm);

        if (house == null || house.rooms == null)
            return;

        float maxDiff = Mathf.Max(0f, ghostParameters.maxNonFavoriteColderThanFavorite);
        float allowedMinTemperature = favoriteTemperature - maxDiff;
        float graceDuration = Mathf.Max(0f, ghostParameters.nonFavoriteOvercoolGraceDuration);
        float correctionPerSecond = Mathf.Max(0f, ghostParameters.nonFavoriteCorrectionPerSecond);

        for (int i = 0; i < house.rooms.Length; i++)
        {
            Room room = house.rooms[i];
            if (room == null || room == favoriteRoom)
                continue;

            float roomTemperature = room.GetTemperatureCelsius();
            if (roomTemperature < allowedMinTemperature)
            {
                float timer = 0f;
                _nonFavoriteOvercoolTimers.TryGetValue(room, out timer);
                timer += Time.deltaTime;
                _nonFavoriteOvercoolTimers[room] = timer;

                if (timer >= graceDuration && correctionPerSecond > 0f)
                    room.AddHeating(correctionPerSecond * Time.deltaTime);

                if (timer >= graceDuration)
                    room.ClampTemperatureCurrentAndTargetMin(allowedMinTemperature);
            }
            else
            {
                _nonFavoriteOvercoolTimers.Remove(room);
            }
        }
    }

    private bool ShouldMaintainFavoriteRoomCold()
    {
        if (ghostParameters == null || favoriteRoom == null)
            return false;

        if (ghostParameters.ghostTypeData != null &&
            ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Blazing)
            return false;

        return true;
    }

    private void HandlePostActivityRoomDecision()
    {
        if (ghostParameters == null || favoriteRoom == null || currentRoom == null)
            return;

        if (currentRoom == favoriteRoom)
            return;

        if (tutorialOverrideEnabled && !tutorialAllowRoomChange)
            return;

        float roll = Random.Range(0f, 100f);
        float cumulative = Mathf.Max(0f, ghostParameters.chancesToDoNothingAfterNonFavoriteActivity);
        if (roll < cumulative)
            return;

        cumulative += Mathf.Max(0f, ghostParameters.chancesToChangeRoomAfterNonFavoriteActivity);
        if (roll < cumulative)
        {
            TryMoveToNeighborRoomImmediately();
            return;
        }

        cumulative += Mathf.Max(0f, ghostParameters.chancesToReturnToFavoriteAfterNonFavoriteActivity);
        if (roll < cumulative)
        {
            ForceReturnToFavoriteRoom();
        }
    }

    private void TryMoveToNeighborRoomImmediately()
    {
        if (currentRoom == null || currentRoom.neighborRooms == null || currentRoom.neighborRooms.Length == 0)
            return;

        List<Room> validNeighbors = new List<Room>();
        for (int i = 0; i < currentRoom.neighborRooms.Length; i++)
        {
            Room neighbor = currentRoom.neighborRooms[i];
            if (neighbor != null)
                validNeighbors.Add(neighbor);
        }

        if (validNeighbors.Count == 0)
            return;

        Room nextRoom = validNeighbors[Random.Range(0, validNeighbors.Count)];
        WayPoint nextWaypoint = house.SelectRandomWayPointFromARoom(nextRoom);
        if (nextWaypoint == null)
            return;

        currentWayPoint = nextWaypoint;
    }

    private void ForceReturnToFavoriteRoom()
    {
        if (favoriteRoom == null)
            return;

        _forceReturnToFavoriteRoom = currentRoom != favoriteRoom;

        WayPoint nextWaypoint = house.SelectRandomWayPointFromARoom(favoriteRoom);
        if (nextWaypoint == null)
            return;

        currentWayPoint = nextWaypoint;
    }

    public void ApplyTutorialGhostParameters(GhostParameters parameters, bool reschedule = true)
    {
        if (parameters == null)
            return;

        ghostParameters = parameters;
        _averageHuntTime = ghostParameters.averageHuntTime;
        angerPercentage = Mathf.Max(0f, ghostParameters.startingAnger);

        if (_baseAverageActivityTime <= 0f)
            _baseAverageActivityTime = averageActivityTime;

        averageActivityTime = _baseAverageActivityTime;
        if (ghostParameters.ghostTypeData != null &&
            ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Trickster)
        {
            averageActivityTime *= 0.75f;
        }

        if (!reschedule)
            return;

        CancelInvoke(ACTIVITY_INVOKE);
        CancelInvoke(ORBS_INVOKE);
        CancelInvoke(PASSIVE_ANGER_INVOKE);

        SchedulePassiveAnger();

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        this.Invoke(ACTIVITY_INVOKE, nextActivityTime * 2, TriggerActivity);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritOrbs))
        {
            float delayBeforeNextGhostOrbs = Random.Range(ghostParameters.nextOrbsDelayMin, ghostParameters.nextOrbsDelayMax);
            this.Invoke(ORBS_INVOKE, delayBeforeNextGhostOrbs, CreateSpiritOrbs);
        }
    }

    private void ForceTutorialRoom(Room room)
    {
        if (room == null || house == null)
            return;

        favoriteRoom = room;
        currentRoom = room;
        _forceReturnToFavoriteRoom = false;
        _favoriteRoomColdestTemperatureReached = room.GetTemperatureCelsius();
        _nonFavoriteOvercoolTimers.Clear();

        WayPoint target = house.SelectRandomWayPointFromARoom(room);
        if (target != null)
        {
            if (agent != null && agent.enabled)
                agent.Warp(target.transform.position);
            else
                transform.position = target.transform.position;

            currentWayPoint = target;
        }
    }

    public bool IsHunting(bool includeWillHunt = true)
    {
        return currentState == GhostState.huntingState || currentState == GhostState.standingState || (includeWillHunt && _willHunt);
    }

    public void ApplyExternalHuntSlow(float slowPercent, float duration)
    {
        if (duration <= 0f || !IsHunting(false))
            return;

        slowPercent = Mathf.Clamp01(slowPercent);
        if (slowPercent <= 0f)
            return;

        _externalHuntSlowPercent = Mathf.Max(_externalHuntSlowPercent, slowPercent);
        _externalHuntSlowEndTime = Mathf.Max(_externalHuntSlowEndTime, Time.time + duration);
    }

    private float GetExternalHuntSpeedMultiplier()
    {
        if (_externalHuntSlowEndTime <= Time.time)
        {
            ResetExternalHuntSlow();
            return 1f;
        }

        return Mathf.Clamp(1f - _externalHuntSlowPercent, 0.05f, 1f);
    }

    private void ResetExternalHuntSlow()
    {
        _externalHuntSlowPercent = 0f;
        _externalHuntSlowEndTime = 0f;
    }

    public void LockGhost()
    {
        CancelInvoke(ORBS_INVOKE);
        CancelInvoke(PASSIVE_ANGER_INVOKE);
        _isLocked = true;
    }
    
    public bool IsLocked() => _isLocked;
}
