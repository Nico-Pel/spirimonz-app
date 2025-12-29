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
    public enum GhostType
    {
        Blazing, //Flamboyant
        Totemic, //Totémique
        Aquatic, //Aqueux
        Glacial, //Glacial
        Misty, //Brumeux
        Demonic, //Démoniaque
        Runic, //Runique
        Grumpy, //Grognon
        Trickster, //Farceur
        Weird, //Bizarre
        Draconic, //Draconique
        Earthbound, //Téllurique
        Psychic, //Psychique
        Striker, //Frappeur
        Voltaic, //Voltaïque
        Luminous //Lumineux
    }

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
        Nothing
    }
    
    public GhostType ghostType;
    public GhostState currentState;

    public House house;
    public Room favoriteRoom;
    public Room currentRoom;
    
    [Header("Ghost Stats : Speed")] 
    public float hidingSpeedBase = 0.75f;
    public float normalSpeedBase = 1f;
    public float targetingSpeedBase = 2f;

    [Header("Ghost Stats : Hunting")] 
    public float angrinessPercentage = 0f;
    public float minimumAngrinessToHunt = 50;
    
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
    public float throwForceMin = 10;
    public float throwForceMax = 25;
    public float throwTorqueMax = 90;
    public LayerMask throwableMask;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room newRoom))
        {
            currentRoom = newRoom;
        }else if (other.TryGetComponent(out Player touchedPlayer))
        {
            if (currentState != GhostState.huntingState) return;
            
            StopHunting();
            Kill(touchedPlayer);
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

        ghostModel.SetActive(true);
        
        SetVisibleRenderer(true);
    }

    private void Update()
    {
        if (currentState == GhostState.huntingState)
        {
            currentHuntTime -= Time.deltaTime;
            if (currentHuntTime == 0)
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
            agent.speed = currentState == GhostState.huntingState && vision.CanSeePlayer(house.currentPlayer) ? targetingSpeedBase : normalSpeedBase;
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
                ThrowRandomObject();
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
                if (angrinessPercentage >= minimumAngrinessToHunt)
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

            default:
                Debug.Log("Activity triggered : Nothing " + Time.time);
                nextActivityTime = nextActivityTime / 2;
                break;
        }
        
        this.Invoke(nextActivityTime, TriggerActivity);
    }

    private void TriggerElectronicLightActivity()
    {
        Switch lightObject = currentRoom.SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType.electronicLight);
        
        //No object found, throw an object instead
        if (lightObject == null)
        {
            ThrowRandomObject();
            return;
        }
        
        if (ghostType == GhostType.Demonic || ghostType == GhostType.Totemic)
        {
            lightObject.activableObject.Deactivate();
        }
        else if (ghostType == GhostType.Luminous || ghostType == GhostType.Voltaic)
        {
            lightObject.activableObject.Activate();
        }
        else
        {
            lightObject.activableObject.Operate();
        }
    }
    
    private void TriggerElectronicObjectActivity()
    {
        Switch elecObject = currentRoom.SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType.electronicObject);
        
        //No object found, throw an object instead
        if (elecObject == null)
        {
            ThrowRandomObject();
            return;
        }
        
        if (ghostType == GhostType.Totemic)
        {
            elecObject.activableObject.Deactivate();
        }
        else if (ghostType == GhostType.Voltaic)
        {
            elecObject.activableObject.Activate();
        }
        else
        {
            elecObject.activableObject.Operate();
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
                throwables.Add(throwable);
            }
        }

        if (throwables.Count == 0) return null;

        return throwables[Random.Range(0, throwables.Count)];
    }

    private void ThrowRandomObject()
    {
        float randomForceX = Random.Range(-throwForceMax, throwForceMax);
        float randomForceY = Random.Range(throwForceMin, throwForceMax) * 2;
        float randomForceZ = Random.Range(-throwForceMax, throwForceMax);
        
        float randomTorqueX = Random.Range(-throwTorqueMax, throwTorqueMax);
        float randomTorqueY = Random.Range(-throwTorqueMax, throwTorqueMax);
        float randomTorqueZ = Random.Range(-throwTorqueMax, throwTorqueMax);
        
        ThrowableObject objectToThrow = GetRandomThrowableObjects();

        if (objectToThrow == null)
        {
            Debug.Log("Activity : ThrowObject Failed");
            return;
        }
        
        Debug.Log("Activity : ThrowObject Success " + objectToThrow);

        objectToThrow.rb.isKinematic = false;
        objectToThrow.rb.AddForce(new Vector3(randomForceX,randomForceY,randomForceZ));
        objectToThrow.rb.AddTorque(new Vector3(randomTorqueX,randomTorqueY,randomTorqueZ));
    }
}