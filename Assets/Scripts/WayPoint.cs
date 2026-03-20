using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    public Room linkedRoom;

    private void Awake()
    {
        if(linkedRoom == null)
            linkedRoom = GetComponentInParent<Room>();
    }
}
