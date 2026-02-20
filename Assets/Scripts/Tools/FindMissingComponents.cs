using UnityEngine;
using UnityEditor;

public class FindMissingComponents : EditorWindow
{
    [MenuItem("Tools/Find Missing Components")]
    static void Init()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        int missingCount = 0;

        foreach (GameObject go in allObjects)
        {
            // Skip assets
            if (EditorUtility.IsPersistent(go))
                continue;

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.Log($"Missing component on GameObject: {go.name}", go);
                    missingCount++;
                }
            }
        }

        if (missingCount == 0)
            Debug.Log("No missing components found!");
        else
            Debug.Log($"Found {missingCount} missing components.");
    }
}