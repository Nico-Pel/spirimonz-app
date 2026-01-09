using System;
using System.Collections;
using UnityEngine;

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
    
    public void ChangeLayer(LayerMask mask)
    {
        int layer = LayerMaskToLayer(mask);
        if (layer == -1) return;

        ApplyLayerRecursively(gameObject, layer);
    }
    
    public void ChangeLayer(int layerIndex)
    {
        if (layerIndex == -1) return;

        ApplyLayerRecursively(gameObject, layerIndex);
    }
    
    private void ApplyLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0 || layer > 31)
        {
            Debug.LogError($"Invalid layer index: {layer}");
            return;
        }

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
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