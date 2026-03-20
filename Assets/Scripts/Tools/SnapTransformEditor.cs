#if UNITY_EDITOR
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
                Vector3 snappedEuler = SnapEuler(t.localEulerAngles);
                t.localRotation = Quaternion.Euler(snappedEuler);
                SetEulerHint(t, snappedEuler);
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
        angle = SnapFloat(angle);

        float eps = SNAP_VALUE * 0.5f;
        if (Mathf.Abs(angle) <= eps)
            return 0f;
        if (Mathf.Abs(angle - 90f) <= eps)
            return 90f;
        if (Mathf.Abs(angle + 90f) <= eps)
            return -90f;
        if (Mathf.Abs(angle - 180f) <= eps || Mathf.Abs(angle + 180f) <= eps)
            return 180f;

        return angle;
    }

    private static void SetEulerHint(Transform t, Vector3 euler)
    {
        if (t == null)
            return;

        SerializedObject so = new SerializedObject(t);
        SerializedProperty prop = so.FindProperty("m_LocalEulerAnglesHint");
        if (prop != null)
        {
            prop.vector3Value = euler;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
