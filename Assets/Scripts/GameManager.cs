using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Player player;
    public Transform[] spawnPoints;
    [ReadOnly] public int currentHouseID = -1;

    public Spirimonz[] allSpirimonzPrefabs;
    private GameData gameData;

    private bool isLoadingFromHouse = false;

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

        SaveManager.allSpirimonzPrefabs = allSpirimonzPrefabs;
        gameData = SaveManager.Load();

        Scene currentScene = SceneManager.GetActiveScene();

        // On est déjà dans le dernier World sauvegardé ?
        bool alreadyInLastWorld = !string.IsNullOrEmpty(gameData.lastWorldSceneName) &&
                                  currentScene.name == gameData.lastWorldSceneName;

        isLoadingFromHouse = (gameData.currentHouseID >= 0);
        currentHouseID = gameData.currentHouseID;

        if (!string.IsNullOrEmpty(gameData.lastWorldSceneName) && !alreadyInLastWorld)
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
                if (currentHouseID >= 0 && currentHouseID < spawnPoints.Length)
                {
                    // Spawn devant la maison
                    player.SetPosition(spawnPoints[currentHouseID].position);
                    player.SetRotation(spawnPoints[currentHouseID].rotation);
                }
                else
                {
                    // Spawn à la position sauvegardée dans le world
                    player.SetPosition(gameData.playerPosition);
                    player.SetRotation(gameData.playerRotation);
                }
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
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

        bool isWorld = scene.name.ToLower().StartsWith("world");

        if (isWorld)
        {
            if (isLoadingFromHouse && currentHouseID >= 0 && currentHouseID < spawnPoints.Length)
            {
                // Spawn devant la maison
                player.SetPosition(spawnPoints[currentHouseID].position);
                player.SetRotation(spawnPoints[currentHouseID].rotation);
            }
            else
            {
                // Spawn à la position sauvegardée dans le world
                player.SetPosition(gameData.playerPosition);
                player.SetRotation(gameData.playerRotation);
            }

            // Reset le flag
            isLoadingFromHouse = false;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
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

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void CheckUniqueSpirimonzIDs()
    {
#if UNITY_EDITOR
        HashSet<string> idSet = new HashSet<string>();
        foreach (var spiri in allSpirimonzPrefabs)
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
