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

    private UISpirimonzPanelSelector _currentSelected;
    private Player _player;

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
    }

    private void Start()
    {
        _player = Player.Instance;
        
        foreach (UISpirimonzPanelSelector spmzSelector in spirimonzSelectorPanel.GetComponentsInChildren<UISpirimonzPanelSelector>())
        {
            spirimonzPanelSelectors.Add(spmzSelector);
            spmzSelector.Initialize(this, _player.inventoryManager);
        }

        UISpirimonzInformationsSetter infoSetter = teamPanel.spmzInfoSetter;
        teamPanel.spmzInfoSetter.onInfoChanges.AddListener(() => SetSecondaryTitleText(infoSetter));
        SetSecondaryTitleText(infoSetter);
    }

    private void SetSecondaryTitleText(UISpirimonzInformationsSetter infoSetter)
    {
        secondaryTitleText.text = infoSetter.GetLastSpirimonzSettings().spirimonzName;
    }

    private void OnEnable()
    {
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

        int currentSpirimonzSelectedID = _player.inventoryManager.currentSelectedIndex;
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
}