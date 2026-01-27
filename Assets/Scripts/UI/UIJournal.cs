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
    
    [Header("Panel animation")]
    public RectTransform journalPanel;
    public float openDuration = 0.4f;
    public Ease openEase = Ease.OutCubic;
    
    private float _targetWidth;
    private int _selectedSlotsCount;

    private void Awake()
    {
        _targetWidth = journalPanel.sizeDelta.x;

        foreach (UIGhostTypeSlot slot in ghostTypeSlots)
        {
            slot.OnChangeForcedState.AddListener(SetCaptureButtonState);
        }
        
        SetCaptureButtonState();
        
        captureButton.onClick.AddListener(StartCapture);
    }
    
    private void OnEnable()
    {
        JournalOpenAnimation();
    }

    private void JournalOpenAnimation()
    {
        journalPanel.DOKill();

        journalPanel.sizeDelta = new Vector2(50f, journalPanel.sizeDelta.y);
        journalPanel.gameObject.SetActive(true);

        journalPanel
            .DOSizeDelta(new Vector2(_targetWidth, journalPanel.sizeDelta.y), openDuration)
            .SetEase(openEase);
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
        int selectedSlotsCount = GetSelectedGhosts().Count;
        float percentageChances = GhostInvestigator.Instance.GetCaptureChancePercentage(selectedSlotsCount);
        captureButton.interactable = selectedSlotsCount > 0 && percentageChances > 0;
        percentageText.text = percentageChances + "%";
    }

    private void StartCapture()
    {
        gameObject.SetActive(false);
        GhostInvestigator.Instance.TryToCapture(GetSelectedGhosts());
    }
}