using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UIJournal : GameBehaviour
{
    [Header("Components")]
    public UIGhostTypeSlot[] ghostTypeSlots;
    public Button captureButton;
    public TextMeshProUGUI percentageText;
    
    private int _selectedSlotsCount;

    private void Awake()
    {
        foreach (UIGhostTypeSlot slot in ghostTypeSlots)
        {
            slot.OnChangeForcedState.AddListener(SetCaptureButtonState);
        }
        
        SetCaptureButtonState();
        
        if(captureButton != null)
            captureButton.onClick.AddListener(StartCapture);

        House house = House.Instance;

        if (house != null)
        {
            House.Instance.currentGhost.onGhostStartToHunt.AddListener(SetCaptureButtonState);
            House.Instance.currentGhost.onGhostStopToHunt.AddListener(SetCaptureButtonState);
        }
    }
    
    private List<GhostParameters> GetSelectedGhosts()
    {
        List<GhostParameters> selectedGhosts = new List<GhostParameters>();
        foreach (UIGhostTypeSlot slot in ghostTypeSlots)
        {
            if (slot.currentForcedState == UIGhostTypeSlot.GhostTypeSlotForcedState.selected)
                selectedGhosts.Add(slot.ghostParameters);
        }
        return selectedGhosts;
    }

    private void SetCaptureButtonState()
    {
        if (captureButton == null) return;
        
        int selectedSlotsCount = GetSelectedGhosts().Count;
        float percentageChances = GhostInvestigator.Instance.GetCaptureChancePercentage(selectedSlotsCount);
        captureButton.interactable = selectedSlotsCount > 0 && percentageChances > 0 && House.Instance.currentGhost.IsHunting() == false;
        percentageText.text = percentageChances + "%";
    }

    private void StartCapture()
    {
        gameObject.SetActive(false);
        GhostInvestigator.Instance.TryToCapture(GetSelectedGhosts());
        UIGame.Instance.tablet.TurnOffTablet();
    }
}