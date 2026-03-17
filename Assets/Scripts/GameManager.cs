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

    [Header("Debug")] 
    public bool ignoreAllHouseDebugs;
    public bool considerEverySpirimonzUnlocked;

    [Header("Mobile Controls")]
    public bool mobileControlsEnabled;

    [Header("Mobile Light Optimization")]
    public bool mobileLightOptimizationEnabled = true;
    
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
        # if UNITY_EDITOR
        if ((!MobileInput.Enabled && Input.GetKeyDown(KeyCode.Y)) || MobileInput.YDown)
        {
            AddMoney(100);
        }
        #endif
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

        if (mobileControlsEnabled)
            mobileLightOptimizationEnabled = true;
        MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);

        CheckUniqueSpirimonzIDs();

        SaveManager.allSpirimonzSettings = allSpirimonzSettings;
        gameData = SaveManager.Load();

        Scene currentScene = SceneManager.GetActiveScene();

        // On est déjà dans le dernier World sauvegardé ?
        bool alreadyInLastWorld = !string.IsNullOrEmpty(gameData.lastWorldSceneName) &&
                                  currentScene.name == gameData.lastWorldSceneName;

        isLoadingFromHouse = (gameData.currentHouseID >= 0);
        SetCurrentHouseID(gameData.currentHouseID);

        bool isATestFromHouse = SceneManager.GetActiveScene().name.StartsWith("House");

        if (isATestFromHouse == false)
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

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            MobileInput.SetEnabled(mobileControlsEnabled);
            MobileLightOptimizerManager.EnsureExists();
            MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);
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
        _inventoryManager = InventoryManager.Instance;
        _inventoryManager.LoadTeamFromSave();

        InitDefaultSpirimonzIfNeeded();
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
        if (player == null)
            player = FindObjectOfType<Player>();

        // Re-apply mobile state after scene load (House / World).
        MobileInput.SetEnabled(mobileControlsEnabled);
        MobileControlsBootstrap.EnsureExists();
        MobileCinemachineInputGate.EnsureExists();
        MobileLightOptimizerManager.EnsureExists();
        MobileLightOptimizerManager.Instance.SetEnabled(mobileLightOptimizationEnabled);

        if (player == null)
        {
            Debug.LogWarning("Player introuvable dans la scène !");
            return;
        }
        
        _isWorld = scene.name.ToLower().StartsWith("world");

        if (_isWorld)
        {
            World world = World.Instance;

            if (world == null)
            {
                world = FindObjectOfType<World>();
            }
            
            if (isLoadingFromHouse && currentHouseID >= 0 && currentHouseID < world.spawnPoints.Length)
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
            if (_inventoryManager == null)
            {
                _inventoryManager = FindObjectOfType<InventoryManager>();
            }
            
            this.Invoke(0.1f, () =>
            {
                _inventoryManager.OnLoadHouseScene();
            });
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
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

    private void OnApplicationQuit()
    {
        SaveGame();
    }
    
    public bool IsWorld() => _isWorld;
    public GameData GetGameData() => gameData;

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

        SaveGame();
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
        SetInt(SaveKeys.GOLD, GetInt(SaveKeys.GOLD) + value);
        onMoneyUpdated?.Invoke();
    }
}
