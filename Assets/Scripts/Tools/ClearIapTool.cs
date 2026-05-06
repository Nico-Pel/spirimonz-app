#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YsoCorp;

public static class ClearIapTool
{
    private const string RemoveAdsOwnedKey = "mobile_store_remove_ads_owned";
    private const string StarterPackOwnedKey = "mobile_store_starter_pack_owned";
    private const string RewardTicketsDateKey = "mobile_store_reward_tickets_date";
    private const string RewardTicketsUsedKey = "mobile_store_reward_tickets_used";
    private const string UnlockedExclusiveSkinIdsKey = "mobile_store_unlocked_exclusive_skin_ids";

    [MenuItem("Tools/Clear IAP")]
    private static void ClearIap()
    {
        if (!EditorUtility.DisplayDialog("Clear IAP", "Voulez-vous vraiment supprimer l'etat local des IAP et des grants lies aux sauvegardes ?", "Oui", "Non"))
            return;

        ClearGlobalIapState();
        ClearSaveIapState();

        if (Application.isPlaying && MobileMonetizationManager.InstanceOrNull != null)
            MobileMonetizationManager.InstanceOrNull.EditorRefreshStoreStateForTesting();

        Debug.Log("Etat local des IAP supprime.");
    }

    private static void ClearGlobalIapState()
    {
        ADataManager.DeleteKey(RemoveAdsOwnedKey);
        ADataManager.DeleteKey(StarterPackOwnedKey);
        ADataManager.DeleteKey(RewardTicketsDateKey);
        ADataManager.DeleteKey(RewardTicketsUsedKey);
        ADataManager.DeleteKey(UnlockedExclusiveSkinIdsKey);

        InApp[] inApps = Resources.LoadAll<InApp>("InApps");
        for (int i = 0; i < inApps.Length; i++)
        {
            InApp inApp = inApps[i];
            if (inApp == null)
                continue;

            string storageId = inApp.GetStorageId();
            if (string.IsNullOrEmpty(storageId))
                continue;

            ADataManager.DeleteKey($"mobile_store_owned_{storageId}");
        }

        ADataManager.ForceSave();
    }

    private static void ClearSaveIapState()
    {
        InApp[] inApps = Resources.LoadAll<InApp>("InApps");
        HashSet<string> grantKeys = new HashSet<string>();
        for (int i = 0; i < inApps.Length; i++)
        {
            InApp inApp = inApps[i];
            if (inApp == null)
                continue;

            string storageId = inApp.GetStorageId();
            if (string.IsNullOrEmpty(storageId))
                continue;

            grantKeys.Add($"mobile_store_granted_{storageId}");
        }

        for (int slot = 1; slot <= 4; slot++)
        {
            GameData data = SaveManager.Load(slot, createIfMissing: false);
            if (data == null)
                continue;

            if (data.bools != null)
            {
                data.bools.RemoveAll(entry =>
                    entry != null &&
                    (entry.id == SaveKeys.MOBILE_STORE_STARTER_CONTENT_GRANTED || grantKeys.Contains(entry.id)));
            }

            SaveManager.Save(data, slot);
        }
    }
}
#endif
