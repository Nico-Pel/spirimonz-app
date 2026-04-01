#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class PlayTempSaveToolbar
{
    static PlayTempSaveToolbar()
    {
        ToolbarCallback.OnToolbarGUIPlay += DrawToolbarButton;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void DrawToolbarButton()
    {
        Color prevBg = GUI.backgroundColor;
        Color prevContent = GUI.contentColor;
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.black;

        if (GUILayout.Button(new GUIContent("▶", "Play with temporary save (slot 4)"), EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            PlayerPrefs.SetInt("ActiveSaveSlot", 4);
            PlayerPrefs.SetInt("TempSaveSlotActive", 1);
            PlayerPrefs.Save();

            EditorApplication.EnterPlaymode();
        }

        GUI.backgroundColor = prevBg;
        GUI.contentColor = prevContent;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (PlayerPrefs.GetInt("TempSaveSlotActive", 0) == 1)
            {
                SaveManager.DeleteSave(4);
                PlayerPrefs.SetInt("TempSaveSlotActive", 0);
                PlayerPrefs.SetInt("ActiveSaveSlot", 1);
                PlayerPrefs.Save();
            }
        }
    }
}

public static class ToolbarCallback
{
    private static Type _toolbarType;
    private static ScriptableObject _toolbar;
    private static readonly FieldInfo _guiField;

    public static Action OnToolbarGUILeft;
    public static Action OnToolbarGUIRight;
    public static Action OnToolbarGUIPlay;

    static ToolbarCallback()
    {
        _toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        _guiField = _toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (_toolbar != null)
            return;

        UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
        if (toolbars == null || toolbars.Length == 0)
            return;

        _toolbar = (ScriptableObject)toolbars[0];
        var root = _guiField.GetValue(_toolbar) as UnityEngine.UIElements.VisualElement;
        if (root == null)
            return;

        var leftZone = root.Query<VisualElement>("ToolbarZoneLeftAlign").First();
        var rightZone = root.Query<VisualElement>("ToolbarZoneRightAlign").First();
        var playZone = root.Query<VisualElement>("ToolbarZonePlayMode").First();
        if (leftZone != null)
            leftZone.Add(new UnityEngine.UIElements.IMGUIContainer(() => { OnToolbarGUILeft?.Invoke(); }));
        if (rightZone != null)
            rightZone.Add(new UnityEngine.UIElements.IMGUIContainer(() => { OnToolbarGUIRight?.Invoke(); }));
        if (playZone != null)
            playZone.Insert(0, new UnityEngine.UIElements.IMGUIContainer(() => { OnToolbarGUIPlay?.Invoke(); }));
    }
}
#endif
