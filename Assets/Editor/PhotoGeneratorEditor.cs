using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhotoGenerator))]
public class PhotoGeneratorEditor : Editor
{
    private SerializedProperty meshRendererProp;
    private SerializedProperty materialsProp;
    private SerializedProperty meshesProp;

    private void OnEnable()
    {
        meshRendererProp = serializedObject.FindProperty("meshRenderer");
        materialsProp = serializedObject.FindProperty("materials");
        meshesProp = serializedObject.FindProperty("meshes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(meshRendererProp);
        EditorGUILayout.PropertyField(materialsProp, true);
        EditorGUILayout.PropertyField(meshesProp, true);

        var canStep = materialsProp.arraySize > 0 && meshesProp.arraySize > 0;
        var hasRenderer = meshRendererProp.objectReferenceValue != null;

        EditorGUILayout.Space();

        if (!hasRenderer)
        {
            EditorGUILayout.HelpBox("Assign a MeshRenderer to preview the selection.", MessageType.Info);
        }
        else if (!canStep)
        {
            EditorGUILayout.HelpBox("Assign at least one material and one mesh.", MessageType.Info);
        }

        DrawCurrentIndices();

        using (new EditorGUI.DisabledScope(!canStep))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous"))
            {
                StepAll(false);
            }
            if (GUILayout.Button("Next"))
            {
                StepAll(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCurrentIndices()
    {
        if (targets.Length != 1)
        {
            return;
        }

        var generator = (PhotoGenerator)target;
        var meshCount = generator.meshes != null ? generator.meshes.Length : 0;
        var materialCount = generator.materials != null ? generator.materials.Length : 0;

        var meshLabel = meshCount > 0 ? $"{generator.CurrentMeshIndex + 1}/{meshCount}" : "0/0";
        var materialLabel = materialCount > 0 ? $"{generator.CurrentMaterialIndex + 1}/{materialCount}" : "0/0";

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Mesh Index", meshLabel);
            EditorGUILayout.TextField("Material Index", materialLabel);
        }
    }

    private void StepAll(bool next)
    {
        foreach (var obj in targets)
        {
            var generator = obj as PhotoGenerator;
            if (generator == null)
            {
                continue;
            }

            Undo.RecordObject(generator, "PhotoGenerator Step");

            if (generator.meshRenderer != null)
            {
                Undo.RecordObject(generator.meshRenderer, "PhotoGenerator Step");
                var meshFilter = generator.meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    Undo.RecordObject(meshFilter, "PhotoGenerator Step");
                }
            }

            if (next)
            {
                generator.StepNext();
            }
            else
            {
                generator.StepPrevious();
            }

            EditorUtility.SetDirty(generator);

            if (generator.meshRenderer != null)
            {
                EditorUtility.SetDirty(generator.meshRenderer);
                var meshFilter = generator.meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    EditorUtility.SetDirty(meshFilter);
                }
            }
        }
    }
}
