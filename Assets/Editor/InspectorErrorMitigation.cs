#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InspectorErrorMitigation
{
    private const string AutoResetKey = "SH.AutoResetLayoutOnInspectorErrors";
    private const string PendingResetKey = "SH.PendingLayoutReset";

    private static bool _resetScheduled;

    static InspectorErrorMitigation()
    {
        if (!EditorPrefs.HasKey(AutoResetKey))
            EditorPrefs.SetBool(AutoResetKey, true);

        Application.logMessageReceived += OnLogMessage;
        EditorApplication.delayCall += TryResetOnStartup;
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
            return;

        if (!EditorPrefs.GetBool(AutoResetKey, true))
            return;

        if (!IsInspectorRelatedError(condition, stackTrace))
            return;

        EditorPrefs.SetBool(PendingResetKey, true);
        ScheduleLayoutReset();
    }

    private static bool IsInspectorRelatedError(string condition, string stackTrace)
    {
        if (!string.IsNullOrEmpty(condition))
        {
            if (condition.Contains("SerializedObjectNotCreatableException") ||
                condition.Contains("GameObjectInspector.OnEnable") ||
                condition.Contains("GameObjectInspector.OnDisable"))
                return true;
        }

        if (string.IsNullOrEmpty(stackTrace))
            return false;

        return stackTrace.Contains("UnityEditor.Graphs.") ||
               stackTrace.Contains("GraphicEditor.OnEnable") ||
               stackTrace.Contains("ImageEditor.OnEnable") ||
               stackTrace.Contains("RectTransformEditor.OnEnable");
    }

    private static void TryResetOnStartup()
    {
        if (!EditorPrefs.GetBool(PendingResetKey, false))
            return;

        ScheduleLayoutReset();
    }

    private static void ScheduleLayoutReset()
    {
        if (_resetScheduled)
            return;

        _resetScheduled = true;
        EditorApplication.delayCall += ResetLayout;
    }

    private static void ResetLayout()
    {
        _resetScheduled = false;
        EditorPrefs.SetBool(PendingResetKey, false);

        Selection.objects = new Object[0];
        ActiveEditorTracker.sharedTracker.ForceRebuild();

        EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
    }

    [MenuItem("Tools/Editor/Fix Inspector Errors (Reset Layout)")]
    private static void ManualReset()
    {
        ScheduleLayoutReset();
    }

    [MenuItem("Tools/Editor/Auto Reset Layout On Inspector Errors")]
    private static void ToggleAutoReset()
    {
        bool enabled = EditorPrefs.GetBool(AutoResetKey, true);
        EditorPrefs.SetBool(AutoResetKey, !enabled);
    }

    [MenuItem("Tools/Editor/Auto Reset Layout On Inspector Errors", true)]
    private static bool ToggleAutoResetValidate()
    {
        Menu.SetChecked("Tools/Editor/Auto Reset Layout On Inspector Errors", EditorPrefs.GetBool(AutoResetKey, true));
        return true;
    }
}
#endif
