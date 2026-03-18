#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class MobileModeToolbarToggle
{
    private const string ButtonTooltip = "Toggle Mobile Controls (GameManager.mobileControlsEnabled)";
    private static bool _installed;

    static MobileModeToolbarToggle()
    {
        EditorApplication.update += TryInstall;
    }

    private static void TryInstall()
    {
        if (_installed)
            return;

        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null)
            return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars == null || toolbars.Length == 0)
            return;

        var toolbar = toolbars[0];
        var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null)
            return;

        var root = rootField.GetValue(toolbar) as VisualElement;
        if (root == null)
            return;

        var playZone = root.Q("ToolbarZonePlayMode");
        if (playZone == null)
            return;

        IMGUIContainer container = new IMGUIContainer(DrawToolbarGUI)
        {
            name = "MobileModeToolbarToggle"
        };

        playZone.Add(container);
        _installed = true;
        EditorApplication.update -= TryInstall;
    }

    private static void DrawToolbarGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(6f);
            GameManager gm = FindGameManager();
            bool hasManager = gm != null;
            bool current = hasManager && gm.mobileControlsEnabled;

            string label = current ? "Mobile" : "PC";
            GUIContent content = new GUIContent(label, ButtonTooltip);
            using (new EditorGUI.DisabledScope(!hasManager))
            {
                bool next = GUILayout.Toggle(current, content, EditorStyles.toolbarButton, GUILayout.Width(70f));
                if (next != current)
                    SetMobileControls(gm, next);
            }
        }
    }

    private static GameManager FindGameManager()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            return GameManager.Instance;

        return Resources.FindObjectsOfTypeAll<GameManager>()
            .FirstOrDefault(g => g != null && g.gameObject.scene.IsValid());
    }

    private static void SetMobileControls(GameManager gm, bool enabled)
    {
        if (gm == null)
            return;

        Undo.RecordObject(gm, "Toggle Mobile Controls");
        gm.mobileControlsEnabled = enabled;
        EditorUtility.SetDirty(gm);

        if (Application.isPlaying)
            gm.SetMobileControlsEnabled(enabled);
    }
}
#endif
