using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameBehaviour : MonoBehaviour
{
    /// <summary>
    /// Exécute une action après un délai en secondes
    /// </summary>
    public void Invoke(float delay, Action action)
    {
        StartCoroutine(InvokeCoroutine(delay, action));
    }

    private IEnumerator InvokeCoroutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    
    public static int LayerMaskToLayer(LayerMask mask)
    {
        int value = mask.value;

        if (value == 0 || (value & (value - 1)) != 0)
        {
            Debug.LogError("LayerMask must contain exactly ONE layer");
            return -1;
        }

        return Mathf.RoundToInt(Mathf.Log(value, 2));
    }
    
    public void ChangeLayer(LayerMask mask, int ignoredLayerIndex = -1)
    {
        int layer = LayerMaskToLayer(mask);
        if (layer == -1) return;

        ApplyLayerRecursively(gameObject, layer, ignoredLayerIndex);
    }
    
    public void ChangeLayer(int layerIndex, int ignoredLayerIndex = -1)
    {
        if (layerIndex == -1) return;

        ApplyLayerRecursively(gameObject, layerIndex, ignoredLayerIndex);
    }
    
    private void ApplyLayerRecursively(GameObject obj, int layer, int ignoreLayerIndex = -1)
    {
        if (layer < 0 || layer > 31)
        {
            Debug.LogError($"Invalid layer index: {layer}");
            return;
        }

        if ((int)obj.layer != ignoreLayerIndex)
        {
            obj.layer = layer;
        }

        foreach (Transform child in obj.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer, ignoreLayerIndex);
        }
    }
    
    public bool IsNearFromMyAgent(
        NavMeshAgent mAgent,
        Transform mTransform,
        float maxPathDistance = 10f,
        float sampleRadius = 5f)
    {
        // 1. Projection sur le NavMesh
        if (!NavMesh.SamplePosition(
                mTransform.position,
                out NavMeshHit hit,
                sampleRadius,
                NavMesh.AllAreas))
        {
            return false;
        }

        // 2. Calcul du path
        NavMeshPath path = new NavMeshPath();
        if (!mAgent.CalculatePath(hit.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        // 3. Calcul de la longueur du chemin
        float pathLength = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        // 4. Comparaison
        return pathLength <= maxPathDistance;
    }
}

#region ReadOnly Attribute

// Attribut à mettre sur n'importe quelle variable pour la rendre non modifiable dans l'Inspector
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // désactive le champ
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;  // réactive pour les autres champs
    }
}
#endif

#endregion