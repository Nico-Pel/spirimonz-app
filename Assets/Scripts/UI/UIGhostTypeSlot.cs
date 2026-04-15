using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIGhostTypeSlot : GameBehaviour
{
    [ReadOnly] public bool currentState;
    [ReadOnly] public GhostTypeSlotForcedState currentForcedState;
    
    public enum GhostTypeSlotForcedState
    {
        none,
        selected,
        crossedOut,
    }
    
    public GhostParameters ghostParameters;

    [Header("Components")] 
    public TextMeshProUGUI titleText;
    public Image ghostIcon;
    public GameObject selector;
    public GameObject cross;
    public Image backgroundImage;
    public Button forcedStateButton;
    public ButtonPointerHandler bInfo;

    private Color _baseTitleColor;
    private Color _baseBackgroundColor;
    private Color _baseIconColor;
    
    public UnityEvent OnChangeForcedState;
    
    private UIJournal _journal;

    private void Start()
    {
        _baseTitleColor = titleText.color;
        _baseBackgroundColor = backgroundImage.color;
        _baseIconColor = ghostIcon.color;
        
        GhostInvestigator.Instance?.OnInvestigationDatasChange.AddListener(ChangeStateDependingOnInvestigation);
        
        forcedStateButton.onClick.AddListener(SwitchForcedState);

        bInfo.onPointerDown.AddListener(OpenGhostInfo);
        bInfo.onPointerUp.AddListener(CloseGhostInfo);
    }

    public void SetJournal(UIJournal j)
    {
        _journal = j;
    }

    private void OnValidate()
    {
        if (ghostParameters != null)
        {
            ghostIcon.sprite = ghostParameters.ghostTypeData.ghostSprite;
            titleText.text = LocalizationManager.GetGhostTypeName(ghostParameters.ghostTypeData.ghostType);
        }
    }

    private void OpenGhostInfo()
    {
        if (_journal != null)
        {
            _journal.OpenGhostFrame(ghostParameters);
        }
    }
    
    private void CloseGhostInfo()
    {
        if (_journal != null)
        {
            _journal.CloseGhostFrame();
        }
    }

    private void ChangeStateDependingOnInvestigation(GhostInvestigator.EvidenceType evidenceType)
    {
        ChangeCurrentState(
            GhostInvestigator.Instance != null &&
            GhostInvestigator.Instance.possibleSuspects.Contains(ghostParameters)
        );
    }

    private void ChangeCurrentState(bool enable)
    {
        titleText.color = enable ? _baseTitleColor : new Color(_baseTitleColor.r, _baseTitleColor.g, _baseTitleColor.b, _baseTitleColor.a / 3f);
        backgroundImage.color = enable ? _baseBackgroundColor : new Color(_baseBackgroundColor.r, _baseBackgroundColor.g, _baseBackgroundColor.b, _baseBackgroundColor.a / 3f);
        ghostIcon.color = enable ? _baseIconColor : new Color(_baseIconColor.r, _baseIconColor.g, _baseIconColor.b, _baseIconColor.a / 3f);
    }

    private void SwitchForcedState()
    {
        switch (currentForcedState)
        {
            case GhostTypeSlotForcedState.none:
                if (_journal != null)
                    _journal.SelectGhostTypeSlot(this);
                else
                    ChangeForcedCurrentState(GhostTypeSlotForcedState.selected);
                break;
            case GhostTypeSlotForcedState.selected:
                ChangeForcedCurrentState(GhostTypeSlotForcedState.crossedOut);
                break;
            case GhostTypeSlotForcedState.crossedOut:
                ChangeForcedCurrentState(GhostTypeSlotForcedState.none);
                break;
        }
    }

    public void SetForcedState(GhostTypeSlotForcedState forcedState, bool notify = true)
    {
        currentForcedState = forcedState;
        selector.SetActive(forcedState == GhostTypeSlotForcedState.selected);
        cross.SetActive(forcedState == GhostTypeSlotForcedState.crossedOut);

        if (notify)
            OnChangeForcedState?.Invoke();
    }

    private void ChangeForcedCurrentState(GhostTypeSlotForcedState forcedState)
    {
        SetForcedState(forcedState);
    }
}
