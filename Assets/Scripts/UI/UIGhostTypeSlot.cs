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

    private Color _baseTitleColor;
    private Color _baseBackgroundColor;
    private Color _baseIconColor;
    
    public UnityEvent OnChangeForcedState;

    private void Start()
    {
        _baseTitleColor = titleText.color;
        _baseBackgroundColor = backgroundImage.color;
        _baseIconColor = ghostIcon.color;
        
        ghostIcon.sprite = ghostParameters.ghostTypeData.ghostSprite;
        titleText.text = ghostParameters.ghostTypeData.ghostType.ToString();
        
        GhostInvestigator.Instance?.OnInvestigationDatasChange.AddListener(ChangeStateDependingOnInvestigation);
        
        forcedStateButton.onClick.AddListener(SwitchForcedState);
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

    private void ChangeForcedCurrentState(GhostTypeSlotForcedState forcedState)
    {
        currentForcedState = forcedState;
        selector.SetActive(forcedState == GhostTypeSlotForcedState.selected);
        cross.SetActive(forcedState == GhostTypeSlotForcedState.crossedOut);
        
        OnChangeForcedState?.Invoke();
    }
}