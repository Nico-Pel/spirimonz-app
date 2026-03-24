using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoGenerator : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    
    public Material[] materials;
    public Mesh[] meshes;

    [SerializeField] private int currentMaterialIndex;
    [SerializeField] private int currentMeshIndex;

    public int CurrentMaterialIndex => currentMaterialIndex;
    public int CurrentMeshIndex => currentMeshIndex;

    public void StepNext()
    {
        if (!HasValidSelection())
        {
            return;
        }

        currentMeshIndex++;
        if (currentMeshIndex >= meshes.Length)
        {
            currentMeshIndex = 0;
            currentMaterialIndex++;
            if (currentMaterialIndex >= materials.Length)
            {
                currentMaterialIndex = 0;
            }
        }

        ApplySelection();
    }

    public void StepPrevious()
    {
        if (!HasValidSelection())
        {
            return;
        }

        currentMeshIndex--;
        if (currentMeshIndex < 0)
        {
            currentMeshIndex = meshes.Length - 1;
            currentMaterialIndex--;
            if (currentMaterialIndex < 0)
            {
                currentMaterialIndex = materials.Length - 1;
            }
        }

        ApplySelection();
    }

    public void ApplySelection()
    {
        ClampIndices();

        if (meshRenderer == null)
        {
            return;
        }

        if (meshes != null && meshes.Length > 0)
        {
            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = meshes[currentMeshIndex];
            }
        }

        if (materials != null && materials.Length > 0)
        {
            var sharedMaterials = meshRenderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                meshRenderer.sharedMaterials = new[] { materials[currentMaterialIndex] };
            }
            else
            {
                sharedMaterials[0] = materials[currentMaterialIndex];
                meshRenderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    private void OnValidate()
    {
        ApplySelection();
    }

    private void ClampIndices()
    {
        if (meshes != null && meshes.Length > 0)
        {
            currentMeshIndex = Mathf.Clamp(currentMeshIndex, 0, meshes.Length - 1);
        }
        else
        {
            currentMeshIndex = 0;
        }

        if (materials != null && materials.Length > 0)
        {
            currentMaterialIndex = Mathf.Clamp(currentMaterialIndex, 0, materials.Length - 1);
        }
        else
        {
            currentMaterialIndex = 0;
        }
    }

    private bool HasValidSelection()
    {
        return meshes != null && meshes.Length > 0 && materials != null && materials.Length > 0;
    }
}
