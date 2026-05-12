using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using YsoCorp.GameUtils;

public class UIEndGame : GameBehaviour
{
    public enum EndTypes
    {
        Win,
        Escape,
        Lose
    }
    
    [Header("Spirit info text")]
    public string spiritVictoryText = "You have captured a {0} Spirimonz!";
    public string spiritEscapeText = "You have fled from a {0} spirit...";
    public string spiritLoseText = "A {0} spirit knocked you out...";
    private const string SpiritVictoryKey = "ui.endgame.spirit_victory";
    private const string SpiritEscapeKey = "ui.endgame.spirit_escape";
    private const string SpiritLoseKey = "ui.endgame.spirit_lose";
    
    [Header("Loot info text")]
    public string lootVictoryText = "Your discoveries have gained value thanks to your achievements!";
    public string lootEscapeText = "Your loot did not increase in value following a victory...";
    public string lootLoseText = "The items found on site were lost...";
    private const string LootVictoryKey = "ui.endgame.loot_victory";
    private const string LootEscapeKey = "ui.endgame.loot_escape";
    private const string LootLoseKey = "ui.endgame.loot_lose";
    public Color normalTextColor = Color.white;
    public Color winTextColor = Color.yellow;
    
    [Header("Components")]
    public TextMeshProUGUI tHouseName;
    public TextMeshProUGUI tSubtitle;
    public TextMeshProUGUI tTotal;

    public TextMeshProUGUI tLootInfo;

    public Transform uiLootPos;
    public UILootRecap uiLootPrefab;
    
    [Header("Rewarded Bonus")]
    public Button bRewardedBonus;
    public TextMeshProUGUI tRewardedBonus;
    [Min(0)] public int minimumRewardedPayout = 50;
    [Min(0.1f)] public float rewardedButtonRefreshInterval = 0.5f;

    public Button bContinue;
    public float uninteractableTime = 3f;

    [Header("Sounds")]
    public SoundParameters continueSound;
    public Button bQuit;
    public Button bRetry;
    public SoundParameters quitSound;
    public SoundParameters retrySound;

    private bool _soundHooksDone;
    private Button _runtimeRewardedBonusButton;
    private TextMeshProUGUI _runtimeRewardedBonusText;
    private int _basePayout;
    private int _selectedPayout;
    private int _rewardedPayout;
    private bool _rewardApplied;
    public void SetTexts(EndTypes endType, House house)
    {
        _rewardApplied = false;
        CancelInvoke(nameof(RefreshRewardedButtonState));

        tHouseName.text = house.map.GetLocalizedName();
        
        string victoryText = null;
        string ghostTypeName = LocalizationManager.GetGhostTypeName(house.currentGhost.ghostParameters.ghostTypeData.ghostType);
        switch (endType)
        {
            case EndTypes.Win:
                victoryText = LocalizationManager.Get(SpiritVictoryKey, spiritVictoryText);
                tLootInfo.text = LocalizationManager.Get(LootVictoryKey, lootVictoryText);
                tLootInfo.color = winTextColor;
                break;
            case EndTypes.Escape:
                victoryText = LocalizationManager.Get(SpiritEscapeKey, spiritEscapeText);
                tLootInfo.text = LocalizationManager.Get(LootEscapeKey, lootEscapeText);
                tLootInfo.color = normalTextColor;
                break;
            case EndTypes.Lose:
                victoryText = LocalizationManager.Get(SpiritLoseKey, spiritLoseText);
                tLootInfo.text = LocalizationManager.Get(LootLoseKey, lootLoseText);
                tLootInfo.color = normalTextColor;
                break;
        }
        tSubtitle.text = string.Format(victoryText, ghostTypeName);

        int totalValue = 0;
        bool isWin = endType == EndTypes.Win;
        int rewardMultiplier = 1;
        if (GameManager.Instance != null && GameManager.Instance.royalChallengeActive)
            rewardMultiplier = 3;

        List<Article> articlesFound = house.currentPlayer.inventoryManager.articlesFoundInGame;

        for (int i = uiLootPos.childCount - 1; i >= 0; i--)
            Destroy(uiLootPos.GetChild(i).gameObject);

// 1️⃣ Regroup article
        Dictionary<Article, int> groupedArticles = new Dictionary<Article, int>();

        foreach (var article in articlesFound)
        {
            if (groupedArticles.ContainsKey(article))
                groupedArticles[article]++;
            else
                groupedArticles.Add(article, 1);
        }
        
        foreach (var pair in groupedArticles)
        {
            Article article = pair.Key;
            int quantity = pair.Value;

            UILootRecap newUILoot = Instantiate(uiLootPrefab, uiLootPos);

            Color valueColorToUse =
                isWin && article.winValueMultiplier > 1
                    ? winTextColor
                    : normalTextColor;

            int unitValue = isWin
                ? (int)(article.value * article.winValueMultiplier)
                : article.value;

            int totalArticleValue = unitValue * quantity;

            if (totalArticleValue == -1) //Is the Victory article
            {
                totalArticleValue = house.map.victoryReward;
            }

            unitValue *= rewardMultiplier;
            totalArticleValue *= rewardMultiplier;

            newUILoot.Init(article, quantity, totalArticleValue, valueColorToUse);

            totalValue += totalArticleValue;
        }

        tTotal.text = totalValue + "#";
        _basePayout = totalValue;
        _rewardedPayout = Mathf.Max(_basePayout * 2, minimumRewardedPayout);
        _selectedPayout = _basePayout;

        EnsureRewardedBonusButton();
        RefreshRewardedButtonLabel();
        RefreshRewardedButtonState();
        if (ShouldShowRewardedBonus())
            InvokeRepeating(nameof(RefreshRewardedButtonState), 0f, rewardedButtonRefreshInterval);

        bContinue.interactable = false;
        this.Invoke(uninteractableTime, () => bContinue.interactable = true);
        bContinue.onClick.RemoveAllListeners();
        bContinue.onClick.AddListener(() =>
        {
            if (continueSound != null)
                continueSound.PlaySound();

            CommitSelectedPayout();

            bool useDeadAnimation = endType == EndTypes.Lose;
            if (TutorialManager.Instance != null &&
                (TutorialManager.Instance.IsTraining || TutorialManager.Instance.IsControlsTutorial))
                useDeadAnimation = false;

            Action exitAction = () =>
            {
                house.houseEntry.Entry(house.currentPlayer, useDeadAnimation);
                house.currentPlayer.inventoryManager.articlesFoundInGame.Clear();
                UIGame.Instance.CloseAllWindows();
            };

            if (ShouldUseMobileMonetization() && YCManager.instance != null && YCManager.instance.adsManager != null)
                YCManager.instance.adsManager.ShowInterstitial(exitAction);
            else
                exitAction();
        });
    }

