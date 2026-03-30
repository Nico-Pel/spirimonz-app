#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GraphErrorMitigation
{
    private const string AutoCloseKey = "SH.GraphErrorAutoClose";
    private const string AutoCloseMigratedKey = "SH.GraphErrorAutoClose.Migrated";

    private static bool _scheduled;

    static GraphErrorMitigation()
    {
        if (!EditorPrefs.HasKey(AutoCloseMigratedKey))
        {
            EditorPrefs.SetBool(AutoCloseKey, true);
            EditorPrefs.SetBool(AutoCloseMigratedKey, true);
        }

        if (!EditorPrefs.GetBool(AutoCloseKey, true))
            return;

        Application.logMessageReceived += OnLogMessage;
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception)
            return;

        if (string.IsNullOrEmpty(stackTrace) || !stackTrace.Contains("UnityEditor.Graphs"))
            return;

        if (_scheduled)
            return;

        LogGraphSource();
        _scheduled = true;
        EditorApplication.delayCall += () =>
        {
            _scheduled = false;
            CloseAnimatorWindows();
            ClearGraphSelection();
        };
    }

    private static void LogGraphSource()
    {
        UnityEngine.Object active = Selection.activeObject != null
            ? Selection.activeObject
            : Selection.activeGameObject;

        if (active == null)
            return;

        string path = AssetDatabase.GetAssetPath(active);
        if (string.IsNullOrEmpty(path))
            path = "(scene object)";

        Debug.LogWarning($"Graph error while selection was '{active.name}' ({active.GetType().Name}) at {path}. " +
                         "This asset may contain a broken graph.");
    }

    private static void CloseAnimatorWindows()
    {
        Type animatorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.AnimatorControllerTool");
        if (animatorWindowType == null)
            return;

        UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(animatorWindowType);
        for (int i = 0; i < windows.Length; i++)
        {
            EditorWindow window = windows[i] as EditorWindow;
            if (window != null)
                window.Close();
        }
    }

    private static void ClearGraphSelection()
    {
        UnityEngine.Object active = Selection.activeObject;
        if (active == null)
            return;

        string typeName = active.GetType().FullName ?? string.Empty;
        if (typeName.Contains("UnityEditor.Animations") || typeName.Contains("Animator"))
            Selection.activeObject = null;
    }

    [MenuItem("Tools/Editor/Graph Error Mitigation")]
    private static void ToggleMitigation()
    {
        bool enabled = EditorPrefs.GetBool(AutoCloseKey, true);
        EditorPrefs.SetBool(AutoCloseKey, !enabled);
    }

    [MenuItem("Tools/Editor/Graph Error Mitigation", true)]
    private static bool ToggleMitigationValidate()
    {
        Menu.SetChecked("Tools/Editor/Graph Error Mitigation", EditorPrefs.GetBool(AutoCloseKey, true));
        return true;
    }
}
#endif
