using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : GameBehaviour
{
    public static GameManager Instance;

    [Header("Debug")] 
    public bool ignoreAllHouseDebugs;
    public bool considerEverySpirimonzUnlocked;
    
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
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
}