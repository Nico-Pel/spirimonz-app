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

    public GhostState currentState;

    public House house;
    public Room favoriteRoom;
    public Room currentRoom;
    
    public Animator animator;
    public ParticleSystem fxApparition;

    public bool isBlinkingGhost;

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

    private float _waitDoorTime = 0.5f;
    private bool _stopMoving = false;
    
    [FormerlySerializedAs("angrinessPercentage")] [Header("Ghost Stats : Angriness")] 
    public float angerPercentage = 0f;
    [FormerlySerializedAs("angrinessToAddByTriggeringPlayer")] public float angerToAddByTriggeringPlayer = 10f;
    
    [Header("Ghost Stats : Hunting")]
    public float forecastTimeBeforeAHunt = 5f;
    public float startHuntingStandingTime = 4;
    public float delayBeforeLosingPlayerTargeting = 4f;
    public float huntTimeVariation = 5f;
    
    [ReadOnly] public float forcedStartTargetingTime = 1f;
    [ReadOnly] public float angerThresholdStep = 10;
    [ReadOnly] public float forcedTargetingTimeIncreasePerStep = 2.5f;
    private float _baseForcedStartTargetingTime;

    private bool _forcedStartTargeting = false;
    private bool _targetingPlayer;
    private bool _losingPlayer;
    private bool _isLocked;

    private float _averageHuntTime;

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

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        _baseForcedStartTargetingTime = forcedStartTargetingTime;
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
        

        if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Trickster)
        {
            averageActivityTime *= 0.75f;
        }
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
            currentRoom = newRoom;
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
                ImproveAnger(angerToAddByTriggeringPlayer);
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
        else if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritPrints) && currentState == GhostState.hideState && other.TryGetComponent(out PrintTrigger printTrigger))
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

            currentHuntTime -= Time.deltaTime;
            if (currentHuntTime <= 0)
            {
                StopHunting();
            }

            if (!_forcedStartTargeting && !vision.CanSeePlayer(house.currentPlayer))
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
            else if(_forcedStartTargeting || vision.CanSeePlayer(house.currentPlayer))
            {
                PlayerFound();
            }
            
            SetHuntingDestination();
            
            if (_stopMoving)
            {
                agent.speed = 0;
                animator.SetBool("Walk", false);
            }
            else
            {
                bool canSeePlayer = vision.CanSeePlayer(house.currentPlayer);
                
                float speed = currentState == GhostState.huntingState && canSeePlayer ? ghostParameters.targetingSpeedBase : ghostParameters.normalSpeedBase;
                if (ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Aquatic && currentRoom == favoriteRoom)
                {
                    speed *= 1.5f;
                }
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

        CancelInvoke(nameof(StandingBeforeHunting));
        CancelInvoke(nameof(StartHunting));

        currentState = GhostState.hideState;
        
        _canHunt = false;
        this.Invoke(ghostParameters.minimumPeaceTime, () =>
        {
            _canHunt = true;
        });

        onGhostStopToHunt?.Invoke();
    }

    private void StopHunting()
    {
        _willHunt = false;
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
    }

    private void Kill()
    {
        if (_isLocked) return;
#if UNITY_EDITOR
        if (house.useDebugs && house.playerCantDie) return;
#endif

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

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        
        //Do not trigger activity during a hunt, re-roll timer
        if (currentState == GhostState.huntingState)
        {
            CancelInvoke(ACTIVITY_INVOKE);
            this.Invoke(ACTIVITY_INVOKE, nextActivityTime, TriggerActivity);
            return;
        }
        
        GhostActivities randomActivity =
            (GhostActivities)Enum.GetValues(typeof(GhostActivities))
                .GetValue(Random.Range(0,
                    Enum.GetValues(typeof(GhostActivities)).Length));

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
            //Le fantôme ne déclenche pas d'activité aléatoire pendant sa chasse
            randomActivity = GhostActivities.Nothing;
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
                if (_canHunt && angerPercentage >= ghostParameters.minimumAngerToHunt && 
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

    private void InteractWithAStandardClickable()
    {
        ClickableObject clickable = currentRoom.SelectRandomClickableObject(true);
        if (clickable != null)
        {
            ActivateActivitySource(clickable.activitySource);
            clickable.OnClick();
        }
    }

    private void BlowUpARandomFlammable()
    {
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
        Room roomToGo = favoriteRoom;
        
        float chances = Random.Range(0f, 100f);
        if (chances <= chancesToRoamInAnotherRoom && favoriteRoom.neighborRooms.Length > 0)
        {
            roomToGo = favoriteRoom.neighborRooms[Random.Range(0, favoriteRoom.neighborRooms.Length)];
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

    private CatchableObject GetRandomCatchableObject()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            throwDetectionRange,
            throwableMask
        );

        List<CatchableObject> validCatchables = new List<CatchableObject>();

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out CatchableObject catchable))
                continue;

            if (!IsCatchableValid(catchable))
                continue;

            if (!HasLineOfSightToCatchable(catchable))
                continue;

            validCatchables.Add(catchable);
        }

        if (validCatchables.Count == 0)
            return null;

        return GetHighestPriorityRandom(validCatchables);
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

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, blockingThrowMask))
        {
            // Something blocks the view before reaching the object
            return false;
        }

        return true;
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
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            throwDetectionRange
        );

        List<FlammableElement> flammables = new List<FlammableElement>();

        foreach (var hit in hits)
        {
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
                    flammables.Add(flammable);
            }
        }

        if (flammables.Count == 0) return null;

        return flammables[Random.Range(0, flammables.Count)];
    }
    
    private Door GetRandomDoor()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            doorDetectionRange,
            doorMask
        );

        List<Door> doors = new List<Door>();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Door door))
            {
                //Ignore if the door is too far (from another floor?)
                if (IsNearFromMyAgent(agent, door.transform, doorDetectionRange * 1.2f, 2) == false) continue;
                
                if(!door.IsGrabbed())
                    doors.Add(door);
            }
        }

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
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
        currentRoom.AddTemperatureDelta(ghostParameters.GetRandomRefreshment());
    }

    public bool IsHunting(bool includeWillHunt = true)
    {
        return currentState == GhostState.huntingState || currentState == GhostState.standingState || (includeWillHunt && _willHunt);
    }

    public void LockGhost()
    {
        CancelInvoke(ORBS_INVOKE);
        _isLocked = true;
    }
    
    public bool IsLocked() => _isLocked;
}
