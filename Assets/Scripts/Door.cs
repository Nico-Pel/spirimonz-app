using System;
using UnityEngine;

public class Door : GameBehaviour
{
    public enum DoorType
    {
        rightDoor,
        leftDoor
    }
    
    [Header("Door Components")]
    public HingeJoint hingeJoint;
    
    [Header("Door Options")]
    public DoorType doorType;
    public bool isOpen = false;
}