using UnityEngine;
using UnityEditor;

public class ClearSaveTool
{
    [MenuItem("Tools/Clear Save")]
    static void ClearSave()
    {
        if (EditorUtility.DisplayDialog("Attention", "Voulez-vous vraiment supprimer la sauvegarde ?", "Oui", "Non"))
        {
            SaveManager.DeleteSave(); // ← ta fonction pour supprimer la save
            Debug.Log("Sauvegarde supprimée !");
        }
    }
}