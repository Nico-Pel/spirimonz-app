using UnityEngine;
using YsoCorp;
using YsoCorp.GameUtils;

[CreateAssetMenu(fileName = "InApp", menuName = "Spirimonz/Monetization/In App", order = 50)]
public class InApp : ScriptableObject
{
    public enum PurchaseMode
    {
        Unique,
        Consumable
    }

    [Header("Store")]
    public string productId;
    public bool useYcRemoveAdsProductId;
    public PurchaseMode purchaseMode = PurchaseMode.Unique;
    public bool grantRewardsOnEachSave;
    public string title;
    [TextArea(2, 5)] public string description;
    public string fallbackPrice;
    
    [Header("Conditional Variant")]
    public InApp switchToThisInAppWhenOwned;
    public InApp switchConditionOwnedInApp;

    [Header("Rewards")]
    public bool removeAds;
    [Min(0)] public int rewardTicketsPerDay;
    [Min(0)] public int moneyAmount;
    public SpirimonzSettings[] spirimonzToUnlock;
    public SpirimonzSettings[] spirimonzSkinsToUnlock;

    public bool IsConsumable => purchaseMode == PurchaseMode.Consumable;
    public bool IsUnique => !IsConsumable;

    public string GetResolvedProductId()
    {
        if (useYcRemoveAdsProductId)
        {
            YCManager ycManager = YCManager.instance;
            YCConfig config = ycManager != null ? ycManager.ycConfig : null;
            return config != null ? config.InAppRemoveAds : string.Empty;
        }

        return productId != null ? productId.Trim() : string.Empty;
    }

    public string GetStorageId()
    {
        string resolvedProductId = GetResolvedProductId();
        if (!string.IsNullOrEmpty(resolvedProductId))
            return resolvedProductId.ToLowerInvariant();

        return name.ToLowerInvariant();
    }
}
