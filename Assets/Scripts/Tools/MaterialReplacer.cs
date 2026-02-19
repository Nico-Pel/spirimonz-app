using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MaterialReplacer : MonoBehaviour
{
    public Material materialToReplace;
    public Material replacementMaterial;

#if UNITY_EDITOR
    [ContextMenu("Replace Material In Children")]
    public void ReplaceMaterials()
    {
        if (materialToReplace == null || replacementMaterial == null)
        {
            Debug.LogWarning("MaterialReplacer: Assign both materials first.");
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int replaceCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == materialToReplace)
                {
                    materials[i] = replacementMaterial;
                    replaceCount++;
                    changed = true;
                }
            }

            if (changed)
            {
                Undo.RecordObject(renderer, "Replace Materials");
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        Debug.Log($"Material replaced {replaceCount} time(s).");
    }

    // Swap the two materials
    public void SwapMaterials()
    {
        Undo.RecordObject(this, "Swap Materials");

        Material temp = materialToReplace;
        materialToReplace = replacementMaterial;
        replacementMaterial = temp;

        EditorUtility.SetDirty(this);
    }

    // Inspector buttons
    [CustomEditor(typeof(MaterialReplacer))]
    private class MaterialReplacerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MaterialReplacer replacer = (MaterialReplacer)target;

            EditorGUILayout.Space();

            // Replace button
            GUI.enabled = replacer.materialToReplace != null && replacer.replacementMaterial != null;
            if (GUILayout.Button("Replace Material In Children"))
            {
                replacer.ReplaceMaterials();
            }

            // Swap button
            if (GUILayout.Button("Swap Materials"))
            {
                replacer.SwapMaterials();
            }

            GUI.enabled = true;
        }
    }
#endif
}