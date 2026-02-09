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
    
    private bool isLoadingFromHouse = false; // indique qu'on vient d'une maison

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Vérifie que tous les Spirimonz ont un ID unique
        CheckUniqueSpirimonzIDs();

        SaveManager.allSpirimonzPrefabs = allSpirimonzPrefabs;
        gameData = SaveManager.Load();

        // Si on a une maison sauvegardée, on veut charger le world associé
        if (gameData.currentHouseID >= 0 && !string.IsNullOrEmpty(gameData.lastWorldSceneName))
        {
            currentHouseID = gameData.currentHouseID;
            LoadScene(gameData.lastWorldSceneName);
        }

        // On attend que la scène soit chargée avant de placer le joueur
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
            if (isLoadingFromHouse)
            {
                // On vient d'une maison → spawn à spawnPoints[currentHouseID]
                if (currentHouseID >= 0 && currentHouseID < spawnPoints.Length)
                {
                    player.SetPosition(spawnPoints[currentHouseID].position);
                    player.transform.rotation = spawnPoints[currentHouseID].rotation;
                }
                else
                {
                    Debug.LogWarning("currentHouseID invalide, spawn par défaut !");
                }

                isLoadingFromHouse = false; // reset le flag
            }
            else
            {
                // Lancement du jeu → spawn à la position sauvegardée
                if (!string.IsNullOrEmpty(gameData.lastWorldSceneName) &&
                    gameData.lastWorldSceneName == scene.name)
                {
                    player.SetPosition(gameData.playerPosition);
                }
                else
                {
                    Debug.Log("Pas de position sauvegardée pour ce world, spawn par défaut.");
                }
            }
        }

        // Retirer le listener pour éviter doublons
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    public void SetCurrentHouseID(int houseID)
    {
        if (houseID < 0) return;
        currentHouseID = houseID;
    }

    public void LoadScene(string sceneName, bool exitHouse = false)
    {
        isLoadingFromHouse = exitHouse;

        // Retire avant d'ajouter pour éviter doublons
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
            // Dans un World → save position + nom du world
            gameData.lastWorldSceneName = currentScene.name;
            gameData.playerPosition = player.transform.position;
        }
        else
        {
            // Dans une maison ou autre → on sauvegarde juste currentHouseID
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