using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIEvidence : GameBehaviour
{
    [Header("Settings")] 
    public EvidenceParameter evidenceParameter;

    [Header("Components")] 
    public Button dotButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;
    public Image dotImage;
    public Sprite dotSprite;
    public Sprite crossSprite;

    private void Start()
    {
        if(dotButton != null)
            dotButton.onClick.AddListener(DotPressed);
        
        titleText.text = evidenceParameter.title;
        infoText.text = evidenceParameter.info;
    }

    private void DotPressed()
    {
        var investigator = GhostInvestigator.Instance;

        // État actuel
        GhostInvestigator.EvidenceState current =
            investigator.GetEvidenceState(evidenceParameter.evidenceType);

        // Cycle : Unknown → Present → Absent → Unknown
        GhostInvestigator.EvidenceState next = current switch
        {
            GhostInvestigator.EvidenceState.Unknown => GhostInvestigator.EvidenceState.Present,
            GhostInvestigator.EvidenceState.Present => GhostInvestigator.EvidenceState.Absent,
            GhostInvestigator.EvidenceState.Absent => GhostInvestigator.EvidenceState.Unknown,
            _ => GhostInvestigator.EvidenceState.Unknown
        };

        investigator.SetEvidenceState(evidenceParameter.evidenceType, next);
        RefreshVisual(next);
    }
    
    private void RefreshVisual(GhostInvestigator.EvidenceState state)
    {
        dotImage.gameObject.SetActive(state != GhostInvestigator.EvidenceState.Unknown);

        if (state == GhostInvestigator.EvidenceState.Present)
            dotImage.sprite = dotSprite;
        else if (state == GhostInvestigator.EvidenceState.Absent)
            dotImage.sprite = crossSprite;
    }
    
    private void OnEnable()
    {
        GhostInvestigator ghostInvestigator = GhostInvestigator.Instance;
        if (ghostInvestigator != null)
        {
            RefreshVisual(
                GhostInvestigator.Instance.GetEvidenceState(evidenceParameter.evidenceType)
            );
        }
    }
}