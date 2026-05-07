using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using YsoCorp;
using YsoCorp.GameUtils;

public sealed class MobileMonetizationManager : MonoBehaviour
{
    public const int DailyRewardTicketLimit = 5;
    public const int StarterPackMoneyAmount = 500;
    public const string StarterPackSkinSpirimonzId = "SPMZ_001";

    private const string RemoveAdsOwnedKey = "mobile_store_remove_ads_owned";
    private const string StarterPackOwnedKey = "mobile_store_starter_pack_owned";
    private const string RewardTicketsDateKey = "mobile_store_reward_tickets_date";
    private const string RewardTicketsUsedKey = "mobile_store_reward_tickets_used";
    private const string UnlockedExclusiveSkinIdsKey = "mobile_store_unlocked_exclusive_skin_ids";

    private const string StarterPackProductId = "starter_pack";
    private const string CurrencyPack1ProductId = "currency_pack_1";
    private const string CurrencyPack2ProductId = "currency_pack_2";
    private const string CurrencyPack3ProductId = "currency_pack_3";
    private const string CurrencyPack4ProductId = "currency_pack_4";
    private const string CurrencyPack5ProductId = "currency_pack_5";

    private static readonly OfferConfig[] OfferConfigs =
    {
        new OfferConfig(MobileStoreOfferType.RemoveAds, null, 0, "Remove Ads", "Remove interstitials and get 5 free reward tickets per day.", "4,99€"),
        new OfferConfig(MobileStoreOfferType.StarterPack, StarterPackProductId, 0, "Starter Pack", "Remove interstitials, 5 free reward tickets per day, 500$, and unlock the skin for SPMZ_001.", "7,99€"),
        new OfferConfig(MobileStoreOfferType.CurrencyPack1, CurrencyPack1ProductId, 300, "Money Pack S", "300$ in game currency.", "1,99€"),
        new OfferConfig(MobileStoreOfferType.CurrencyPack2, CurrencyPack2ProductId, 800, "Money Pack M", "800$ in game currency.", "4,99€"),
        new OfferConfig(MobileStoreOfferType.CurrencyPack3, CurrencyPack3ProductId, 1800, "Money Pack L", "1 800$ in game currency.", "9,99€"),
        new OfferConfig(MobileStoreOfferType.CurrencyPack4, CurrencyPack4ProductId, 4000, "Money Pack XL", "4 000$ in game currency.", "19,99€"),
        new OfferConfig(MobileStoreOfferType.CurrencyPack5, CurrencyPack5ProductId, 9000, "Money Pack XXL", "9 000$ in game currency.", "39,99€")
    };

    private static MobileMonetizationManager _instance;

    private bool _iapListenersRegistered;
    private int _lastStarterGrantSlot = int.MinValue;
    private bool _resourceInAppsLoaded;
    private readonly List<InApp> _registeredInApps = new List<InApp>();
    private readonly Dictionary<string, InApp> _inAppsByProductId = new Dictionary<string, InApp>(StringComparer.Ordinal);
    private readonly HashSet<string> _registeredPurchaseListeners = new HashSet<string>(StringComparer.Ordinal);

    public static MobileMonetizationManager Instance
    {
        get
        {
            if (_instance == null)
                CreateInstance();

            return _instance;
        }
    }

    public static MobileMonetizationManager InstanceOrNull => _instance;

    public event Action OnStoreStateChanged;

    public void EditorRefreshStoreStateForTesting()
    {
        OnStoreStateChanged?.Invoke();
    }

    public struct OfferViewData
    {
        public MobileStoreOfferType offerType;
        public string productId;
        public string title;
        public string description;
        public string priceText;
        public string valueText;
        public bool owned;
        public bool available;
        public bool canPurchase;
    }

