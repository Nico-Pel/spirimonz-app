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
   [ReadOnly] public GhostParameters ghostParameters;
   
   [Space]
   
    # if UNITY_EDITOR
        public GhostParameters forcedGhostParameters;
        public GhostActivities forcedGhostActivity = GhostActivities.Nothing;
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
    
    public GhostState currentState;

    public House house;
    public Room favoriteRoom;
    public Room currentRoom;
    
    public Animator animator;
    public ParticleSystem fxApparition;

    public bool isBlinkingGhost;

    [Header("Sounds")]
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
    
    [Header("Ghost Stats : Speed")] 
    public float hidingSpeedBase = 0.75f;
    public float normalSpeedBase = 1f;
    public float targetingSpeedBase = 2f;

    private float _waitDoorTime = 1f;
    private bool _stopMoving = false;
    
    [Header("Ghost Stats : Angriness")] 
    public float angrinessPercentage = 0f;
    public float minimumAngrinessToHunt = 50;
    public float angrinessToAddByTriggeringPlayer = 10f;
    
    [Header("Ghost Stats : Hunting")]
    public float forecastTimeBeforeAHunt = 5f;
    public float startHuntingStandingTime = 4;
    public float forcedStartTargetingTime = 4;
    public float delayBeforeLosingPlayerTargeting = 4f;
    public float averageHuntTime = 10f;
    public float huntTimeVariation = 5f;
    public float minimumPeaceTime = 60f;
    
    private bool _forcedStartTargeting = false;
    private bool _targetingPlayer;
    private bool _losingPlayer;
    private bool _isLocked;

    [Header("Ghost Stats : Waypoints Hunting")]

    [ReadOnly] public float currentHuntTime;
    [ReadOnly] public WayPoint currentWayPoint;
    public List<WayPoint> huntingWayPoints = new List<WayPoint>();
    [ReadOnly] public float currentHuntingWayPointDistanceTargeted;
    public float chancesPercentageToIgnoreAWayPoint = 40f;
    public float detectPlayerActivityRange = 20f;

    [Header("Ghost Stats : Throwing")] 
    public float throwDetectionRange = 5;
    public float throwForceMin = 0.5f;
    public float throwForceMax = 4;
    public float throwTorqueMax = 90;
    public LayerMask throwableMask;

    [Header("Ghost Stats : Doors Playing")]
    public float slamChances = 50;
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
    
    int agentContacts = 0;

    public void Initialize(House h)
    {
        house = h;

        if (house.possibleGhosts.Length > 0)
        {
            ghostParameters = house.possibleGhostParameters[Random.Range(0, house.possibleGhostParameters.Length)];
        }
        
        # if UNITY_EDITOR
        if (forcedGhostParameters != null)
        {
            ghostParameters = forcedGhostParameters;
        }
        # endif
        
        favoriteRoom = house.hauntableRooms[Random.Range(0, house.hauntableRooms.Length)];
        currentRoom = favoriteRoom;

        transform.position = house.SelectRandomWayPointFromARoom(favoriteRoom).transform.position;

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        this.Invoke(nextActivityTime, TriggerActivity);
        
        ghostModel.SetActive(false);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.SpiritOrbs))
        {
            float delayBeforeNextGhostOrbs = Random.Range(ghostParameters.nextOrbsDelayMin, ghostParameters.nextOrbsDelayMax);
            this.Invoke(delayBeforeNextGhostOrbs, () =>
            {
                CreateSpiritOrbs();
            });
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
                StopHunting();
                Kill(touchedPlayer);
            }
            if (currentState == GhostState.hideState)
            {
                ImproveAngriness(angrinessToAddByTriggeringPlayer);
            }
        }
        else if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.Radioactivity) && other.TryGetComponent(out RadiationDetector radiationDetector))
        {
            Debug.Log("TRIGGER RADIATIONS DETECTOR");
            if (ghostParameters.ShouldDetectRadiationOnTrigger())
            {
                Debug.Log("TRIGGER RADIATIONS");
                radiationDetector.TriggerDetection(ghostParameters.radiationDurationOnTrigger);
            }
        }
        else if (other.TryGetComponent(out Door door))
        {
            if (currentState != GhostState.huntingState /*|| door.isOpen*/)
                return;


            Vector3 directionToDoor = (door.transform.position - transform.position).normalized;
            Vector3 moveDirection = agent.velocity.normalized;

            float dot = Vector3.Dot(moveDirection, directionToDoor);

            if (dot > 0.15f) // seuil à ajuster
            {
                _stopMoving = true;
                this.Invoke(_waitDoorTime, () => _stopMoving = false);

                door.GhostDoorInteraction(
                    Random.Range(0.8f, 1f),   // 80% à 100% ouvert, propre et clampé
                    Random.Range(50f, 65f)     // vitesse raisonnable pour le hinge
                );
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

        _willHunt = true;
        _canHunt = false;
        
        onGhostCallForAHunt?.Invoke();
        this.Invoke(forecastTimeBeforeAHunt, StandingBeforeHunting);
    }

    private void StandingBeforeHunting()
    {
        if (_isLocked) return;
        
        fxApparition.Play();
        SoundManager.Instance.PlaySound(apparitionSound, transform.position, 1f, 1f, -1f, 25f);
        
        onGhostStartToHunt?.Invoke();
        currentState = GhostState.standingState;
        agent.velocity = Vector3.zero;
        
        float startingHuntDelay = DivideByPercentage(startHuntingStandingTime, angrinessPercentage);
        this.Invoke(startingHuntDelay, StartHunting);
        
        ghostModel.SetActive(true);
        SetVisibleRenderer(true);
        animator.SetTrigger("Apparition");
    }

    private void StartHunting()
    {
        InitWayPoints();
        _huntingSound = SoundManager.Instance.PlaySound(huntingSound, transform.position, 1f, 1, -1f, 20f, true, this.transform);
        
        currentState = GhostState.huntingState;
        
        //Start blinking
        SetVisibleRenderer(true);

        //Le fantôme va forcément aller vers le joueur durant cette période.
        _forcedStartTargeting = true;
        this.Invoke(forcedStartTargetingTime, () => _forcedStartTargeting = false);
        _targetingPlayer = true;

        currentHuntTime = Random.Range(averageHuntTime - huntTimeVariation, averageHuntTime + huntTimeVariation);
        Debug.Log("Starting a HUNT for: " + currentHuntTime + " seconds");
    }

    private void Update()
    {
        if (currentState == GhostState.standingState)
        {
            agent.speed = 0;
            LookAtPlayer();
        }
        else if (currentState == GhostState.huntingState)
        {
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
                _targetingPlayer = true;
                _losingPlayer = false;
                this.CancelInvoke("LosingTarget");
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
                agent.speed = currentState == GhostState.huntingState && canSeePlayer ? targetingSpeedBase : normalSpeedBase;
                animator.SetBool("Walk", true);
                animator.SetBool("Targeting", canSeePlayer);
            }
        }
        else
        {
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
        ForceNewWaypoint(Player.Instance.currentRoom);
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

        if (!_targetingPlayer)
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
                    agent.speed = hidingSpeedBase;
                }
            
                SelectNewHidingWaypoint();
            }
        }
        else
        {
            SelectNewHidingWaypoint();
        }
    }

    public void StopHunting()
    {
        _willHunt = false;
        
        if (huntingSound != null)
        {
            _huntingSound.Stop(false);
        }

        currentState = GhostState.hideState;
        ghostModel.SetActive(false);

        //Ghost go back to its room
        agent.speed = normalSpeedBase;
        
        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        this.Invoke(nextActivityTime, TriggerActivity);

        if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.Radioactivity))
        {
            currentRoom.StartRadiation(ghostParameters.radiationDurationAfterAttack);
        }
        
        onGhostStopToHunt?.Invoke();
        
        this.Invoke(minimumPeaceTime, () =>
        {
            _canHunt = true;
        });

        SetVisibleRenderer(false);
    }

    private void Kill(Player player)
    {
        if (_isLocked) return;

        player.Die();
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
    
    float DivideByPercentage(float value, float percentage)
    {
        percentage = Mathf.Clamp(percentage, 0f, 100f);

        float t = percentage / 100f;

        // Courbe exponentielle : 1 → 4
        float divisor = Mathf.Pow(4f, t);

        return value / divisor;
    }

    private void TriggerActivity()
    {
        if (_isLocked) return;
        
        GhostActivities randomActivity =
            (GhostActivities)Enum.GetValues(typeof(GhostActivities))
                .GetValue(Random.Range(0,
                    Enum.GetValues(typeof(GhostActivities)).Length));

# if UNITY_EDITOR
        if (forcedGhostActivity != GhostActivities.Nothing)
        {
            randomActivity = forcedGhostActivity;
        }
#endif
        
        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        //Le fantôme trigger plus vite le prochain event s'il est enervé
        nextActivityTime = DivideByPercentage(nextActivityTime, angrinessPercentage);

        if (currentState != GhostState.hideState)
        {
            //Le fantôme ne déclenche pas d'activité aléatoire pendant sa chasse
            randomActivity = GhostActivities.Nothing;
        }
        
        switch (randomActivity)
        {
            case GhostActivities.ObjectInteraction:
                ThrowObject();
                Debug.Log("Activity triggered : Throw Object");
                break;

            case GhostActivities.ChangeLightState:
                TriggerElectronicLightActivity();
                Debug.Log("Activity triggered : Elec Light " + Time.time);
                break;

            case GhostActivities.ChangeElectronicObjectState:
                TriggerElectronicObjectActivity();
                Debug.Log("Activity triggered : Electronic Object " + Time.time);
                break;
            
            case GhostActivities.Hunt:
                //Can't attack if not enough angry
                //Can't attack if Earthbound ghost and not in its favorite room
                if (_canHunt && angrinessPercentage >= minimumAngrinessToHunt && 
                    (ghostParameters.ghostType != GhostParameters.GhostType.Earthbound || 
                     (ghostParameters.ghostType == GhostParameters.GhostType.Earthbound && currentRoom == favoriteRoom)))
                {
                    TriggerHunting();
                    Debug.Log("Activity triggered : Hunt " + Time.time);
                    return;
                }
                else
                {
                    TriggerActivity();
                    Debug.Log("Activity triggered : Reroll activity " + Time.time);
                    return;
                }
                break;
            
            case GhostActivities.PlayWithDoor:
                PlayWithDoor();
                Debug.Log("Activity triggered : Play With Door " + Time.time);
                break;
            
            case GhostActivities.BlowOutAFlammable:
                if (ghostParameters.HasEvidence(GhostInvestigator.EvidenceType.BlowUpFlammables))
                {
                    BlowUpARandomFlammable();
                }
                else
                {
                    TriggerActivity();
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
                    Debug.Log("Activity triggered : Nothing " + Time.time);
                    nextActivityTime = nextActivityTime / 2;
                }
                break;
        }
        
        this.Invoke(nextActivityTime, TriggerActivity);
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
            Debug.Log("No door find");
            return;
        }
        else
        {
            Debug.Log("Door activity! : ", selectedDoor);
        }

        if (selectedDoor.isOpen)
        {
            if(ghostParameters.ghostType == GhostParameters.GhostType.Misty) return;
            
            float roll = Random.Range(0f, 100f);
            if (roll <= slamChances)
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
            if(ghostParameters.ghostType == GhostParameters.GhostType.Grumpy) return;
            
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
        
        if (ghostParameters.ghostType == GhostParameters.GhostType.Demonic || ghostParameters.ghostType == GhostParameters.GhostType.Totemic)
        {
            if (switchLightObject.activableObject.isActivated)
            {
                switchLightObject.activableObject.Deactivate();
                ActivateActivitySource(switchLightObject.activitySource);
            }
        }
        else if (ghostParameters.ghostType == GhostParameters.GhostType.Luminous || ghostParameters.ghostType == GhostParameters.GhostType.Voltaic)
        {
            if (!switchLightObject.activableObject.isActivated)
            {
                switchLightObject.activableObject.Activate();
                ActivateActivitySource(switchLightObject.activitySource);
            }
        }
        else
        {
            switchLightObject.activableObject.Operate();
            ActivateActivitySource(switchLightObject.activitySource);
        }
        
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
    
    private void TriggerElectronicObjectActivity()
    {
        Switch electronicObject = currentRoom.SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType.electronicObject);
        
        //No object found, throw an object instead
        if (electronicObject == null)
        {
            ThrowObject();
            return;
        }
        
        if (ghostParameters.ghostType == GhostParameters.GhostType.Totemic)
        {
            if (electronicObject.activableObject.isActivated)
            {
                electronicObject.activableObject.Deactivate();
                ActivateActivitySource(electronicObject.activitySource);
            }
        }
        else if (ghostParameters.ghostType == GhostParameters.GhostType.Voltaic)
        {
            if (!electronicObject.activableObject.isActivated)
            {
                electronicObject.activableObject.Activate();
                ActivateActivitySource(electronicObject.activitySource);
            }
        }
        else
        {
            electronicObject.activableObject.Operate();
            ActivateActivitySource(electronicObject.activitySource);
        }
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
        currentHuntingWayPointDistanceTargeted = Random.Range(1f, 5f);

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
        if(huntingWayPoints.Count == 0)
            ResetWaypoints();
        
        WayPoint selectedWaypoint = huntingWayPoints[0];
        float bestDist = 1000;
        
        foreach (WayPoint w in huntingWayPoints)
        {
            float dist = Vector3.Distance(transform.position, w.transform.position);
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
        currentWayPoint = house.SelectRandomWayPointFromARoom(room);
    }

    private CatchableObject GetRandomCatchableObject()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            throwDetectionRange,
            throwableMask
        );

        List<CatchableObject> catchables = new List<CatchableObject>();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out CatchableObject catchable))
            {
                //Ignore objects from another stair
                if (catchable.transform.position.y - transform.position.y > 3 || catchable.transform.position.y - transform.position.y < -3) continue;
                if(!catchable.isGrabbed && catchable.canBeThrownByGhost)
                    catchables.Add(catchable);
            }
        }

        if (catchables.Count == 0) return null;

        return catchables[Random.Range(0, catchables.Count)];
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
            Random.Range(-throwForceMax, throwForceMax),
            Random.Range(throwForceMin, throwForceMax) * 2,
            Random.Range(-throwForceMax, throwForceMax)
        );

        Vector3 randomTorque = new Vector3(
            Random.Range(-throwTorqueMax, throwTorqueMax),
            Random.Range(-throwTorqueMax, throwTorqueMax),
            Random.Range(-throwTorqueMax, throwTorqueMax)
        );

        objectToThrow.ApplyForce(randomForce, randomTorque);
        ActivateActivitySource(objectToThrow.activitySource);
    }

    public void ImproveAngriness(float percentageToAdd)
    {
        angrinessPercentage += percentageToAdd;
        if (angrinessPercentage > 100)
        {
            angrinessPercentage = 100;
        }
    }

    public void DecreaseAngriness(float percentageToDecrease)
    {
        if (angrinessPercentage < 0)
        {
            angrinessPercentage = 0;
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

    public bool IsHunting()
    {
        return currentState == GhostState.huntingState || currentState == GhostState.standingState || _willHunt;
    }

    public void LockGhost()
    {
        _isLocked = true;
    }
    
    public bool IsLocked() => _isLocked;
}