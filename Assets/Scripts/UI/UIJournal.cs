using System;
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

    [Header("Ghost frame")]
    public GameObject ghostFrame;
    public TextMeshProUGUI tGhostName;
    public Image iGhostImage;
    public GameObject[] ghostClues;
    public GameObject[] ghostEvidenceIcons;
    public TextMeshProUGUI[] tGhostClues;
    
    private int _selectedSlotsCount;

    private void Awake()
    {
        foreach (UIGhostTypeSlot slot in ghostTypeSlots)
        {
            slot.OnChangeForcedState.AddListener(SetCaptureButtonState);
            slot.SetJournal(this);
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

    private void OnEnable()
    {
        CloseGhostFrame();
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
        captureButton.interactable = selectedSlotsCount > 0 && House.Instance.currentGhost.IsHunting() == false;

        if (percentageText != null)
        {
            percentageText.text = string.Empty;
            percentageText.gameObject.SetActive(false);
        }
    }

    private void StartCapture()
    {
        gameObject.SetActive(false);
        GhostInvestigator.Instance.TryToCapture(GetSelectedGhosts());
        UIGame.Instance.tablet.TurnOffTablet();
    }

    public void OpenGhostFrame(GhostParameters ghostParameters)
    {
        tGhostName.text = ghostParameters.ghostTypeData.ghostType.ToString() + " spirit";
        iGhostImage.sprite = ghostParameters.ghostTypeData.ghostSprite;
        for (int i = 0; i < ghostClues.Length; i++)
        {
            bool visible = i < ghostParameters.ghostClues.Length;
            ghostClues[i].SetActive(visible);
            if (visible)
            {
                tGhostClues[i].text = ghostParameters.ghostClues[i].description;
            }
        }
        
        for (int i = 0; i < ghostEvidenceIcons.Length; i++)
        {
            ghostEvidenceIcons[i].SetActive(
                ghostParameters.HasEvidence((GhostInvestigator.EvidenceType)i)
            );
        }
        
        ghostFrame.SetActive(true);
    }

    public void CloseGhostFrame()
    {
        ghostFrame.SetActive(false);
    }

    public void SelectGhostTypeSlot(UIGhostTypeSlot selectedSlot)
    {
        if (selectedSlot == null || ghostTypeSlots == null)
            return;

        for (int i = 0; i < ghostTypeSlots.Length; i++)
        {
            UIGhostTypeSlot slot = ghostTypeSlots[i];
            if (slot == null)
                continue;

            if (slot == selectedSlot)
                slot.SetForcedState(UIGhostTypeSlot.GhostTypeSlotForcedState.selected, false);
            else if (slot.currentForcedState == UIGhostTypeSlot.GhostTypeSlotForcedState.selected)
                slot.SetForcedState(UIGhostTypeSlot.GhostTypeSlotForcedState.none, false);
        }

        SetCaptureButtonState();
    }
}
