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
        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzSelectorPanel.GetComponentsInChildren<UISpirimonzPanelSelector>())
        {
            spirimonzPanelSelectors.Add(spmzSelector);
            spmzSelector.Initialize(this, _player.inventoryManager);
        }
        _isSelectorInitialized = true;
    }

    private void SetSecondaryTitleText(UISpirimonzInformationsSetter infoSetter)
    {
        SpirimonzSettings spmzSettings = infoSetter.GetLastSpirimonzSettings();
        if (spmzSettings == null)
        {
            spmzSettings = _player.inventoryManager.spirimonzTeamSettings[0];
        }
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
        
        selectedSpirimonzInfoSetter.SetSpirimonz(spmzSelector.spirimonzSettings);
        selectionFooter.SetActive(true);

        _currentSelected = spmzSelector;

        int currentSpirimonzSelectedID = teamPanel.GetCurrentSelectionID();
        bChooseSpirimonz.interactable = spmzSelector.spirimonzSettings != _player.inventoryManager.spirimonzTeamSettings[currentSpirimonzSelectedID];
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
        
        _player.inventoryManager.AddSpirimonzToTeam(_currentSelected.spirimonzSettings, forcedPosID);
        
        teamPanel.spmzInfoSetter.SetSpirimonz(_currentSelected.spirimonzSettings);
        
        if (currentSpirimonzAtThisPosition != null)
        {
            GetSelectedSpirimonzPanelSelector(currentSpirimonzAtThisPosition).Select();
        }
    }

    private UISpirimonzPanelSelector GetSelectedSpirimonzPanelSelector(SpirimonzSettings spmzSettings)
    {
        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzPanelSelectors)
        {
            if (spmzSelector.spirimonzSettings == spmzSettings)
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
                        continue;
                    }
                    else if (bFilterEvidences[i].GetState() == 2 && spmzSelector.spirimonzSettings.IsUsefulForEvidence(evidence) == true)
                    {
                        spmzSelector.gameObject.SetActive(false);
                        continue;
                    }
                }
            }
            
            spmzSelector.gameObject.SetActive(true);
        }
    }
}