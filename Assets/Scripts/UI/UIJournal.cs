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

    [Header("Ghost frame")]
    public GameObject ghostFrame;
    public TextMeshProUGUI tGhostName;
    public Image iGhostImage;
    public GameObject[] ghostClues;
    public GameObject[] ghostEvidenceIcons;
    public TextMeshProUGUI[] tGhostClues;

    [Header("Mode Visibility")]
    public GameObject[] normalOrTrainingObjects;

    [Header("Training CTA")]
    public bool enableTrainingCta = true;
    public int trainingEvidenceCountForCta = 3;
    public float trainingCtaScale = 1.08f;
    public float trainingCtaDuration = 0.25f;
    public Ease trainingCtaEase = Ease.OutBack;

    [Header("Sounds")]
    public SoundParameters selectGhostTypeSound;
    public SoundParameters captureSound;
    
    private int _selectedSlotsCount;
    private Tweener _captureCtaTween;
    private Tweener _ghostCtaTween;
    private UIGhostTypeSlot _ghostCtaSlot;
    private Vector3 _ghostCtaBaseScale;
    private Vector3 _captureCtaBaseScale;
    private bool _captureBaseCached;

    private void Awake()
    {
        CacheCaptureBaseScale();

        foreach (UIGhostTypeSlot slot in ghostTypeSlots)
        {
            slot.OnChangeForcedState.AddListener(SetCaptureButtonState);
            slot.SetJournal(this);
        }

        ApplyModeVisibility();
        
        SetCaptureButtonState();
        
        if(captureButton != null)
            captureButton.onClick.AddListener(StartCapture);

        House house = House.Instance;

        if (house != null)
        {
            House.Instance.currentGhost.onGhostStartToHunt.AddListener(SetCaptureButtonState);
            House.Instance.currentGhost.onGhostStopToHunt.AddListener(SetCaptureButtonState);
        }

        if (GhostInvestigator.Instance != null)
            GhostInvestigator.Instance.OnInvestigationDatasChange.AddListener(OnInvestigationChanged);
    }

    private void OnEnable()
    {
        CloseGhostFrame();
        ApplyModeVisibility();
        UpdateTrainingCtas();
    }

    private void ApplyModeVisibility()
    {
        if (normalOrTrainingObjects == null || normalOrTrainingObjects.Length == 0)
            return;

        bool show = true;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsControlsTutorial)
            show = false;

        for (int i = 0; i < normalOrTrainingObjects.Length; i++)
        {
            if (normalOrTrainingObjects[i] != null)
                normalOrTrainingObjects[i].SetActive(show);
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
        captureButton.interactable = selectedSlotsCount > 0 && House.Instance.currentGhost.IsHunting() == false;

        UpdateTrainingCtas();
    }

    private void StartCapture()
    {
        gameObject.SetActive(false);
        if (captureSound != null)
            captureSound.PlaySound();
        GhostInvestigator.Instance.TryToCapture(GetSelectedGhosts());
        UIGame.Instance.tablet.TurnOffTablet();
    }

    public void OpenGhostFrame(GhostParameters ghostParameters)
    {
        string ghostTypeName = LocalizationManager.GetGhostTypeName(ghostParameters.ghostTypeData.ghostType);
        tGhostName.text = LocalizationManager.Format("ui.journal.ghost_name", ghostTypeName);
        iGhostImage.sprite = ghostParameters.ghostTypeData.ghostSprite;
        for (int i = 0; i < ghostClues.Length; i++)
        {
            bool visible = i < ghostParameters.ghostClues.Length;
            ghostClues[i].SetActive(visible);
            if (visible)
            {
                tGhostClues[i].text = ghostParameters.GetLocalizedClue(i);
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

        if (selectGhostTypeSound != null)
            selectGhostTypeSound.PlaySound();
    }

    public void ClearForcedSelections()
    {
        if (ghostTypeSlots == null)
            return;

        for (int i = 0; i < ghostTypeSlots.Length; i++)
        {
            UIGhostTypeSlot slot = ghostTypeSlots[i];
            if (slot == null)
                continue;

            slot.SetForcedState(UIGhostTypeSlot.GhostTypeSlotForcedState.none, false);
        }

        SetCaptureButtonState();
    }

    public void RefreshModeVisibility()
    {
        ApplyModeVisibility();
        UpdateTrainingCtas();
    }

    private void OnInvestigationChanged(GhostInvestigator.EvidenceType evidenceType)
    {
        UpdateTrainingCtas();
    }

    private bool IsTrainingMode()
    {
        return TutorialManager.Instance != null && TutorialManager.Instance.IsTraining;
    }

    private bool IsTutorialMode()
    {
        return TutorialManager.Instance != null && TutorialManager.Instance.IsControlsTutorial;
    }

    private void UpdateTrainingCtas()
    {
        if (!enableTrainingCta || !IsTrainingMode())
        {
            StopTrainingCtas();
            return;
        }

        GhostInvestigator investigator = GhostInvestigator.Instance;
        if (investigator == null)
        {
            StopTrainingCtas();
            return;
        }

        int presentCount = 0;
        foreach (GhostInvestigator.EvidenceType type in Enum.GetValues(typeof(GhostInvestigator.EvidenceType)))
        {
            if (investigator.GetEvidenceState(type) == GhostInvestigator.EvidenceState.Present)
                presentCount++;
        }

        if (presentCount < trainingEvidenceCountForCta)
        {
            StopTrainingCtas();
            return;
        }

        List<GhostParameters> suspects = investigator.possibleSuspects;
        if (suspects == null || suspects.Count != 1)
        {
            StopTrainingCtas();
            return;
        }

        GhostParameters target = suspects[0];
        UIGhostTypeSlot slot = FindSlotForGhost(target);
        if (slot == null)
        {
            StopTrainingCtas();
            return;
        }

        if (slot.currentForcedState == UIGhostTypeSlot.GhostTypeSlotForcedState.selected)
        {
            StopGhostCta();
            StartCaptureCta();
        }
        else
        {
            StopCaptureCta();
            StartGhostCta(slot);
        }
    }

    private UIGhostTypeSlot FindSlotForGhost(GhostParameters target)
    {
        if (ghostTypeSlots == null)
            return null;

        for (int i = 0; i < ghostTypeSlots.Length; i++)
        {
            UIGhostTypeSlot slot = ghostTypeSlots[i];
            if (slot != null && slot.ghostParameters == target)
                return slot;
        }

        return null;
    }

    private void StartGhostCta(UIGhostTypeSlot slot)
    {
        if (slot == null)
            return;

        if (_ghostCtaSlot != slot)
        {
            StopGhostCta();
            _ghostCtaSlot = slot;
            _ghostCtaBaseScale = slot.transform.localScale;
        }

        if (_ghostCtaTween != null && _ghostCtaTween.IsActive())
            return;

        _ghostCtaTween = slot.transform
            .DOScale(_ghostCtaBaseScale * trainingCtaScale, trainingCtaDuration)
            .SetEase(trainingCtaEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopGhostCta()
    {
        if (_ghostCtaTween != null)
        {
            _ghostCtaTween.Kill();
            _ghostCtaTween = null;
        }

        if (_ghostCtaSlot != null)
        {
            _ghostCtaSlot.transform.localScale = _ghostCtaBaseScale;
            _ghostCtaSlot = null;
        }
    }

    private void StartCaptureCta()
    {
        if (captureButton == null)
            return;

        if (_captureCtaTween != null && _captureCtaTween.IsActive())
            return;

        CacheCaptureBaseScale();
        _captureCtaTween = captureButton.transform
            .DOScale(_captureCtaBaseScale * trainingCtaScale, trainingCtaDuration)
            .SetEase(trainingCtaEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopCaptureCta()
    {
        if (_captureCtaTween != null)
        {
            _captureCtaTween.Kill();
            _captureCtaTween = null;
        }

        CacheCaptureBaseScale();
        if (captureButton != null)
            captureButton.transform.localScale = _captureCtaBaseScale;
    }

    private void StopTrainingCtas()
    {
        StopGhostCta();
        StopCaptureCta();
    }

    private void CacheCaptureBaseScale()
    {
        if (_captureBaseCached)
            return;

        if (captureButton != null)
        {
            _captureCtaBaseScale = captureButton.transform.localScale;
            if (_captureCtaBaseScale == Vector3.zero)
                _captureCtaBaseScale = Vector3.one;
        }
        else
        {
            _captureCtaBaseScale = Vector3.one;
        }

        _captureBaseCached = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref selectGhostTypeSound);
        UISoundDefaults.AssignIfNull(ref captureSound);
    }
#endif
}
