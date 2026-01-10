using UnityEngine;
using UnityEditor;

public class FindMissingComponents : EditorWindow
{
    [MenuItem("Tools/Find Missing Components")]
    static void Init()
    {
        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.Log($"Missing component on GameObject: {go.name}", go);
                }
            }
        }
    }
}