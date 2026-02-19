using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : GameBehaviour
{
    public static World Instance { get; private set; }

    public string worldName;
    public Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }
}