using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class House : GameBehaviour
{
    public static House Instance { get; private set; }
    
       
# if UNITY_EDITOR
    public GhostParameters forcedGhostParameters;
    public Ghost.GhostActivities forcedGhostActivity = Ghost.GhostActivities.Nothing;
    public int forcedFavoriteRoomID = -1;
    public bool tripleActivityDebug;
    public bool useHuntTimeMultiplierDebug;
    public float huntTimeMultiplierDebug = 10f;
#endif
    
    [Space]

    public HouseBiome biome;
    public GhostParameters[] possibleGhostParameters;
    
    public Room[] rooms;
    public Room[] hauntableRooms;
    public bool electricCurrentEnabled = true;

    public float averageStartTemperature = 17;
    public float temperatureMaxRoomVariation = 3.5f;
    public float temperatureMaxHouseVariation = 3.5f;

    public Ghost[] possibleGhosts;
    [ReadOnly] public Ghost currentGhost;
    
    public Player currentPlayer;
    
    public List<WayPoint> wayPoints = new List<WayPoint>();

    private void Awake()
    {
        Instance = this;
        InitializeHouse();
    }

    private void Start()
    {
        UIGame.Instance.EnableOverlay(true, 0);
        UIGame.Instance.EnableOverlay(false, 3);
    }

    private void InitializeHouse()
    {
        foreach (WayPoint wp in this.GetComponentsInChildren<WayPoint>())
        {
            wayPoints.Add(wp);
        }
        
        InstantiateGhost();

        //Change the average start temperature for the rooms
        averageStartTemperature += Random.Range(-temperatureMaxHouseVariation, temperatureMaxHouseVariation);
        foreach (Room r in rooms)
        {
            r.Initialize(this);
        }
    }

    public void InstantiateGhost()
    {
        currentGhost = Instantiate(possibleGhosts[Random.Range(0, possibleGhosts.Length)]);
        currentGhost.Initialize(this);
    }

    public WayPoint SelectRandomWayPointFromARoom(Room room)
    {
        List<WayPoint> selectableWayPoints = new List<WayPoint>();

        foreach (WayPoint w in wayPoints)
        {
            if (w.linkedRoom == room)
                selectableWayPoints.Add(w);
        }

        if (selectableWayPoints.Count == 0)
        {
            Debug.LogWarning("Aucun WayPoint disponible dans cette salle !");
            return null;
        }

        return selectableWayPoints[Random.Range(0, selectableWayPoints.Count)];
    }

    public WayPoint SelectRandomWaypointFurthestFromPosition(Vector3 pos, int nbOfRandomPossibilities)
    {
        List<WayPoint> selectableWayPoints = new List<WayPoint>();
        
        while (selectableWayPoints.Count < nbOfRandomPossibilities || selectableWayPoints.Count == wayPoints.Count - 1 || /*ERROR*/wayPoints.Count == 0)
        {
            List<WayPoint> waypointsToTest = wayPoints;
            waypointsToTest.RemoveAll(swp => selectableWayPoints.Contains(swp));

            WayPoint furthestWayPoint = null;
            float currentBestDist = 0;
            foreach (WayPoint w in waypointsToTest)
            {
                float dist = Vector3.Distance(w.transform.position, pos);
                if (dist > currentBestDist)
                {
                    furthestWayPoint = w;
                    currentBestDist = dist;
                }
            }
            if (furthestWayPoint == null) break;
            selectableWayPoints.Add(furthestWayPoint);
        }
        return selectableWayPoints[Random.Range(0, selectableWayPoints.Count)];
    }
    
    public WayPoint SelectRandomWaypointFurthestFromPosition(NavMeshAgent agent, int nbOfRandomPossibilities)
    {
        List<WayPoint> selectableWayPoints = new List<WayPoint>();
        
        while (selectableWayPoints.Count < nbOfRandomPossibilities || selectableWayPoints.Count == wayPoints.Count - 1 || /*ERROR*/wayPoints.Count == 0)
        {
            List<WayPoint> waypointsToTest = wayPoints;
            waypointsToTest.RemoveAll(swp => selectableWayPoints.Contains(swp));

            WayPoint furthestWayPoint = null;
            float currentBestDist = 0;
            foreach (WayPoint w in waypointsToTest)
            {
                float dist = PathDistanceForAnAgent(agent, w.transform.position);
                if (dist > currentBestDist)
                {
                    furthestWayPoint = w;
                    currentBestDist = dist;
                }
            }
            if (furthestWayPoint == null) break;
            selectableWayPoints.Add(furthestWayPoint);
        }
        return selectableWayPoints[Random.Range(0, selectableWayPoints.Count)];
    }

    public Spirimonz GetSpirimonzPrefab()
    {
        return biome.GetSpirimonzPrefab(currentGhost.ghostParameters.ghostTypeData.ghostType);
    }
}
