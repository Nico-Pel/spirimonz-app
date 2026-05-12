using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class UIQuest : GameBehaviour
{
    public TextMeshProUGUI tTitle;
    public TextMeshProUGUI tProgression;
    public TextMeshProUGUI tDescription;

    [Header("Reward")]
    public Button rewardButton;
    public TextMeshProUGUI rewardText;
    public Color rewardColor = Color.white;
    public Color rewardTextColor = Color.white;
    public float rewardCtaScale = 1.1f;
    public float rewardCtaDuration = 0.4f;
    public Ease rewardCtaEase = Ease.OutSine;
    public SoundParameters rewardClaimSound;

    public Image iBackground;
    public GameObject validationMarker;

    public Color textColorBase;
    public Color textColorValidate;

    public Color backgroundBaseColor;
    private GameManager _gameManager;
    private Quest _quest;
    private HouseMap _map;
    private Tween _rewardTween;
    private Vector3 _rewardBaseScale;
    private Color _rewardBaseColor;
    private Color _rewardTextBaseColor;
    private bool _rewardBaseCached;

    private void Start()
    {
        _gameManager = GameManager.Instance;

        CacheRewardBaseStateIfNeeded();

        if (rewardButton != null)
            rewardButton.onClick.AddListener(ClaimReward);
    }

    public void SetQuest(Quest quest, HouseMap map)
    {
        if(_gameManager == null)
            _gameManager = GameManager.Instance;

        int questProgress = _gameManager.GetQuestProgress(quest, map.houseID);
        bool questComplete = false;
        
        if (questProgress >= quest.goal)
        {
            questProgress = quest.goal;
            questComplete = true;
        }
        
        tTitle.text = quest.GetLocalizedName();
        tTitle.color = questComplete ? textColorValidate : textColorBase;
        
        tProgression.text = questProgress + "/" + quest.goal;
        tProgression.color = questComplete ? textColorValidate : textColorBase;
        
        tDescription.text = quest.GetLocalizedDescription();

        Color bc = backgroundBaseColor;
        iBackground.color = questComplete ? new Color(bc.r, bc.g, bc.b, bc.a / 2) : backgroundBaseColor;
        
        validationMarker.gameObject.SetActive(questComplete);

        _quest = quest;
        _map = map;
        RefreshRewardState(questComplete);
    }

    private void RefreshRewardState(bool questComplete)
    {
        if (rewardButton == null)
            return;

        CacheRewardBaseStateIfNeeded();

        if (_quest == null || _map == null || _gameManager == null)
        {
            rewardButton.interactable = false;
            StopRewardCta();
            return;
        }

        bool rewardClaimed = _gameManager.IsQuestRewardClaimed(_quest, _map.houseID);
        if (rewardClaimed)
        {
            rewardButton.gameObject.SetActive(false);
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);
            StopRewardCta();
            return;
        }

        rewardButton.gameObject.SetActive(true);
        if (rewardText != null)
            rewardText.gameObject.SetActive(true);

        rewardButton.interactable = questComplete;
        if (rewardText != null)
            rewardText.text = $"{Mathf.Max(0, _quest.rewardPrice)}#";

        if (rewardButton.image != null)
            rewardButton.image.color = questComplete ? rewardColor : _rewardBaseColor;
        if (rewardText != null)
            rewardText.color = questComplete ? rewardTextColor : _rewardTextBaseColor;

        if (questComplete)
            StartRewardCta();
        else
            StopRewardCta();
    }

    private void ClaimReward()
    {
        if (_quest == null || _map == null || _gameManager == null)
            return;

        if (_gameManager.TryClaimQuestReward(_quest, _map.houseID))
        {
            if (rewardClaimSound != null)
                rewardClaimSound.PlaySound();
            RefreshRewardState(questComplete: true);
        }
    }

    private void StartRewardCta()
    {
        if (rewardButton == null)
            return;

        if (_rewardTween != null && _rewardTween.IsActive())
            return;

        CacheRewardBaseStateIfNeeded();
        _rewardTween = rewardButton.transform
            .DOScale(_rewardBaseScale * rewardCtaScale, rewardCtaDuration)
            .SetEase(rewardCtaEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopRewardCta()
    {
        if (_rewardTween != null)
        {
            _rewardTween.Kill();
            _rewardTween = null;
        }

        if (rewardButton != null)
            rewardButton.transform.localScale = _rewardBaseScale;
    }

    private void CacheRewardBaseStateIfNeeded()
    {
        if (_rewardBaseCached)
            return;

        if (rewardButton != null)
        {
            _rewardBaseScale = rewardButton.transform.localScale;
            _rewardBaseColor = rewardButton.image != null ? rewardButton.image.color : Color.white;
        }
        if (rewardText != null)
            _rewardTextBaseColor = rewardText.color;

        _rewardBaseCached = true;
    }
}
