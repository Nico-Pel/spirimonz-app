using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using YsoCorp.GameUtils;

public class UIArticleHarvestPanel : MonoBehaviour
{
    private const string NotFoundKey = "ui.article_harvest.not_found";
    private const string RewardClaimedKey = "ui.article_harvest.reward_claimed";

    [Serializable]
    private sealed class SlotBinding
    {
        public UIArticleShop slot;
    }

    public float refreshInterval = 0.5f;
    public string titlePrefix = "Grigris Harvest : <color=#F9AB2D>Reset in: </color> ";
    [TextArea] public string notFoundTextEnglish = "Not found yet";
    [TextArea] public string notFoundTextFrench = "Pas encore trouve";
    [TextArea] public string rewardClaimedTextEnglish = "Reward claimed";
    [TextArea] public string rewardClaimedTextFrench = "Recompense recuperee";
    public SoundParameters rewardClaimSound;

    private readonly List<SlotBinding> _bindings = new List<SlotBinding>();
    private TextMeshProUGUI _titleText;
    private bool _initialized;

    private void Initialize()
    {
        if (_initialized)
            return;

        _titleText = FindTitleText();
        BuildBindings();
        _initialized = true;
    }

    private void OnEnable()
    {
        Initialize();
        WeeklyArticleHarvestState.OnStateChanged -= Refresh;
        WeeklyArticleHarvestState.OnStateChanged += Refresh;
        WeeklyArticleHarvestState.EnsureCurrentWeek();
        Refresh();
        CancelInvoke(nameof(Refresh));
        InvokeRepeating(nameof(Refresh), 0f, Mathf.Max(0.1f, refreshInterval));
    }

    private void OnDisable()
    {
        WeeklyArticleHarvestState.OnStateChanged -= Refresh;
        CancelInvoke(nameof(Refresh));
    }

