using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCursor : GameBehaviour
{
    private void OnEnable()
    {
        UIGame.Instance.AddShowCursor();
    }

    private void OnDisable()
    {
        UIGame.Instance.RemoveShowCursor();
    }
}