    private readonly struct OfferConfig
    {
        public readonly MobileStoreOfferType OfferType;
        public readonly string DefaultProductId;
        public readonly int CurrencyAmount;
        public readonly string Title;
        public readonly string Description;
        public readonly string FallbackPrice;

        public OfferConfig(
            MobileStoreOfferType offerType,
            string defaultProductId,
            int currencyAmount,
            string title,
            string description,
            string fallbackPrice)
        {
            OfferType = offerType;
            DefaultProductId = defaultProductId;
            CurrencyAmount = currencyAmount;
            Title = title;
            Description = description;
            FallbackPrice = fallbackPrice;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        CreateInstance();
    }

    private static void CreateInstance()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject(nameof(MobileMonetizationManager));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<MobileMonetizationManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        EnsureInAppsLoaded();
        TryRegisterIapListeners();
        EnsureOwnedInAppsGrantedForCurrentSave();
        EnsureRemoveAdsStateApplied();
        EnsureStarterPackGrantedForCurrentSave();
        EnsureRewardTicketDayIsCurrent();
    }

    public void RegisterInApp(InApp inApp)
    {
        if (inApp == null)
            return;

        if (!_registeredInApps.Contains(inApp))
            _registeredInApps.Add(inApp);

        string productId = inApp.GetResolvedProductId();
        if (!string.IsNullOrEmpty(productId))
            _inAppsByProductId[productId] = inApp;

        if (_iapListenersRegistered)
            TryRegisterListenerForInApp(inApp);
    }

    public bool ShouldUseMobileStore()
    {
        return Application.isMobilePlatform ||
               MobileInput.Enabled ||
               (GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled);
    }

    public IEnumerable<MobileStoreOfferType> GetAllOffers()
    {
        for (int i = 0; i < OfferConfigs.Length; i++)
            yield return OfferConfigs[i].OfferType;
    }

    public OfferViewData GetOfferViewData(MobileStoreOfferType offerType)
    {
        EnsureInAppsLoaded();

        InApp boundInApp = GetRegisteredInApp(GetProductId(offerType));
        if (boundInApp != null)
            return GetInAppViewData(boundInApp);

        OfferConfig config = GetConfig(offerType);
        string productId = GetProductId(offerType);
        bool owned = IsOfferOwned(offerType);
        bool available = ShouldUseMobileStore() && IsOfferConfigured(offerType);

        return new OfferViewData
        {
            offerType = offerType,
            productId = productId,
            title = config.Title,
            description = config.Description,
            priceText = GetLocalizedPrice(productId, config.FallbackPrice),
            valueText = GetValueText(config),
            owned = owned,
            available = available,
            canPurchase = available && !owned
        };
    }

    public OfferViewData GetInAppViewData(InApp inApp)
    {
        RegisterInApp(inApp);

        if (inApp == null)
            return default;

        InApp effectiveInApp = ResolveEffectiveInApp(inApp);
        string productId = effectiveInApp.GetResolvedProductId();
        bool owned = IsInAppOwned(inApp);
        bool allowEditorTestPurchase = CanUseEditorTestPurchase(effectiveInApp);
        bool available = (ShouldUseMobileStore() && IsInAppConfigured(effectiveInApp)) || allowEditorTestPurchase;
        string localizedPrice = GetLocalizedPrice(productId);
        if (allowEditorTestPurchase && string.IsNullOrWhiteSpace(localizedPrice))
            localizedPrice = "TEST";

        return new OfferViewData
        {
            offerType = default,
            productId = productId,
            title = string.IsNullOrWhiteSpace(effectiveInApp.title) ? inApp.title : effectiveInApp.title,
            description = string.IsNullOrWhiteSpace(effectiveInApp.description) ? inApp.description : effectiveInApp.description,
            priceText = localizedPrice,
            valueText = GetInAppValueText(effectiveInApp),
            owned = owned,
            available = available,
            canPurchase = available && (!owned || effectiveInApp.IsConsumable)
        };
    }

