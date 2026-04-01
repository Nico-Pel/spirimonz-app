using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpirimonzPanelSelector : GameBehaviour
{
    public SpirimonzSettings spirimonzSettings;
    public bool isEmptySelector;
    
    [Space]
    
    public GameObject selector;
    public GameObject inTeam;
    public Image spmzImg;
    public Button bSelect;
    
    private UITeamBuilder _teamBuilder;
    private bool _isSelected;

    private InventoryManager _inventoryManager;

    public bool IsEmptySelector => isEmptySelector || spirimonzSettings == null;
    private void Awake()
    {
        bSelect.onClick.AddListener(SwitchSelectionState);
    }

    public void Initialize(UITeamBuilder tb, InventoryManager inventoryManager)
    {
        _teamBuilder = tb;
        _inventoryManager = inventoryManager;
        UpdateIsInTeamIcon();
        inventoryManager.onTeamChange.AddListener(UpdateIsInTeamIcon);
    }

    private void UpdateIsInTeamIcon()
    {
        if (IsEmptySelector)
        {
            if (inTeam != null)
                inTeam.SetActive(false);
            return;
        }

        if (inTeam != null)
            inTeam.SetActive(_inventoryManager.IsSpirimonzInTeam(spirimonzSettings));
    }

    private void SwitchSelectionState()
    {
        if (_isSelected == false)
        {
            Select();
        }
        else
        {
            Unselect();
            _teamBuilder.UnselectSpirimonzInPanel();
        }
    }

    public void Select()
    {
        _isSelected = true;
        selector.SetActive(true);
        _teamBuilder.SelectSpirimonzInPanel(this);
    }

    public void Unselect()
    {
        _isSelected = false;
        selector.SetActive(false);
    }

    public void OnValidate()
    {
        if (spmzImg == null)
            return;

        if (spirimonzSettings != null)
        {
            spmzImg.sprite = spirimonzSettings.img;
            spmzImg.enabled = true;
        }
        else
        {
            spmzImg.sprite = null;
            spmzImg.enabled = false;
        }
    }
}
