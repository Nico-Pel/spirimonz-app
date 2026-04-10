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
    public const string UI_VOLUME_MULTIPLIER = "ui_volume_multiplier";
    public const string TPS_SENSITIVITY_MULTIPLIER = "tps_sensitivity_multiplier";
    public const string FPS_SENSITIVITY_MULTIPLIER = "fps_sensitivity_multiplier";
    public const string LANGUAGE = "language";
    public const string TUTORIAL_DOOR_UNLOCKED = "tutorial_door_unlocked";
    public const string SECRET_WORLD_INDEX = "secret_world_index";
    public const string SECRET_WORLD_START_UTC_TICKS = "secret_world_start_utc_ticks";
    public const string SECRET_WORLD_LAST_ROTATION_LOCAL_TICKS = "secret_world_last_rotation_local_ticks";
    public const string SECRET_WORLD_SCENE_NAME = "secret_world_scene_name";
    public const string SECRET_WORLD_PRICE_STEPS = "secret_world_price_steps";
    public const string SECRET_WORLD_PRICE_ROTATION_TICKS = "secret_world_price_rotation_ticks";
    public const string SECRET_WORLD_PRICE_INDEX = "secret_world_price_index";
    public const string SECRET_WORLD_PRICE_MAX_STEPS = "secret_world_price_max_steps";
    public const string SECRET_WORLD_PRICE_INCREMENT = "secret_world_price_increment";
    public const string SECRET_WORLD_TRAVEL_PAID_INDEX = "secret_world_travel_paid_index";
    public const string SECRET_WORLD_TRAVEL_PAID_ROTATION_TICKS = "secret_world_travel_paid_rotation_ticks";
    public const string SECRET_WORLD_ROTATION_HOUR = "secret_world_rotation_hour";
    public const string SECRET_WORLD_ROTATION_MINUTE = "secret_world_rotation_minute";
    public const string SECRET_WORLD_RETURN_TO_TAXI = "secret_world_return_to_taxi";
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
    public bool rewardClaimed; // récompense récupérée ou pas

    public QuestData(string questID, string contextID)
    {
        this.questID = questID;
        this.contextID = contextID;
        progress = 0;
        completed = false;
        rewardClaimed = false;
    }
}

public static class SaveManager
{
    private const string LegacyFileName = "savefile.json";
    private const string SaveFilePrefix = "savefile_";
    private const int DefaultSlot = 1;
    private const int MaxSlot = 4;
    private const string ActiveSlotPref = "ActiveSaveSlot";
    private const string TempSlotPref = "TempSaveSlotActive";

    private static int _currentSlot = DefaultSlot;
    private static bool _isTemporarySlot;

    private static string LegacyFilePath => Path.Combine(Application.persistentDataPath, LegacyFileName);
    private static string GetFilePath(int slot) => Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slot}.json");

    public static int CurrentSlot => _currentSlot;
    public static bool IsTemporarySlot => _isTemporarySlot;

    public static void InitializeActiveSlotFromPrefs()
    {
        int slot = PlayerPrefs.GetInt(ActiveSlotPref, DefaultSlot);
        if (slot < DefaultSlot || slot > MaxSlot)
            slot = DefaultSlot;

        bool temp = PlayerPrefs.GetInt(TempSlotPref, 0) == 1;
        SetActiveSlot(slot, temp, persist: false);
    }

    public static void SetActiveSlot(int slot, bool temporary = false, bool persist = true)
    {
        if (slot < DefaultSlot || slot > MaxSlot)
            slot = DefaultSlot;

        _currentSlot = slot;
        _isTemporarySlot = temporary;

        if (!persist)
            return;

        PlayerPrefs.SetInt(ActiveSlotPref, _currentSlot);
        PlayerPrefs.SetInt(TempSlotPref, _isTemporarySlot ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool SaveExists(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
            return true;

        return slot == DefaultSlot && File.Exists(LegacyFilePath);
    }

    // Gère la liste de tous tes prefabs Spirimonz
    public static SpirimonzSettings[] allSpirimonzSettings;

    // Sauvegarde
    public static void Save(GameData data)
    {
        Save(data, _currentSlot);
    }

    // Chargement
    public static GameData Load()
    {
        return Load(_currentSlot, createIfMissing: true);
    }

    public static void Save(GameData data, int slot)
    {
        if (data == null)
            return;

        string path = GetFilePath(slot);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log("Game saved to: " + path);
    }

    public static GameData Load(int slot, bool createIfMissing)
    {
        string path = GetFilePath(slot);

        if (!File.Exists(path))
        {
            if (slot == DefaultSlot && File.Exists(LegacyFilePath))
            {
                File.Copy(LegacyFilePath, path, overwrite: true);
            }
        }

        if (!File.Exists(path))
        {
            if (!createIfMissing)
                return null;

            Debug.Log("No save file found, creating new one.");
            GameData created = CreateNewData();
            Save(created, slot);
            return created;
        }

        string json = File.ReadAllText(path);
        GameData data = JsonUtility.FromJson<GameData>(json);

        // Vérifie si de nouveaux Spirimonz ont été ajoutés et les ajoute automatiquement
        data = AddMissingSpirimonz(data);

        return data;
    }

    public static GameData CreateNewSave(int slot)
    {
        GameData data = CreateNewData();
        Save(data, slot);
        return data;
    }

    public static void DeleteSave()
    {
        DeleteSave(_currentSlot);
    }

    public static void DeleteSave(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
            File.Delete(path);
        if (slot == DefaultSlot && File.Exists(LegacyFilePath))
            File.Delete(LegacyFilePath);
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