    public bool PurchaseOffer(MobileStoreOfferType offerType)
    {
        EnsureInAppsLoaded();

        InApp boundInApp = GetRegisteredInApp(GetProductId(offerType));
        if (boundInApp != null)
            return PurchaseInApp(boundInApp);

        if (!ShouldUseMobileStore() || IsOfferOwned(offerType))
            return false;

        string productId = GetProductId(offerType);
        if (!IsOfferConfigured(offerType) || string.IsNullOrEmpty(productId))
            return false;

#if IN_APP_PURCHASING
        InAppManager inAppManager = YCManager.instance != null ? YCManager.instance.inAppManager : null;
        if (inAppManager == null)
            return false;
        return TryInvokeBuyProduct(inAppManager, productId);
#else
        InAppManager inAppManager = YCManager.instance != null ? YCManager.instance.inAppManager : null;
        return TryInvokeBuyProduct(inAppManager, productId);
#endif
    }

    public bool PurchaseInApp(InApp inApp)
    {
        RegisterInApp(inApp);

#if UNITY_EDITOR
        Debug.Log($"Pouet PurchaseInApp start root={(inApp != null ? inApp.name : "null")} shouldUseMobileStore={ShouldUseMobileStore()}");
#endif

        if (inApp == null)
            return false;

        InApp effectiveInApp = ResolveEffectiveInApp(inApp);
#if UNITY_EDITOR
        Debug.Log($"Pouet PurchaseInApp effective={(effectiveInApp != null ? effectiveInApp.name : "null")}");
#endif
        if (effectiveInApp == null)
            return false;

        if (CanUseEditorTestPurchase(effectiveInApp))
        {
            if (IsInAppOwned(inApp) && effectiveInApp.IsUnique)
            {
#if UNITY_EDITOR
                Debug.Log($"Pouet PurchaseInApp TEST blocked already owned root={inApp.name}");
#endif
                return false;
            }

#if UNITY_EDITOR
            Debug.Log($"Pouet TEST purchase triggered effective='{effectiveInApp.name}' productId='{effectiveInApp.GetResolvedProductId()}'");
#endif
            OnInAppPurchased(inApp);
            return true;
        }

        if (!ShouldUseMobileStore())
        {
#if UNITY_EDITOR
            Debug.Log("Pouet PurchaseInApp blocked: ShouldUseMobileStore=false");
#endif
            return false;
        }

        if (IsInAppOwned(inApp) && effectiveInApp.IsUnique)
        {
#if UNITY_EDITOR
            Debug.Log($"Pouet PurchaseInApp blocked already owned root={inApp.name}");
#endif
            return false;
        }

        string productId = effectiveInApp.GetResolvedProductId();
#if UNITY_EDITOR
        Debug.Log($"Pouet PurchaseInApp resolved productId='{productId}' configured={IsInAppConfigured(effectiveInApp)}");
#endif
        if (!IsInAppConfigured(effectiveInApp) || string.IsNullOrEmpty(productId))
            return false;

        InAppManager inAppManager = YCManager.instance != null ? YCManager.instance.inAppManager : null;
        bool result = TryInvokeBuyProduct(inAppManager, productId);
#if UNITY_EDITOR
        Debug.Log($"Pouet PurchaseInApp TryInvokeBuyProduct result={result}");
#endif
        return result;
    }

    public bool CanRestorePurchases()
    {
        if (!ShouldUseMobileStore())
            return false;

        YCManager ycManager = YCManager.instance;
        return ycManager != null &&
               ycManager.ycConfig != null &&
               ycManager.ycConfig.HasInApps() &&
               ycManager.inAppManager != null;
    }

    public bool RestorePurchases()
    {
        if (!CanRestorePurchases())
            return false;

        InAppManager inAppManager = YCManager.instance != null ? YCManager.instance.inAppManager : null;
        if (inAppManager == null)
            return false;

        MethodInfo restoreMethod = inAppManager.GetType().GetMethod("RestorePurchases", Type.EmptyTypes);
        if (restoreMethod == null)
            return false;

        restoreMethod.Invoke(inAppManager, null);
        return true;
    }

