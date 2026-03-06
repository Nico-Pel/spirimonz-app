using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : GameBehaviour
{
    public static InputManager Instance { get; private set; }

    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode turnLight = KeyCode.T;
    public KeyCode grabObject = KeyCode.E;
    public KeyCode dropObject = KeyCode.D;
    public KeyCode throwObject = KeyCode.G;
    public KeyCode openJournal = KeyCode.J;
    public KeyCode openTeamMenu = KeyCode.Tab;
    public KeyCode exitMenus = KeyCode.Escape;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode primaryNext = KeyCode.RightArrow;
    public KeyCode primaryPrevious = KeyCode.LeftArrow;
    public KeyCode secondaryNext = KeyCode.E;
    public KeyCode secondaryPrevious = KeyCode.A;
    public KeyCode[] inventoryKeys = new KeyCode[6];
    public KeyCode worldInteractions = KeyCode.E;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        Instance = this;
    }
}