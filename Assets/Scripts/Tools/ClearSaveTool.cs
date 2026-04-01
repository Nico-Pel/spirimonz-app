#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ClearSaveTool
{
    [MenuItem("Tools/Clear Save 1")]
    static void ClearSave1()
    {
        ClearSaveSlot(1);
    }

    [MenuItem("Tools/Clear Save 2")]
    static void ClearSave2()
    {
        ClearSaveSlot(2);
    }

    [MenuItem("Tools/Clear Save 3")]
    static void ClearSave3()
    {
        ClearSaveSlot(3);
    }

    private static void ClearSaveSlot(int slot)
    {
        if (EditorUtility.DisplayDialog("Attention", $"Voulez-vous vraiment supprimer la sauvegarde {slot} ?", "Oui", "Non"))
        {
            SaveManager.DeleteSave(slot);
            Debug.Log($"Sauvegarde {slot} supprimée !");
        }
    }
}
#endif