    private void OnEnable()
    {
        UIGame.Instance.tablet.bClose.gameObject.SetActive(false);

        if (!_soundHooksDone)
        {
            _soundHooksDone = true;
            if (bQuit != null)
                bQuit.onClick.AddListener(() =>
                {
                    if (quitSound != null)
                        quitSound.PlaySound();
                });
            if (bRetry != null)
                bRetry.onClick.AddListener(() =>
                {
                    if (retrySound != null)
                        retrySound.PlaySound();
                });
        }
    }
    
    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshRewardedButtonState));
        UIGame.Instance.tablet.bClose.gameObject.SetActive(true);
    }

    private void EnsureRewardedBonusButton()
    {
        if (bContinue == null)
            return;

        if (bRewardedBonus == null)
        {
            if (_runtimeRewardedBonusButton == null)
            {
                GameObject clone = Instantiate(bContinue.gameObject, bContinue.transform.parent);
                clone.name = "bRewardedBonus";
                clone.transform.SetSiblingIndex(Mathf.Max(0, bContinue.transform.GetSiblingIndex()));

                _runtimeRewardedBonusButton = clone.GetComponent<Button>();
                _runtimeRewardedBonusButton.onClick.RemoveAllListeners();

                TextMeshProUGUI text = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                _runtimeRewardedBonusText = text;
                if (_runtimeRewardedBonusText != null)
                    _runtimeRewardedBonusText.text = "x2 0#";
            }

            bRewardedBonus = _runtimeRewardedBonusButton;
            tRewardedBonus = _runtimeRewardedBonusText;
        }

        if (bRewardedBonus != null)
        {
            bRewardedBonus.onClick.RemoveAllListeners();
            bRewardedBonus.onClick.AddListener(OnRewardedBonusPressed);
        }
    }

    private void OnRewardedBonusPressed()
    {
        if (!ShouldShowRewardedBonus() || _rewardApplied)
            return;

        bRewardedBonus.interactable = false;
        MobileMonetizationManager store = MobileMonetizationManager.Instance;
        store.ShowRewardedOrConsumeTicket(rewardGranted =>
        {
            if (rewardGranted)
            {
                _rewardApplied = true;
                _selectedPayout = _rewardedPayout;
                tTotal.text = _selectedPayout + "#";
                RefreshRewardedButtonLabel();
                RefreshRewardedButtonState();
            }
            else
            {
                RefreshRewardedButtonState();
            }
        });
    }

    private void RefreshRewardedButtonLabel()
    {
        if (tRewardedBonus == null)
            return;

        int displayAmount = _rewardApplied ? _selectedPayout : _rewardedPayout;
        tRewardedBonus.text = $"{displayAmount}#";
    }

    private void RefreshRewardedButtonState()
    {
        if (bRewardedBonus == null)
            return;

        bool shouldShow = ShouldShowRewardedBonus();
        bRewardedBonus.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        if (_rewardApplied)
        {
            bRewardedBonus.interactable = false;
            return;
        }

        bool rewardedReady = YCManager.instance != null &&
                            YCManager.instance.adsManager != null &&
                            YCManager.instance.adsManager.IsRewardedAdReady();
        bRewardedBonus.interactable = rewardedReady;
    }

    private bool ShouldShowRewardedBonus()
    {
        return ShouldUseMobileMonetization() && _basePayout > 0;
    }

    private bool ShouldUseMobileMonetization()
    {
        return MobileInput.Enabled || Application.isMobilePlatform;
    }

    private void CommitSelectedPayout()
    {
        if (_selectedPayout <= 0)
            return;

        GameManager.Instance.AddMoney(_selectedPayout);
        _selectedPayout = 0;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref continueSound);
        UISoundDefaults.AssignIfNull(ref quitSound);
        UISoundDefaults.AssignIfNull(ref retrySound);
    }
#endif
}
