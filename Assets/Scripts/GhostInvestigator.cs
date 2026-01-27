using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[ExecuteAlways] // Permet d’exécuter OnValidate même hors playmode
public class GhostInvestigator : GameBehaviour
{
    public static GhostInvestigator Instance { get; private set; }
    
    [Header("CaptureScene")]
    public CaptureScene captureScene;
    public float delayBeforeActivatingCaptureScene = 3f;
    public float fadeDuration = 2f;
    
    public enum EvidenceType
    {
        SpiritPrints,
        EatFruits,
        BlowUpFlammables,
        FreezingTemperature,
        HighSpiritActivities,
        SpiritOrbs,
        Radioactivity
    }
    
    public enum EvidenceState
    {
        Unknown,   // Rien
        Present,   // +
        Absent     // -
    }
    
    [Header("Références des Ghosts")]
    public List<GhostParameters> allGhostParameters = new List<GhostParameters>();

    [Header("Evidences")]
    public EvidenceState SpiritPrints;
    public EvidenceState EatFruits;
    public EvidenceState BlowUpFlammables;
    public EvidenceState FreezingTemperature;
    public EvidenceState HighSpiritActivities;
    public EvidenceState SpiritOrbs;
    public EvidenceState Radioactivity;

    [Header("Suspects dynamiques")]
    [ReadOnly] // Optionnel : pour ne pas éditer dans l’inspecteur, nécessite un attribut custom
    public List<GhostParameters> possibleSuspects = new List<GhostParameters>();
    
    private Dictionary<EvidenceType, EvidenceState> evidences = new();
    
    public UnityEvent<EvidenceType> OnInvestigationDatasChange;

    private bool _success;
    
    private void Awake()
    {
        Instance = this;
        captureScene.gameObject.SetActive(false);
    }
    
    private void Start()
    {
        foreach (EvidenceType type in Enum.GetValues(typeof(EvidenceType)))
            evidences[type] = EvidenceState.Unknown;

        UpdatePossibleSuspects();
    }

    private void OnValidate()
    {
        InitEvidences();
        UpdatePossibleSuspects();
    }

    private void UpdatePossibleSuspects()
    {
        possibleSuspects = new List<GhostParameters>(allGhostParameters);

        Filter(EvidenceType.SpiritPrints, gp => gp.SpiritPrints);
        Filter(EvidenceType.EatFruits, gp => gp.EatFruits);
        Filter(EvidenceType.BlowUpFlammables, gp => gp.BlowUpFlammables);
        Filter(EvidenceType.FreezingTemperature, gp => gp.FreezingTemperature);
        Filter(EvidenceType.HighSpiritActivities, gp => gp.HighSpiritActivities);
        Filter(EvidenceType.SpiritOrbs, gp => gp.SpiritOrbs);
        Filter(EvidenceType.Radioactivity, gp => gp.Radioactivity);
    }

    private void Filter(EvidenceType type, Func<GhostParameters, bool> hasEvidence)
    {
        EvidenceState state = evidences[type];

        if (state == EvidenceState.Present)
            possibleSuspects.RemoveAll(gp => !hasEvidence(gp));
        else if (state == EvidenceState.Absent)
            possibleSuspects.RemoveAll(gp => hasEvidence(gp));
    }
    
    public EvidenceState GetEvidenceState(EvidenceType type)
    {
        return evidences[type];
    }

    public void SetEvidenceState(EvidenceType type, EvidenceState state)
    {
        evidences[type] = state;
        UpdatePossibleSuspects();
        
        OnInvestigationDatasChange?.Invoke(type);
    }
    
    private void OnEnable()
    {
        InitEvidences();
    }
    
    private void InitEvidences()
    {
        if (evidences == null)
            evidences = new Dictionary<EvidenceType, EvidenceState>();

        foreach (EvidenceType type in Enum.GetValues(typeof(EvidenceType)))
        {
            if (!evidences.ContainsKey(type))
                evidences[type] = EvidenceState.Unknown;
        }
    }
    
    public int GetCaptureChancePercentage(int selectedSlotsCount)
    {
        switch (selectedSlotsCount)
        {
            case 1:
                return 100;
            case 2:
                return 45;
            case 3:
                return 10;
            default:
                return 0;
        }
    }

    private bool IsCaptureSuccessful(List<GhostParameters> selectedGhosts)
    {
        GhostParameters answer = House.Instance.currentGhost.ghostParameters;
        if (!selectedGhosts.Contains(answer)) return false;

        float percentageChances = GetCaptureChancePercentage(selectedGhosts.Count);
        float chanceRoll = Random.Range(0f, 100f);
        
        return chanceRoll <= percentageChances;
    }

    public void TryToCapture(List<GhostParameters> selectedGhosts)
    {
        _success = IsCaptureSuccessful(selectedGhosts);
        Player.Instance.LockControls(true);
        House.Instance.currentGhost.LockGhost();

        UIGame uiGame = UIGame.Instance;
        uiGame.EnablePointer(false);
        uiGame.EnableOverlay(true, fadeDuration);
        
        this.Invoke(delayBeforeActivatingCaptureScene, () =>
        {
            captureScene.gameObject.SetActive(true);
        });
    }
    
    public bool IsSuccess() => _success;
}