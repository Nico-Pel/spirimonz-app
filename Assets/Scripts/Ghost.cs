using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

    [Header("Ghost Stats")] 
    public float angrinessPercentage = 0f;
    public float minimumAngrinessToHunt = 50;
    public float hidingSpeedBase = 0.75f;
    public float normalSpeedBase = 1f;
    public float targetingSpeedBase = 2f;

    public float detectPlayerActivityRange = 20f;

    public float averageActivityTime = 60f;
    public float activityTimeVariation = 10f;
    
    public float averageHuntTime = 10f;
    public float huntTimeVariation = 5f;

    public float averageVisibleTime = 1;
    public float visibleTimeVariation = 0.5f;
    
    public float averageInvisibleTime = 1;
    public float invisibleTimeVariation = 0.5f;

    public float startHuntingStandingTime = 2;
    public float forcedStartTargetingTime = 4;
    private bool _forcedStartTargeting = false;
    
    public float currentHuntTime { get; set; }
    public List<WayPoint> huntingWayPoints = new List<WayPoint>();
    public WayPoint currentWayPoint { get; set; }
    public float currentHuntingWayPointDistanceTargeted { get; set; }
    public float chancesPercentageToIgnoreAWayPoint = 40f;
    public float chancesToRoamInAnotherRoom = 25f;
    private bool _targetingPlayer;

    [Header("Ghost Components")] 
    public MeshRenderer renderer;
    public NavMeshAgent agent;
    public GameObject ghostModel;
    public Collider collider;
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
        
        collider.enabled = false;
        float startingHuntDelay = DivideByPercentage(startHuntingStandingTime, angrinessPercentage);
        this.Invoke(startingHuntDelay, StartHunting);
    }

    private void StartHunting()
    {
        InitWayPoints();
        
        currentState = GhostState.huntingState;
        collider.enabled = true;

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
            else if(vision.CanSeePlayer(house.currentPlayer))
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
        if (dist < 0.5f)
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
                break;

            case GhostActivities.ChangeLightState:
                TriggerElectronicLightActivity();
                break;

            case GhostActivities.ChangeElectronicObjectState:
                TriggerElectronicObjectActivity();
                break;
            
            case GhostActivities.Hunt:
                if (angrinessPercentage > minimumAngrinessToHunt)
                {
                    TriggerHunting();
                }
                else
                {
                    TriggerActivity();
                }
                break;

            default:
                nextActivityTime = nextActivityTime / 2;
                break;
        }
        
        this.Invoke(nextActivityTime, TriggerActivity);
    }

    private void TriggerElectronicLightActivity()
    {
        Switch lightObject = currentRoom.SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType.electronicLight);
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
}
