using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : GameBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static bool IsTutorialActive => Instance != null && Instance.isActiveAndEnabled && Instance._isControlsTutorial;
    public static bool TutorialDoorUnlockedRuntime { get; private set; }
    private enum StepState
    {
        None,
        WaitingDialogue,
        InProgress,
        WaitingReturn,
        AutoAdvance
    }

    [Serializable]
    public class TutorialStepHooks
    {
        public UnityEvent onStepStart;
        public UnityEvent onStepComplete;
    }

    public enum TutorialSceneMode
    {
        Controls,
        Investigation
    }

    [Header("References")]
    public NPC questNpc;
    public UITutorialObjective objectiveUI;

    [Header("Input Gate")]
    public bool enableInputGate = true;

    [Header("Mode")]
    public TutorialSceneMode sceneMode = TutorialSceneMode.Controls;

    [Header("Team Override")]
    public bool applyForcedTeamOnStart = true;
    public float teamApplyDelay = 0.2f;
    public List<SpirimonzSettings> forcedTeam = new List<SpirimonzSettings>(5);

    [Header("UI Text")]
    public string returnToNpcKey = "tutorial.return_to_npc";
    public string returnToNpcEnglish = "Return to Vaness !";
    public string returnToNpcFrench = "Retourne voir Vaness !";
    public string objectiveCompleteKey = "tutorial.objective_complete";
    public string objectiveCompleteEnglish = "Objective completed !";
    public string objectiveCompleteFrench = "Objectif termine !";

    [Header("Ghost")]
    public bool blockGhostActivitiesByDefault = true;
    public bool applyGhostSetupOnStart = true;
    public bool forceGhostParameters = false;
    public GhostParameters forcedGhostParameters;
    public bool forceGhostModel = false;
    public Ghost forcedGhostModel;
    public bool forceGhostRoom = false;
    public Room forcedGhostRoom;
    public string forcedGhostRoomName;
    public int forcedGhostRoomIndex = -1;

    [Header("Steps")]
    public List<TutorialStepSO> steps = new List<TutorialStepSO>();
    public List<TutorialStepHooks> stepHooks = new List<TutorialStepHooks>();

#if UNITY_EDITOR
    [Header("Debug (Editor Only)")]
    [Tooltip("Start directly at this step index (0-based). Set to -1 to disable.")]
    public int debugStep = -1;
