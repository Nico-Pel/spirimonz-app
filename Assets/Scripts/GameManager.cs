using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Player player;
    public Transform[] spawnPoints;
    [ReadOnly] public int currentHouseID = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerSpawnPos()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (player == null)
        {
            return;
        }
        
        if (currentHouseID >= 0 && spawnPoints.Length > currentHouseID && spawnPoints[currentHouseID] != null)
        {
            player.SetPosition(spawnPoints[currentHouseID].position);
            player.SetRotation(spawnPoints[currentHouseID].rotation);
        }
    }

    public void SetCurrentHouseID(int houseID)
    {
        if (houseID < 0) return;
        currentHouseID = houseID;
    }
    
    public void LoadScene(string sceneName)
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name.StartsWith("World"))
        {
            SetPlayerSpawnPos();
        }
    }
}