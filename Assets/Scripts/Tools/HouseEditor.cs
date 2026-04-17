#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(House))]
public class HouseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(prop, true);
                continue;
            }

            if (prop.name == "useDebugs")
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            }

            EditorGUILayout.PropertyField(prop, true);

            if (prop.name == "huntTimeMultiplierDebug")
            {
                if (GUILayout.Button("Reset Debug Parameters"))
                {
                    ResetDebugParameters();
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("House Tools", EditorStyles.boldLabel);

        House house = (House)target;
        if (GUILayout.Button("Bake Rooms Count Into HouseMap"))
        {
            house.BakeRoomsCount();
        }

        if (GUILayout.Button("Fix WayPoints"))
        {
            house.FixWayPoints();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ResetDebugParameters()
    {
        serializedObject.FindProperty("useDebugs").boolValue = false;
        serializedObject.FindProperty("playerCantDie").boolValue = false;
        serializedObject.FindProperty("forcedGhostParameters").objectReferenceValue = null;
        serializedObject.FindProperty("forcedGhostModel").objectReferenceValue = null;
        serializedObject.FindProperty("forcedGhostActivity").enumValueIndex = (int)Ghost.GhostActivities.Nothing;
        serializedObject.FindProperty("forcedFavoriteRoomID").intValue = -1;
        serializedObject.FindProperty("tripleActivityDebug").boolValue = false;
        serializedObject.FindProperty("useHuntTimeMultiplierDebug").boolValue = false;
        serializedObject.FindProperty("huntTimeMultiplierDebug").floatValue = 1f;
    }
}
#endif
