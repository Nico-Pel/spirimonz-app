using System;
using UnityEngine;

public class Door : GameBehaviour
{
    [Header("Door Components")]
    public HingeJoint hingeJoint;
    public Rigidbody rb;
    
    [Header("Door Options")]
    public bool isOpen = false;
}