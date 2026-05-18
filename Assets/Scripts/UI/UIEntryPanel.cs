using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using YsoCorp;
using YsoCorp.GameUtils;

public class UIEntryPanel : GameBehaviour
{
    [Header("Components")] 
    public UIQuest[] quests;
    
    [Space]
    public TextMeshProUGUI tTitleMap;
    public Image mapImage;
    public TextMeshProUGUI tRoomsNb;
    public TextMeshProUGUI tPrice;

    public Button bGo;
    public Button bGoTuto;
    public Button bGoTraining;
    public Button bClose;
    public Color goColorBase;
    public Color goColorQuestsCompleted;

    [Header("Mobile Free Entry")]
    public Button bRewardedEntry;
    public TextMeshProUGUI tRewardedEntry;
    public Button bTicketEntry;
    public TextMeshProUGUI tTicketEntry;
    public string rewardedEntryLabel = "FREE";
    public string ticketEntryLabel = "FREE";
    public string rewardedEntryRemainingFormat = "{0}/{1}";
    [Min(0.1f)] public float freeEntryButtonRefreshInterval = 0.5f;

    [Header("Sounds")]
    public SoundParameters goSound;
    public SoundParameters goPaidSound;
    public SoundParameters goTutoSound;
    public SoundParameters goTrainingSound;
    public SoundParameters closeSound;

    [Header("Panels")]
    public GameObject normalPanel;
    public GameObject tutoPanel;

    [Header("Tips")]
    public GameObject iTips;
    public Image tipsBackground;
    public Color tipsBackgroundDefault = new Color(1f, 1f, 1f, 0.15f);
    public Color tipsBackgroundSecretWorld = new Color(1f, 0.85f, 0.3f, 0.2f);
    public TextMeshProUGUI tTips;
    [TextArea] public string tipsHasQuestsEnglish = "Complete all quests to gain free access to this location.";
    [TextArea] public string tipsHasQuestsFrench = "Complete toutes les quêtes pour accéder gratuitement à cet endroit.";
    [TextArea] public string tipsAllCompletedEnglish = "All quests completed, access is now free.";
    [TextArea] public string tipsAllCompletedFrench = "Toutes les quêtes sont complétées, l'accès est maintenant gratuit.";
    [TextArea] public string tipsSecretWorldEnglish = "The entry cost for this house increases with the number of runs in this world until your next visit.";
    [TextArea] public string tipsSecretWorldFrench = "Le coût d'entrée augmente avec le nombre de runs dans ce monde jusqu'à ta prochaine visite.";
    [TextArea] public string enterTextEnglish = "Enter";
    [TextArea] public string enterTextFrench = "Entrer";
    [TextArea] public string freeTextEnglish = "Free";
    [TextArea] public string freeTextFrench = "Gratuit";

    private const string TipsHasQuestsKey = "ui.entry.tips.has_quests";
    private const string TipsAllCompletedKey = "ui.entry.tips.all_completed";
    private const string TipsSecretWorldKey = "ui.entry.tips.secret_world";
    private const string EnterKey = "ui.entry.enter";
    private const string FreeKey = "ui.common.free";
    private const string FreeEntryRewardDateKey = "mobile_store_entry_reward_date";
    private const string FreeEntryRewardUsedKey = "mobile_store_entry_reward_used";

    private GameManager _gameManager;
    private HouseEntry _entry;

    private HouseEntry _currentEntry;
    private bool _allQuestCompleted;

    public void OpenPanel(HouseEntry entry)
    {
        _currentEntry = entry;

        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }

        bool allQuestCompleted = true;
        int questCount = entry != null && entry.map != null && entry.map.quests != null ? entry.map.quests.Length : 0;
        for (int i = 0; i < quests.Length; i++)
        {
            bool isActive = i < questCount;
            quests[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                quests[i].SetQuest(entry.map.quests[i], entry.map);
                if (entry.map.quests[i].IsCompleted(entry.map.houseID) == false)
                {
                    allQuestCompleted = false;
                }
            }
        }

        bool freeByPrice = entry != null && entry.map != null && entry.map.entryPrince <= 0;
        bool freeAccess = freeByPrice || (allQuestCompleted && questCount > 0);
        _allQuestCompleted = freeAccess;

