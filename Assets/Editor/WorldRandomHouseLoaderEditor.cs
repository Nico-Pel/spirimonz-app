using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldRandomHouseLoader))]
[CanEditMultipleObjects]
public class WorldRandomHouseLoaderEditor : Editor
{
    private SerializedProperty _houseSceneNames;
    private SerializedProperty _randomTeamSize;

    private void OnEnable()
    {
        _houseSceneNames = serializedObject.FindProperty("houseSceneNames");
        _randomTeamSize = serializedObject.FindProperty("randomTeamSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(_houseSceneNames, true);
        EditorGUILayout.PropertyField(_randomTeamSize);
        serializedObject.ApplyModifiedProperties();

        WorldRandomHouseLoader loader = (WorldRandomHouseLoader)target;

        GUILayout.Space(8f);

        if (GUILayout.Button("Random House"))
        {
            if (!Application.isPlaying && targets.Length > 1)
            {
                ((WorldRandomHouseLoader)targets[0]).EditorRequestRandomHouse();
            }
            else
            {
                foreach (Object t in targets)
                    ((WorldRandomHouseLoader)t).EditorRequestRandomHouse();
            }
        }

        if (GUILayout.Button("Random House + Random Team"))
        {
            if (!Application.isPlaying && targets.Length > 1)
            {
                ((WorldRandomHouseLoader)targets[0]).EditorRequestRandomHouseWithRandomTeam();
            }
            else
            {
                foreach (Object t in targets)
                    ((WorldRandomHouseLoader)t).EditorRequestRandomHouseWithRandomTeam();
            }
        }
    }
}
