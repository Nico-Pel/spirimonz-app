using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(House))]
public class HouseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Affiche tous les champs normaux

        House house = (House)target;

        GUILayout.Space(10);
        GUILayout.Label("Debug Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Bake Rooms Count Into HouseMap"))
        {
            house.BakeRoomsCount();
        }
    }
}