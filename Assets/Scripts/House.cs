using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class House : MonoBehaviour
{
    public static House Instance { get; private set; }

    public Room[] rooms;
    public Room[] hauntableRooms;
    public bool electricCurrentEnabled = true;

    public float averageStartTemperature = 17;
    public float temperatureMaxRoomVariation = 3.5f;
    public float temperatureMaxHouseVariation = 3.5f;

    public Ghost[] possibleGhosts;
    public Ghost currentGhost;
    
    public Player currentPlayer;
    
    public List<WayPoint> wayPoints = new List<WayPoint>();

    private void Awake()
    {
        Instance = this;
        InitializeHouse();
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
}