    private TextMeshProUGUI FindTitleText()
    {
        if (_titleText != null)
            return _titleText;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null &&
                texts[i].text.IndexOf("Grigris Harvest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void BuildBindings()
    {
        _bindings.Clear();

        UIArticleShop[] slots = GetComponentsInChildren<UIArticleShop>(true);

        for (int i = 0; i < slots.Length; i++)
        {
            bool active = slots[i] != null && slots[i].Article != null;
            slots[i].gameObject.SetActive(active);
            if (!active)
                continue;

            SlotBinding binding = new SlotBinding
            {
                slot = slots[i]
            };

            int capturedIndex = _bindings.Count;
            slots[i].SetCallbacks(
                () => OnBaseClaimPressed(capturedIndex),
                () => OnDoubleClaimPressed(capturedIndex, false),
                () => OnDoubleClaimPressed(capturedIndex, true));

            _bindings.Add(binding);
        }
    }

    private void Refresh()
    {
        WeeklyArticleHarvestState.EnsureCurrentWeek();
        RefreshTitle();

        for (int i = 0; i < _bindings.Count; i++)
            RefreshBinding(_bindings[i]);
    }

    private void RefreshTitle()
    {
        if (_titleText == null)
            return;

        TimeSpan remaining = WeeklyArticleHarvestState.GetNextResetUtc() - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        _titleText.text = $"{titlePrefix}{FormatRemainingTime(remaining)}";
    }

    private void RefreshBinding(SlotBinding binding)
    {
        Article article = binding != null && binding.slot != null ? binding.slot.Article : null;
        if (binding == null || binding.slot == null || article == null)
            return;

        int baseReward = article.GetWeeklyHarvestReward();
        bool found = WeeklyArticleHarvestState.IsFoundThisWeek(article);
        bool claimed = WeeklyArticleHarvestState.IsClaimedThisWeek(article);
        bool canClaim = found && !claimed;
        bool mobile = ShouldUseMobileMonetization();
        bool hasTickets = HasAvailableRewardTickets();
        bool rewardedReady = IsRewardedReady();
        bool showDoubleOptions = mobile && canClaim;
        bool showStateText = !found || claimed;
        string stateText = claimed ? GetRewardClaimedText() : GetNotFoundText();
        Color stateTextColor = claimed ? binding.slot.claimedTextColor : binding.slot.GetNotFoundBaseColor();
        string ticketCountText = GetTicketCountText();

        binding.slot.SetVisualState(
            article.GetLocalizedName(),
            baseReward,
            baseReward * 2,
            found,
            claimed,
            canClaim,
            showStateText,
            stateText,
            stateTextColor,
            ticketCountText,
            showDoubleOptions && !hasTickets,
            showDoubleOptions && !hasTickets && rewardedReady,
            showDoubleOptions && hasTickets,
            showDoubleOptions && hasTickets);
    }

    private void OnBaseClaimPressed(int index)
    {
        if (index < 0 || index >= _bindings.Count)
            return;

        Article article = _bindings[index].slot != null ? _bindings[index].slot.Article : null;
        if (article == null)
            return;

        int amount = WeeklyArticleHarvestState.Claim(article, doubled: false);
        if (amount <= 0)
            return;

        GameManager.Instance?.AddMoney(amount);
        rewardClaimSound?.PlaySound();
        Refresh();
    }

    private void OnDoubleClaimPressed(int index, bool preferTicket)
    {
        if (index < 0 || index >= _bindings.Count)
            return;

        Article article = _bindings[index].slot != null ? _bindings[index].slot.Article : null;
        if (article == null)
            return;

        if (!WeeklyArticleHarvestState.CanClaim(article))
            return;

        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        if (store == null)
            return;

        Action<bool> onComplete = rewardGranted =>
        {
            if (!rewardGranted)
            {
                Refresh();
                return;
            }

            int amount = WeeklyArticleHarvestState.Claim(article, doubled: true);
            if (amount > 0)
            {
                GameManager.Instance?.AddMoney(amount);
                rewardClaimSound?.PlaySound();
            }

            Refresh();
        };

        if (preferTicket && HasAvailableRewardTickets())
        {
            store.ShowRewardedOrConsumeTicket(onComplete);
            return;
        }

        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        if (adsManager == null)
        {
            Refresh();
            return;
        }

        adsManager.ShowRewarded(onComplete);
    }

    private bool ShouldUseMobileMonetization()
    {
        return MobileInput.Enabled ||
               Application.isMobilePlatform ||
               (GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled);
    }

    private static bool HasAvailableRewardTickets()
    {
        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        return store != null && store.GetRemainingDailyRewardTickets() > 0;
    }

    private static bool IsRewardedReady()
    {
        AdsManager adsManager = YCManager.instance != null ? YCManager.instance.adsManager : null;
        return adsManager != null && adsManager.IsRewardedAdReady();
    }

    private string GetNotFoundText()
    {
        return LocalizationManager.Get(NotFoundKey, LocalizeFallback(notFoundTextEnglish, notFoundTextFrench));
    }

    private string GetRewardClaimedText()
    {
        return LocalizationManager.Get(RewardClaimedKey, LocalizeFallback(rewardClaimedTextEnglish, rewardClaimedTextFrench));
    }

    private static string GetTicketCountText()
    {
        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        if (store == null)
            return "0/0";

        int remaining = store.GetRemainingDailyRewardTickets();
        int total = Mathf.Max(1, store.GetDailyRewardTicketLimit());
        return $"{remaining}/{total}";
    }

    private static string FormatRemainingTime(TimeSpan remaining)
    {
        int days = Mathf.Max(0, remaining.Days);
        int hours = Mathf.Max(0, remaining.Hours);
        int seconds = Mathf.Max(0, remaining.Seconds);
        GetTimeSuffixes(out string daySuffix, out string hourSuffix, out string secondSuffix);
        return $"{days:00}{daySuffix} {hours:00}{hourSuffix} {seconds:00}{secondSuffix}";
    }

    private static void GetTimeSuffixes(out string daySuffix, out string hourSuffix, out string secondSuffix)
    {
        daySuffix = "d";
        hourSuffix = "h";
        secondSuffix = "s";

        if (LanguageManager.CurrentLanguage == Language.French)
        {
            daySuffix = "j";
            return;
        }
    }

    private string LocalizeFallback(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }
}