    public bool ShowRewardedOrConsumeTicket(Action<bool> callback)
    {
        if (!ShouldUseMobileStore())
        {
            callback?.Invoke(false);
            return false;
        }

        EnsureRewardTicketDayIsCurrent();
        if (TryConsumeRewardTicket())
        {
            ResetInterstitialDelay();
            callback?.Invoke(true);
            NotifyStateChanged();
            return true;
        }

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager == null)
        {
            callback?.Invoke(false);
            return false;
        }

        return adsManager.ShowRewarded(success =>
        {
            if (success)
                ResetInterstitialDelay();

            callback?.Invoke(success);
            NotifyStateChanged();
        });
    }

    public int GetRemainingDailyRewardTickets()
    {
        int ticketLimit = GetDailyRewardTicketLimit();
        if (ticketLimit <= 0)
            return 0;

        EnsureRewardTicketDayIsCurrent();
        int used = Mathf.Max(0, ADataManager.GetInt(RewardTicketsUsedKey, 0));
        return Mathf.Max(0, ticketLimit - used);
    }

    public int GetDailyRewardTicketLimit()
    {
        EnsureInAppsLoaded();

        int ticketLimit = 0;
        if (ADataManager.GetBool(RemoveAdsOwnedKey) || ADataManager.GetBool(StarterPackOwnedKey))
            ticketLimit = Mathf.Max(ticketLimit, DailyRewardTicketLimit);

        for (int i = 0; i < _registeredInApps.Count; i++)
        {
            InApp inApp = _registeredInApps[i];
            if (inApp == null || !IsInAppOwned(inApp))
                continue;

            ticketLimit = Mathf.Max(ticketLimit, Mathf.Max(0, inApp.rewardTicketsPerDay));
        }

        return ticketLimit;
    }

    public bool HasRemoveAdsEntitlement()
    {
        EnsureInAppsLoaded();

        if (ADataManager.GetBool(RemoveAdsOwnedKey))
            return true;

        if (HasStarterPackOwnership())
            return true;

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager != null && !adsManager.IsAdsShow())
        {
            ADataManager.SetBool(RemoveAdsOwnedKey, true);
            ADataManager.ForceSave();
            return true;
        }

        for (int i = 0; i < _registeredInApps.Count; i++)
        {
            InApp inApp = _registeredInApps[i];
            if (inApp != null && inApp.removeAds && IsInAppOwned(inApp))
                return true;
        }

        return false;
    }

    public bool HasStarterPackOwnership()
    {
        return ADataManager.GetBool(StarterPackOwnedKey);
    }

    private void TryRegisterIapListeners()
    {
        EnsureInAppsLoaded();

        if (_iapListenersRegistered)
            return;

#if !IN_APP_PURCHASING
        _iapListenersRegistered = true;
        return;
#else
        YCManager ycManager = YCManager.instance;
        if (ycManager == null || ycManager.inAppManager == null)
            return;

        for (int i = 0; i < _registeredInApps.Count; i++)
            TryRegisterListenerForInApp(_registeredInApps[i]);

        string removeAdsProductId = GetProductId(MobileStoreOfferType.RemoveAds);
        if (GetRegisteredInApp(removeAdsProductId) == null &&
            IsOfferConfigured(MobileStoreOfferType.RemoveAds) &&
            !string.IsNullOrEmpty(removeAdsProductId))
            ycManager.inAppManager.AddListener(removeAdsProductId, OnRemoveAdsPurchased);

        if (GetRegisteredInApp(StarterPackProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.StarterPack, OnStarterPackPurchased);

        if (GetRegisteredInApp(CurrencyPack1ProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.CurrencyPack1, () => GrantCurrency(300));
        if (GetRegisteredInApp(CurrencyPack2ProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.CurrencyPack2, () => GrantCurrency(800));
        if (GetRegisteredInApp(CurrencyPack3ProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.CurrencyPack3, () => GrantCurrency(1800));
        if (GetRegisteredInApp(CurrencyPack4ProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.CurrencyPack4, () => GrantCurrency(4000));
        if (GetRegisteredInApp(CurrencyPack5ProductId) == null)
            TryRegisterListener(ycManager.inAppManager, MobileStoreOfferType.CurrencyPack5, () => GrantCurrency(9000));

        _iapListenersRegistered = true;
        NotifyStateChanged();
#endif
    }

    private void OnRemoveAdsPurchased()
    {
        GrantRemoveAdsEntitlement();
        NotifyStateChanged();
    }

    private void OnStarterPackPurchased()
    {
        ADataManager.SetBool(StarterPackOwnedKey, true);
        ADataManager.ForceSave();
        GrantRemoveAdsEntitlement();
        EnsureStarterPackGrantedForCurrentSave(true);
        NotifyStateChanged();
    }

    private void GrantRemoveAdsEntitlement()
    {
        ADataManager.SetBool(RemoveAdsOwnedKey, true);
        ADataManager.ForceSave();

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager != null && adsManager.IsAdsShow())
            adsManager.BuyAdsShow();
    }

    private void EnsureStarterPackGrantedForCurrentSave(bool force = false)
    {
        if (GetRegisteredInApp(StarterPackProductId) != null)
            return;

        if (!HasStarterPackOwnership())
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        int currentSlot = SaveManager.CurrentSlot;
        bool alreadyGranted = gameManager.GetBool(SaveKeys.MOBILE_STORE_STARTER_CONTENT_GRANTED);
        if (!force && _lastStarterGrantSlot == currentSlot && alreadyGranted)
            return;

        _lastStarterGrantSlot = currentSlot;

        if (!force && alreadyGranted)
            return;

        gameManager.AddMoney(StarterPackMoneyAmount);
        gameManager.UnlockSpirimonzSkin(StarterPackSkinSpirimonzId);
        gameManager.SetBool(SaveKeys.MOBILE_STORE_STARTER_CONTENT_GRANTED, true);
        NotifyStateChanged();
    }

    private void GrantCurrency(int amount)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && amount > 0)
            gameManager.AddMoney(amount);

        NotifyStateChanged();
    }

    private bool TryConsumeRewardTicket()
    {
        if (GetDailyRewardTicketLimit() <= 0)
            return false;

        EnsureRewardTicketDayIsCurrent();
        int remaining = GetRemainingDailyRewardTickets();
        if (remaining <= 0)
            return false;

        int used = Mathf.Max(0, ADataManager.GetInt(RewardTicketsUsedKey, 0));
        ADataManager.SetInt(RewardTicketsUsedKey, used + 1);
        ADataManager.ForceSave();
        return true;
    }

    private bool HasRewardTicketEntitlement()
    {
        return GetDailyRewardTicketLimit() > 0;
    }

    private void EnsureRewardTicketDayIsCurrent()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        string savedDay = ADataManager.GetString(RewardTicketsDateKey, string.Empty);
        if (savedDay == today)
            return;

        ADataManager.SetString(RewardTicketsDateKey, today);
        ADataManager.SetInt(RewardTicketsUsedKey, 0);
        ADataManager.ForceSave();
    }

    private void ResetInterstitialDelay()
    {
        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager != null)
            adsManager.ResetInterstitialDelay();
    }

    private OfferConfig GetConfig(MobileStoreOfferType offerType)
    {
        for (int i = 0; i < OfferConfigs.Length; i++)
        {
            if (OfferConfigs[i].OfferType == offerType)
                return OfferConfigs[i];
        }

        return OfferConfigs[0];
    }

    private string GetProductId(MobileStoreOfferType offerType)
    {
        if (offerType == MobileStoreOfferType.RemoveAds)
        {
            YCManager ycManager = YCManager.instance;
            return ycManager != null && ycManager.ycConfig != null ? ycManager.ycConfig.InAppRemoveAds : string.Empty;
        }

        return GetConfig(offerType).DefaultProductId;
    }

    private bool IsOfferOwned(MobileStoreOfferType offerType)
    {
        switch (offerType)
        {
            case MobileStoreOfferType.RemoveAds:
                return HasRemoveAdsEntitlement();
            case MobileStoreOfferType.StarterPack:
                return HasStarterPackOwnership();
            default:
                return false;
        }
    }

    private string GetLocalizedPrice(string productId, string fallbackPrice = "")
    {
        if (!string.IsNullOrEmpty(productId) && YCManager.instance != null && YCManager.instance.inAppManager != null)
        {
            string localized = YCManager.instance.inAppManager.GetProductPrice(productId);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return fallbackPrice;
    }

    private bool CanUseEditorTestPurchase(InApp inApp)
    {
        if (inApp == null || !Application.isEditor)
            return false;

        string productId = inApp.GetResolvedProductId();
        return string.IsNullOrWhiteSpace(GetLocalizedPrice(productId));
    }

    private string GetValueText(OfferConfig config)
    {
        switch (config.OfferType)
        {
            case MobileStoreOfferType.RemoveAds:
            case MobileStoreOfferType.StarterPack:
                return $"{GetRemainingDailyRewardTickets()}/{Mathf.Max(1, GetDailyRewardTicketLimit())} tickets left today";
            default:
                return config.CurrencyAmount > 0 ? $"+{config.CurrencyAmount}$" : string.Empty;
        }
    }

    private string GetInAppValueText(InApp inApp)
    {
        if (inApp == null)
            return string.Empty;

        if (inApp.moneyAmount > 0)
            return $"+{inApp.moneyAmount}$";

        if (inApp.rewardTicketsPerDay > 0)
            return $"{GetRemainingDailyRewardTickets()}/{Mathf.Max(1, GetDailyRewardTicketLimit())} tickets left today";

        return string.Empty;
    }

    private void NotifyStateChanged()
    {
        OnStoreStateChanged?.Invoke();
    }

    private bool IsOfferConfigured(MobileStoreOfferType offerType)
    {
        string productId = GetProductId(offerType);
        if (string.IsNullOrEmpty(productId))
            return false;

        YCManager ycManager = YCManager.instance;
        if (ycManager == null || ycManager.ycConfig == null)
            return false;

        if (offerType == MobileStoreOfferType.RemoveAds)
            return !string.IsNullOrEmpty(ycManager.ycConfig.InAppRemoveAds);

        InAppManager.CustomInapp[] customInapps = ycManager.ycConfig.CustomInapps;
        if (customInapps == null)
            return false;

        for (int i = 0; i < customInapps.Length; i++)
        {
            if (string.Equals(customInapps[i].inappKey, productId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private bool IsInAppConfigured(InApp inApp)
    {
        if (inApp == null)
            return false;

        string productId = inApp.GetResolvedProductId();
        if (string.IsNullOrEmpty(productId))
            return false;

        YCManager ycManager = YCManager.instance;
        if (ycManager == null || ycManager.ycConfig == null)
            return false;

        if (inApp.useYcRemoveAdsProductId)
            return !string.IsNullOrEmpty(ycManager.ycConfig.InAppRemoveAds);

        InAppManager.CustomInapp[] customInapps = ycManager.ycConfig.CustomInapps;
        if (customInapps == null)
            return false;

        for (int i = 0; i < customInapps.Length; i++)
        {
            if (string.Equals(customInapps[i].inappKey, productId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void TryRegisterListener(InAppManager inAppManager, MobileStoreOfferType offerType, Action callback)
    {
        if (inAppManager == null || callback == null || !IsOfferConfigured(offerType))
            return;

        string productId = GetProductId(offerType);
        if (string.IsNullOrEmpty(productId))
            return;

        MethodInfo addListener = inAppManager.GetType().GetMethod("AddListener", new[] { typeof(string), typeof(UnityAction) });
        if (addListener == null)
            return;

        UnityAction unityAction = () => callback();
        addListener.Invoke(inAppManager, new object[] { productId, unityAction });
    }

    private bool TryInvokeBuyProduct(InAppManager inAppManager, string productId)
    {
#if UNITY_EDITOR
        Debug.Log($"Pouet TryInvokeBuyProduct hasManager={inAppManager != null} productId='{productId}'");
#endif
        if (inAppManager == null || string.IsNullOrEmpty(productId))
            return false;

        MethodInfo buyMethod = inAppManager.GetType().GetMethod("BuyProductID", new[] { typeof(string) });
        if (buyMethod == null)
            return false;

        buyMethod.Invoke(inAppManager, new object[] { productId });
        return true;
    }

    private void EnsureInAppsLoaded()
    {
        if (_resourceInAppsLoaded)
            return;

        _resourceInAppsLoaded = true;
        InApp[] resourceInApps = Resources.LoadAll<InApp>("InApps");
        for (int i = 0; i < resourceInApps.Length; i++)
            RegisterInApp(resourceInApps[i]);
    }

    private InApp GetRegisteredInApp(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return null;

        _inAppsByProductId.TryGetValue(productId, out InApp inApp);
        return inApp;
    }

    private void TryRegisterListenerForInApp(InApp inApp)
    {
        if (inApp == null)
            return;

        string productId = inApp.GetResolvedProductId();
        if (string.IsNullOrEmpty(productId) || _registeredPurchaseListeners.Contains(productId) || !IsInAppConfigured(inApp))
            return;

        InAppManager inAppManager = YCManager.instance != null ? YCManager.instance.inAppManager : null;
        if (inAppManager == null)
            return;

        MethodInfo addListener = inAppManager.GetType().GetMethod("AddListener", new[] { typeof(string), typeof(UnityAction) });
        if (addListener == null)
            return;

        UnityAction unityAction = () => OnInAppPurchased(inApp);
        addListener.Invoke(inAppManager, new object[] { productId, unityAction });
        _registeredPurchaseListeners.Add(productId);
    }

    private void OnInAppPurchased(InApp inApp)
    {
        if (inApp == null)
            return;

#if UNITY_EDITOR
        Debug.Log($"Pouet OnInAppPurchased root='{inApp.name}'");
#endif

        if (inApp.IsUnique)
            SetInAppOwned(inApp, true);

        ApplyInAppGlobalEntitlements(inApp);

        if (inApp.IsUnique)
        {
            if (inApp.grantRewardsOnEachSave)
                TryGrantInAppRewardsForCurrentSave(inApp, true);
            else
                GrantInAppRewards(inApp);
        }
        else
        {
            GrantInAppRewards(inApp);
        }

        NotifyStateChanged();
    }

    private void EnsureOwnedInAppsGrantedForCurrentSave()
    {
        for (int i = 0; i < _registeredInApps.Count; i++)
        {
            InApp inApp = _registeredInApps[i];
            if (inApp == null || !inApp.IsUnique || !inApp.grantRewardsOnEachSave || !IsInAppOwned(inApp))
                continue;

            TryGrantInAppRewardsForCurrentSave(inApp, false);
        }
    }

    private void TryGrantInAppRewardsForCurrentSave(InApp inApp, bool force)
    {
        if (inApp == null)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        string saveGrantKey = GetPerSaveGrantKey(inApp);
        if (!force && gameManager.GetBool(saveGrantKey))
            return;

        GrantInAppRewards(inApp);
        gameManager.SetBool(saveGrantKey, true);
    }

    private void GrantInAppRewards(InApp inApp)
    {
        if (inApp == null)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Pouet GrantInAppRewards aborted: GameManager.Instance is null for '{inApp.name}'");
#endif
            return;
        }

        if (inApp.moneyAmount > 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Pouet GrantInAppRewards AddMoney add={inApp.moneyAmount} inApp='{inApp.name}'");
#endif
            gameManager.AddMoney(inApp.moneyAmount);
        }

        GrantSpirimonzUnlocks(gameManager, inApp.spirimonzToUnlock);
        GrantSpirimonzSkinUnlocks(gameManager, inApp.spirimonzSkinsToUnlock);
    }

    private void ApplyInAppGlobalEntitlements(InApp inApp)
    {
        if (inApp == null)
            return;

        if (inApp.removeAds)
            GrantRemoveAdsEntitlement();
    }

    private void EnsureRemoveAdsStateApplied()
    {
        if (!HasRemoveAdsEntitlement())
            return;

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager != null && adsManager.IsAdsShow())
            adsManager.BuyAdsShow();
    }

    private bool IsInAppOwned(InApp inApp)
    {
        return IsInAppOwned(inApp, new HashSet<InApp>());
    }

    private bool IsInAppOwned(InApp inApp, HashSet<InApp> visited)
    {
        if (inApp == null || !inApp.IsUnique)
            return false;

        if (!visited.Add(inApp))
            return false;

        string resolvedProductId = inApp.GetResolvedProductId();
        if (inApp.useYcRemoveAdsProductId && ADataManager.GetBool(RemoveAdsOwnedKey))
            return true;

        if (string.Equals(resolvedProductId, StarterPackProductId, StringComparison.Ordinal) && ADataManager.GetBool(StarterPackOwnedKey))
            return true;

        if (ADataManager.GetBool(GetOwnedKey(inApp)))
            return true;

        if (inApp.switchToThisInAppWhenOwned != null && IsInAppOwned(inApp.switchToThisInAppWhenOwned, visited))
            return true;

        return false;
    }

    private void SetInAppOwned(InApp inApp, bool owned)
    {
        if (inApp == null || !inApp.IsUnique)
            return;

        ADataManager.SetBool(GetOwnedKey(inApp), owned);

        string resolvedProductId = inApp.GetResolvedProductId();
        if (inApp.useYcRemoveAdsProductId)
            ADataManager.SetBool(RemoveAdsOwnedKey, owned);

        if (string.Equals(resolvedProductId, StarterPackProductId, StringComparison.Ordinal))
            ADataManager.SetBool(StarterPackOwnedKey, owned);

        ADataManager.ForceSave();
    }

    private static void GrantSpirimonzUnlocks(GameManager gameManager, SpirimonzSettings[] spirimonzToUnlock)
    {
        if (gameManager == null || spirimonzToUnlock == null)
            return;

        for (int i = 0; i < spirimonzToUnlock.Length; i++)
        {
            SpirimonzSettings settings = spirimonzToUnlock[i];
            if (settings != null)
                gameManager.UnlockSpirimonz(settings.spirimonzID);
        }
    }

    private static void GrantSpirimonzSkinUnlocks(GameManager gameManager, SpirimonzSettings[] spirimonzSkinsToUnlock)
    {
        if (gameManager == null || spirimonzSkinsToUnlock == null)
            return;

        for (int i = 0; i < spirimonzSkinsToUnlock.Length; i++)
        {
            SpirimonzSettings settings = spirimonzSkinsToUnlock[i];
            if (settings != null)
                gameManager.UnlockSpirimonzSkin(settings.spirimonzID);
        }
    }

    private static string GetOwnedKey(InApp inApp)
    {
        return $"mobile_store_owned_{inApp.GetStorageId()}";
    }

    private static string GetPerSaveGrantKey(InApp inApp)
    {
        return $"mobile_store_granted_{inApp.GetStorageId()}";
    }

    private InApp ResolveEffectiveInApp(InApp inApp)
    {
        return ResolveEffectiveInApp(inApp, new HashSet<InApp>());
    }

    private InApp ResolveEffectiveInApp(InApp inApp, HashSet<InApp> visited)
    {
        if (inApp == null)
            return null;

        if (!visited.Add(inApp))
            return inApp;

        RegisterInApp(inApp);

        if (inApp.switchToThisInAppWhenOwned == null || inApp.switchConditionOwnedInApp == null)
            return inApp;

        RegisterInApp(inApp.switchConditionOwnedInApp);
        RegisterInApp(inApp.switchToThisInAppWhenOwned);

        if (!IsInAppOwned(inApp.switchConditionOwnedInApp))
            return inApp;

        return ResolveEffectiveInApp(inApp.switchToThisInAppWhenOwned, visited);
    }
}
