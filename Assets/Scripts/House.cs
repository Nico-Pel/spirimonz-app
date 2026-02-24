using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class House : GameBehaviour
{
    public static House Instance { get; private set; }
    public HouseMap map;
       
# if UNITY_EDITOR
    public bool useDebugs;
    public bool playerCantDie;
    public GhostParameters forcedGhostParameters;
    public Ghost.GhostActivities forcedGhostActivity = Ghost.GhostActivities.Nothing;
    public int forcedFavoriteRoomID = -1;
    public bool tripleActivityDebug;
    public bool useHuntTimeMultiplierDebug;
    public float huntTimeMultiplierDebug = 10f;
#endif
    
    [Space]

    public HouseEntry houseEntry;
    
    public Room[] rooms;
    
#if UNITY_EDITOR
    public void BakeRoomsCount()
    {
        if (map == null) return;

        map.roomsNumber = rooms.Length;
        EditorUtility.SetDirty(map);
        Debug.Log($"Baked {rooms.Length} rooms into {map.name}");
    }
#endif
    
    public Room[] hauntableRooms;
    public bool electricCurrentEnabled = true;

    public float averageStartTemperature = 17;
    public float temperatureMaxRoomVariation = 3.5f;
    public float temperatureMaxHouseVariation = 3.5f;

    [ReadOnly] public Ghost currentGhost;
    
    public Player currentPlayer;
    
    public List<WayPoint> wayPoints = new List<WayPoint>();

    private void Awake()
    {
        Instance = this;
        
        #if UNITY_EDITOR
        CheckIgnoreDebugs();
        #endif
        
        InitializeHouse();
    }

    private void CheckIgnoreDebugs()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        if (gameManager != null)
        {
            if (gameManager.ignoreAllHouseDebugs)
                useDebugs = false;
        }
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
        currentGhost = Instantiate(map.possibleGhosts[Random.Range(0, map.possibleGhosts.Length)]);
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

    public SpirimonzSettings GetSpirimonzSettings()
    {
        return map.linkedHouseBiome.GetCapturedSpirimonz(currentGhost.ghostParameters.ghostTypeData.ghostType);
    }
    
    public void ExpelPlayerFromHouse()
    {
        this.Invoke(6, () =>
        {
            UIGame.Instance.OpenEndGame(UIEndGame.EndTypes.Lose, this);
        });
    }
}