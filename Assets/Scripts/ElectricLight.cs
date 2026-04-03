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
                ApplyMaterialSwap(mr, group.materialOff, group.materialOn);
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
                ApplyMaterialSwap(mr, group.materialOn, group.materialOff);
            }
        }

        // Désactiver les objets
        foreach (GameObject g in objectsToEnable)
        {
            g.SetActive(false);
        }
    }

    private void ApplyMaterialSwap(MeshRenderer renderer, Material fromMaterial, Material toMaterial)
    {
        if (renderer == null || fromMaterial == null || toMaterial == null)
            return;

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            return;

        bool found = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != fromMaterial)
                continue;

            materials[i] = toMaterial;
            found = true;
        }

        if (found)
        {
            renderer.sharedMaterials = materials;
        }
        else
        {
            Debug.LogWarning($"ElectricLight: material '{fromMaterial.name}' not found on renderer '{renderer.name}'.", renderer);
        }
    }
}