        bool useSecretWorldPricing = ShouldUseSecretWorldPricing(entry);
        if (useSecretWorldPricing && _gameManager != null)
        {
            _gameManager.SetSecretWorldPriceConfig(
                entry.map.linkedSecretWorld.houseEntryPriceIncrease,
                entry.map.linkedSecretWorld.maxHouseEntryPriceIncreases);
        }

        if (normalPanel != null)
            normalPanel.SetActive(entry == null || !entry.hasTutorialModes);
        if (tutoPanel != null)
            tutoPanel.SetActive(entry != null && entry.hasTutorialModes);
        
        gameObject.SetActive(true);
        tTitleMap.text = entry.map.GetLocalizedName();
        tRoomsNb.text = entry.map.roomsNumber.ToString();
        mapImage.sprite = entry.map.sprite;
        
        bGo.image.color = freeAccess && !freeByPrice ? goColorQuestsCompleted : goColorBase;

        int priceToUse = GetEntryPrice(entry, freeAccess);

        if (tPrice != null)
        {
            if (priceToUse <= 0)
                tPrice.text = LocalizationManager.Get(FreeKey, LocalizeFallback(freeTextEnglish, freeTextFrench));
            else if (!useSecretWorldPricing && freeAccess && !freeByPrice)
                tPrice.text = LocalizationManager.Get(EnterKey, LocalizeFallback(enterTextEnglish, enterTextFrench));
            else
                tPrice.text = priceToUse + "#";
        }

        _entry = entry;
        
        bGo.onClick.RemoveAllListeners();
        bGo.onClick.AddListener(GoNormal);

        if (bClose != null)
        {
            bClose.onClick.RemoveAllListeners();
            bClose.onClick.AddListener(ClosePanel);
        }

        if (bGoTuto != null)
        {
            bGoTuto.onClick.RemoveAllListeners();
            bGoTuto.onClick.AddListener(GoTutorial);
        }

        if (bGoTraining != null)
        {
            bGoTraining.onClick.RemoveAllListeners();
            bGoTraining.onClick.AddListener(GoTraining);
        }

        EnsureFreeEntryButtons();
        if (bRewardedEntry != null)
        {
            bRewardedEntry.onClick.RemoveAllListeners();
            bRewardedEntry.onClick.AddListener(OnRewardedEntryPressed);
        }

        if (bTicketEntry != null)
        {
            bTicketEntry.onClick.RemoveAllListeners();
            bTicketEntry.onClick.AddListener(OnTicketEntryPressed);
        }

        int price = priceToUse;
        bool enoughMoney = price <= 0 || _gameManager.CanBuy(price);
        bGo.interactable = enoughMoney;
        tPrice.color = enoughMoney ? Color.white : Color.red;
        if (bGoTuto != null)
            bGoTuto.interactable = enoughMoney;
        if (bGoTraining != null)
            bGoTraining.interactable = enoughMoney;

        if (iTips != null)
        {
            bool showTips = useSecretWorldPricing || (questCount > 0 && !freeByPrice);
            iTips.SetActive(showTips);
            if (tipsBackground != null)
                tipsBackground.color = useSecretWorldPricing ? tipsBackgroundSecretWorld : tipsBackgroundDefault;

            if (showTips && tTips != null)
            {
                if (useSecretWorldPricing)
                {
                    tTips.text = LocalizationManager.Get(TipsSecretWorldKey, LocalizeFallback(tipsSecretWorldEnglish, tipsSecretWorldFrench));
                }
                else
                {
                    string tips = freeAccess
                        ? LocalizationManager.Get(TipsAllCompletedKey, LocalizeFallback(tipsAllCompletedEnglish, tipsAllCompletedFrench))
                        : LocalizationManager.Get(TipsHasQuestsKey, LocalizeFallback(tipsHasQuestsEnglish, tipsHasQuestsFrench));
                    tTips.text = tips;
                }
            }
        }

