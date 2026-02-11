using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpirimonzAdder))]
public class SpirimonzAdderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpirimonzAdder adder = (SpirimonzAdder)target;
        if (GUILayout.Button("Add Spirimonz"))
        {
            adder.AddSpirimonz();
        }
    }
}