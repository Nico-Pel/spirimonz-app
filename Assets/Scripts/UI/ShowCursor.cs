using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCursor : GameBehaviour
{
    private UIGame _uiGame;
    
    private void OnEnable()
    {
        if(_uiGame == null)
            _uiGame = UIGame.Instance;
        
        if(_uiGame != null)
            _uiGame.AddShowCursor();
    }

    private void OnDisable()
    {
        if(_uiGame != null)
            _uiGame.RemoveShowCursor();
    }
}
