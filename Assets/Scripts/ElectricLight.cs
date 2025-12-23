using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RendererGroup
{
    public MeshRenderer[] renderers;
    public Material materialOff;
    public Material materialOn;
}

public class ElectricLight : ActivableObject
{
    [Header("Objects to enable")]
    public GameObject[] objectsToEnable;

    [Header("Renderer groups")]
    public RendererGroup[] rendererGroups;

    public override void Activate()
    {
        base.Activate();

        // Appliquer les matériaux On
        foreach (RendererGroup group in rendererGroups)
        {
            foreach (MeshRenderer mr in group.renderers)
            {
                mr.material = group.materialOn;
            }
        }

        // Activer les objets
        foreach (GameObject g in objectsToEnable)
        {
            g.SetActive(true);
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();

        // Appliquer les matériaux Off
        foreach (RendererGroup group in rendererGroups)
        {
            foreach (MeshRenderer mr in group.renderers)
            {
                mr.material = group.materialOff;
            }
        }

        // Désactiver les objets
        foreach (GameObject g in objectsToEnable)
        {
            g.SetActive(false);
        }
    }
}