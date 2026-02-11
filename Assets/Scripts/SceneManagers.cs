using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManagers : GameBehaviour
{
    private void Awake()
    {
        foreach (Transform t in transform.GetComponentsInChildren<Transform>())
        {
            if (t == transform) continue;
            t.parent = null;
        }
    }
}