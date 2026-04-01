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

    [Header("Sounds")]
    public SoundParameters selectSlotSound;
    
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

        if (_inventoryManager != null)
            _inventoryManager.onTeamChange.AddListener(RefreshFromTeam);

        RefreshFromTeam();

        _initialized = true;
        SelectTargetedSpirimonz();
    }

    private void OnEnable()
    {
        RefreshFromTeam();

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
            if ((!MobileInput.Enabled && _inputManager.GetNextDown()) || MobileInput.NextDown)
            {
                NextSpirimonz();
            }
        
            if ((!MobileInput.Enabled && _inputManager.GetPreviousDown()) || MobileInput.PreviousDown)
            {
                PreviousSpirimonz();
            }
        }
    }

    private void NextSpirimonz()
    {
        int next = GetNextAvailableIndex(_currentSelectionID, 1);
        if (next >= 0)
            SelectSpirimonz(next);
    }
    
    private void PreviousSpirimonz()
    {
        int prev = GetNextAvailableIndex(_currentSelectionID, -1);
        if (prev >= 0)
            SelectSpirimonz(prev);
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
            if (!IsSlotValid(indexToFocus))
                indexToFocus = GetFirstAvailableIndex();
            if (indexToFocus >= 0)
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

        if (selectSlotSound != null)
            selectSlotSound.PlaySound();
    }
    
    public int GetCurrentSelectionID() => _currentSelectionID;

    public void RefreshFromTeam()
    {
        if (_inventoryManager == null)
            return;

        for (int i = 0; i < switchButtons.Length; i++)
        {
            bool isNull = i >= _inventoryManager.spirimonzTeamSettings.Count || _inventoryManager.spirimonzTeamSettings[i] == null;
            if (switchButtons[i] != null)
            {
                switchButtons[i].interactable = !isNull;
                switchButtons[i].onClick.RemoveAllListeners();
                if (!isNull)
                {
                    int index = i;
                    switchButtons[i].onClick.AddListener(() => SelectSpirimonz(index));
                }
            }
        }

        if (!IsSlotValid(_currentSelectionID))
        {
            int first = GetFirstAvailableIndex();
            if (first >= 0)
                SelectSpirimonz(first);
        }
    }

    private bool IsSlotValid(int index)
    {
        if (_inventoryManager == null)
            return false;
        if (index < 0 || index >= _inventoryManager.spirimonzTeamSettings.Count)
            return false;
        return _inventoryManager.spirimonzTeamSettings[index] != null;
    }

    private int GetFirstAvailableIndex()
    {
        if (_inventoryManager == null)
            return -1;

        for (int i = 0; i < _inventoryManager.spirimonzTeamSettings.Count; i++)
        {
            if (_inventoryManager.spirimonzTeamSettings[i] != null)
                return i;
        }

        return -1;
    }

    private int GetNextAvailableIndex(int startIndex, int direction)
    {
        if (_inventoryManager == null || _inventoryManager.spirimonzTeamSettings == null)
            return -1;

        int count = _inventoryManager.spirimonzTeamSettings.Count;
        if (count == 0)
            return -1;

        int index = startIndex;
        for (int i = 0; i < count; i++)
        {
            index += direction;
            if (index >= count) index = 0;
            if (index < 0) index = count - 1;

            if (_inventoryManager.spirimonzTeamSettings[index] != null)
                return index;
        }

        return -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref selectSlotSound);
    }
#endif
}
