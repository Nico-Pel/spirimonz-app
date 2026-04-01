using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : GameBehaviour
{
    public static GameManager Instance;

    public enum HouseSceneMode
    {
        NormalMap,
        Tutorial,
        Training
    }

    [Header("Debug")] 
    public bool ignoreAllHouseDebugs;
    public bool considerEverySpirimonzUnlocked;
    public bool enableDebugMoneyButton = true;

    [Header("Mobile Controls")]
    public bool mobileControlsEnabled;

    [Header("Mobile Light Optimization")]
    public bool mobileLightOptimizationEnabled = true;

    [Header("Tutorial")]
    public bool useTutorialWorldSpawn;
    public bool disableMoneyGain;
    [SerializeField] private HouseSceneMode nextHouseSceneMode = HouseSceneMode.NormalMap;

    [Header("Challenge")]
    public bool royalChallengeActive;

    [Header("Title Screen")]
    public string titleScreenSceneName = "TitleScreen";
    public string defaultWorldSceneName = "World01";
    
    [Space]

    public GhostTypeDatabase ghostTypeDatabase;
    
    [ReadOnly] public Player player;
    [ReadOnly] private int currentHouseID = -1;

    [FormerlySerializedAs("allSpirimonzPrefabs")] public SpirimonzSettings[] allSpirimonzSettings;
    private GameData gameData;

    private bool isLoadingFromHouse = false;
    private bool _isWorld;
    private bool _firstLoad = true;

    private InventoryManager _inventoryManager;
    private bool _isDead;

    public UnityEvent onMoneyUpdated;

    private void Update()
    {
        if (enableDebugMoneyButton && ((!MobileInput.Enabled && Input.GetKeyDown(KeyCode.Y)) || MobileInput.ConsumeYDown()))
        {
            AddMoney(100);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        MobileInput.SetEnabled(mobileControlsEnabled);
        MobileControlsBootstrap.EnsureExists();
        MobileLightOptimizerManager.EnsureExists();
        MobileCinemachineInputGate.EnsureExists();
        MobilePerformanceManager.EnsureExists();

        if (mobileControlsEnabled)
            mobileLightOptimizationEnabled = true;
        MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);
        MobilePerformanceManager.Instance.SetEnabled(mobileControlsEnabled);

        CheckUniqueSpirimonzIDs();

        Scene currentScene = SceneManager.GetActiveScene();
        bool isTitleScreen = !string.IsNullOrEmpty(titleScreenSceneName) &&
                             currentScene.name == titleScreenSceneName;

        SaveManager.allSpirimonzSettings = allSpirimonzSettings;
        SaveManager.InitializeActiveSlotFromPrefs();
#if UNITY_EDITOR
        if (!isTitleScreen && PlayerPrefs.GetInt("TempSaveSlotActive", 0) == 0)
            SaveManager.SetActiveSlot(1, temporary: false, persist: false);
#endif

        gameData = SaveManager.Load();
        EnsureSaveDefaults();
        ApplySavedFrameRateSetting();
        ApplyInputBindingsIfReady();
        ApplySavedSettingsIfPossible();

        // On est déjà dans le dernier World sauvegardé ?
        bool alreadyInLastWorld = !string.IsNullOrEmpty(gameData.lastWorldSceneName) &&
                                  currentScene.name == gameData.lastWorldSceneName;

        isLoadingFromHouse = (gameData.currentHouseID >= 0);
        SetCurrentHouseID(gameData.currentHouseID);

        bool isATestFromHouse = SceneManager.GetActiveScene().name.StartsWith("House");

        if (isATestFromHouse == false && !isTitleScreen)
        {
            if ( !string.IsNullOrEmpty(gameData.lastWorldSceneName) && !alreadyInLastWorld)
            {
                // On n'est pas encore dans le World → load la scène
                LoadScene(gameData.lastWorldSceneName, exitHouse: isLoadingFromHouse);
            }
            else if (alreadyInLastWorld)
            {
                // On est déjà dans le bon World → place le player directement
                if (player == null)
                    player = FindObjectOfType<Player>();
  
                if (player != null)
                {
                    World world = World.Instance;
                    if (world == null)
                    {
                        world = FindObjectOfType<World>();
                    }
                    if (currentHouseID >= 0 && currentHouseID < world.spawnPoints.Length)
                    {
                        // Spawn devant la maison
                        player.SetPosition(world.spawnPoints[currentHouseID].position);
                        player.SetRotation(world.spawnPoints[currentHouseID].rotation);
                    }
                    else
                    {
                        // Spawn à la position sauvegardée dans le world
                        player.SetPosition(gameData.playerPosition);
                        player.SetRotation(gameData.playerRotation);
                    }
                }
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void EnsureSaveDefaults()
    {
        if (gameData == null)
            return;

        if (gameData.ints.Find(i => i.id == SaveKeys.GOLD) == null)
        {
            gameData.ints.Add(new SaveVariableInt { id = SaveKeys.GOLD, value = 0 });
            SaveGame();
        }
    }

    private void ApplySavedFrameRateSetting()
    {
        int savedFps = GetInt(SaveKeys.TARGET_FPS, int.MinValue);
        if (savedFps != int.MinValue)
            ApplyFrameRateSetting(savedFps, save: false);
    }

    private void ApplySavedSettingsIfPossible()
    {
        if (gameData == null)
            return;

        SoundManager sound = SoundManager.Instance;
        float ambient = GetFloat(SaveKeys.AMBIENT_VOLUME_MULTIPLIER, float.NaN);
        if (!float.IsNaN(ambient) && sound != null)
            sound.SetAmbientVolumeMultiplier(ambient);

        float sfx = GetFloat(SaveKeys.SFX_VOLUME_MULTIPLIER, float.NaN);
        if (!float.IsNaN(sfx) && sound != null)
            sound.SetSfxVolumeMultiplier(sfx);

        InputManager input = InputManager.Instance;
        if (input != null)
        {
            float tps = GetFloat(SaveKeys.TPS_SENSITIVITY_MULTIPLIER, float.NaN);
            if (!float.IsNaN(tps))
                input.tpsLookSensitivityMultiplier = tps;

            float fps = GetFloat(SaveKeys.FPS_SENSITIVITY_MULTIPLIER, float.NaN);
            if (!float.IsNaN(fps))
                input.fpsLookSensitivityMultiplier = fps;
        }

        int langIndex = GetInt(SaveKeys.LANGUAGE, -1);
        if (langIndex >= 0)
        {
            Language[] languages = (Language[])Enum.GetValues(typeof(Language));
            if (langIndex < languages.Length)
                LanguageManager.CurrentLanguage = languages[langIndex];
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            MobileInput.SetEnabled(mobileControlsEnabled);
            MobileLightOptimizerManager.EnsureExists();
            MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);
            MobilePerformanceManager.EnsureExists();
            MobilePerformanceManager.Instance.SetEnabled(mobileControlsEnabled);
        }
    }

    public void SetMobileControlsEnabled(bool enable)
    {
        mobileControlsEnabled = enable;
        MobileInput.SetEnabled(enable);

        if (enable)
            mobileLightOptimizationEnabled = true;

        MobileLightOptimizerManager.EnsureExists();
        MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);

        MobilePerformanceManager.EnsureExists();
        MobilePerformanceManager.Instance.SetEnabled(enable);
    }

    public void SetMobileLightOptimizationEnabled(bool enable)
    {
        mobileLightOptimizationEnabled = enable;
        MobileLightOptimizerManager.EnsureExists();
        MobileLightOptimizerManager.Instance.SetEnabled(enable);
    }

    [ContextMenu("Toggle Mobile Light Optimization")]
    private void ToggleMobileLightOptimization()
    {
        SetMobileLightOptimizationEnabled(!mobileLightOptimizationEnabled);
    }

    [ContextMenu("Toggle Mobile Controls")]
    private void ToggleMobileControls()
    {
        SetMobileControlsEnabled(!mobileControlsEnabled);
    }

    private void Start()
    {
        ApplyInputBindingsIfReady();
        ApplySavedSettingsIfPossible();
        TryInitInventoryManager();
    }
    
    private void InitDefaultSpirimonzIfNeeded()
    {
        if (gameData == null || _inventoryManager == null)
            return;

        // 1. Vérifie si la team est vide dans la save
        bool hasAnySpirimonzInTeam = gameData.spirimonzCollection
            .Any(s => s.inTeam);

        if (hasAnySpirimonzInTeam)
            return;

        Debug.Log("Team is empty, initializing default Spirimonz...");

        foreach (var settings in allSpirimonzSettings)
        {
            if (!settings.unlockedByDefault)
                continue;

            SpirimonzData spData = Array.Find(
                gameData.spirimonzCollection,
                s => s.id == settings.spirimonzID
            );

            if (spData == null)
                continue;

            // 2. Unlock UNIQUEMENT
            spData.unlocked = true;

            // 3. Ajout à la team (InventoryManager gère TOUT)
            _inventoryManager.AddSpirimonzToTeam(settings);
        }

        // Save globale (unlock + team déjà sync via GameManager)
        SaveManager.Save(gameData);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isTitleScreen = !string.IsNullOrEmpty(titleScreenSceneName) &&
                             scene.name == titleScreenSceneName;
        if (isTitleScreen)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        if (player == null)
            player = FindObjectOfType<Player>();

        // Re-apply mobile state after scene load (House / World).
        MobileInput.SetEnabled(mobileControlsEnabled);
        MobileControlsBootstrap.EnsureExists();
        MobileCinemachineInputGate.EnsureExists();
        MobileLightOptimizerManager.EnsureExists();
        MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);
        MobilePerformanceManager.EnsureExists();
        MobilePerformanceManager.Instance.SetEnabled(mobileControlsEnabled);
        ApplySavedSettingsIfPossible();

        if (player == null)
        {
            Debug.LogWarning("Player introuvable dans la scène !");
            return;
        }
        
        _isWorld = scene.name.ToLower().StartsWith("world");

        if (_isWorld)
        {
            royalChallengeActive = false;
            World world = World.Instance;

            if (world == null)
            {
                world = FindObjectOfType<World>();
            }

            if (useTutorialWorldSpawn && world != null && world.startPosTuto != null)
            {
                player.SetPosition(world.startPosTuto.position);
                player.SetRotation(world.startPosTuto.rotation);
                useTutorialWorldSpawn = false;
            }
            else if (isLoadingFromHouse && currentHouseID >= 0 && currentHouseID < world.spawnPoints.Length)
            {
                // Spawn devant la maison
                player.SetPosition(world.spawnPoints[currentHouseID].position);
                player.SetRotation(world.spawnPoints[currentHouseID].rotation);
            }
            else
            {
                // Spawn à la position sauvegardée dans le world
                player.SetPosition(gameData.playerPosition);
                player.SetRotation(gameData.playerRotation);
            }

            if (_isDead)
            {
                TryToTriggerReviveAnimation();
            }

            // Reset le flag
            isLoadingFromHouse = false;
        }
        else
        {
            TryInitInventoryManager();
            if (_inventoryManager != null)
            {
                this.Invoke(0.1f, () =>
                {
                    _inventoryManager.OnLoadHouseScene();
                });
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void TryInitInventoryManager()
    {
        if (_inventoryManager == null)
            _inventoryManager = InventoryManager.Instance;
        if (_inventoryManager == null)
            return;

        _inventoryManager.LoadTeamFromSave();
        InitDefaultSpirimonzIfNeeded();
    }

    private void TryToTriggerReviveAnimation()
    {
        _isDead = false;
        
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        WorldPlayer wPlayer = player as WorldPlayer;

        if (wPlayer != null)
        {
            wPlayer.PlayReviveAnimation();
        }
    }

    public void SetCurrentHouseID(int houseID)
    {
        currentHouseID = houseID;
    }

    public void LoadScene(string sceneName, bool exitHouse = false)
    {
        isLoadingFromHouse = exitHouse;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    public void LoadHouseSceneWithMode(string sceneName, HouseSceneMode mode, bool exitHouse = false)
    {
        SetNextHouseSceneMode(mode);
        LoadScene(sceneName, exitHouse);
    }

    public void SetNextHouseSceneMode(HouseSceneMode mode)
    {
        nextHouseSceneMode = mode;
    }

    public HouseSceneMode PeekNextHouseSceneMode()
    {
        return nextHouseSceneMode;
    }

    public HouseSceneMode ConsumeNextHouseSceneMode()
    {
        HouseSceneMode mode = nextHouseSceneMode;
        nextHouseSceneMode = HouseSceneMode.NormalMap;
        return mode;
    }
    
    /// <summary>Met à jour l'état d'un Spirimonz dans la team</summary>
    public void SetSpirimonzInTeam(string spirimonzID, int position, bool inTeam)
    {
        SpirimonzData spData = Array.Find(gameData.spirimonzCollection, s => s.id == spirimonzID);
        if (spData != null)
        {
            spData.inTeam = inTeam;
            spData.teamPosition = inTeam ? Mathf.Max(0, position) : -1;
            SaveGame();
        }
    }

    public void UnlockSpirimonz(string spirimonzID)
    {
        SpirimonzData spData = Array.Find(gameData.spirimonzCollection, s => s.id == spirimonzID);
        if (spData != null)
        {
            spData.unlocked = true;
            SaveGame();
        }
    }

    public bool IsSpirimonzCaptured(string spirimonzID)
    {
        # if UNITY_EDITOR
            if (considerEverySpirimonzUnlocked) return true;
        # endif
        
        SpirimonzSettings spirimonzSettings =
            allSpirimonzSettings.FirstOrDefault(s => s.spirimonzID == spirimonzID);

        if (spirimonzSettings != null && spirimonzSettings.unlockedByDefault)
        {
            return true;
        }
        
        SpirimonzData spData = Array.Find(gameData.spirimonzCollection, s => s.id == spirimonzID);
        if (spData != null)
        {
            return spData.unlocked;
        }

        return false;
    }

    public int GetUnlockedSpirimonzCount()
    {
        if (allSpirimonzSettings == null)
            return 0;

        #if UNITY_EDITOR
            if (considerEverySpirimonzUnlocked)
                return allSpirimonzSettings.Length;
        #endif

        if (gameData == null || gameData.spirimonzCollection == null)
            return 0;

        int count = 0;
        foreach (SpirimonzSettings settings in allSpirimonzSettings)
        {
            if (settings == null)
                continue;

            if (settings.unlockedByDefault)
            {
                count++;
                continue;
            }

            SpirimonzData spData = Array.Find(gameData.spirimonzCollection, s => s.id == settings.spirimonzID);
            if (spData != null && spData.unlocked)
                count++;
        }

        return count;
    }

    public void SaveGame()
    {
        if (gameData == null || player == null)
            return;

        Scene currentScene = SceneManager.GetActiveScene();
        bool isWorld = currentScene.name.ToLower().StartsWith("world");

        if (isWorld)
        {
            // Sauvegarde position + rotation dans le world
            gameData.lastWorldSceneName = currentScene.name;
            gameData.playerPosition = player.GetPosition();
            gameData.playerRotation = player.GetRotation(); // <<< ajouté
            gameData.currentHouseID = -1;
        }
        else
        {
            // Sauvegarde maison
            gameData.currentHouseID = currentHouseID;
        }

        SaveManager.SaveInputBindings(gameData, InputManager.Instance);
        SaveManager.Save(gameData);
        Debug.Log("Game saved!");
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    public void UseDeadAnimation()
    {
        _isDead = true;
    }

    public bool IsWorld() => _isWorld;
    public GameData GetGameData() => gameData;

    public void SaveInputBindings()
    {
        if (gameData == null)
            return;
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        SaveManager.SaveInputBindings(gameData, input);
        SaveManager.Save(gameData);
    }

    public void UseSaveSlot(int slot, bool createIfMissing = true, bool temporary = false)
    {
        SaveManager.SetActiveSlot(slot, temporary);
        gameData = SaveManager.Load(slot, createIfMissing);
        EnsureSaveDefaults();
        ApplySavedFrameRateSetting();
        ApplyInputBindingsIfReady();
        ApplySavedSettingsIfPossible();

        isLoadingFromHouse = gameData != null && gameData.currentHouseID >= 0;
        SetCurrentHouseID(gameData != null ? gameData.currentHouseID : -1);
    }

    public void LoadWorldFromCurrentSave()
    {
        string sceneToLoad = gameData != null && !string.IsNullOrEmpty(gameData.lastWorldSceneName)
            ? gameData.lastWorldSceneName
            : defaultWorldSceneName;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            bool exitHouse = gameData != null && gameData.currentHouseID >= 0;
            LoadScene(sceneToLoad, exitHouse);
        }
    }

    private void CleanupTemporarySaveIfNeeded()
    {
        if (!SaveManager.IsTemporarySlot)
            return;

        SaveManager.DeleteSave(SaveManager.CurrentSlot);
        SaveManager.SetActiveSlot(1, temporary: false, persist: true);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
            CleanupTemporarySaveIfNeeded();
    }

    private void OnApplicationQuit()
    {
        if (SaveManager.IsTemporarySlot)
        {
            CleanupTemporarySaveIfNeeded();
            return;
        }

        SaveGame();
    }

    private void ApplyInputBindingsIfReady()
    {
        if (gameData == null)
            return;
        InputManager input = InputManager.Instance;
        if (input == null)
            return;
        SaveManager.LoadInputBindings(gameData, input);
    }

    private void CheckUniqueSpirimonzIDs()
    {
#if UNITY_EDITOR
        HashSet<string> idSet = new HashSet<string>();
        foreach (var spiri in allSpirimonzSettings)
        {
            if (spiri == null) continue;
            if (string.IsNullOrEmpty(spiri.spirimonzID))
                Debug.LogError($"Spirimonz '{spiri.name}' n’a pas de spirimonzID défini !");
            if (!idSet.Add(spiri.spirimonzID))
            {
                Debug.LogError($"Erreur : Le spirimonzID '{spiri.spirimonzID}' est dupliqué ! Vérifie le prefab '{spiri.name}' !");
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
#endif
    }
    
    public QuestData GetOrCreateQuestProgress(Quest quest, string contextID)
    {
        if (gameData == null) return null;

        // Cherche la progression existante
        QuestData qData = gameData.questProgression
            .Find(q => q.questID == quest.name && q.contextID == contextID);

        // Si aucune progression existante, en crée une
        if (qData == null)
        {
            qData = new QuestData(quest.name, contextID);
            gameData.questProgression.Add(qData);
            SaveGame();
        }

        return qData;
    }

    public void UpdateQuestProgress(Quest quest, string contextID, int progressToAdd)
    {
        QuestData qData = GetOrCreateQuestProgress(quest, contextID);
        if (qData.completed) return;

        qData.progress += progressToAdd;
        if (qData.progress >= quest.goal)
        {
            qData.progress = quest.goal;
            qData.completed = true;
            Debug.Log($"Quest '{quest.questName}' completed in context '{contextID}' !");
        }

        SaveGame();
    }

    public int GetQuestProgress(Quest quest, string contextID)
    {
        QuestData qData = GetOrCreateQuestProgress(quest, contextID);
        return qData.progress;
    }

    public bool IsQuestCompleted(Quest quest, string contextID)
    {
        QuestData qData = GetOrCreateQuestProgress(quest, contextID);
        return qData.completed;
    }

    public bool IsQuestRewardClaimed(Quest quest, string contextID)
    {
        QuestData qData = GetOrCreateQuestProgress(quest, contextID);
        return qData.rewardClaimed;
    }

    public bool TryClaimQuestReward(Quest quest, string contextID)
    {
        QuestData qData = GetOrCreateQuestProgress(quest, contextID);
        if (qData == null || qData.completed == false || qData.rewardClaimed)
            return false;

        qData.rewardClaimed = true;
        AddMoney(Mathf.Max(0, quest.rewardPrice));
        SaveGame();
        return true;
    }
    
    public void SetInt(string id, int value)
    {
        var entry = gameData.ints.Find(i => i.id == id);

        if (entry == null)
        {
            gameData.ints.Add(new SaveVariableInt { id = id, value = value });
        }
        else
        {
            entry.value = value;
        }

        SaveGame();
    }

    public int GetInt(string id, int defaultValue = 0)
    {
        var entry = gameData.ints.Find(i => i.id == id);
        return entry != null ? entry.value : defaultValue;
    }
    
    public void SetBool(string id, bool value)
    {
        var entry = gameData.bools.Find(b => b.id == id);

        if (entry == null)
            gameData.bools.Add(new SaveVariableBool { id = id, value = value });
        else
            entry.value = value;

        if (player != null)
        {
            SaveGame();
        }
        else
        {
            SaveManager.Save(gameData);
        }
    }

    public bool GetBool(string id, bool defaultValue = false)
    {
        var entry = gameData.bools.Find(b => b.id == id);
        return entry != null ? entry.value : defaultValue;
    }
    
    public void SetFloat(string id, float value)
    {
        var entry = gameData.floats.Find(f => f.id == id);

        if (entry == null)
            gameData.floats.Add(new SaveVariableFloat { id = id, value = value });
        else
            entry.value = value;

        SaveGame();
    }

    public float GetFloat(string id, float defaultValue = 0f)
    {
        var entry = gameData.floats.Find(f => f.id == id);
        return entry != null ? entry.value : defaultValue;
    }
    
    public void SetString(string id, string value)
    {
        var entry = gameData.strings.Find(s => s.id == id);

        if (entry == null)
            gameData.strings.Add(new SaveVariableString { id = id, value = value });
        else
            entry.value = value;

        SaveGame();
    }

    public string GetString(string id, string defaultValue = "")
    {
        var entry = gameData.strings.Find(s => s.id == id);
        return entry != null ? entry.value : defaultValue;
    }

    public void ApplyFrameRateSetting(int fpsSetting, bool save = true)
    {
        if (save)
            SetInt(SaveKeys.TARGET_FPS, fpsSetting);

        if (fpsSetting == 0)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            if (MobilePerformanceManager.Instance != null)
                MobilePerformanceManager.Instance.autoAdjust = true;
        }
        else if (fpsSetting < 0)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (MobilePerformanceManager.Instance != null)
                MobilePerformanceManager.Instance.autoAdjust = false;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fpsSetting;
            if (MobilePerformanceManager.Instance != null)
                MobilePerformanceManager.Instance.autoAdjust = false;
        }
    }
    
    public bool CanBuy(int price)
    {
        return GetInt(SaveKeys.GOLD) >= price;
    }

    public bool Buy(int price)
    {
        if (CanBuy(price) == false) return false;
        
        SetInt(SaveKeys.GOLD, GetInt(SaveKeys.GOLD) - price);
        onMoneyUpdated?.Invoke();
        
        return true;
    }

    public void AddMoney(int value)
    {
        if (disableMoneyGain)
            return;

        SetInt(SaveKeys.GOLD, GetInt(SaveKeys.GOLD) + value);
        onMoneyUpdated?.Invoke();
    }
}
