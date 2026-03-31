using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIRandomHouseLauncher : GameBehaviour
{
    [Header("Screens")]
    public GameObject payScreen;
    public GameObject setupScreen;

    [Header("Payment")]
    public int price = 0;
    public TextMeshProUGUI tPrice;
    public Color priceEnoughColor = Color.white;
    public Color priceNotEnoughColor = Color.red;
    public Button bPay;
    public Button bCancel;
    public Button bClose;

    [Header("Random House")]
    public string[] houseSceneNames = { "House01", "House02", "House03", "House04", "House05" };

    [Header("Evidence Order")]
    public int roundCount = 5;
    public EvidenceParameter[] evidenceParameters;
    public Image[] evidenceIcons;
    public GameObject[] evidenceValidatedMarks;

    [Header("Choices")]
    public Button[] choiceButtons;
    public Image[] choiceImages;
    public GameObject[] choiceSelectors;
    public Button[] choiceInfoButtons;
    public Button bValidate;

    [Header("Info")]
    public GameObject infoPanel;
    public GameObject selectionPanel;
    public Button bInfoBack;
    public UISpirimonzInformationsSetter infoSetter;

    [Header("Team")]
    public Image[] teamSlotImages;
    public Image[] teamSlotSpmzImages;
    public Color teamSlotEmptyColor = new Color(1f, 1f, 1f, 0.35f);
    public Color teamSlotFilledColor = Color.white;

    private GameManager _gameManager;
    private InventoryManager _inventoryManager;

    private readonly List<GhostInvestigator.EvidenceType> _evidenceOrder = new List<GhostInvestigator.EvidenceType>();
    private readonly HashSet<SpirimonzSettings> _proposedSpirimonz = new HashSet<SpirimonzSettings>();
    private readonly List<SpirimonzSettings> _team = new List<SpirimonzSettings>();

    private SpirimonzSettings[] _currentChoices = new SpirimonzSettings[3];
    private int _currentRound;
    private int _selectedChoiceIndex = -1;
    private string _chosenSceneName;

    private void Awake()
    {
        if (bPay != null) bPay.onClick.AddListener(TryPayAndStart);
        if (bCancel != null) bCancel.onClick.AddListener(CloseWindow);
        if (bClose != null) bClose.onClick.AddListener(CloseWindow);
        if (bValidate != null) bValidate.onClick.AddListener(ValidateChoice);
        if (bInfoBack != null) bInfoBack.onClick.AddListener(CloseInfo);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            if (choiceButtons[i] != null)
                choiceButtons[i].onClick.AddListener(() => SelectChoice(index));
            if (choiceInfoButtons != null && i < choiceInfoButtons.Length && choiceInfoButtons[i] != null)
                choiceInfoButtons[i].onClick.AddListener(() => OpenInfo(index));
        }
    }

    private void OnEnable()
    {
        _gameManager = GameManager.Instance;
        _inventoryManager = InventoryManager.Instance;

        if (_gameManager != null)
            _gameManager.onMoneyUpdated.AddListener(RefreshPayButton);

        ResetWindow();
    }

    private void OnDisable()
    {
        if (_gameManager != null)
            _gameManager.onMoneyUpdated.RemoveListener(RefreshPayButton);
    }

    private void ResetWindow()
    {
        if (payScreen != null) payScreen.SetActive(true);
        if (setupScreen != null) setupScreen.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);

        _proposedSpirimonz.Clear();
        _team.Clear();
        _currentRound = 0;
        _selectedChoiceIndex = -1;
        _chosenSceneName = null;

        RefreshPayButton();
        ResetTeamSlots();
        ClearChoices();
        ClearEvidenceIcons();
    }

    private void RefreshPayButton()
    {
        if (tPrice != null)
            tPrice.text = price + "$";

        if (_gameManager == null)
        {
            if (bPay != null) bPay.interactable = false;
            if (tPrice != null) tPrice.color = priceNotEnoughColor;
            return;
        }

        bool enoughMoney = _gameManager.CanBuy(price);
        if (bPay != null) bPay.interactable = enoughMoney;
        if (tPrice != null) tPrice.color = enoughMoney ? priceEnoughColor : priceNotEnoughColor;
    }

    private void TryPayAndStart()
    {
        if (_gameManager == null)
            return;

        if (_gameManager.Buy(price) == false)
        {
            RefreshPayButton();
            return;
        }

        StartSetup();
    }

    private void StartSetup()
    {
        if (payScreen != null) payScreen.SetActive(false);
        if (setupScreen != null) setupScreen.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);

        _chosenSceneName = GetRandomHouseSceneName();
        BuildEvidenceOrder();
        UpdateEvidenceIcons();

        _team.Clear();
        for (int i = 0; i < _evidenceOrder.Count; i++)
            _team.Add(null);

        _currentRound = 0;
        SetupRound();
        ResetTeamSlots();
    }

    private void SetupRound()
    {
        _selectedChoiceIndex = -1;
        if (bValidate != null) bValidate.interactable = false;

        if (_currentRound >= _evidenceOrder.Count)
        {
            CompleteAndLaunch();
            return;
        }

        GhostInvestigator.EvidenceType evidenceType = _evidenceOrder[_currentRound];
        List<SpirimonzSettings> choices = BuildChoicesForEvidence(evidenceType);

        for (int i = 0; i < _currentChoices.Length; i++)
        {
            _currentChoices[i] = i < choices.Count ? choices[i] : null;
            UpdateChoiceVisual(i, _currentChoices[i]);
        }

        UpdateEvidenceProgress();
    }

    private void ValidateChoice()
    {
        if (_selectedChoiceIndex < 0 || _selectedChoiceIndex >= _currentChoices.Length)
            return;

        SpirimonzSettings chosen = _currentChoices[_selectedChoiceIndex];
        if (chosen == null)
            return;

        if (_currentRound >= 0 && _currentRound < _team.Count)
            _team[_currentRound] = chosen;

        UpdateTeamSlots();
        MarkEvidenceValidated(_currentRound);

        _currentRound++;
        SetupRound();
    }

    private void CompleteAndLaunch()
    {
        ApplyTeamToInventory();

        if (string.IsNullOrEmpty(_chosenSceneName))
        {
            Debug.LogError("UIRandomHouseLauncher: No house scene name available.");
            return;
        }

        GameManager.Instance.LoadScene(_chosenSceneName);
    }

    private void ApplyTeamToInventory()
    {
        if (_inventoryManager == null)
            return;

        _inventoryManager.spirimonzTeamSettings.Clear();
        for (int i = 0; i < _team.Count; i++)
            _inventoryManager.spirimonzTeamSettings.Add(_team[i]);

        _inventoryManager.onTeamChange?.Invoke();
    }

    private void SelectChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Length)
            return;

        if (_currentChoices[index] == null)
            return;

        _selectedChoiceIndex = index;
        if (bValidate != null) bValidate.interactable = true;

        for (int i = 0; i < choiceSelectors.Length; i++)
        {
            if (choiceSelectors[i] != null)
                choiceSelectors[i].SetActive(i == index);
        }
    }

    private void OpenInfo(int index)
    {
        if (index < 0 || index >= _currentChoices.Length)
            return;

        SpirimonzSettings spmz = _currentChoices[index];
        if (spmz == null)
            return;

        if (infoSetter != null)
            infoSetter.SetSpirimonz(spmz);

        if (infoPanel != null) infoPanel.SetActive(true);
        if (selectionPanel != null) selectionPanel.SetActive(false);
    }

    private void CloseInfo()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);
    }

    private void CloseWindow()
    {
        if (UIGame.Instance != null)
            UIGame.Instance.CloseAllWindows();
    }

    private void ResetTeamSlots()
    {
        if (teamSlotImages != null)
        {
            for (int i = 0; i < teamSlotImages.Length; i++)
            {
                if (teamSlotImages[i] == null) continue;
                teamSlotImages[i].color = teamSlotEmptyColor;
            }
        }

        if (teamSlotSpmzImages != null)
        {
            for (int i = 0; i < teamSlotSpmzImages.Length; i++)
            {
                if (teamSlotSpmzImages[i] == null) continue;
                teamSlotSpmzImages[i].sprite = null;
                teamSlotSpmzImages[i].enabled = false;
            }
        }
    }

    private void UpdateTeamSlots()
    {
        int slotCount = 0;
        if (teamSlotImages != null)
            slotCount = Mathf.Max(slotCount, teamSlotImages.Length);
        if (teamSlotSpmzImages != null)
            slotCount = Mathf.Max(slotCount, teamSlotSpmzImages.Length);

        for (int i = 0; i < slotCount; i++)
        {
            SpirimonzSettings spmz = i < _team.Count ? _team[i] : null;
            bool hasSpmz = spmz != null;

            if (teamSlotImages != null && i < teamSlotImages.Length && teamSlotImages[i] != null)
                teamSlotImages[i].color = hasSpmz ? teamSlotFilledColor : teamSlotEmptyColor;

            if (teamSlotSpmzImages != null && i < teamSlotSpmzImages.Length && teamSlotSpmzImages[i] != null)
            {
                teamSlotSpmzImages[i].sprite = hasSpmz ? spmz.img : null;
                teamSlotSpmzImages[i].enabled = hasSpmz;
            }
        }
    }

    private void UpdateChoiceVisual(int index, SpirimonzSettings spmz)
    {
        if (index < 0) return;
        if (choiceImages != null && index < choiceImages.Length && choiceImages[index] != null)
        {
            choiceImages[index].sprite = spmz != null ? spmz.img : null;
            choiceImages[index].enabled = spmz != null;
        }

        if (choiceButtons != null && index < choiceButtons.Length && choiceButtons[index] != null)
            choiceButtons[index].interactable = spmz != null;

        if (choiceSelectors != null && index < choiceSelectors.Length && choiceSelectors[index] != null)
            choiceSelectors[index].SetActive(false);

        if (choiceInfoButtons != null && index < choiceInfoButtons.Length && choiceInfoButtons[index] != null)
            choiceInfoButtons[index].interactable = spmz != null;
    }

    private void ClearChoices()
    {
        for (int i = 0; i < _currentChoices.Length; i++)
        {
            _currentChoices[i] = null;
            UpdateChoiceVisual(i, null);
        }
    }

    private void ClearEvidenceIcons()
    {
        for (int i = 0; i < evidenceIcons.Length; i++)
        {
            if (evidenceIcons[i] != null)
                evidenceIcons[i].sprite = null;
        }

        if (evidenceValidatedMarks != null)
        {
            for (int i = 0; i < evidenceValidatedMarks.Length; i++)
            {
                if (evidenceValidatedMarks[i] != null)
                    evidenceValidatedMarks[i].SetActive(false);
            }
        }
    }

    private void UpdateEvidenceIcons()
    {
        for (int i = 0; i < evidenceIcons.Length; i++)
        {
            if (evidenceIcons[i] == null) continue;

            if (i >= _evidenceOrder.Count)
            {
                evidenceIcons[i].sprite = null;
                evidenceIcons[i].gameObject.SetActive(false);
                continue;
            }

            EvidenceParameter param = GetEvidenceParameter(_evidenceOrder[i]);
            evidenceIcons[i].sprite = param != null ? param.icon : null;
            evidenceIcons[i].gameObject.SetActive(true);
        }

        UpdateEvidenceProgress();
    }

    private void UpdateEvidenceProgress()
    {
        if (evidenceValidatedMarks == null) return;

        for (int i = 0; i < evidenceValidatedMarks.Length; i++)
        {
            if (evidenceValidatedMarks[i] == null) continue;
            evidenceValidatedMarks[i].SetActive(i < _currentRound);
        }
    }

    private void MarkEvidenceValidated(int index)
    {
        if (evidenceValidatedMarks == null) return;
        if (index < 0 || index >= evidenceValidatedMarks.Length) return;
        if (evidenceValidatedMarks[index] != null)
            evidenceValidatedMarks[index].SetActive(true);
    }

    private EvidenceParameter GetEvidenceParameter(GhostInvestigator.EvidenceType type)
    {
        if (evidenceParameters == null) return null;
        for (int i = 0; i < evidenceParameters.Length; i++)
        {
            EvidenceParameter param = evidenceParameters[i];
            if (param != null && param.evidenceType == type)
                return param;
        }
        return null;
    }

    private void BuildEvidenceOrder()
    {
        _evidenceOrder.Clear();
        foreach (GhostInvestigator.EvidenceType type in Enum.GetValues(typeof(GhostInvestigator.EvidenceType)))
        {
            if (type == GhostInvestigator.EvidenceType.SpiritOrbs)
                continue;

            _evidenceOrder.Add(type);
        }

        for (int i = 0; i < _evidenceOrder.Count; i++)
        {
            int j = Random.Range(i, _evidenceOrder.Count);
            (_evidenceOrder[i], _evidenceOrder[j]) = (_evidenceOrder[j], _evidenceOrder[i]);
        }

        int targetCount = Mathf.Clamp(roundCount, 1, _evidenceOrder.Count);
        if (_evidenceOrder.Count > targetCount)
            _evidenceOrder.RemoveRange(targetCount, _evidenceOrder.Count - targetCount);
    }

    private List<SpirimonzSettings> BuildChoicesForEvidence(GhostInvestigator.EvidenceType evidenceType)
    {
        List<SpirimonzSettings> all = GetAllSpirimonzSettings();
        HashSet<SpirimonzSettings> teamSet = new HashSet<SpirimonzSettings>();
        foreach (SpirimonzSettings teamSpmz in _team)
        {
            if (teamSpmz != null)
                teamSet.Add(teamSpmz);
        }

        List<SpirimonzSettings> available = new List<SpirimonzSettings>();
        foreach (SpirimonzSettings s in all)
        {
            if (s != null && _proposedSpirimonz.Contains(s) == false && teamSet.Contains(s) == false)
                available.Add(s);
        }

        List<SpirimonzSettings> choices = new List<SpirimonzSettings>(3);

        // 1) New Spirimonz that match the evidence
        AddRandomFromPool(choices, available, s => s.IsUsefulForEvidence(evidenceType));

        // 2) If none left, pick Spirimonz with no evidence types at all
        if (choices.Count < 3)
            AddRandomFromPool(choices, available, s => !HasAnyEvidenceType(s));

        // 3) If still missing, reuse previously proposed (but not in team) that match the evidence
        if (choices.Count < 3)
        {
            List<SpirimonzSettings> previouslyProposed = new List<SpirimonzSettings>();
            foreach (SpirimonzSettings s in _proposedSpirimonz)
            {
                if (s != null && teamSet.Contains(s) == false)
                    previouslyProposed.Add(s);
            }
            AddRandomFromPool(choices, previouslyProposed, s => s.IsUsefulForEvidence(evidenceType), allowDuplicates: true);
        }

        // 4) Last resort: any non-assigned Spirimonz at random
        if (choices.Count < 3)
        {
            List<SpirimonzSettings> nonAssigned = new List<SpirimonzSettings>();
            foreach (SpirimonzSettings s in all)
            {
                if (s != null && teamSet.Contains(s) == false)
                    nonAssigned.Add(s);
            }
            AddRandomFromPool(choices, nonAssigned, s => true, allowDuplicates: true);
        }

        foreach (SpirimonzSettings spmz in choices)
            _proposedSpirimonz.Add(spmz);

        return choices;
    }

    private void AddRandomFromPool(
        List<SpirimonzSettings> choices,
        List<SpirimonzSettings> pool,
        Func<SpirimonzSettings, bool> predicate,
        bool allowDuplicates = false)
    {
        if (choices == null || pool == null) return;

        List<SpirimonzSettings> candidates = new List<SpirimonzSettings>();
        foreach (SpirimonzSettings s in pool)
        {
            if (s != null && predicate(s))
                candidates.Add(s);
        }

        while (choices.Count < 3 && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            SpirimonzSettings picked = candidates[index];
            candidates.RemoveAt(index);

            if (allowDuplicates == false)
            {
                pool.Remove(picked);
            }

            if (choices.Contains(picked) == false)
                choices.Add(picked);
        }
    }

    private bool HasAnyEvidenceType(SpirimonzSettings spmz)
    {
        if (spmz == null || spmz.abilities == null)
            return false;

        foreach (var ability in spmz.abilities)
        {
            if (ability == null || ability.evidenceTypes == null)
                continue;

            if (ability.evidenceTypes.Length > 0)
                return true;
        }

        return false;
    }

    private List<SpirimonzSettings> GetAllSpirimonzSettings()
    {
        List<SpirimonzSettings> results = new List<SpirimonzSettings>();
        if (_gameManager == null || _gameManager.allSpirimonzSettings == null)
            return results;

        foreach (SpirimonzSettings s in _gameManager.allSpirimonzSettings)
        {
            if (s != null && _gameManager.IsSpirimonzCaptured(s.spirimonzID))
                results.Add(s);
        }

        return results;
    }

    private string GetRandomHouseSceneName()
    {
        if (houseSceneNames == null || houseSceneNames.Length == 0)
            return null;

        int index = Random.Range(0, houseSceneNames.Length);
        return houseSceneNames[index];
    }
}
