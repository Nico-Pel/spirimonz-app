#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FindMissingScript : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScript>("Missing Scripts Finder");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Scene"))
        {
            FindMissingScripts();
        }
    }

    private static void FindMissingScripts()
    {
        int goCount = 0;
        int componentsCount = 0;
        int missingCount = 0;

        GameObject[] goArray = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in goArray)
        {
            goCount++;
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                componentsCount++;
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.Log($"Missing script in GameObject: {GetFullPath(go)}", go);
                }
            }
        }

        Debug.Log($"Searched {goCount} GameObjects, {componentsCount} components, found {missingCount} missing scripts.");
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            path = go.name + "/" + path;
        }
        return path;
    }
}
#endif
