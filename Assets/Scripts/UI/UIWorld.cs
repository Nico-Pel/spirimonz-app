using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWorld : UIManager
{
    public static UIWorld Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        
        UIGame.Instance.EnableOverlay(true, 0);
        UIGame.Instance.EnableOverlay(false, 1);
    }
}