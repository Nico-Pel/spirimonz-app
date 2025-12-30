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