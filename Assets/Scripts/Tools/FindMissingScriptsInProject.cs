#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingScriptsInProject : EditorWindow
{
    private Vector2 scrollPos;
    private List<Object> missingAssets = new List<Object>();

    [MenuItem("Tools/Find Missing Scripts in Project")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScriptsInProject>("Missing Scripts Finder");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Project for Missing Scripts"))
        {
            ScanProject();
        }

        GUILayout.Space(10);
        GUILayout.Label($"Found {missingAssets.Count} assets with missing scripts", EditorStyles.boldLabel);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
        foreach (var asset in missingAssets)
        {
            if (GUILayout.Button(AssetDatabase.GetAssetPath(asset), EditorStyles.label))
            {
                Selection.activeObject = asset; // sélectionne l'asset dans le Project
            }
        }
        GUILayout.EndScrollView();
    }

    private void ScanProject()
    {
        missingAssets.Clear();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject");

        List<Object> allAssets = new List<Object>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) allAssets.Add(prefab);
        }

        foreach (string guid in soGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so != null) allAssets.Add(so);
        }

        foreach (Object asset in allAssets)
        {
            GameObject go = asset as GameObject;
            bool hasMissing = false;

            if (go != null)
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        hasMissing = true;
                        break;
                    }
                }
            }
            else
            {
                // Pour ScriptableObjects, on peut détecter si le SerializedObject échoue
                try
                {
                    SerializedObject so = new SerializedObject(asset);
                    if (so == null)
                    {
                        hasMissing = true;
                    }
                }
                catch
                {
                    hasMissing = true;
                }
            }

            if (hasMissing)
            {
                missingAssets.Add(asset);
                Debug.Log($"Missing script detected: {AssetDatabase.GetAssetPath(asset)}", asset);
            }
        }

        Debug.Log($"Scan complete. Found {missingAssets.Count} assets with missing scripts.");
    }
}
#endif
