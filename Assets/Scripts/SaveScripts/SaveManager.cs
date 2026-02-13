using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameData
{
    public SpirimonzData[] spirimonzCollection;

    public string lastWorldSceneName; // Le dernier world visité
    public Vector3 playerPosition;    // Position dans ce world
    public Quaternion playerRotation; // Rotation dans ce world
    public int currentHouseID = -1;        // House actuelle si on est dans une house
}

[Serializable]
public class SpirimonzData
{
    public string id;          
    public bool captured;      
    public bool inTeam;        
    public int teamPosition;   
    public int level;          

    public SpirimonzData(string id)
    {
        this.id = id;
        captured = false;
        inTeam = false;
        teamPosition = 0;
        level = 1;
    }
}

public static class SaveManager
{
    private static string filePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    // Gère la liste de tous tes prefabs Spirimonz
    public static SpirimonzSettings[] allSpirimonzSettings;

    // Sauvegarde
    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Game saved to: " + filePath);
    }

    // Chargement
    public static GameData Load()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("No save file found, creating new one.");
            return CreateNewData();
        }

        string json = File.ReadAllText(filePath);
        GameData data = JsonUtility.FromJson<GameData>(json);

        // Vérifie si de nouveaux Spirimonz ont été ajoutés et les ajoute automatiquement
        data = AddMissingSpirimonz(data);

        return data;
    }

    public static void DeleteSave()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    // Crée une nouvelle GameData à partir des prefabs
    private static GameData CreateNewData()
    {
        GameData newData = new GameData();

        if (allSpirimonzSettings == null || allSpirimonzSettings.Length == 0)
        {
            Debug.LogError("Aucun prefab Spirimonz référencé dans SaveManager.allSpirimonzPrefabs !");
            newData.spirimonzCollection = new SpirimonzData[0];
            return newData;
        }

        newData.spirimonzCollection = new SpirimonzData[allSpirimonzSettings.Length];
        for (int i = 0; i < allSpirimonzSettings.Length; i++)
        {
            newData.spirimonzCollection[i] = new SpirimonzData(allSpirimonzSettings[i].spirimonzID);
        }

        return newData;
    }

    // Ajoute automatiquement les Spirimonz manquants dans la save si tu en ajoutes un nouveau prefab
    private static GameData AddMissingSpirimonz(GameData data)
    {
        if (allSpirimonzSettings == null || allSpirimonzSettings.Length == 0)
            return data;

        var spirimonzList = new System.Collections.Generic.List<SpirimonzData>(data.spirimonzCollection);

        foreach (var prefab in allSpirimonzSettings)
        {
            bool exists = spirimonzList.Exists(s => s.id == prefab.spirimonzID);
            if (!exists)
            {
                spirimonzList.Add(new SpirimonzData(prefab.spirimonzID));
                Debug.Log("Added new Spirimonz to save: " + prefab.spirimonzID);
            }
        }

        data.spirimonzCollection = spirimonzList.ToArray();
        return data;
    }
}