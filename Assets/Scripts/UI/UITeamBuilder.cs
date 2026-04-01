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
    
    private UISpirimonzPanelSelector _currentSelected;
    private Player _player;
    private GameManager _gameManager;

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
                filter.onStateChanged.AddListener(UpdateSpirimonzPanel);
        }
    }

    private void Start()
    {
        _player = Player.Instance;
        _gameManager = GameManager.Instance;
        
        InitializeSelectors();
        UpdateSpirimonzPanel();
        
        UISpirimonzInformationsSetter infoSetter = teamPanel.spmzInfoSetter;
        teamPanel.spmzInfoSetter.onInfoChanges.AddListener(() => SetSecondaryTitleText(infoSetter));
        SetSecondaryTitleText(infoSetter);
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

    private void SetSecondaryTitleText(UISpirimonzInformationsSetter infoSetter)
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
        if (spmzSettings != null)
            secondaryTitleText.text = spmzSettings.spirimonzName;
    }

    private void OnEnable()
    {
        UpdateSpirimonzPanel();

        if (_currentSelected != null)
        {
            UnselectSpirimonzInPanel();
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
        if (_currentSelected != null)
        {
            _currentSelected.Unselect();
        }
        
        if (!spmzSelector.IsEmptySelector)
            selectedSpirimonzInfoSetter.SetSpirimonz(spmzSelector.spirimonzSettings);
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
        }

        if (selectSpmzSound != null)
            selectSpmzSound.PlaySound();
    }

    public void UnselectSpirimonzInPanel()
    {
        selectionFooter.SetActive(false);
        _currentSelected = null;
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
            UnselectSpirimonzInPanel();
            return;
        }
        
        _player.inventoryManager.AddSpirimonzToTeam(_currentSelected.spirimonzSettings, forcedPosID);

        if (addToTeamSound != null)
            addToTeamSound.PlaySound();
        
        teamPanel.spmzInfoSetter.SetSpirimonz(_currentSelected.spirimonzSettings);
        
        if (currentSpirimonzAtThisPosition != null)
        {
            UISpirimonzPanelSelector selector = GetSelectedSpirimonzPanelSelector(currentSpirimonzAtThisPosition);
            if (selector != null)
                selector.Select();
        }
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
    }
#endif
}
