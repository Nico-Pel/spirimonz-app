using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Ghost : GameBehaviour
{
   public GhostParameters ghostParameters;
    # if UNITY_EDITOR
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
    
    [Header("Ghost Prints")]
    public float sprintDurationMin = 6f;
    public float sprintDurationMax = 15f;
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
    public float startHuntingStandingTime = 2;
    public float forcedStartTargetingTime = 4;
    public float averageHuntTime = 10f;
    public float huntTimeVariation = 5f;

    [Header("Ghost Stats : Waypoints Hunting")]

    [ReadOnly] public float currentHuntTime;
    [ReadOnly] public WayPoint currentWayPoint;
    public List<WayPoint> huntingWayPoints = new List<WayPoint>();
    [ReadOnly] public float currentHuntingWayPointDistanceTargeted;
    public float chancesPercentageToIgnoreAWayPoint = 40f;
    public float detectPlayerActivityRange = 50f;

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
    public float openAngleMin = 35;
    public float openAngleMax = 90;
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

    private bool _forcedStartTargeting = false;
    private bool _targetingPlayer;

    [Header("Ghost Components")] 
    public MeshRenderer renderer;
    public NavMeshAgent agent;
    public GameObject ghostModel;
    public GhostVision vision;

    public void Initialize(House h)
    {
        house = h;
        favoriteRoom = house.hauntableRooms[Random.Range(0, house.hauntableRooms.Length)];
        currentRoom = favoriteRoom;

        transform.position = house.SelectRandomWayPointFromARoom(favoriteRoom).transform.position;

        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        this.Invoke(nextActivityTime, TriggerActivity);
        
        ghostModel.SetActive(false);

        if (ghostParameters.SpiritOrbs)
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
        else if (other.TryGetComponent(out Door door))
        {
            if (currentState != GhostState.huntingState /*|| door.isOpen*/)
                return;

            Vector3 directionToDoor = (door.transform.position - transform.position).normalized;
            Vector3 moveDirection = agent.velocity.normalized;

            float dot = Vector3.Dot(moveDirection, directionToDoor);

            if (dot > 0.6f) // seuil à ajuster
            {
                _stopMoving = true;
                this.Invoke(_waitDoorTime, () => _stopMoving = false);

                door.GhostDoorInteraction(
                    Random.Range(80, 100),
                    Random.Range(80, 100)
                );
            }
        }
    }

    public void TriggerHunting()
    {
        currentState = GhostState.standingState;
        
        float startingHuntDelay = DivideByPercentage(startHuntingStandingTime, angrinessPercentage);
        this.Invoke(startingHuntDelay, StartHunting);
    }

    private void StartHunting()
    {
        InitWayPoints();
        
        currentState = GhostState.huntingState;

        //Le fantôme va forcément aller vers le joueur durant cette période.
        _forcedStartTargeting = true;
        this.Invoke(forcedStartTargetingTime, () => _forcedStartTargeting = false);
        _targetingPlayer = true;

        currentHuntTime = Random.Range(averageHuntTime - huntTimeVariation, averageHuntTime + huntTimeVariation);
        Debug.Log("Starting a HUNT for: " + currentHuntTime + " seconds");

        ghostModel.SetActive(true);
        
        SetVisibleRenderer(true);
    }

    private void Update()
    {
        if (currentState == GhostState.huntingState)
        {
            currentHuntTime -= Time.deltaTime;
            if (currentHuntTime <= 0)
            {
                StopHunting();
            }

            if (!_forcedStartTargeting && !vision.CanSeePlayer(house.currentPlayer))
            {
                _targetingPlayer = false;
            }
            else if(_forcedStartTargeting || vision.CanSeePlayer(house.currentPlayer))
            {
                _targetingPlayer = true;
            }
            
            SetHuntingDestination();
            if (_stopMoving)
            {
                agent.speed = 0;
            }
            else
            {
                agent.speed = currentState == GhostState.huntingState && vision.CanSeePlayer(house.currentPlayer) ? targetingSpeedBase : normalSpeedBase;
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
                TriggerHunting();
            }
            else if (currentState == GhostState.huntingState)
            {
                StopHunting();
            }
        }
        #endif
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
        }
        else
        {
            SelectNewHidingWaypoint();
        }

        float dist = Vector3.Distance(transform.position, currentWayPoint.transform.position);
        if (dist < 1)
        {
            //if ghost come back after a hunt, its speed become slow again
            agent.speed = hidingSpeedBase;
            
            SelectNewHidingWaypoint();
        }
    }

    public void StopHunting()
    {
        currentState = GhostState.hideState;
        ghostModel.SetActive(false);

        //Ghost go back to its room
        agent.speed = normalSpeedBase;
        
        float nextActivityTime = Random.Range(averageActivityTime - activityTimeVariation, averageActivityTime + activityTimeVariation);
        this.Invoke(nextActivityTime, TriggerActivity);
    }

    private void Kill(Player player)
    {
        player.Die();
    }

    private void SetVisibleRenderer(bool enable)
    {
        if (currentState == GhostState.huntingState || currentState == GhostState.standingState)
        {
            renderer.enabled = enable;
            float averageChangeTime = enable == true ? averageVisibleTime : averageInvisibleTime;
            float changeTimeVariation = enable == true ? visibleTimeVariation : invisibleTimeVariation;
            float nextChange = Random.Range(averageChangeTime - changeTimeVariation, averageChangeTime + changeTimeVariation);
            this.Invoke(nextChange, () => SetVisibleRenderer(!enable));
        }
        else
        {
            //Stop the loop
            renderer.enabled = true;
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
                if (angrinessPercentage >= minimumAngrinessToHunt && 
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

            default:
                Debug.Log("Activity triggered : Nothing " + Time.time);
                nextActivityTime = nextActivityTime / 2;
                break;
        }
        
        this.Invoke(nextActivityTime, TriggerActivity);
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

        if (ghostParameters.SpiritPrints)
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
        float duration = Random.Range(sprintDurationMin, sprintDurationMax);
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
        
        if (ghostParameters.SpiritPrints)
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
            InitWayPoints();
        
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

    private ThrowableObject GetRandomThrowableObjects()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            throwDetectionRange,
            throwableMask
        );

        List<ThrowableObject> throwables = new List<ThrowableObject>();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out ThrowableObject throwable))
            {
                //Ignore objects from another stair
                if (throwable.transform.position.y - transform.position.y > 3 || throwable.transform.position.y - transform.position.y < -3) continue;
                if(!throwable.isGrabbed)
                    throwables.Add(throwable);
            }
        }

        if (throwables.Count == 0) return null;

        return throwables[Random.Range(0, throwables.Count)];
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
                //Ignore doors from another stair
                if (door.transform.position.y - transform.position.y > 3 || door.transform.position.y - transform.position.y < -3) continue;
                
                if(!door.IsGrabbed())
                    doors.Add(door);
            }
        }

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
    }

    public void ThrowObject(ThrowableObject objectToThrow = null)
    {
        if (objectToThrow == null) objectToThrow = GetRandomThrowableObjects();

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
}