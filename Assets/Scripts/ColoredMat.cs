using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColoredMat : MonoBehaviour
{
    public Renderer renderer;
    public Color color;

    private void Awake()
    {
        renderer.material.color = color;
    }
}