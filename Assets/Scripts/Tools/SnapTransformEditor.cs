using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SnapTransformEditor
{
    private const float SNAP_VALUE = 0.05f;

    static SnapTransformEditor()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (EditorApplication.isPlaying)
            return;

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.M)
        {
            Transform[] selection = Selection.transforms;
            if (selection == null || selection.Length == 0)
                return;

            Undo.RecordObjects(selection, "Snap Transforms");

            foreach (Transform t in selection)
            {
                t.localPosition = SnapVector(t.localPosition);
                t.localEulerAngles = SnapEuler(t.localEulerAngles);
                EditorUtility.SetDirty(t);
            }

            e.Use();
        }
    }

    private static Vector3 SnapVector(Vector3 value)
    {
        return new Vector3(
            SnapFloat(value.x),
            SnapFloat(value.y),
            SnapFloat(value.z)
        );
    }

    private static Vector3 SnapEuler(Vector3 euler)
    {
        return new Vector3(
            SnapAngle(euler.x),
            SnapAngle(euler.y),
            SnapAngle(euler.z)
        );
    }

    private static float SnapFloat(float value)
    {
        float snapped = Mathf.Round(value / SNAP_VALUE) * SNAP_VALUE;

        // Clean floating point imprecision
        int decimals = Mathf.CeilToInt(-Mathf.Log10(SNAP_VALUE));
        return (float)System.Math.Round(snapped, decimals);
    }

    private static float SnapAngle(float angle)
    {
        angle = Mathf.DeltaAngle(0f, angle);
        return SnapFloat(angle);
    }
}