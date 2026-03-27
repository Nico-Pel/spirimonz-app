using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectionGhostInteraction : GameBehaviour
{
    public Spirimonz linkedSpirimonz;

    [Header("Doors settings")]
    public List<Door> doors = new List<Door>();
    public UnityEvent onDoorInteractions;
    public float interactionDelay = 1f;
    public float interactionRangeMax = 20f;

    private void Awake()
    {
        doors.AddRange(FindObjectsOfType<Door>());
        foreach (Door door in doors)
        {
            door.onGhostInteracted.AddListener(DoorInteractions);
        }
    }

    private void DoorInteractions(Door door)
    {
        if (linkedSpirimonz != null)
        {
            if (linkedSpirimonz.isOnTheMap == false && linkedSpirimonz.powerActiveInHands == false) return;
            if (linkedSpirimonz.spirimonzGameObject.activeInHierarchy == false) return;
            float dist = Vector3.Distance(door.transform.position, linkedSpirimonz.transform.position);
            if (dist > interactionRangeMax) return;
        }
        
        this.Invoke(interactionDelay, () =>
        {
            onDoorInteractions?.Invoke();
        });
    }
}