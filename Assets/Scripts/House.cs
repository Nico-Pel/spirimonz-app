using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class House : MonoBehaviour
{
    public Room[] rooms;
    public Room[] hauntableRooms;
    public bool electricCurrentEnabled = true;

    public Ghost[] possibleGhosts;
    public Ghost currentGhost;
    
    public Player currentPlayer;
    
    public List<WayPoint> wayPoints = new List<WayPoint>();

    private void Awake()
    {
        InitializeHouse();
    }

    private void InitializeHouse()
    {
        foreach (WayPoint wp in this.GetComponentsInChildren<WayPoint>())
        {
            wayPoints.Add(wp);
        }
        
        foreach (Room r in rooms)
        {
            r.Initialize(this);
        }
        
        InstantiateGhost();
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