#endif

    [Header("Sounds")]
    public SoundParameters progressSoundParameters;
    public SoundParameters completeSoundParameters;

    [Header("Hunt Fail")]
    public Dialogue huntFailDialogue;
    public Transform huntFailTeleportPoint;
    public float huntFailTeleportDistance = 2f;
    public float huntFailFadeDuration = 2f;
    public float huntFailBlackDelay = 0.2f;
    public bool huntFailUseDialogueCamera = false;

    [Header("Journal Reset")]
    public GameObject ghostTypesRoot;
    public GameObject captureButtonRoot;

    [Header("Tutorial Radiation")]
    [Min(0.1f)] public float tutorialRadiationDuration = 9999f;

    [Header("Tutorial Exit")]
    public bool skipEndGameOnExit = true;
    public bool useWorldTutoSpawnOnExit = true;

    [Header("Tutorial Temperature")]
    [Min(0.1f)] public float tutorialCoolingMultiplier = 1.5f;

    [Header("Training Objective")]
    public bool showTrainingObjective = true;
    public string trainingObjectiveKey = "tutorial.training_objective";
    [TextArea] public string trainingObjectiveEnglish = "Find the 3 evidences and capture the Spirimonz!";
    [TextArea] public string trainingObjectiveFrench = "Trouve les 3 preuves et capture le spirimonz !";

    [Header("Training Reload")]
    [Min(0f)] public float trainingReloadFadeDuration = 1f;

    [Header("Mode Visibility")]
    public List<GameObject> tutorialOnlyObjects = new List<GameObject>();
    public List<GameObject> trainingOnlyObjects = new List<GameObject>();

    [Header("Mode Doors")]
    public List<HouseEntry> tutorialLockedEntries = new List<HouseEntry>();
    public List<HouseEntry> trainingUnlockedEntries = new List<HouseEntry>();

    [Header("Training Capture")]
    public SpirimonzSettings forcedCapturedSpirimonz;

    public UnityEvent onTutorialComplete;

    private StepState _state = StepState.None;
    private int _currentStepIndex = -1;
    private TutorialStepSO _currentStep;
    private int _progress;
    private int _lastProgressValue;
    private float _waitElapsed;
    private float _dropZoneStableTime;
    private CatchableObject _dropZoneTracked;
    private readonly List<CatchableObject> _dropZoneCandidates = new List<CatchableObject>();
    private readonly List<Collider> _dropZoneRuntimeZones = new List<Collider>();
    private readonly List<FlammableElement> _flammableCandidates = new List<FlammableElement>();
    private readonly List<SpmzDetector> _detectorCandidates = new List<SpmzDetector>();
    private readonly HashSet<SpmzDetector> _detectorsCounted = new HashSet<SpmzDetector>();
    private readonly HashSet<SpmzDetector> _detectorListeners = new HashSet<SpmzDetector>();
    private readonly Dictionary<SpmzDetector, int> _detectorLastValues = new Dictionary<SpmzDetector, int>();
    private float _nextDetectorRefreshTime;
    private readonly List<RadiationDetector> _radiationCandidates = new List<RadiationDetector>();
    private readonly HashSet<RadiationDetector> _radiationListeners = new HashSet<RadiationDetector>();
    private float _nextRadiationRefreshTime;
    private readonly List<GhostOrbsParticles> _orbsCandidates = new List<GhostOrbsParticles>();
    private GhostOrbsParticles _currentOrbsTarget;
    private float _orbsHoldTimer;
    private float _nextOrbsRefreshTime;
    private Room _tutorialRadiationRoom;
    private bool _tutorialRadiationActive;
    private int _tutorialRadiationStopAfterStepIndex = -1;
    private bool _watchFlammableUpright;
    private bool _huntFailed;
    private bool _huntInProgress;
    private Coroutine _huntFailRoutine;
    private bool _restartStepAfterHuntFailDialogue;

    private InventoryManager _inventory;
    private Ghost _ghost;
    private UIGame _uiGame;
    private Coroutine _autoAdvanceRoutine;
    private GhostParameters _appliedGhostParameters;
    private GameManager.HouseSceneMode _houseMode = GameManager.HouseSceneMode.NormalMap;
    private bool _isControlsTutorial;
    private bool _isTraining;
    private bool _initialized;
    private bool _consumedHouseMode;

    private readonly List<Action> _unsubscribers = new List<Action>();
    private readonly HashSet<UnityEngine.Object> _counted = new HashSet<UnityEngine.Object>();

    public GameManager.HouseSceneMode CurrentHouseMode => _houseMode;
    public bool IsControlsTutorial => _isControlsTutorial;
    public bool IsTraining => _isTraining;

    public bool ShouldForceGhostModelForMode(GameManager.HouseSceneMode mode)
    {
        return (mode == GameManager.HouseSceneMode.Tutorial || mode == GameManager.HouseSceneMode.Training)
               && forceGhostModel
               && forcedGhostModel != null;
    }

    public void ForceCaptureCurrentGhostSpirimonz()
    {
        if (House.Instance == null || GameManager.Instance == null)
            return;

        SpirimonzSettings settings = forcedCapturedSpirimonz != null
            ? forcedCapturedSpirimonz
            : House.Instance.GetSpirimonzSettings();
        if (settings == null)
            return;

        GameManager.Instance.UnlockSpirimonz(settings.spirimonzID);
    }

    public void ReloadSceneAsTraining()
    {
        StartCoroutine(ReloadSceneAsTrainingRoutine());
    }

    private IEnumerator ReloadSceneAsTrainingRoutine()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UIGame uiGame = UIGame.Instance;
        if (uiGame != null)
            uiGame.EnableOverlay(true, trainingReloadFadeDuration);

        if (trainingReloadFadeDuration > 0f)
            yield return new WaitForSeconds(trainingReloadFadeDuration);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHouseSceneWithMode(sceneName, GameManager.HouseSceneMode.Training);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    public void MarkTutorialDoorUnlocked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetBool(SaveKeys.TUTORIAL_DOOR_UNLOCKED, true);

        TutorialDoorUnlockedRuntime = true;
    }

    private GameManager.HouseSceneMode GetFallbackHouseSceneMode()
    {
        return sceneMode == TutorialSceneMode.Controls
            ? GameManager.HouseSceneMode.Tutorial
            : GameManager.HouseSceneMode.Training;
    }

    private void ResolveHouseSceneMode()
    {
        if (GameManager.Instance != null)
        {
            _houseMode = GameManager.Instance.PeekNextHouseSceneMode();
        }
        else
        {
            _houseMode = GetFallbackHouseSceneMode();
        }

        _isControlsTutorial = _houseMode == GameManager.HouseSceneMode.Tutorial;
        _isTraining = _houseMode == GameManager.HouseSceneMode.Training;
    }

    private void ApplyModeVisibility()
    {
        if (tutorialOnlyObjects != null)
        {
            bool enable = _isControlsTutorial;
            for (int i = 0; i < tutorialOnlyObjects.Count; i++)
            {
                if (tutorialOnlyObjects[i] != null)
                    tutorialOnlyObjects[i].SetActive(enable);
            }
        }

        if (trainingOnlyObjects != null)
        {
            bool enable = _isTraining;
            for (int i = 0; i < trainingOnlyObjects.Count; i++)
            {
                if (trainingOnlyObjects[i] != null)
                    trainingOnlyObjects[i].SetActive(enable);
            }
        }
    }

    private void ApplyModeDoorLocks()
    {
        if (_isControlsTutorial && tutorialLockedEntries != null)
        {
            for (int i = 0; i < tutorialLockedEntries.Count; i++)
            {
                if (tutorialLockedEntries[i] != null)
                    tutorialLockedEntries[i].SetLocked(true, true);
            }
        }

        if (_isTraining && trainingUnlockedEntries != null)
        {
            for (int i = 0; i < trainingUnlockedEntries.Count; i++)
            {
                if (trainingUnlockedEntries[i] != null)
                    trainingUnlockedEntries[i].SetLocked(false, true);
            }
        }
    }

    private void ConsumeHouseSceneModeIfNeeded()
    {
        if (_consumedHouseMode || GameManager.Instance == null)
            return;

        GameManager.Instance.ConsumeNextHouseSceneMode();
        _consumedHouseMode = true;
    }

    private void Awake()
    {
        ResolveHouseSceneMode();
        ApplyModeVisibility();
        if (!_isControlsTutorial && !_isTraining)
        {
            enabled = false;
            return;
        }

        Instance = this;
        _initialized = true;

        if (enableInputGate && _isControlsTutorial)
        {
            TutorialInputGate.Enabled = true;
            TutorialInputGate.ResetAll(false);
            AllowNpcNavigationInputs();
        }

        if (questNpc != null)
        {
            questNpc.onDialogueStart.AddListener(OnNpcDialogueStart);
            questNpc.onDialogueEnd.AddListener(OnNpcDialogueEnd);
        }
    }

    private IEnumerator Start()
    {
        ConsumeHouseSceneModeIfNeeded();

        _inventory = InventoryManager.Instance;
        _uiGame = UIGame.Instance;
        _ghost = House.Instance != null ? House.Instance.currentGhost : null;

        ApplyTutorialUiLayout();

        if (_isControlsTutorial)
        {
            if (applyForcedTeamOnStart)
                yield return ApplyForcedTeamRoutine();

            if (applyGhostSetupOnStart)
                ApplyTutorialGhostSetup();
        }
        else if (_isTraining)
        {
            if (applyForcedTeamOnStart)
                yield return ApplyForcedTeamRoutine();

            if (enableInputGate)
            {
                TutorialInputGate.ResetAll(true);
                TutorialInputGate.Enabled = false;
            }

            if (!skipEndGameOnExit)
                skipEndGameOnExit = true;
            if (!useWorldTutoSpawnOnExit)
                useWorldTutoSpawnOnExit = true;

            if (GameManager.Instance != null)
                GameManager.Instance.disableMoneyGain = true;

            if (applyGhostSetupOnStart)
                ApplyTrainingGhostSetup();

            EnsureTrainingJournalVisibility();

            if (showTrainingObjective && objectiveUI != null)
                objectiveUI.ShowMessage(GetLocalizedTrainingObjective(), false);
        }

#if UNITY_EDITOR
        int startIndex = 0;
        if (debugStep >= 0)
        {
            if (steps != null && steps.Count > 0 && debugStep < steps.Count)
                startIndex = debugStep;
            else
                Debug.LogWarning($"TutorialManager: debugStep {debugStep} is out of range, starting at 0.");
        }
#else
        int startIndex = 0;
#endif

        if (_isControlsTutorial)
            SetStep(startIndex);

        this.Invoke(0.1f, ApplyModeDoorLocks);
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;

        if (_uiGame != null)
            _uiGame.tablet?.ApplyControlsTutorialLayout(false);
        objectiveUI?.ApplyControlsTutorialLayout(false);

        if (!_initialized)
            return;

        ClearSubscriptions();
        StopAutoAdvance();
        StopTutorialRadiation();
        if (enableInputGate && _isControlsTutorial)
        {
            TutorialInputGate.ResetAll(true);
            TutorialInputGate.Enabled = false;
        }

        if (_isTraining && GameManager.Instance != null)
            GameManager.Instance.disableMoneyGain = false;

        if (Instance == this)
            Instance = null;
    }

    private IEnumerator ApplyForcedTeamRoutine()
    {
        if (teamApplyDelay > 0f)
            yield return new WaitForSeconds(teamApplyDelay);
        else
            yield return null;

        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory != null && forcedTeam != null && forcedTeam.Count > 0)
            _inventory.ApplyTemporaryTeam(forcedTeam);
    }

    private void ApplyTutorialUiLayout()
    {
        bool enableCompactTutorialLayout = _isControlsTutorial;

        if (_uiGame != null && _uiGame.tablet != null)
            _uiGame.tablet.ApplyControlsTutorialLayout(enableCompactTutorialLayout);

        if (objectiveUI != null)
            objectiveUI.ApplyControlsTutorialLayout(enableCompactTutorialLayout);
    }

    private void OnNpcDialogueEnd()
    {
        if (_state == StepState.WaitingDialogue)
        {
            StartCurrentStep();
        }
        else if (_state == StepState.WaitingReturn)
        {
            AdvanceToNextStep();
        }
        else if (_state == StepState.InProgress)
        {
            InitObjectiveUI();
            if (_restartStepAfterHuntFailDialogue)
            {
                ReplayCurrentStepStartHooks();
                _restartStepAfterHuntFailDialogue = false;
            }
        }
    }

    private void OnNpcDialogueStart()
    {
        objectiveUI?.Hide();
    }

    private void SetStep(int index)
    {
        ClearSubscriptions();
        StopAutoAdvance();
        _counted.Clear();
        _progress = 0;
        _lastProgressValue = 0;
        _waitElapsed = 0f;
        _dropZoneStableTime = 0f;
        _dropZoneTracked = null;
        _dropZoneCandidates.Clear();
        _dropZoneRuntimeZones.Clear();
        _detectorCandidates.Clear();
        _detectorsCounted.Clear();
        _detectorListeners.Clear();
        _detectorLastValues.Clear();
        _nextDetectorRefreshTime = 0f;
        _radiationCandidates.Clear();
        _radiationListeners.Clear();
        _nextRadiationRefreshTime = 0f;
        _orbsCandidates.Clear();
        _currentOrbsTarget = null;
        _orbsHoldTimer = 0f;
        _nextOrbsRefreshTime = 0f;
        _huntFailed = false;
        _huntInProgress = false;
        _restartStepAfterHuntFailDialogue = false;
        if (_huntFailRoutine != null)
        {
            StopCoroutine(_huntFailRoutine);
            _huntFailRoutine = null;
        }

        _currentStepIndex = index;
        if (steps == null || index < 0 || index >= steps.Count)
        {
            CompleteTutorial();
            return;
        }

        _currentStep = steps[index];
        _state = StepState.WaitingDialogue;
        AllowNpcNavigationInputs();

        if (questNpc != null && _currentStep != null && _currentStep.dialogue != null)
            questNpc.dialogue = _currentStep.dialogue;
    }

    private void StartCurrentStep()
    {
        if (_currentStep == null)
            return;

        _state = StepState.InProgress;
        _progress = 0;
        _lastProgressValue = 0;
        _waitElapsed = 0f;
        _dropZoneStableTime = 0f;
        _dropZoneTracked = null;
        _dropZoneCandidates.Clear();
        _dropZoneRuntimeZones.Clear();
        _detectorCandidates.Clear();
        _detectorsCounted.Clear();
        _detectorListeners.Clear();
        _detectorLastValues.Clear();
        _nextDetectorRefreshTime = 0f;
        _radiationCandidates.Clear();
        _radiationListeners.Clear();
        _nextRadiationRefreshTime = 0f;
        _orbsCandidates.Clear();
        _currentOrbsTarget = null;
        _orbsHoldTimer = 0f;
        _nextOrbsRefreshTime = 0f;
        _counted.Clear();
        _huntFailed = false;
        _huntInProgress = false;
        _restartStepAfterHuntFailDialogue = false;
        if (_huntFailRoutine != null)
        {
            StopCoroutine(_huntFailRoutine);
            _huntFailRoutine = null;
        }

        ApplyInputMask(_currentStep.inputMask);
        ApplyGhostOverrideForStep();
        GetStepHooks(_currentStepIndex)?.onStepStart?.Invoke();

        SetupObjectiveTracking(_currentStep.objective);
        if (_state != StepState.InProgress)
            return;

        InitObjectiveUI();

        if (_currentStep.objective == null || _currentStep.objective.type == TutorialObjectiveType.None)
        {
            CompleteCurrentStep();
        }
    }

    private void CompleteCurrentStep()
    {
        if (_state != StepState.InProgress)
            return;

        ClearSubscriptions();
        StopAutoAdvance();
        PlayCompleteSound();
        SetProgress(GetGoal(), false);
        GetStepHooks(_currentStepIndex)?.onStepComplete?.Invoke();
        CheckTutorialRadiationStop();

        bool requireNpcReturn = _currentStep == null || _currentStep.requireNpcReturn;
        if (requireNpcReturn)
        {
            _state = StepState.WaitingReturn;
            AllowNpcNavigationInputs();
            ShowCompletionCTA(GetLocalizedReturnToNpc(), true);
        }
        else
        {
            _state = StepState.AutoAdvance;
            ShowCompletionCTA(GetLocalizedObjectiveComplete(), true);
            float delay = _currentStep != null ? Mathf.Max(0f, _currentStep.autoAdvanceDelay) : 0f;
            _autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine(delay));
        }

        int nextIndex = _currentStepIndex + 1;
        if (questNpc != null && steps != null && nextIndex >= 0 && nextIndex < steps.Count)
        {
            Dialogue nextDialogue = steps[nextIndex].dialogue;
            if (nextDialogue != null)
                questNpc.dialogue = nextDialogue;
        }
    }

    private void AdvanceToNextStep()
    {
        int nextIndex = _currentStepIndex + 1;
        if (steps == null || nextIndex >= steps.Count)
        {
            CompleteTutorial();
            return;
        }

        SetStep(nextIndex);
        StartCurrentStep();
    }

    private void CompleteTutorial()
    {
        _state = StepState.None;
        ClearSubscriptions();
        StopAutoAdvance();
        objectiveUI?.Hide();
        _ghost?.ClearTutorialOverride();
        StopTutorialRadiation();

        if (enableInputGate)
        {
            TutorialInputGate.ResetAll(true);
            TutorialInputGate.Enabled = false;
        }

        onTutorialComplete?.Invoke();
    }

    private void ApplyInputMask(TutorialInputMask mask)
    {
        if (!enableInputGate || mask == null)
            return;

        TutorialInputGate.Enabled = true;
        TutorialInputGate.AllowMovement = mask.allowMovement;
        TutorialInputGate.AllowLook = mask.allowLook;
        TutorialInputGate.AllowInteract = mask.allowInteract;
        TutorialInputGate.AllowInteractSpmz = mask.allowInteractSpmz;
        TutorialInputGate.AllowUseWatch = mask.allowUseWatch;
        TutorialInputGate.AllowGrab = mask.allowGrab;
        TutorialInputGate.AllowPickupSpmz = mask.allowPickupSpmz;
        TutorialInputGate.AllowLight = true;
        TutorialInputGate.AllowSecondary = mask.allowSecondary;
        TutorialInputGate.AllowJournal = mask.allowJournal;
        TutorialInputGate.AllowTeamMenu = mask.allowTeamMenu;
        TutorialInputGate.AllowDropSpmz = mask.allowDropSpmz;

        if (!mask.useSeparateDropThrow)
        {
            if (mask.allowDrop || mask.allowThrow)
            {
                TutorialInputGate.AllowDrop = mask.allowDrop;
                TutorialInputGate.AllowThrow = mask.allowThrow;
            }
            else
            {
                TutorialInputGate.AllowDrop = mask.allowThrowDrop;
                TutorialInputGate.AllowThrow = mask.allowThrowDrop;
            }
        }
        else
        {
            TutorialInputGate.AllowDrop = mask.allowDrop;
            TutorialInputGate.AllowThrow = mask.allowThrow;
        }

        if (mask.overrideInventorySlots && mask.allowInventorySlots != null && mask.allowInventorySlots.Length > 0)
        {
            if (TutorialInputGate.AllowInventorySlots == null || TutorialInputGate.AllowInventorySlots.Length != 6)
                TutorialInputGate.AllowInventorySlots = new bool[6];

            for (int i = 0; i < 6; i++)
                TutorialInputGate.AllowInventorySlots[i] = i < mask.allowInventorySlots.Length && mask.allowInventorySlots[i];
        }
    }

    private void AllowNpcNavigationInputs()
    {
        if (!enableInputGate || !_isControlsTutorial)
            return;

        TutorialInputGate.Enabled = true;
        TutorialInputGate.AllowMovement = true;
        TutorialInputGate.AllowLook = true;
        TutorialInputGate.AllowInteract = true;
    }

    private void SetupObjectiveTracking(TutorialObjective objective)
    {
        if (objective == null)
            return;

        switch (objective.type)
        {
            case TutorialObjectiveType.ActivateActivablesSimultaneous:
                SetupActivablesObjective(objective);
                break;
            case TutorialObjectiveType.GrabCatchable:
                SetupCatchableObjective(objective);
                break;
            case TutorialObjectiveType.DropSpirimonz:
                SetupSpirimonzDropObjective();
                break;
            case TutorialObjectiveType.GrabSpirimonz:
                SetupSpirimonzGrabObjective();
                break;
            case TutorialObjectiveType.LightFlammables:
                SetupFlammableObjective(objective);
                break;
            case TutorialObjectiveType.RevealPrints:
                SetupPrintObjective(objective);
                break;
            case TutorialObjectiveType.DetectActivity:
                SetupDetectorObjective(objective);
                break;
            case TutorialObjectiveType.DetectSpiritOrbs:
                SetupSpiritOrbsObjective(objective);
                break;
            case TutorialObjectiveType.LeaveHouse:
                SetupLeaveHouseObjective();
                break;
            case TutorialObjectiveType.CheckEvidence:
                SetupEvidenceObjective(objective);
                break;
            case TutorialObjectiveType.DetectFreezing:
                SetupFreezingObjective(objective);
                break;
            case TutorialObjectiveType.GhostEatFruit:
                SetupEatFruitObjective(objective);
                break;
            case TutorialObjectiveType.DetectRadiation:
                SetupRadiationObjective(objective);
                break;
            case TutorialObjectiveType.SurviveHunt:
                SetupSurviveHuntObjective(objective);
                break;
            case TutorialObjectiveType.OpenJournal:
                SetupJournalObjective();
                break;
            case TutorialObjectiveType.WaitSpirimonz:
                SetupWaitObjective(objective);
                break;
            case TutorialObjectiveType.PlaceObjectInZone:
                SetupDropZoneObjective(objective);
                break;
        }
    }

    private void SetupWaitObjective(TutorialObjective objective)
    {
        _waitElapsed = 0f;
        SetProgress(0, false);
    }

    private void SetupFreezingObjective(TutorialObjective objective)
    {
        SetProgress(0, false);
    }

    private void SetupDropZoneObjective(TutorialObjective objective)
    {
        _dropZoneStableTime = 0f;
        _dropZoneTracked = null;
        _dropZoneCandidates.Clear();
        _dropZoneRuntimeZones.Clear();

        if (objective == null)
            return;

        if (objective.dropZoneObjects != null && objective.dropZoneObjects.Length > 0)
            _dropZoneCandidates.AddRange(objective.dropZoneObjects);

        foreach (var catchable in FindObjectsOfType<CatchableObject>())
        {
            if (catchable != null)
                _dropZoneCandidates.Add(catchable);
        }

        SetProgress(0, false);

        if (objective.dropZones != null && objective.dropZones.Length > 0)
        {
            _dropZoneRuntimeZones.AddRange(objective.dropZones);
        }
        else if (!string.IsNullOrWhiteSpace(objective.dropZoneId))
        {
            TutorialDropZone[] zones = FindObjectsOfType<TutorialDropZone>(true);
            for (int i = 0; i < zones.Length; i++)
            {
                TutorialDropZone zone = zones[i];
                if (zone == null)
                    continue;

                if (!string.Equals(zone.zoneId?.Trim(), objective.dropZoneId.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                zone.CollectColliders(_dropZoneRuntimeZones);
            }
        }
    }

    private void SetupActivablesObjective(TutorialObjective objective)
    {
        List<ActivableObject> activables = new List<ActivableObject>();

        if (objective.activables != null && objective.activables.Length > 0)
        {
            activables.AddRange(objective.activables);
        }
        else if (objective.activableTypeFilter != ActivableObject.ActivationSpecialType.none)
        {
            foreach (var activable in FindObjectsOfType<ActivableObject>())
            {
                if (activable != null && activable.activationType == objective.activableTypeFilter)
                    activables.Add(activable);
            }
        }

        if (activables.Count == 0)
            return;

        foreach (var activable in activables)
        {
            if (activable == null)
                continue;

            activable.OnActivated.AddListener(OnActivableChanged);
            activable.OnDeactivated.AddListener(OnActivableChanged);
            _unsubscribers.Add(() =>
            {
                activable.OnActivated.RemoveListener(OnActivableChanged);
                activable.OnDeactivated.RemoveListener(OnActivableChanged);
            });
        }

        OnActivableChanged();
    }

    private void OnActivableChanged()
    {
        if (_currentStep == null || _currentStep.objective == null)
            return;

        int count = 0;
        ActivableObject[] activables = _currentStep.objective.activables;
        if (activables != null && activables.Length > 0)
        {
            foreach (var a in activables)
                if (a != null && a.isActivated)
                    count++;
        }
        else if (_currentStep.objective.activableTypeFilter != ActivableObject.ActivationSpecialType.none)
        {
            foreach (var a in FindObjectsOfType<ActivableObject>())
                if (a != null && a.activationType == _currentStep.objective.activableTypeFilter && a.isActivated)
                    count++;
        }

        SetProgress(count, true);

        if (_progress >= GetGoal())
            CompleteCurrentStep();
    }

    private void SetupCatchableObjective(TutorialObjective objective)
    {
        SetupCatchableDropGate(objective);

        List<CatchableObject> catchables = new List<CatchableObject>();
        if (objective.catchables != null && objective.catchables.Length > 0)
        {
            catchables.AddRange(objective.catchables);
        }
        else
        {
            foreach (var c in FindObjectsOfType<CatchableObject>())
                catchables.Add(c);
        }

        foreach (var catchable in catchables)
        {
            if (catchable == null)
                continue;

            if (objective.requireCatchableFireObject)
            {
                if (catchable is not CatchableFireObject fireObject)
                    continue;
                if (fireObject.linkedFlammableElement != null &&
                    fireObject.linkedFlammableElement.type != FlammableElement.FlammableType.Candle)
                    continue;
            }

            UnityAction action = () => OnCatchableGrabbed(catchable);
            catchable.onGrab.AddListener(action);
            _unsubscribers.Add(() => catchable.onGrab.RemoveListener(action));
        }
    }

    private void SetupCatchableDropGate(TutorialObjective objective)
    {
        if (!enableInputGate || objective == null || !objective.requireCatchableFireObject)
            return;

        InteractionController interaction = FindObjectOfType<InteractionController>();

        if (interaction == null)
            return;

        UnityAction<CatchableObject> refreshOnGrab = _ => RefreshCatchableDropGate(interaction);
        UnityAction<CatchableObject> refreshOnDrop = _ => RefreshCatchableDropGate(interaction);
        interaction.OnGrabItem.AddListener(refreshOnGrab);
        interaction.OnDropItem.AddListener(refreshOnDrop);
        _unsubscribers.Add(() => interaction.OnGrabItem.RemoveListener(refreshOnGrab));
        _unsubscribers.Add(() => interaction.OnDropItem.RemoveListener(refreshOnDrop));

        RefreshCatchableDropGate(interaction);
    }

    private void RefreshCatchableDropGate(InteractionController interaction)
    {
        if (!enableInputGate || interaction == null || _state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        TutorialObjective objective = _currentStep.objective;
        if (objective.type != TutorialObjectiveType.GrabCatchable || !objective.requireCatchableFireObject)
            return;

        CatchableObject heldObject = interaction.objectInHands;
        bool isHoldingFlammable = heldObject is CatchableFireObject fireObject && fireObject.linkedFlammableElement != null;

        // During the candle pickup step, allow dropping only when the player grabbed
        // the wrong non-flammable object, so they cannot get stuck with it in hands.
        TutorialInputGate.AllowDrop = !isHoldingFlammable;
    }

    private void OnCatchableGrabbed(CatchableObject catchable)
    {
        if (_state != StepState.InProgress)
            return;

        if (_counted.Contains(catchable))
            return;

        _counted.Add(catchable);
        AddProgress(1);
    }

    private void SetupSpirimonzDropObjective()
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null)
            return;

        foreach (var spmz in _inventory.spirimonzTeam)
        {
            if (spmz == null)
                continue;

            UnityAction action = () => OnSpirimonzDropped(spmz);
            spmz.onDroppedOnMap.AddListener(action);
            _unsubscribers.Add(() => spmz.onDroppedOnMap.RemoveListener(action));
        }
    }

    private void SetupSpirimonzGrabObjective()
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null)
            return;

        foreach (var spmz in _inventory.spirimonzTeam)
        {
            if (spmz == null)
                continue;

            UnityAction action = () => OnSpirimonzGrabbed(spmz);
            spmz.onGoingBackToHands.AddListener(action);
            _unsubscribers.Add(() => spmz.onGoingBackToHands.RemoveListener(action));
        }
    }

    private void OnSpirimonzDropped(Spirimonz spmz)
    {
        if (_state != StepState.InProgress || spmz == null)
            return;

        if (!MatchesRequiredSpirimonz(spmz))
            return;

        AddProgress(1);
    }

    private void OnSpirimonzGrabbed(Spirimonz spmz)
    {
        if (_state != StepState.InProgress || spmz == null)
            return;

        if (!MatchesRequiredSpirimonz(spmz))
            return;

        AddProgress(1);
    }

    private void SetupFlammableObjective(TutorialObjective objective)
    {
        _flammableCandidates.Clear();
        _watchFlammableUpright = objective.requireCandlePlacedAndUpright;

        List<FlammableElement> flammables = new List<FlammableElement>();
        if (objective.flammables != null && objective.flammables.Length > 0)
        {
            flammables.AddRange(objective.flammables);
        }
        else
        {
            foreach (var f in FindObjectsOfType<FlammableElement>())
                flammables.Add(f);
        }

        foreach (var flammable in flammables)
        {
            if (flammable == null)
                continue;

            if (objective.requireCandleFlammables && flammable.type != FlammableElement.FlammableType.Candle)
                continue;

            _flammableCandidates.Add(flammable);

            UnityAction<bool> action = (state) => OnFlammableChanged(flammable, state);
            flammable.onChangeFireState.AddListener(action);
            _unsubscribers.Add(() => flammable.onChangeFireState.RemoveListener(action));
        }

        EvaluateFlammableObjective();
    }

    private void OnFlammableChanged(FlammableElement flammable, bool isOn)
    {
        if (_state != StepState.InProgress || flammable == null || !isOn)
            return;

        if (_currentStep == null || _currentStep.objective == null)
            return;

        if (!IsFlammableValidForObjective(flammable, _currentStep.objective))
            return;

        if (_counted.Contains(flammable))
            return;

        _counted.Add(flammable);
        AddProgress(1);
    }

    private void UpdateFlammableObjective(TutorialObjective objective)
    {
        if (!_watchFlammableUpright || objective == null)
            return;

        EvaluateFlammableObjective();
    }

    private void EvaluateFlammableObjective()
    {
        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        TutorialObjective objective = _currentStep.objective;
        for (int i = 0; i < _flammableCandidates.Count; i++)
        {
            FlammableElement flammable = _flammableCandidates[i];
            if (flammable == null)
                continue;

            if (!flammable.IsOnFire())
                continue;

            if (!IsFlammableValidForObjective(flammable, objective))
                continue;

            if (_counted.Contains(flammable))
                continue;

            _counted.Add(flammable);
            AddProgress(1);

            if (_state != StepState.InProgress)
                return;
        }
    }

    private static bool IsFlammableValidForObjective(FlammableElement flammable, TutorialObjective objective)
    {
        if (objective == null || flammable == null)
            return false;

        if (!objective.requireCandlePlacedAndUpright)
            return true;

        if (flammable.type != FlammableElement.FlammableType.Candle)
            return true;

        CatchableFireObject fireObject = flammable.GetComponentInParent<CatchableFireObject>();
        if (fireObject == null)
            return true;

        if (fireObject.isGrabbed)
            return false;

        return IsUpright(fireObject, objective.candleUprightMaxAngle);
    }

    private void SetupPrintObjective(TutorialObjective objective)
    {
        List<PrintSource> prints = new List<PrintSource>();
        if (objective.printSources != null && objective.printSources.Length > 0)
        {
            prints.AddRange(objective.printSources);
        }
        else
        {
            foreach (var p in FindObjectsOfType<PrintSource>())
                prints.Add(p);
        }

        foreach (var print in prints)
        {
            if (print == null)
                continue;

            UnityAction action = () => OnPrintRevealed(print);
            print.OnFirstReveal.AddListener(action);
            _unsubscribers.Add(() => print.OnFirstReveal.RemoveListener(action));
        }
    }

    private void OnPrintRevealed(PrintSource print)
    {
        if (_state != StepState.InProgress || print == null)
            return;

        if (!IsRequiredSpirimonzSelected())
            return;

        if (_counted.Contains(print))
            return;

        _counted.Add(print);
        AddProgress(1);
    }

    private void SetupDetectorObjective(TutorialObjective objective)
    {
        _detectorCandidates.Clear();
        _detectorsCounted.Clear();
        _detectorListeners.Clear();
        _nextDetectorRefreshTime = 0f;

        RefreshDetectorCandidates(objective);
    }

    private void OnDetectorActivity(SpmzDetector detector, ActivitySource source)
    {
        TryCountDetectorActivity(detector);
    }

    private void TryCountDetectorActivity(SpmzDetector detector)
    {
        if (_state != StepState.InProgress || detector == null)
            return;

        ActivitySource source = detector.GetCurrentActivitySource();
        int currentValue = source != null ? source.activityValue : 0;

        if (currentValue <= 0)
        {
            _detectorsCounted.Remove(detector);
            _detectorLastValues[detector] = 0;
            return;
        }

        if (!IsRequiredSpirimonzSelected())
            return;

        int lastValue = 0;
        _detectorLastValues.TryGetValue(detector, out lastValue);

        if (currentValue == lastValue && _detectorsCounted.Contains(detector))
            return;

        _detectorLastValues[detector] = currentValue;
        _detectorsCounted.Add(detector);
        AddProgress(1);
    }

    private void SetupEatFruitObjective(TutorialObjective objective)
    {
        List<Fruit> fruits = new List<Fruit>();
        if (objective.fruits != null && objective.fruits.Length > 0)
        {
            fruits.AddRange(objective.fruits);
        }
        else
        {
            foreach (var f in FindObjectsOfType<Fruit>())
                fruits.Add(f);
        }

        foreach (Fruit fruit in fruits)
        {
            if (fruit == null)
                continue;

            UnityAction action = () => OnFruitEaten(fruit);
            fruit.onGhostEat.AddListener(action);
            _unsubscribers.Add(() => fruit.onGhostEat.RemoveListener(action));
        }
    }

    private void OnFruitEaten(Fruit fruit)
    {
        if (_state != StepState.InProgress)
            return;

        if (fruit == null)
            return;

        if (_counted.Contains(fruit))
            return;

        _counted.Add(fruit);
        AddProgress(1);
    }

    private void SetupRadiationObjective(TutorialObjective objective)
    {
        _radiationCandidates.Clear();
        _radiationListeners.Clear();
        _nextRadiationRefreshTime = 0f;

        EnsureTutorialRadiationForObjective(objective);
        RefreshRadiationCandidates(objective);
    }

    private void SetupLeaveHouseObjective()
    {
        Action<UIEndGame.EndTypes> action = (endType) => OnEndGameOpened(endType);
        UIGame.OnEndGameOpened += action;
        _unsubscribers.Add(() => UIGame.OnEndGameOpened -= action);
    }

    private void OnEndGameOpened(UIEndGame.EndTypes endType)
    {
        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        if (_currentStep.objective.type != TutorialObjectiveType.LeaveHouse)
            return;

        if (endType != UIEndGame.EndTypes.Escape)
            return;

        AddProgress(1);
    }

    private void OnRadiationDetected(RadiationDetector detector)
    {
        TryCountRadiation(detector);
    }

    private void OnRadiationEnded(RadiationDetector detector)
    {
        if (detector == null)
            return;

        _counted.Remove(detector);
    }

    private bool MatchesRadiationObjective(RadiationDetector detector, TutorialObjective objective)
    {
        if (objective == null)
            return true;

        Spirimonz linked = detector.linkedSpirimonz;
        if (linked == null)
            return true;

        if (!MatchesRequiredSpirimonz(linked))
            return false;

        if (objective.requireSpirimonzInHands && linked.isOnTheMap)
            return false;

        if (objective.requireSpirimonzOnMap && !linked.isOnTheMap)
            return false;

        return true;
    }

    private void SetupSurviveHuntObjective(TutorialObjective objective)
    {
        if (_ghost == null)
            return;

        _huntFailed = false;
        _huntInProgress = _ghost.IsHunting(true);

        UnityAction startAction = OnHuntStarted;
        UnityAction stopAction = OnHuntStopped;
        _ghost.onGhostStartToHunt.AddListener(startAction);
        _ghost.onGhostStopToHunt.AddListener(stopAction);
        _unsubscribers.Add(() => _ghost.onGhostStartToHunt.RemoveListener(startAction));
        _unsubscribers.Add(() => _ghost.onGhostStopToHunt.RemoveListener(stopAction));
    }

    private void OnHuntStarted()
    {
        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        if (_currentStep.objective.type != TutorialObjectiveType.SurviveHunt)
            return;

        _huntInProgress = true;
    }

    private void OnHuntStopped()
    {
        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        if (_currentStep.objective.type != TutorialObjectiveType.SurviveHunt)
            return;

        if (_huntFailed)
        {
            _huntFailed = false;
            _huntInProgress = false;
            SetProgress(0, false);
            return;
        }

        if (!_huntInProgress)
            return;

        _huntInProgress = false;
        AddProgress(1);
    }

    private void SetupJournalObjective()
    {
        if (_uiGame == null)
            _uiGame = UIGame.Instance;

        if (_uiGame == null)
            return;

        UnityAction action = OnJournalOpened;
        _uiGame.onJournalOpened.AddListener(action);
        _unsubscribers.Add(() => _uiGame.onJournalOpened.RemoveListener(action));
    }

    private void SetupEvidenceObjective(TutorialObjective objective)
    {
        GhostInvestigator investigator = GhostInvestigator.Instance;
        if (investigator == null)
            return;

        UnityAction<GhostInvestigator.EvidenceType> action = OnEvidenceChanged;
        investigator.OnInvestigationDatasChange.AddListener(action);
        _unsubscribers.Add(() => investigator.OnInvestigationDatasChange.RemoveListener(action));

        EvaluateEvidenceObjective();
    }

    private void OnEvidenceChanged(GhostInvestigator.EvidenceType type)
    {
        if (_state != StepState.InProgress)
            return;

        if (_currentStep == null || _currentStep.objective == null)
            return;

        if (_currentStep.objective.evidenceType != type)
            return;

        EvaluateEvidenceObjective();
    }

    private void EvaluateEvidenceObjective()
    {
        if (_currentStep == null || _currentStep.objective == null)
            return;

        GhostInvestigator investigator = GhostInvestigator.Instance;
        if (investigator == null)
            return;

        GhostInvestigator.EvidenceState state = investigator.GetEvidenceState(_currentStep.objective.evidenceType);
        if (state != _currentStep.objective.evidenceState)
            return;

        SetProgress(GetGoal(), false);
        CompleteCurrentStep();
    }

    private void OnJournalOpened()
    {
        if (_state != StepState.InProgress)
            return;

        AddProgress(1);
    }

    private void AddProgress(int amount)
    {
        int newValue = _progress + amount;
        SetProgress(newValue, true);

        if (_progress >= GetGoal())
            CompleteCurrentStep();
    }

    private int GetGoal()
    {
        int goal = _currentStep != null && _currentStep.objective != null ? _currentStep.objective.goal : 1;
        return Mathf.Max(1, goal);
    }

    private void InitObjectiveUI()
    {
        if (objectiveUI == null || _currentStep == null || _currentStep.objective == null)
            return;

        string title = _currentStep != null ? _currentStep.GetLocalizedObjectiveTitle() : string.Empty;
        objectiveUI.ShowObjective(title, _progress, GetGoal());
        objectiveUI.BounceOnce();
    }

    private void RefreshObjectiveUI()
    {
        if (objectiveUI == null || _currentStep == null || _currentStep.objective == null)
            return;

        string title = _currentStep.GetLocalizedObjectiveTitle();
        if (objectiveUI.tTitle != null && objectiveUI.tTitle.text != title)
            objectiveUI.tTitle.text = title;

        objectiveUI.SetProgress(_progress, GetGoal());
    }

    private void SetProgress(int value, bool allowSound)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == _progress)
            return;

        int previous = _progress;
        _progress = clamped;
        RefreshObjectiveUI();

        int goal = GetGoal();
        if (allowSound && progressSoundParameters != null && _progress > previous && _progress < goal)
            progressSoundParameters.PlaySound(GetSoundPosition());
    }

    private void ShowReturnToNpc()
    {
        if (objectiveUI == null)
            return;

        string text = GetLocalizedReturnToNpc();
        objectiveUI.ShowReturnToNpc(text);
    }

    private void ShowCompletionCTA(string text, bool loop)
    {
        if (objectiveUI == null)
            return;

        objectiveUI.ShowCompletionCTA(text, loop);
        objectiveUI.BounceOnce(loop);
    }

    private string GetLocalizedReturnToNpc()
    {
        return GetLocalizedText(returnToNpcKey, returnToNpcEnglish, returnToNpcFrench);
    }

    private string GetLocalizedObjectiveComplete()
    {
        return GetLocalizedText(objectiveCompleteKey, objectiveCompleteEnglish, objectiveCompleteFrench);
    }

    private string GetLocalizedTrainingObjective()
    {
        return GetLocalizedText(trainingObjectiveKey, trainingObjectiveEnglish, trainingObjectiveFrench);
    }

    private void HandleLanguageChanged(Language language)
    {
        if (objectiveUI == null)
            return;

        if (_isTraining && showTrainingObjective && _state == StepState.None)
        {
            objectiveUI.ShowMessage(GetLocalizedTrainingObjective(), false);
            return;
        }

        switch (_state)
        {
            case StepState.InProgress:
                InitObjectiveUI();
                break;
            case StepState.WaitingReturn:
                ShowCompletionCTA(GetLocalizedReturnToNpc(), true);
                break;
            case StepState.AutoAdvance:
                ShowCompletionCTA(GetLocalizedObjectiveComplete(), true);
                break;
        }
    }

    private string GetLocalizedText(string key, string english, string french)
    {
        string fallback = LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french)
            ? french
            : english;

        string result = LocalizationManager.Get(key, fallback);

        InputManager input = InputManager.Instance;
        if (input != null)
            result = input.ReplaceInputTokens(result);

        return result;
    }

    private Vector3 GetSoundPosition()
    {
        if (Player.Instance != null)
            return Player.Instance.transform.position;

        if (objectiveUI != null)
            return objectiveUI.transform.position;

        return transform.position;
    }

    private void PlayCompleteSound()
    {
        if (completeSoundParameters != null)
            completeSoundParameters.PlaySound(GetSoundPosition());
    }

    private bool MatchesRequiredSpirimonz(Spirimonz spmz)
    {
        if (_currentStep == null || _currentStep.objective == null)
            return true;

        int requiredIndex = _currentStep.objective.requiredTeamSlotIndex - 1;
        if (requiredIndex < 0)
            return true;

        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null)
            return true;

        int actualIndex = _inventory.GetSpirimonzIndex(spmz);
        return actualIndex == requiredIndex;
    }

    private bool IsRequiredSpirimonzSelected()
    {
        if (_currentStep == null || _currentStep.objective == null)
            return true;

        int requiredIndex = _currentStep.objective.requiredTeamSlotIndex - 1;
        if (requiredIndex < 0)
            return true;

        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null || requiredIndex >= _inventory.spirimonzTeam.Count)
            return false;

        Spirimonz required = _inventory.spirimonzTeam[requiredIndex];
        if (required == null)
            return false;

        if (_inventory.selectedSpirimonz != required)
            return false;

        if (_currentStep.objective.requireSpirimonzInHands && required.isOnTheMap)
            return false;

        return true;
    }

    private void ClearSubscriptions()
    {
        for (int i = 0; i < _unsubscribers.Count; i++)
        {
            _unsubscribers[i]?.Invoke();
        }
        _unsubscribers.Clear();
    }

    private void ApplyGhostOverrideForStep()
    {
        if (_ghost == null)
            return;

        ApplyTutorialGhostParametersIfNeeded();

        Room forcedRoom = ResolveForcedGhostRoom();

        if (_currentStep != null && _currentStep.ghostOverride != null && _currentStep.ghostOverride.enabled)
        {
            _currentStep.ghostOverride.Apply(_ghost);
            if (forceGhostRoom && !_currentStep.ghostOverride.forceRoom && forcedRoom != null)
            {
                List<Ghost.GhostActivities> allowed = _currentStep.ghostOverride.allowedActivities;
                _ghost.ApplyTutorialOverride(
                    enabled: true,
                    blockAllActivities: _ghost.tutorialBlockAllActivities,
                    forceRoom: true,
                    forcedRoom: forcedRoom,
                    restrictActivities: _ghost.tutorialRestrictActivities,
                    allowedActivities: allowed,
                    forceActivity: _ghost.tutorialForceActivity,
                    forcedActivity: _ghost.tutorialForcedActivity,
                    allowHunt: _ghost.tutorialAllowHunt,
                    allowRoomChange: _ghost.tutorialAllowRoomChange);
            }
            return;
        }

        if (blockGhostActivitiesByDefault || forceGhostRoom)
        {
            _ghost.ApplyTutorialOverride(
                enabled: true,
                blockAllActivities: blockGhostActivitiesByDefault,
                forceRoom: forceGhostRoom && forcedRoom != null,
                forcedRoom: forcedRoom,
                restrictActivities: false,
                allowedActivities: null,
                forceActivity: false,
                forcedActivity: Ghost.GhostActivities.Nothing,
                allowHunt: !blockGhostActivitiesByDefault,
                allowRoomChange: false);
        }
        else
        {
            _ghost.ClearTutorialOverride();
        }
    }

    private void ApplyTutorialGhostSetup()
    {
        ApplyTutorialGhostParametersIfNeeded();
        ApplyGhostOverrideForStep();
    }

    private void ApplyTrainingGhostSetup()
    {
        if (_ghost == null)
            return;

        _ghost.ClearTutorialOverride();
    }

    private void EnsureTrainingJournalVisibility()
    {
        if (ghostTypesRoot != null)
            ghostTypesRoot.SetActive(true);
        if (captureButtonRoot != null)
            captureButtonRoot.SetActive(true);

        if (UIGame.Instance != null && UIGame.Instance.tablet != null)
        {
            UIJournal journal = UIGame.Instance.tablet.GetComponentInChildren<UIJournal>(true);
            if (journal != null)
                journal.RefreshModeVisibility();
        }
    }

    private void ApplyTutorialGhostParametersIfNeeded()
    {
        if (!forceGhostParameters || forcedGhostParameters == null || _ghost == null)
            return;

        if (_appliedGhostParameters == forcedGhostParameters)
            return;

        _appliedGhostParameters = forcedGhostParameters;
        _ghost.ApplyTutorialGhostParameters(forcedGhostParameters);

        if (House.Instance != null)
            House.Instance.selectedGhostParameter = forcedGhostParameters;
    }

    private Room ResolveForcedGhostRoom()
    {
        if (!forceGhostRoom)
            return null;

        if (forcedGhostRoom != null)
            return forcedGhostRoom;

        House house = House.Instance;
        if (house == null)
            return null;

        if (!string.IsNullOrWhiteSpace(forcedGhostRoomName))
        {
            foreach (Room room in house.rooms)
            {
                if (room != null && room.name == forcedGhostRoomName)
                    return room;
            }
        }

        if (forcedGhostRoomIndex >= 0 && forcedGhostRoomIndex < house.rooms.Length)
            return house.rooms[forcedGhostRoomIndex];

        return null;
    }

    private void StopAutoAdvance()
    {
        if (_autoAdvanceRoutine != null)
        {
            StopCoroutine(_autoAdvanceRoutine);
            _autoAdvanceRoutine = null;
        }
    }

    private IEnumerator AutoAdvanceRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_state == StepState.AutoAdvance)
            AdvanceToNextStep();
    }

    private TutorialStepHooks GetStepHooks(int index)
    {
        if (stepHooks == null || index < 0 || index >= stepHooks.Count)
            return null;

        return stepHooks[index];
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K))
        {
            ForceCompleteCurrentStep();
        }
