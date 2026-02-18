using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class OpenPrivateWindowTrigger : GameBehaviour
{
    public int windowID;
    private UIGame _uiGame;
    
    private float _securityTime = 1f;
    private bool _canBeTriggered;

    private void Start()
    {
        _uiGame = UIGame.Instance;
        this.Invoke(_securityTime, () => _canBeTriggered = true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_canBeTriggered == false) return;
        
        if (other.CompareTag("Player"))
        {
            _canBeTriggered = false;
            this.Invoke(_securityTime, () => _canBeTriggered = true);
            
            OpenWindow();
        }
    }

    private void OpenWindow()
    {
        if (_uiGame == null) return;
        
        _uiGame.OpenPrivateTabletWindow(windowID);
    }
}