        CancelInvoke(nameof(RefreshFreeEntryButtonState));
        RefreshFreeEntryButtonState();
        if (ShouldUseMobileMonetization())
            InvokeRepeating(nameof(RefreshFreeEntryButtonState), 0f, freeEntryButtonRefreshInterval);
    }

    private void GoNormal()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.NormalMap, goSound, goPaidSound);
    }

    private void GoTutorial()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Tutorial, goTutoSound, goPaidSound);
    }

    private void GoTraining()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Training, goTrainingSound, goPaidSound);
    }

    private void TryEnterWithMode(GameManager.HouseSceneMode mode, SoundParameters freeSound, SoundParameters paidSound)
    {
        if (_gameManager == null || _currentEntry == null)
            return;

        int price = GetCurrentEntryPrice();

        if (price > 0 && !_gameManager.Buy(price))
            return;

        EnterCurrentEntry(mode, price > 0 ? paidSound : freeSound);
    }

    private void EnterCurrentEntry(GameManager.HouseSceneMode mode, SoundParameters soundToPlay)
    {
        if (_gameManager == null || _entry == null)
            return;

        if (soundToPlay != null)
            soundToPlay.PlaySound();

        _gameManager.SetNextHouseSceneMode(mode);
        UIGame.Instance.CloseAllWindows();
        _entry.Entry(Player.Instance);
    }

    private void OnRewardedEntryPressed()
    {
        if (!CanUseRewardedEntry())
            return;

        if (bRewardedEntry != null)
            bRewardedEntry.interactable = false;

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager == null)
        {
            RefreshFreeEntryButtonState();
            return;
        }

        adsManager.ShowRewarded(rewardGranted =>
        {
            if (!rewardGranted)
            {
                RefreshFreeEntryButtonState();
                return;
            }

            adsManager.ResetInterstitialDelay();
            ConsumeRewardedEntryUse();
            EnterCurrentEntry(GameManager.HouseSceneMode.NormalMap, goSound);
        });
    }

    private void OnTicketEntryPressed()
    {
        if (!CanUseTicketEntry())
            return;

        if (bTicketEntry != null)
            bTicketEntry.interactable = false;

        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        if (store == null)
        {
            RefreshFreeEntryButtonState();
            return;
        }

        store.ShowRewardedOrConsumeTicket(rewardGranted =>
        {
            if (!rewardGranted)
            {
                RefreshFreeEntryButtonState();
                return;
            }

            EnterCurrentEntry(GameManager.HouseSceneMode.NormalMap, goSound);
        });
    }

    private void RefreshFreeEntryButtonState()
    {
        EnsureFreeEntryButtons();

        if (bRewardedEntry == null && bTicketEntry == null)
            return;

        bool shouldShowRewarded = CanUseRewardedEntry();
        bool shouldShowTicket = CanUseTicketEntry();

        if (bRewardedEntry != null)
            bRewardedEntry.gameObject.SetActive(shouldShowRewarded);
        if (bTicketEntry != null)
            bTicketEntry.gameObject.SetActive(shouldShowTicket);

        if (tRewardedEntry != null)
        {
            int remaining = HasUnusedRewardedEntry() ? 1 : 0;
            tRewardedEntry.text = $"{rewardedEntryLabel} {string.Format(rewardedEntryRemainingFormat, remaining, 1)}";
        }

        if (tTicketEntry != null)
        {
            MobileMonetizationManager store = MobileMonetizationManager.Instance;
            if (store != null)
            {
                int remaining = store.GetRemainingDailyRewardTickets();
                int total = Mathf.Max(1, store.GetDailyRewardTicketLimit());
                tTicketEntry.text = $"{ticketEntryLabel} {string.Format(rewardedEntryRemainingFormat, remaining, total)}";
            }
            else
            {
                tTicketEntry.text = ticketEntryLabel;
            }
        }

        if (bRewardedEntry != null)
        {
            AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
            bool rewardedReady = adsManager != null && adsManager.IsRewardedAdReady();
            bRewardedEntry.interactable = shouldShowRewarded && rewardedReady;
        }

        if (bTicketEntry != null)
            bTicketEntry.interactable = shouldShowTicket;
    }

    private bool CanUseRewardedEntry()
    {
        return ShouldShowAnyFreeEntryButton() &&
               !HasAvailableRewardTickets() &&
               HasUnusedRewardedEntry();
    }

    private bool CanUseTicketEntry()
    {
        return ShouldShowAnyFreeEntryButton() &&
               HasAvailableRewardTickets();
    }

    private bool ShouldShowAnyFreeEntryButton()
    {
        if (!ShouldUseMobileMonetization() ||
            _gameManager == null ||
            _currentEntry == null ||
            !_gameManager.IsWorld())
        {
            return false;
        }

        if (normalPanel != null && !normalPanel.activeInHierarchy)
            return false;

        return GetCurrentEntryPrice() > 0;
    }

    private bool HasAvailableRewardTickets()
    {
        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        return store != null && store.GetRemainingDailyRewardTickets() > 0;
    }

    private bool HasUnusedRewardedEntry()
    {
        EnsureRewardedEntryDayIsCurrent();
        return ADataManager.GetInt(FreeEntryRewardUsedKey, 0) <= 0;
    }

    private void ConsumeRewardedEntryUse()
    {
        EnsureRewardedEntryDayIsCurrent();
        ADataManager.SetInt(FreeEntryRewardUsedKey, 1);
        ADataManager.ForceSave();
    }

    private void EnsureRewardedEntryDayIsCurrent()
    {
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
        string savedDay = ADataManager.GetString(FreeEntryRewardDateKey, string.Empty);
        if (savedDay == today)
            return;

        ADataManager.SetString(FreeEntryRewardDateKey, today);
        ADataManager.SetInt(FreeEntryRewardUsedKey, 0);
        ADataManager.ForceSave();
    }

    private bool ShouldUseMobileMonetization()
    {
        return MobileInput.Enabled ||
               Application.isMobilePlatform ||
               (_gameManager != null && _gameManager.mobileControlsEnabled);
    }

    private bool ShouldUseSecretWorldPricing(HouseEntry entry)
    {
        bool isSecretWorldHouse = entry != null && entry.map != null && entry.map.linkedSecretWorld != null;
        bool isInSecretWorld = _gameManager != null &&
                               _gameManager.IsTemporaryWorldScene(SceneManager.GetActiveScene().name);
        return isSecretWorldHouse && isInSecretWorld;
    }

    private int GetEntryPrice(HouseEntry entry, bool freeAccess)
    {
        if (entry == null || entry.map == null)
            return 0;

        if (ShouldUseSecretWorldPricing(entry))
            return _gameManager != null ? _gameManager.GetSecretWorldHouseEntryPrice(entry.map.linkedSecretWorld) : 0;

        return freeAccess ? 0 : entry.map.entryPrince;
    }

    private int GetCurrentEntryPrice()
    {
        return GetEntryPrice(_currentEntry, _allQuestCompleted);
    }

    private void EnsureFreeEntryButtons()
    {
        if (bRewardedEntry == null)
            bRewardedEntry = FindOptionalButton(normalPanel != null ? normalPanel.transform : transform, "BReward");
        if (bTicketEntry == null)
            bTicketEntry = FindOptionalButton(normalPanel != null ? normalPanel.transform : transform, "BTicket");

        if (tRewardedEntry == null && bRewardedEntry != null)
            tRewardedEntry = bRewardedEntry.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tTicketEntry == null && bTicketEntry != null)
            tTicketEntry = bTicketEntry.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Button FindOptionalButton(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null || children[i].name != objectName)
                continue;

            return children[i].GetComponent<Button>();
        }

        return null;
    }

    private void ClosePanel()
    {
        if (closeSound != null)
        {
            UITablet tablet = UIGame.Instance != null ? UIGame.Instance.tablet : null;
            if (tablet == null || tablet.closeTabletSound == null)
                closeSound.PlaySound();
        }

        UIGame.Instance.CloseAllWindows();
    }

    private string LocalizeFallback(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshFreeEntryButtonState));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref goSound);
        UISoundDefaults.AssignIfNull(ref goPaidSound);
        UISoundDefaults.AssignIfNull(ref goTutoSound);
        UISoundDefaults.AssignIfNull(ref goTrainingSound);
        UISoundDefaults.AssignIfNull(ref closeSound);
    }
#endif
}
