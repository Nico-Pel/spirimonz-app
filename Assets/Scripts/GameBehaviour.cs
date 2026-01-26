using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameBehaviour : MonoBehaviour
{
    private Dictionary<string, Coroutine> _invokes = new Dictionary<string, Coroutine>();

    // Invoke avec nom optionnel
    public void Invoke(float delay, Action action)
    {
        // Générer un nom unique pour cet invoke interne (on ne pourra pas l'annuler de l'extérieur)
        string uniqueName = Guid.NewGuid().ToString();
        Invoke(uniqueName, delay, action);
    }

    // Invoke avec nom fourni
    public void Invoke(string name, float delay, Action action)
    {
        // Si un invoke du même nom existe, on l'annule
        CancelInvoke(name);

        Coroutine coroutine = StartCoroutine(InvokeCoroutine(delay, action, name));
        _invokes[name] = coroutine;
    }

    private IEnumerator InvokeCoroutine(float delay, Action action, string name)
    {
        yield return new WaitForSeconds(delay);
        _invokes.Remove(name); // Supprime après exécution
        action?.Invoke();
    }

    public void CancelInvoke(string name)
    {
        if (_invokes.TryGetValue(name, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
            _invokes.Remove(name);
        }
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

    public float PathDistanceForAnAgent(NavMeshAgent mAgent,
        Vector3 positionToTarget,
        float sampleRadius = 5f)
    {
        if (mAgent == null || !mAgent.isOnNavMesh)
            return -1f;

        // 1. Projection de la position cible sur le NavMesh
        if (!NavMesh.SamplePosition(
                positionToTarget,
                out NavMeshHit hit,
                sampleRadius,
                NavMesh.AllAreas))
        {
            return -1f;
        }

        // 2. Calcul du path
        NavMeshPath path = new NavMeshPath();
        if (!mAgent.CalculatePath(hit.position, path))
            return -1f;

        if (path.status != NavMeshPathStatus.PathComplete)
            return -1f;

        // 3. Calcul de la longueur du chemin
        float pathLength = 0f;
        Vector3[] corners = path.corners;

        for (int i = 1; i < corners.Length; i++)
        {
            pathLength += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return pathLength;
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