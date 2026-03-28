#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectionNullCleaner
{
    private const string EnabledKey = "SH.SelectionNullCleanerEnabled";
    private const string EnabledMigratedKey = "SH.SelectionNullCleanerEnabled.Migrated";
    private const int StartupCleanupFrames = 30;
    private static int _startupCleanupFramesRemaining;

    static SelectionNullCleaner()
    {
        // Migration: disable by default (it can be aggressive and cause inspector noise).
        if (!EditorPrefs.HasKey(EnabledMigratedKey))
        {
            EditorPrefs.SetBool(EnabledKey, false);
            EditorPrefs.SetBool(EnabledMigratedKey, true);
        }

        if (!EditorPrefs.GetBool(EnabledKey, false))
            return;

        Selection.selectionChanged += CleanSelection;
        EditorApplication.hierarchyChanged += CleanSelection;
        EditorApplication.projectChanged += CleanSelection;
        EditorApplication.delayCall += CleanSelection;

        _startupCleanupFramesRemaining = StartupCleanupFrames;
        EditorApplication.update += StartupCleanup;
    }

    private static void CleanSelection()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        Object[] selection = Selection.objects;
        bool cleaned = false;

        if (selection != null && selection.Length > 0)
        {
            bool hasNull = false;
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i] == null)
                {
                    hasNull = true;
                    break;
                }
            }

            if (hasNull)
            {
                Selection.objects = selection.Where(obj => obj != null).ToArray();
                cleaned = true;
            }
        }

        bool hasNullEditors = HasNullEditors();
        if (cleaned || hasNullEditors)
            ActiveEditorTracker.sharedTracker.ForceRebuild();

        if (hasNullEditors)
            SanitizeInspectors();
    }

    private static bool HasNullEditors()
    {
        Editor[] editors = ActiveEditorTracker.sharedTracker.activeEditors;
        if (editors == null || editors.Length == 0)
            return false;

        for (int i = 0; i < editors.Length; i++)
        {
            Editor editor = editors[i];
            if (editor == null)
                return true;

            Object[] targets = editor.targets;
            if (targets == null || targets.Length == 0)
                continue;

            for (int j = 0; j < targets.Length; j++)
            {
                if (targets[j] == null)
                    return true;
            }
        }

        return false;
    }

    private static void StartupCleanup()
    {
        if (_startupCleanupFramesRemaining <= 0)
        {
            EditorApplication.update -= StartupCleanup;
            return;
        }

        _startupCleanupFramesRemaining--;
        CleanSelection();
        SanitizeInspectors();
    }

    private static void SanitizeInspectors()
    {
        System.Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null)
            return;

        Object[] inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
        if (inspectors == null || inspectors.Length == 0)
            return;

        PropertyInfo trackerProp = inspectorType.GetProperty("tracker", BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo isLockedProp = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < inspectors.Length; i++)
        {
            Object inspector = inspectors[i];
            if (inspector == null)
                continue;

            ActiveEditorTracker tracker = trackerProp?.GetValue(inspector, null) as ActiveEditorTracker;
            if (tracker == null || !TrackerHasNullTargets(tracker))
                continue;

            if (isLockedProp != null)
            {
                bool locked = (bool)isLockedProp.GetValue(inspector, null);
                if (locked)
                    isLockedProp.SetValue(inspector, false, null);
            }

            tracker.ForceRebuild();
        }
    }

    private static bool TrackerHasNullTargets(ActiveEditorTracker tracker)
    {
        Editor[] editors = tracker.activeEditors;
        if (editors == null || editors.Length == 0)
            return false;

        for (int i = 0; i < editors.Length; i++)
        {
            Editor editor = editors[i];
            if (editor == null)
                return true;

            Object[] targets = editor.targets;
            if (targets == null || targets.Length == 0)
                continue;

            for (int j = 0; j < targets.Length; j++)
            {
                if (targets[j] == null)
                    return true;
            }
        }

        return false;
    }

    [MenuItem("Tools/Editor/Selection Null Cleaner")]
    private static void ToggleCleaner()
    {
        bool enabled = EditorPrefs.GetBool(EnabledKey, false);
        EditorPrefs.SetBool(EnabledKey, !enabled);
        EditorApplication.delayCall += () =>
        {
            // Force a domain reload-like effect for init
            if (!enabled)
            {
                Selection.selectionChanged += CleanSelection;
                EditorApplication.hierarchyChanged += CleanSelection;
                EditorApplication.projectChanged += CleanSelection;
                EditorApplication.delayCall += CleanSelection;
                _startupCleanupFramesRemaining = StartupCleanupFrames;
                EditorApplication.update += StartupCleanup;
            }
        };
    }

    [MenuItem("Tools/Editor/Selection Null Cleaner", true)]
    private static bool ToggleCleanerValidate()
    {
        Menu.SetChecked("Tools/Editor/Selection Null Cleaner", EditorPrefs.GetBool(EnabledKey, false));
        return true;
    }
}
#endif
