#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectionNullCleaner
{
    static SelectionNullCleaner()
    {
        Selection.selectionChanged += CleanSelection;
        EditorApplication.hierarchyChanged += CleanSelection;
        EditorApplication.projectChanged += CleanSelection;
        EditorApplication.delayCall += CleanSelection;
    }

    private static void CleanSelection()
    {
        Object[] selection = Selection.objects;
        if (selection == null || selection.Length == 0)
            return;

        bool hasNull = false;
        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i] == null)
            {
                hasNull = true;
                break;
            }
        }

        if (!hasNull)
            return;

        Selection.objects = selection.Where(obj => obj != null).ToArray();
    }
}
#endif
