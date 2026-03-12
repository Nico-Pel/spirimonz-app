using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCMovingPoint : GameBehaviour
{
    public NPCMovingPoint[] linkedPoints;

    public NPCMovingPoint SelectNextMovingPoint(NPC npc)
    {
        List<NPCMovingPoint> possibleWaypoints = new List<NPCMovingPoint>();
        possibleWaypoints.AddRange(linkedPoints);

        NPCMovingPoint lastNpcMovingPoint = npc.GetLastMovingPoint();
        if (lastNpcMovingPoint != null && possibleWaypoints.Contains(lastNpcMovingPoint))
        {
            possibleWaypoints.Remove(lastNpcMovingPoint);
        }
            
        NPCMovingPoint movingPointToUse = possibleWaypoints[Random.Range(0, possibleWaypoints.Count)];

        if (movingPointToUse == null)
            return lastNpcMovingPoint;

        return movingPointToUse;
    }
}