#endif

        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return;

        switch (_currentStep.objective.type)
        {
            case TutorialObjectiveType.WaitSpirimonz:
                UpdateWaitObjective(_currentStep.objective);
                break;
            case TutorialObjectiveType.PlaceObjectInZone:
                UpdateDropZoneObjective(_currentStep.objective);
                break;
            case TutorialObjectiveType.DetectFreezing:
                UpdateFreezingObjective(_currentStep.objective);
                break;
            case TutorialObjectiveType.LightFlammables:
                UpdateFlammableObjective(_currentStep.objective);
                break;
            case TutorialObjectiveType.DetectActivity:
                UpdateDetectorObjective();
                break;
            case TutorialObjectiveType.DetectSpiritOrbs:
                UpdateSpiritOrbsObjective(_currentStep.objective);
                break;
            case TutorialObjectiveType.DetectRadiation:
                UpdateRadiationObjective(_currentStep.objective);
                break;
        }
    }

    private void ForceCompleteCurrentStep()
    {
        if (_currentStep == null)
            return;

        switch (_state)
        {
            case StepState.WaitingDialogue:
                StartCurrentStep();
                CompleteCurrentStep();
                break;
            case StepState.InProgress:
                CompleteCurrentStep();
                break;
            case StepState.WaitingReturn:
            case StepState.AutoAdvance:
                AdvanceToNextStep();
                break;
        }
    }

    private void UpdateDetectorObjective()
    {
        if (Time.time >= _nextDetectorRefreshTime)
        {
            RefreshDetectorCandidates(_currentStep != null ? _currentStep.objective : null);
            _nextDetectorRefreshTime = Time.time + 0.5f;
        }

        for (int i = 0; i < _detectorCandidates.Count; i++)
        {
            SpmzDetector detector = _detectorCandidates[i];
            if (detector == null)
                continue;

            TryCountDetectorActivity(detector);
        }
    }

    private void RefreshDetectorCandidates(TutorialObjective objective)
    {
        if (objective == null)
            return;

        List<SpmzDetector> detectors = new List<SpmzDetector>();
        if (objective.detectors != null && objective.detectors.Length > 0)
        {
            detectors.AddRange(objective.detectors);
        }
        else
        {
            foreach (var d in FindObjectsOfType<SpmzDetector>())
                detectors.Add(d);
        }

        for (int i = 0; i < detectors.Count; i++)
        {
            SpmzDetector detector = detectors[i];
            if (detector == null)
                continue;

            if (!_detectorCandidates.Contains(detector))
                _detectorCandidates.Add(detector);

            if (_detectorListeners.Add(detector))
            {
                UnityAction<ActivitySource> action = (source) => OnDetectorActivity(detector, source);
                detector.onActivityDetected.AddListener(action);
                _unsubscribers.Add(() => detector.onActivityDetected.RemoveListener(action));
            }

            TryCountDetectorActivity(detector);
        }
    }

    private void SetupSpiritOrbsObjective(TutorialObjective objective)
    {
        _orbsCandidates.Clear();
        _currentOrbsTarget = null;
        _orbsHoldTimer = 0f;
        _nextOrbsRefreshTime = 0f;
        RefreshOrbsCandidates(objective);
    }

    private void UpdateSpiritOrbsObjective(TutorialObjective objective)
    {
        if (objective == null)
            return;

        if (!IsNightVisionActive())
        {
            ResetOrbsHold();
            return;
        }

        if (Time.time >= _nextOrbsRefreshTime)
        {
            RefreshOrbsCandidates(objective);
            _nextOrbsRefreshTime = Time.time + 0.5f;
        }

        GhostOrbsParticles target = FindBestOrbsTarget(objective);
        if (target == null)
        {
            ResetOrbsHold();
            return;
        }

        if (_currentOrbsTarget != target)
        {
            _currentOrbsTarget = target;
            _orbsHoldTimer = 0f;
        }

        _orbsHoldTimer += Time.deltaTime;
        if (_orbsHoldTimer >= Mathf.Max(0.1f, objective.orbsHoldDuration))
        {
            AddProgress(1);
            _orbsHoldTimer = 0f;
            _currentOrbsTarget = null;
        }
    }

    private void ResetOrbsHold()
    {
        _orbsHoldTimer = 0f;
        _currentOrbsTarget = null;
    }

    private bool IsNightVisionActive()
    {
        if (!TutorialInputGate.IsAllowed(TutorialInputGate.AllowSecondary) ||
            !TutorialInputGate.IsAllowed(TutorialInputGate.AllowUseWatch))
            return false;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
            return false;

        return inventory.IsNightVisionOn();
    }

    private void RefreshOrbsCandidates(TutorialObjective objective)
    {
        if (objective == null)
            return;

        List<GhostOrbsParticles> orbs = new List<GhostOrbsParticles>();
        if (objective.orbsParticles != null && objective.orbsParticles.Length > 0)
        {
            orbs.AddRange(objective.orbsParticles);
        }
        else
        {
            foreach (var orb in FindObjectsOfType<GhostOrbsParticles>())
                orbs.Add(orb);

            if (orbs.Count == 0)
            {
                foreach (var ps in FindObjectsOfType<ParticleSystem>())
                {
                    if (ps == null)
                        continue;

                    if (ps.name.IndexOf("GhostOrbsParticles", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    GhostOrbsParticles marker = ps.GetComponent<GhostOrbsParticles>();
                    if (marker == null)
                    {
                        marker = ps.gameObject.AddComponent<GhostOrbsParticles>();
                        marker.hideFlags = HideFlags.DontSave;
                    }

                    orbs.Add(marker);
                }
            }
        }

        _orbsCandidates.Clear();
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] != null)
                _orbsCandidates.Add(orbs[i]);
        }
    }

    private GhostOrbsParticles FindBestOrbsTarget(TutorialObjective objective)
    {
        Camera cam = Player.Instance != null ? Player.Instance.camera : null;
        if (cam == null)
            return null;

        Vector3 camPos = cam.transform.position;
        Vector3 camForward = cam.transform.forward;
        float maxDist = Mathf.Max(0f, objective.orbsMaxDistance);
        float maxAngle = Mathf.Clamp(objective.orbsMaxAngle, 0f, 90f);
        GhostOrbsParticles best = null;
        float bestScore = float.MaxValue;
        GhostOrbsParticles bestLoose = null;
        float bestLooseDistance = float.MaxValue;

        for (int i = 0; i < _orbsCandidates.Count; i++)
        {
            GhostOrbsParticles candidate = _orbsCandidates[i];
            if (candidate == null)
                continue;

            if (!TryGetOrbsTargetPoint(candidate, cam, out Vector3 targetPos, out float distance, out float angle))
                continue;

            if (maxDist > 0f && distance > maxDist)
                continue;

            if (distance < bestLooseDistance)
            {
                bestLooseDistance = distance;
                bestLoose = candidate;
            }

            if (angle > maxAngle)
                continue;

            if (!HasLineOfSightToOrbs(candidate, camPos, targetPos, distance))
                continue;

            float score = angle * 1000f + distance;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best ?? bestLoose;
    }

    private bool TryGetOrbsTargetPoint(GhostOrbsParticles candidate, Camera cam, out Vector3 targetPos, out float distance, out float angle)
    {
        targetPos = candidate.GetAimPosition();
        distance = 0f;
        angle = 0f;

        Vector3 camPos = cam.transform.position;
        Vector3 camForward = cam.transform.forward;

        Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 closest = bounds.ClosestPoint(camPos);
            if ((closest - camPos).sqrMagnitude > 0.0001f)
                targetPos = closest;
            else
                targetPos = camPos + camForward * 0.1f;
        }

        Vector3 toTarget = targetPos - camPos;
        float sqr = toTarget.sqrMagnitude;
        if (sqr <= 0.0001f)
        {
            distance = 0f;
            angle = 0f;
            return true;
        }

        distance = Mathf.Sqrt(sqr);
        angle = Vector3.Angle(camForward, toTarget);
        return true;
    }

    private bool HasLineOfSightToOrbs(GhostOrbsParticles candidate, Vector3 origin, Vector3 targetPos, float distance)
    {
        if (candidate == null || distance <= 0.01f)
            return false;

        if (Physics.Linecast(origin, targetPos, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == candidate.transform || hit.transform.IsChildOf(candidate.transform))
                return true;

            return false;
        }

        return true;
    }

    private Spirimonz ResolveFreezingTarget(TutorialObjective objective)
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null)
            return null;

        int requiredIndex = objective.requiredTeamSlotIndex - 1;
        if (requiredIndex >= 0 && requiredIndex < _inventory.spirimonzTeam.Count)
            return _inventory.spirimonzTeam[requiredIndex];

        if (_inventory.selectedSpirimonz != null)
            return _inventory.selectedSpirimonz;

        for (int i = 0; i < _inventory.spirimonzTeam.Count; i++)
        {
            Spirimonz candidate = _inventory.spirimonzTeam[i];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private bool IsFreezingConditionMet(Spirimonz target, TutorialObjective objective)
    {
        if (target == null)
            return false;

        if (objective.requireSpirimonzInHands && target.isOnTheMap)
            return false;

        if (objective.requireSpirimonzOnMap && !target.isOnTheMap)
            return false;

        SpmzTemperatureColor tempColorComponent = target.GetComponent<SpmzTemperatureColor>();
        if (tempColorComponent != null)
        {
            float thresholdVisual = Mathf.Clamp01(objective.freezingVisualPercent);
            if (thresholdVisual <= 0f || thresholdVisual > 0.8f)
                thresholdVisual = 0.8f;
            return tempColorComponent.VisualFreezingPercent >= thresholdVisual;
        }

        Room room = target.currentRoom;
        if (room == null)
            return false;

        float threshold = objective.freezingTemperatureThreshold;
        if (objective.useSpirimonzTemperatureThreshold)
        {
            if (tempColorComponent != null)
                threshold = tempColorComponent.FreezingThreshold;
        }

        return room.GetTemperatureCelsius() < threshold;
    }

    private Spirimonz ResolveWaitTarget(TutorialObjective objective)
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventory == null)
            return null;

        int requiredIndex = objective.requiredTeamSlotIndex - 1;
        if (requiredIndex >= 0 && requiredIndex < _inventory.spirimonzTeam.Count)
            return _inventory.spirimonzTeam[requiredIndex];

        Spirimonz selected = _inventory.selectedSpirimonz;
        if (selected != null && IsWaitConditionMet(selected, objective))
            return selected;

        for (int i = 0; i < _inventory.spirimonzTeam.Count; i++)
        {
            Spirimonz candidate = _inventory.spirimonzTeam[i];
            if (candidate == null)
                continue;

            if (IsWaitConditionMet(candidate, objective))
                return candidate;
        }

        if (selected != null)
            return selected;

        for (int i = 0; i < _inventory.spirimonzTeam.Count; i++)
        {
            Spirimonz candidate = _inventory.spirimonzTeam[i];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private bool IsWaitConditionMet(Spirimonz target, TutorialObjective objective)
    {
        if (target == null)
            return false;

        if (objective.requireSpirimonzInHands && target.isOnTheMap)
            return false;

        if (objective.requireSpirimonzOnMap && !target.isOnTheMap)
            return false;

        return target.CurrentBehaviour() == Spirimonz.SpirimonzBehaviourState.Wait;
    }

    private void UpdateWaitObjective(TutorialObjective objective)
    {
        Spirimonz target = ResolveWaitTarget(objective);
        if (!IsWaitConditionMet(target, objective))
        {
            if (_waitElapsed > 0f || _progress > 0)
            {
                _waitElapsed = 0f;
                SetProgress(0, false);
            }
            return;
        }

        _waitElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.1f, objective.waitDuration);
        int goal = GetGoal();
        float ratio = Mathf.Clamp01(_waitElapsed / duration);
        int progress = Mathf.Clamp(Mathf.FloorToInt(ratio * goal), 0, goal);
        SetProgress(progress, true);

        if (_waitElapsed >= duration)
        {
            SetProgress(goal, false);
            CompleteCurrentStep();
        }
    }

    private void UpdateDropZoneObjective(TutorialObjective objective)
    {
        CatchableObject candidate = FindValidDropZoneCandidate(objective);
        if (candidate == null)
        {
            if (_dropZoneStableTime > 0f || _progress > 0)
            {
                _dropZoneStableTime = 0f;
                _dropZoneTracked = null;
                SetProgress(0, false);
            }
            return;
        }

        if (!objective.requireStableRotation)
        {
            SetProgress(GetGoal(), false);
            CompleteCurrentStep();
            return;
        }

        if (_dropZoneTracked != candidate)
        {
            _dropZoneTracked = candidate;
            _dropZoneStableTime = 0f;
            SetProgress(0, false);
        }

        _dropZoneStableTime += Time.deltaTime;
        if (_dropZoneStableTime >= Mathf.Max(0f, objective.stableRotationDuration))
        {
            SetProgress(GetGoal(), false);
            CompleteCurrentStep();
        }
    }

    private void UpdateFreezingObjective(TutorialObjective objective)
    {
        if (!IsFreezingConditionMet(ResolveFreezingTarget(objective), objective))
            return;

        SetProgress(GetGoal(), false);
        CompleteCurrentStep();
    }

    private CatchableObject FindValidDropZoneCandidate(TutorialObjective objective)
    {
        if (_dropZoneCandidates.Count == 0)
            return null;

        for (int i = 0; i < _dropZoneCandidates.Count; i++)
        {
            CatchableObject candidate = _dropZoneCandidates[i];
            if (candidate == null || candidate.isGrabbed)
                continue;

            if (!MatchesDropZoneFilter(candidate, objective))
                continue;

            if (!IsInsideAnyDropZone(candidate, _dropZoneRuntimeZones))
                continue;

            if (objective.requireStableRotation && !IsUpright(candidate, objective.maxUprightAngle))
                continue;

            return candidate;
        }

        return null;
    }

    public void BlowUpRandomFlammableOnFire()
    {
        FlammableElement target = GetRandomFlammableElement(requireOnFire: true);
        if (target == null)
        {
            Vector3 referencePos = transform.position;
            if (Player.Instance != null)
                referencePos = Player.Instance.transform.position;

            FlammableElement closest = GetClosestFlammableElement(referencePos);
            if (closest != null && closest.blowUpByGhostSoundClip != null)
                SoundManager.Instance.PlaySound(closest.blowUpByGhostSoundClip, closest.transform.position, closest.ghostVolume, closest.ghostPitch, -1f, 15f);

            return;
        }

        target.EnableFire(false, true, true);
    }

    public void StartRadiationUntilNextStepComplete(Room room)
    {
        if (room == null)
            return;

        StartRadiationUntilNextStepComplete(room, tutorialRadiationDuration);
    }

    public void StartRadiationUntilNextStepComplete(Room room, float duration)
    {
        if (room == null)
            return;

        if (_tutorialRadiationRoom != null && _tutorialRadiationRoom != room)
            _tutorialRadiationRoom.StopRadiation();

        _tutorialRadiationRoom = room;
        _tutorialRadiationActive = true;
        _tutorialRadiationStopAfterStepIndex = _currentStepIndex + 1;

        float clampedDuration = Mathf.Max(0.1f, duration);
        room.StartRadiation(clampedDuration);
    }

    public void StopTutorialRadiation()
    {
        if (_tutorialRadiationRoom != null)
            _tutorialRadiationRoom.StopRadiation();

        _tutorialRadiationRoom = null;
        _tutorialRadiationActive = false;
        _tutorialRadiationStopAfterStepIndex = -1;
    }

    public void UnlockHouseEntry()
    {
        HouseEntry entry = House.Instance != null ? House.Instance.houseEntry : null;
        if (entry != null)
            entry.SetLocked(false);
    }

    public void UnlockHouseEntry(HouseEntry entry)
    {
        if (entry != null)
            entry.SetLocked(false);
    }

    private void CheckTutorialRadiationStop()
    {
        if (!_tutorialRadiationActive)
            return;

        if (_tutorialRadiationStopAfterStepIndex >= 0 && _currentStepIndex >= _tutorialRadiationStopAfterStepIndex)
        {
            StopTutorialRadiation();
        }
    }

    private void ReplayCurrentStepStartHooks()
    {
        GetStepHooks(_currentStepIndex)?.onStepStart?.Invoke();
    }

    public void StartAHunt()
    {
        if (_ghost == null && House.Instance != null)
            _ghost = House.Instance.currentGhost;

        if (_ghost == null)
            return;

        _ghost.ForceStartHunt();
    }

    public void StartAHuntWithDelay(float delay)
    {
        if (_ghost == null && House.Instance != null)
            _ghost = House.Instance.currentGhost;

        if (_ghost == null)
            return;

        float clampedDelay = Mathf.Max(0f, delay);
        if (clampedDelay <= 0f)
        {
            _ghost.ForceStartHunt();
            return;
        }

        this.Invoke(clampedDelay, () =>
        {
            if (_ghost != null)
                _ghost.ForceStartHunt();
        });
    }

    public void ResetJournalAndGhostTypes()
    {
        GhostInvestigator investigator = GhostInvestigator.Instance;
        if (investigator != null)
        {
            foreach (GhostInvestigator.EvidenceType type in Enum.GetValues(typeof(GhostInvestigator.EvidenceType)))
            {
                investigator.SetEvidenceState(type, GhostInvestigator.EvidenceState.Unknown);
            }
        }

        UIJournal journal = null;
        if (UIGame.Instance != null && UIGame.Instance.tablet != null)
        {
            journal = UIGame.Instance.tablet.GetComponentInChildren<UIJournal>(true);
        }
        if (journal != null)
        {
            journal.CloseGhostFrame();
            journal.ClearForcedSelections();
        }

        if (ghostTypesRoot != null)
            ghostTypesRoot.SetActive(true);

        if (captureButtonRoot != null)
            captureButtonRoot.SetActive(true);
    }

    public bool TryHandleTutorialHuntFailure(GamePlayer player)
    {
        if (_state != StepState.InProgress || _currentStep == null || _currentStep.objective == null)
            return false;

        if (_currentStep.objective.type != TutorialObjectiveType.SurviveHunt)
            return false;

        if (_huntFailRoutine != null)
            return true;

        _huntFailed = true;
        _huntInProgress = false;
        _restartStepAfterHuntFailDialogue = true;
        SetProgress(0, false);

        _huntFailRoutine = StartCoroutine(HuntFailRoutine(player));
        return true;
    }

    private IEnumerator HuntFailRoutine(GamePlayer player)
    {
        if (player == null)
        {
            _huntFailRoutine = null;
            yield break;
        }

        player.ResetDeathState();
        player.LockControls(true);

        UIGame uiGame = UIGame.Instance;
        if (uiGame != null)
        {
            uiGame.CloseAllWindows();
            uiGame.EnablePointer(false);
            uiGame.EnableOverlay(true, huntFailFadeDuration);
        }

        if (huntFailFadeDuration > 0f)
            yield return new WaitForSeconds(huntFailFadeDuration);

        if (huntFailBlackDelay > 0f)
            yield return new WaitForSeconds(huntFailBlackDelay);

        if (_ghost != null)
            _ghost.CancelHuntCompletely();

        TeleportPlayerForHuntFail(player);

        if (uiGame != null)
        {
            uiGame.EnableOverlay(false, huntFailFadeDuration);
            uiGame.EnablePointer(true);
        }

        if (huntFailFadeDuration > 0f)
            yield return new WaitForSeconds(huntFailFadeDuration);

        if (questNpc != null && huntFailDialogue != null)
        {
            if (!questNpc.gameObject.activeSelf)
                questNpc.gameObject.SetActive(true);
            questNpc.ForceReadyToInteract(true);
            questNpc.dialogue = huntFailDialogue;
            questNpc.Interact(player, huntFailUseDialogueCamera);
        }
        else
        {
            player.LockControls(false);
            if (_restartStepAfterHuntFailDialogue)
            {
                ReplayCurrentStepStartHooks();
                _restartStepAfterHuntFailDialogue = false;
            }
        }

        _huntFailRoutine = null;
    }

    private void TeleportPlayerForHuntFail(GamePlayer player)
    {
        if (player == null)
            return;

        if (huntFailTeleportPoint != null)
        {
            player.SetPosition(huntFailTeleportPoint.position);
            player.SetRotation(huntFailTeleportPoint.rotation);
            return;
        }

        if (questNpc == null)
            return;

        Vector3 npcPos = questNpc.transform.position;
        Vector3 forward = questNpc.transform.forward;
        Vector3 targetPos = npcPos + forward * Mathf.Max(0.1f, huntFailTeleportDistance);
        targetPos.y = npcPos.y;

        player.SetPosition(targetPos);

        Vector3 lookDir = npcPos - targetPos;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            player.SetRotation(Quaternion.LookRotation(lookDir.normalized, Vector3.up));
    }

    private static FlammableElement GetRandomFlammableElement(bool requireOnFire)
    {
        FlammableElement[] flammables = FindObjectsOfType<FlammableElement>();
        if (flammables == null || flammables.Length == 0)
            return null;

        List<FlammableElement> candidates = new List<FlammableElement>();
        for (int i = 0; i < flammables.Length; i++)
        {
            FlammableElement flammable = flammables[i];
            if (flammable == null)
                continue;

            if (requireOnFire && !flammable.IsOnFire())
                continue;

            candidates.Add(flammable);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static FlammableElement GetClosestFlammableElement(Vector3 referencePosition)
    {
        FlammableElement[] flammables = FindObjectsOfType<FlammableElement>();
        if (flammables == null || flammables.Length == 0)
            return null;

        FlammableElement closest = null;
        float closestSq = float.MaxValue;

        for (int i = 0; i < flammables.Length; i++)
        {
            FlammableElement flammable = flammables[i];
            if (flammable == null)
                continue;

            float sqrDist = (flammable.transform.position - referencePosition).sqrMagnitude;
            if (sqrDist < closestSq)
            {
                closestSq = sqrDist;
                closest = flammable;
            }
        }

        return closest;
    }

    private bool MatchesDropZoneFilter(CatchableObject candidate, TutorialObjective objective)
    {
        if (objective.dropZoneObjects != null && objective.dropZoneObjects.Length > 0)
        {
            for (int i = 0; i < objective.dropZoneObjects.Length; i++)
            {
                if (objective.dropZoneObjects[i] == candidate)
                    return true;
            }
            return false;
        }

        if (objective.dropZoneFlammableType != FlammableElement.FlammableType.None)
        {
            CatchableFireObject fireObject = candidate as CatchableFireObject;
            if (fireObject == null)
                return false;

            if (fireObject.linkedFlammableElement == null)
                return false;

            if (fireObject.linkedFlammableElement.type != objective.dropZoneFlammableType)
                return false;
        }

        if (objective.dropZoneRequireCatchableFireObject)
        {
            CatchableFireObject fire = candidate as CatchableFireObject;
            if (fire == null)
                return false;

            if (fire.linkedFlammableElement == null)
                return false;
        }

        return true;
    }

    private bool IsInsideAnyDropZone(CatchableObject candidate, Collider[] zones)
    {
        if (zones == null || zones.Length == 0)
            return false;

        Collider[] candidateColliders = candidate.GetComponentsInChildren<Collider>(true);
        if (candidateColliders == null || candidateColliders.Length == 0)
            return false;

        for (int i = 0; i < zones.Length; i++)
        {
            Collider zone = zones[i];
            if (zone == null || !zone.enabled)
                continue;

            if (IsOverlappingZone(zone, candidateColliders))
                return true;
        }

        return false;
    }

    private bool IsInsideAnyDropZone(CatchableObject candidate, List<Collider> zones)
    {
        if (zones == null || zones.Count == 0)
            return false;

        Collider[] candidateColliders = candidate.GetComponentsInChildren<Collider>(true);
        if (candidateColliders == null || candidateColliders.Length == 0)
            return false;

        for (int i = 0; i < zones.Count; i++)
        {
            Collider zone = zones[i];
            if (zone == null || !zone.enabled)
                continue;

            if (IsOverlappingZone(zone, candidateColliders))
                return true;
        }

        return false;
    }

    private static bool IsOverlappingZone(Collider zone, Collider[] candidateColliders)
    {
        for (int i = 0; i < candidateColliders.Length; i++)
        {
            Collider candidateCollider = candidateColliders[i];
            if (candidateCollider == null || !candidateCollider.enabled)
                continue;

            if (zone.isTrigger || candidateCollider.isTrigger)
            {
                if (zone.bounds.Intersects(candidateCollider.bounds))
                    return true;

                continue;
            }

            if (Physics.ComputePenetration(
                    zone, zone.transform.position, zone.transform.rotation,
                    candidateCollider, candidateCollider.transform.position, candidateCollider.transform.rotation,
                    out _, out _))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateRadiationObjective(TutorialObjective objective)
    {
        if (objective == null)
            return;

        EnsureTutorialRadiationForObjective(objective);

        if (Time.time >= _nextRadiationRefreshTime)
        {
            RefreshRadiationCandidates(objective);
            _nextRadiationRefreshTime = Time.time + 0.5f;
        }

        for (int i = 0; i < _radiationCandidates.Count; i++)
        {
            RadiationDetector detector = _radiationCandidates[i];
            if (detector == null)
                continue;

            TryCountRadiation(detector);
        }
    }

    private void RefreshRadiationCandidates(TutorialObjective objective)
    {
        if (objective == null)
            return;

        List<RadiationDetector> detectors = new List<RadiationDetector>();
        if (objective.radiationDetectors != null && objective.radiationDetectors.Length > 0)
        {
            detectors.AddRange(objective.radiationDetectors);
        }
        else
        {
            foreach (var d in FindObjectsOfType<RadiationDetector>())
                detectors.Add(d);
        }

        for (int i = 0; i < detectors.Count; i++)
        {
            RadiationDetector detector = detectors[i];
            if (detector == null)
                continue;

            if (!_radiationCandidates.Contains(detector))
                _radiationCandidates.Add(detector);

            if (_radiationListeners.Add(detector))
            {
                UnityAction startAction = () => OnRadiationDetected(detector);
                UnityAction endAction = () => OnRadiationEnded(detector);
                detector.OnDetectionStart.AddListener(startAction);
                detector.OnDetectionEnd.AddListener(endAction);
                _unsubscribers.Add(() => detector.OnDetectionStart.RemoveListener(startAction));
                _unsubscribers.Add(() => detector.OnDetectionEnd.RemoveListener(endAction));
            }

            TryCountRadiation(detector);
        }
    }

    private void TryCountRadiation(RadiationDetector detector)
    {
        if (_state != StepState.InProgress || detector == null)
            return;

        if (!detector.IsDetectingRadiation())
        {
            _counted.Remove(detector);
            return;
        }

        if (!MatchesRadiationObjective(detector, _currentStep?.objective))
            return;

        if (_counted.Contains(detector))
            return;

        _counted.Add(detector);
        AddProgress(1);
    }

    private void EnsureTutorialRadiationForObjective(TutorialObjective objective)
    {
        if (!IsTutorialActive || objective == null || objective.type != TutorialObjectiveType.DetectRadiation)
            return;

        if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "HouseTuto", StringComparison.OrdinalIgnoreCase))
            return;

        Room targetRoom = _tutorialRadiationRoom != null ? _tutorialRadiationRoom : GetHouseTutoKitchenRoom();
        if (targetRoom == null)
            return;

        bool needsRestart = !_tutorialRadiationActive ||
                            _tutorialRadiationRoom != targetRoom ||
                            !targetRoom.radiationInTheRoom ||
                            targetRoom.radiationDuration <= 0.1f;

        if (needsRestart)
            StartRadiationUntilNextStepComplete(targetRoom, tutorialRadiationDuration);
    }

    private Room GetHouseTutoKitchenRoom()
    {
        House house = House.Instance;
        if (house == null || house.rooms == null)
            return null;

        for (int i = 0; i < house.rooms.Length; i++)
        {
            Room room = house.rooms[i];
            if (room == null)
                continue;

            if (room.roomType == Room.RoomType.kitchen)
                return room;
        }

        for (int i = 0; i < house.rooms.Length; i++)
        {
            Room room = house.rooms[i];
            if (room == null)
                continue;

            if (string.Equals(room.name, "Kitchen", StringComparison.OrdinalIgnoreCase))
                return room;
        }

        return null;
    }

    private static bool IsUpright(CatchableObject candidate, float maxAngle)
    {
        float angle = Vector3.Angle(candidate.transform.up, Vector3.up);
        return angle <= Mathf.Max(0f, maxAngle);
    }
}
