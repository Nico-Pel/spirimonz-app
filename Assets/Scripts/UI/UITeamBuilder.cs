using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UITeamBuilder : GameBehaviour
{
    public UITeamPanel teamPanel;
    public List<UISpirimonzPanelSelector> spirimonzPanelSelectors;
    public Transform spirimonzSelectorPanel;
    public UISpirimonzPanelSelector emptySelectorPrefab;
    public bool insertEmptySelector = true;
    
    [Space]
    
    public GameObject spirimonzRightInfo;
    public GameObject spirimonzLeftInfo;

    public GameObject spirimonzRightSecondaryTitle;
    public TextMeshProUGUI secondaryTitleText;
    public TextMeshProUGUI leftTitleText;

    public Button bCloseRight;
    public Button bCloseLeft;
    
    public Button bInfoRight;
    public Button bInfoLeft;

    public UISpirimonzInformationsSetter selectedSpirimonzInfoSetter;

    [Header("Footer")]
    public GameObject selectionFooter;
    public Button bChooseSpirimonz;

    [Space] [Header("Filters")] 
    public UIFilterButton[] bFilterEvidences;

    [Header("Sounds")]
    public SoundParameters selectSpmzSound;
    public SoundParameters addToTeamSound;
    public SoundParameters filterToggleSound;

    [Header("Remove UI")]
    public Color removeLabelColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    private UISpirimonzPanelSelector _currentSelected;
    private Player _player;
    private GameManager _gameManager;
    private Color _secondaryTitleBaseColor;

    private bool _isSelectorInitialized;
    private void Awake()
    {
        bCloseRight.onClick.AddListener(() => spirimonzRightInfo.SetActive(false));
        bCloseLeft.onClick.AddListener(() =>
        {
            spirimonzLeftInfo.SetActive(false);
            spirimonzRightSecondaryTitle.SetActive(false);
        });
        
        bInfoRight.onClick.AddListener(SwitchRightInfoState);
        bInfoLeft.onClick.AddListener(SwitchLeftInfoState);
        
        bChooseSpirimonz.onClick.AddListener(ChooseSpirimonz);

        foreach (UIFilterButton filter in bFilterEvidences)
        {
            if (filter != null)
            {
                if (filter.changeSound == null && filterToggleSound != null)
                    filter.changeSound = filterToggleSound;

                if (filter.changeSound != null)
                    UISoundDefaults.MarkAsUi(filter.changeSound);

                filter.onStateChanged.AddListener(UpdateSpirimonzPanel);
            }
        }

        if (secondaryTitleText != null)
            _secondaryTitleBaseColor = secondaryTitleText.color;
    }

    private void Start()
    {
        _player = Player.Instance;
        _gameManager = GameManager.Instance;
        
        InitializeSelectors();
        UpdateSpirimonzPanel();
        
        UISpirimonzInformationsSetter infoSetter = teamPanel.spmzInfoSetter;
        teamPanel.spmzInfoSetter.onInfoChanges.AddListener(() => SetLeftTitleText(infoSetter));
        SetLeftTitleText(infoSetter);
    }

    private void InitializeSelectors()
    {
        if (spirimonzPanelSelectors == null)
            spirimonzPanelSelectors = new List<UISpirimonzPanelSelector>();
        spirimonzPanelSelectors.Clear();

        if (insertEmptySelector && emptySelectorPrefab != null && spirimonzSelectorPanel != null)
        {
            bool hasEmpty = false;
            foreach (UISpirimonzPanelSelector existing in spirimonzSelectorPanel.GetComponentsInChildren<UISpirimonzPanelSelector>(true))
            {
                if (existing != null && existing.IsEmptySelector)
                {
                    hasEmpty = true;
                    break;
                }
            }

            if (!hasEmpty)
            {
                UISpirimonzPanelSelector instance = Instantiate(emptySelectorPrefab, spirimonzSelectorPanel);
                instance.transform.SetAsFirstSibling();
            }
        }

        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzSelectorPanel.GetComponentsInChildren<UISpirimonzPanelSelector>(true))
        {
            spirimonzPanelSelectors.Add(spmzSelector);
            spmzSelector.Initialize(this, _player.inventoryManager);
            if (spmzSelector.IsEmptySelector)
                spmzSelector.transform.SetAsFirstSibling();
        }
        _isSelectorInitialized = true;
    }

    private void SetLeftTitleText(UISpirimonzInformationsSetter infoSetter)
    {
        SpirimonzSettings spmzSettings = infoSetter.GetLastSpirimonzSettings();
        if (spmzSettings == null)
        {
            for (int i = 0; i < _player.inventoryManager.spirimonzTeamSettings.Count; i++)
            {
                if (_player.inventoryManager.spirimonzTeamSettings[i] != null)
                {
                    spmzSettings = _player.inventoryManager.spirimonzTeamSettings[i];
                    break;
                }
            }
        }
        if (leftTitleText != null && spmzSettings != null)
            leftTitleText.text = spmzSettings.spirimonzName;
    }

    private void OnEnable()
    {
        UpdateSpirimonzPanel();

        if (_currentSelected != null)
        {
            UnselectSpirimonzInPanel();
        }

        if (teamPanel != null)
        {
            teamPanel.allowEmptySelection = true;
            teamPanel.RefreshFromTeam();
        }
    }

    private void OnDisable()
    {
        if (teamPanel != null)
        {
            teamPanel.allowEmptySelection = false;
            teamPanel.RefreshFromTeam();
        }
    }

    private void SwitchRightInfoState()
    {
        spirimonzRightInfo.SetActive(!spirimonzRightInfo.activeInHierarchy);
        if(spirimonzRightInfo.activeInHierarchy)
            spirimonzLeftInfo.SetActive(false);
    }
    
    private void SwitchLeftInfoState()
    {
        bool isActiveInHierarchy = spirimonzLeftInfo.activeInHierarchy;
        spirimonzLeftInfo.SetActive(!isActiveInHierarchy);
        spirimonzRightSecondaryTitle.SetActive(!isActiveInHierarchy);
        
        if(spirimonzLeftInfo.activeInHierarchy)
            spirimonzRightInfo.SetActive(false);
    }

    public void SelectSpirimonzInPanel(UISpirimonzPanelSelector spmzSelector)
    {
        UnselectAllPanelSelectors(spmzSelector);
        
        if (spmzSelector.IsEmptySelector)
        {
            ApplyRemoveSelectionState();
            if (selectedSpirimonzInfoSetter != null)
                selectedSpirimonzInfoSetter.SetTypeIconsVisible(false);
        }
        else if (spmzSelector.spirimonzSettings != null)
        {
            if (secondaryTitleText != null)
            {
                secondaryTitleText.text = spmzSelector.spirimonzSettings.spirimonzName;
                secondaryTitleText.color = Color.white;
            }
            if (selectedSpirimonzInfoSetter != null)
                selectedSpirimonzInfoSetter.SetTypeIconsVisible(true);
        }

        selectionFooter.SetActive(true);

        _currentSelected = spmzSelector;

        int currentSpirimonzSelectedID = teamPanel.GetCurrentSelectionID();
        if (spmzSelector.IsEmptySelector)
        {
            bChooseSpirimonz.interactable = CanRemoveSelectedSlot(currentSpirimonzSelectedID);
        }
        else
        {
            bChooseSpirimonz.interactable = spmzSelector.spirimonzSettings != _player.inventoryManager.spirimonzTeamSettings[currentSpirimonzSelectedID];
            ApplyNormalSelectionState();
        }

        if (selectSpmzSound != null)
            selectSpmzSound.PlaySound();
    }

    public void UnselectSpirimonzInPanel()
    {
        selectionFooter.SetActive(false);
        _currentSelected = null;
        ApplyNormalSelectionState();
    }

    private void ChooseSpirimonz()
    {
        int forcedPosID = teamPanel.GetCurrentSelectionID();

        SpirimonzSettings currentSpirimonzAtThisPosition = _player.inventoryManager.spirimonzTeamSettings[forcedPosID];

        if (_currentSelected.IsEmptySelector)
        {
            if (!CanRemoveSelectedSlot(forcedPosID))
                return;

            _player.inventoryManager.RemoveSpirimonzFromTeam(forcedPosID);
            teamPanel.RefreshFromTeam();
            if (currentSpirimonzAtThisPosition != null)
            {
                UISpirimonzPanelSelector oldSelector = GetSelectedSpirimonzPanelSelector(currentSpirimonzAtThisPosition);
                if (oldSelector != null)
                    oldSelector.Unselect();
            }
            bChooseSpirimonz.interactable = false;
            ApplyRemoveSelectionState();
            return;
        }
        
        _player.inventoryManager.AddSpirimonzToTeam(_currentSelected.spirimonzSettings, forcedPosID);

        if (addToTeamSound != null)
            addToTeamSound.PlaySound();
        
        teamPanel.spmzInfoSetter.SetSpirimonz(_currentSelected.spirimonzSettings);
    }

    private UISpirimonzPanelSelector GetSelectedSpirimonzPanelSelector(SpirimonzSettings spmzSettings)
    {
        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzPanelSelectors)
        {
            if (!spmzSelector.IsEmptySelector && spmzSelector.spirimonzSettings == spmzSettings)
            {
                return spmzSelector;
            }
        }

        return null;
    }

    private void UnselectAllPanelSelectors(UISpirimonzPanelSelector keep)
    {
        if (spirimonzPanelSelectors == null)
            return;

        foreach (UISpirimonzPanelSelector selector in spirimonzPanelSelectors)
        {
            if (selector != null && selector != keep)
                selector.Unselect();
        }
    }

    private void ApplyRemoveSelectionState()
    {
        if (bInfoLeft != null)
            bInfoLeft.interactable = false;
        if (bInfoRight != null)
            bInfoRight.interactable = false;
        if (secondaryTitleText != null)
        {
            secondaryTitleText.text = "Remove";
            secondaryTitleText.color = removeLabelColor;
        }
    }

    private void ApplyNormalSelectionState()
    {
        if (bInfoLeft != null)
            bInfoLeft.interactable = true;
        if (bInfoRight != null)
            bInfoRight.interactable = true;
    }

    private void UpdateSpirimonzPanel()
    {
        if (_isSelectorInitialized == false) return;

        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzPanelSelectors)
        {
            if (spmzSelector.IsEmptySelector)
            {
                spmzSelector.gameObject.SetActive(true);
                continue;
            }

            if (_gameManager.IsSpirimonzCaptured(spmzSelector.spirimonzSettings.spirimonzID) == false)
            {
                spmzSelector.gameObject.SetActive(false);
                continue;
            }

            for (int i = 0; i < bFilterEvidences.Length; i++)
            {
                if (bFilterEvidences[i] != null)
                {
                    GhostInvestigator.EvidenceType evidence = (GhostInvestigator.EvidenceType)i;
                    if (bFilterEvidences[i].GetState() == 1 && spmzSelector.spirimonzSettings.IsUsefulForEvidence(evidence) == false)
                    {
                        spmzSelector.gameObject.SetActive(false);
                        break;
                    }
                    else if (bFilterEvidences[i].GetState() == 2 && spmzSelector.spirimonzSettings.IsUsefulForEvidence(evidence) == true)
                    {
                        spmzSelector.gameObject.SetActive(false);
                        break;
                    }
                }
                
                spmzSelector.gameObject.SetActive(true);
            }
        }
    }

    private bool CanRemoveSelectedSlot(int slotIndex)
    {
        if (_player == null || _player.inventoryManager == null)
            return false;

        int count = 0;
        foreach (SpirimonzSettings s in _player.inventoryManager.spirimonzTeamSettings)
        {
            if (s != null)
                count++;
        }

        if (count <= 1)
            return false;

        if (slotIndex < 0 || slotIndex >= _player.inventoryManager.spirimonzTeamSettings.Count)
            return false;

        return _player.inventoryManager.spirimonzTeamSettings[slotIndex] != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref selectSpmzSound);
        UISoundDefaults.AssignIfNull(ref addToTeamSound);
        if (filterToggleSound != null)
            UISoundDefaults.MarkAsUi(filterToggleSound);
    }
#endif
}
