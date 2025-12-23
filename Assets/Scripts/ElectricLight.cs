using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricLight : ActivableObject
{
    public MeshRenderer[] lightObjectsRenderers;
    public GameObject[] lightObjects;
    public Material lightObjectMatOff;
    public Material lightObjectMatOn;

    public override void Activate()
    {
        base.Activate();
        foreach (MeshRenderer mr in lightObjectsRenderers)
        {
            mr.material = lightObjectMatOn;
        }
        foreach (GameObject g in lightObjects)
        {
            g.SetActive(true);
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        foreach (MeshRenderer mr in lightObjectsRenderers)
        {
            mr.material = lightObjectMatOff;
        }
        foreach (GameObject g in lightObjects)
        {
            g.SetActive(false);
        }
    }
}