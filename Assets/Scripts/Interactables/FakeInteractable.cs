using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeInteractable : GameBehaviour, IInteractable
{
    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
    }

    public void OnInteractHold()
    {
    }

    public void OnInteractEnd()
    {
    }
    
    public void SetCursor(Sprite sprite, float size = 1)
    {
        SpecialCursor = sprite;
        CursorSize = size;
    }
}