using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float sanity = 100f;
    public Room currentRoom;
    public House house;

    public Transform head;
    public Transform body;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room room))
        {
            currentRoom = room;
        }
    }

    public void AlertTheHuntingGhost()
    {
        if (house.currentGhost == null || house.currentGhost.currentState != Ghost.GhostState.huntingState)
        {
            return;
        }
        
        float distance = Vector3.Distance(this.transform.position, house.currentGhost.transform.position);
        if (distance < house.currentGhost.detectPlayerActivityRange)
        {
            house.currentGhost.ForceNewWaypoint(currentRoom);
        }
    }

    public void Die()
    {
        //Game Over!
    }
}