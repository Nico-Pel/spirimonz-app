using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UITeamPanel : GameBehaviour
{
    public enum AutoSelectionType
    {
        firstSpirimonz,
        equipedSpirimonz
    }
    
    public AutoSelectionType autoSelection = AutoSelectionType.equipedSpirimonz;
    
    [Header("Components")] 
    public UISpirimonzInformationsSetter spmzInfoSetter;
    
    [Header("Texts")]
    public TextMeshProUGUI[] tSwitchNbs;

    [Header("Buttons")] 
    public Button[] switchButtons;
    
    public Color selectColor;
    public Color selectIconColor;
    private Color _baseColor;
    private Color _baseIconColor;
    
    private InventoryManager _inventoryManager;
    private bool _initialized;
    
    private InputManager _inputManager;
    private int _currentSelectionID;

    private void Awake()
    {
        _baseColor = switchButtons[0].image.color;
        _baseIconColor = tSwitchNbs[0].color;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _inventoryManager = InventoryManager.Instance;
        _inputManager = InputManager.Instance;
        
        for (int i = 0; i < switchButtons.Length; i++)
        {
            bool isNull = _inventoryManager.spirimonzTeamSettings[i] == null;
            switchButtons[i].interactable = !isNull;
            if (isNull == false)
            {
                int index = i;
                switchButtons[i].onClick.AddListener(() => SelectSpirimonz(index));
            }
        }

        _initialized = true;
        SelectTargetedSpirimonz();
    }

    private void OnEnable()
    {
        if (autoSelection == AutoSelectionType.firstSpirimonz)
        {
            SelectSpirimonz(0);
        }
        else if (autoSelection == AutoSelectionType.equipedSpirimonz)
        {
            SelectTargetedSpirimonz();
        }
    }

    private void Update()
    {
        if (_inventoryManager != null)
        {
            if ((!MobileInput.Enabled && (Input.GetKeyDown(_inputManager.primaryNext) || Input.GetKeyDown(_inputManager.secondaryNext))) || MobileInput.NextDown)
            {
                NextSpirimonz();
            }
        
            if ((!MobileInput.Enabled && (Input.GetKeyDown(_inputManager.primaryPrevious) || Input.GetKeyDown(_inputManager.secondaryPrevious))) || MobileInput.PreviousDown)
            {
                PreviousSpirimonz();
            }
        }
    }

    private void NextSpirimonz()
    {
        int targetedID = _currentSelectionID + 1;
        if (targetedID >= _inventoryManager.spirimonzTeam.Count)
            targetedID = 0;
        
        SelectSpirimonz(targetedID);
    }
    
    private void PreviousSpirimonz()
    {
        int targetedID = _currentSelectionID - 1;
        if (targetedID < 0)
            targetedID = _inventoryManager.spirimonzTeam.Count - 1;
        
        SelectSpirimonz(targetedID);
    }

    private void SelectTargetedSpirimonz()
    {
        if (_inventoryManager != null && _initialized)
        {
            int indexToFocus = _inventoryManager.currentSelectedIndex;
            if (indexToFocus < 0)
            {
                indexToFocus = 0;
            }
            SelectSpirimonz(indexToFocus);
        }
    }

    private void SelectSpirimonz(int teamID)
    {
        if (teamID < 0) return;
        if (teamID >= _inventoryManager.spirimonzTeamSettings.Count) return;
        if (_inventoryManager.spirimonzTeamSettings[teamID] == null) return;
        
        SpirimonzSettings spmz = _inventoryManager.spirimonzTeamSettings[teamID];
        spmzInfoSetter.SetSpirimonz(spmz);

        for (int i = 0; i < switchButtons.Length; i++)
        {
            bool isSelected = i == teamID;
            switchButtons[i].image.color = isSelected ? selectColor : _baseColor;
            tSwitchNbs[i].color = isSelected ? selectIconColor : _baseIconColor;
        }

        _currentSelectionID = teamID;
    }
    
    public int GetCurrentSelectionID() => _currentSelectionID;
}
