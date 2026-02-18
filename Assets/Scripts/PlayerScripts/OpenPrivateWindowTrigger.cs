using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class OpenPrivateWindowTrigger : GameBehaviour
{
    public int windowID;
    private UIGame _uiGame;

    private void Start()
    {
        _uiGame = UIGame.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OpenWindow();
        }
    }

    private void OpenWindow()
    {
        if (_uiGame == null) return;
        
        _uiGame.OpenPrivateTabletWindow(windowID);
    }
}