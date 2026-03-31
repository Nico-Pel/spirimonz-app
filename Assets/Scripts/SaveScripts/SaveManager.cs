using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public static class SaveKeys
{
    public const string GOLD = "gold";
    public const string MUSIC_VOLUME = "music_volume"; //Example
    public const string INTRO_DONE = "intro_done"; //Example
    public const string TARGET_FPS = "target_fps";
    public const string AMBIENT_VOLUME_MULTIPLIER = "ambient_volume_multiplier";
    public const string SFX_VOLUME_MULTIPLIER = "sfx_volume_multiplier";
    public const string TPS_SENSITIVITY_MULTIPLIER = "tps_sensitivity_multiplier";
    public const string FPS_SENSITIVITY_MULTIPLIER = "fps_sensitivity_multiplier";
    public const string LANGUAGE = "language";
    public const string TUTORIAL_DOOR_UNLOCKED = "tutorial_door_unlocked";
}

[Serializable]
public class SaveVariableInt
{
    public string id;
    public int value;
}

[Serializable]
public class SaveVariableFloat
{
    public string id;
    public float value;
}

[Serializable]
public class SaveVariableBool
{
    public string id;
    public bool value;
}

[Serializable]
public class SaveVariableString
{
    public string id;
    public string value;
}

[Serializable]
public class InputBindingData
{
    public string id;
    public int primary;
    public int secondary;
}

[Serializable]
public class GameData
{
    public SpirimonzData[] spirimonzCollection;

    public string lastWorldSceneName;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public int currentHouseID = -1;
    
    public List<QuestData> questProgression = new List<QuestData>();

    // === Global save variables ===
    public List<SaveVariableInt> ints = new();
    public List<SaveVariableFloat> floats = new();
    public List<SaveVariableBool> bools = new();
    public List<SaveVariableString> strings = new();
    public List<InputBindingData> inputBindings = new();
}

[Serializable]
public class SpirimonzData
{
    public string id;          
    [FormerlySerializedAs("captured")] public bool unlocked;      
    public bool inTeam;        
    public int teamPosition;   
    public int level;          

    public SpirimonzData(string id)
    {
        this.id = id;
        unlocked = false;
        inTeam = false;
        teamPosition = 0;
        level = 1;
    }
}

[System.Serializable]
public class QuestData
{
    public string questID;     // ID unique du Quest ScriptableObject
    public string contextID;   // ID du contexte (ex: map ou house)
    public int progress;       // progression actuelle
    public bool completed;     // terminé ou pas

    public QuestData(string questID, string contextID)
    {
        this.questID = questID;
        this.contextID = contextID;
        progress = 0;
        completed = false;
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

    public static void SaveInputBindings(GameData data, InputManager input)
    {
        if (data == null || input == null)
            return;

        if (data.inputBindings == null)
            data.inputBindings = new List<InputBindingData>();
        else
            data.inputBindings.Clear();

        List<InputManager.BindingDefinition> defs = input.GetBindingDefinitions();
        foreach (var def in defs)
        {
            data.inputBindings.Add(new InputBindingData
            {
                id = def.id,
                primary = (int)def.getPrimary(),
                secondary = (int)(def.getSecondary != null ? def.getSecondary() : KeyCode.None)
            });
        }
    }

    public static void LoadInputBindings(GameData data, InputManager input)
    {
        if (data == null || input == null)
            return;
        if (data.inputBindings == null || data.inputBindings.Count == 0)
            return;

        Dictionary<string, InputBindingData> map = new Dictionary<string, InputBindingData>();
        foreach (var entry in data.inputBindings)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.id))
                map[entry.id] = entry;
        }

        List<InputManager.BindingDefinition> defs = input.GetBindingDefinitions();
        foreach (var def in defs)
        {
            if (map.TryGetValue(def.id, out InputBindingData saved))
            {
                def.setPrimary?.Invoke((KeyCode)saved.primary);
                def.setSecondary?.Invoke((KeyCode)saved.secondary);
            }
        }
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
