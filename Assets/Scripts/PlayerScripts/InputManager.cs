using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : GameBehaviour
{
    public static InputManager Instance { get; private set; }

    public KeyCode forwardKey = KeyCode.W;
    public KeyCode forwardKeyAlt = KeyCode.None;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode backwardKeyAlt = KeyCode.None;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode leftKeyAlt = KeyCode.None;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode rightKeyAlt = KeyCode.None;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode sprintKeyAlt = KeyCode.None;
    public KeyCode turnLight = KeyCode.T;
    public KeyCode turnLightAlt = KeyCode.None;
    public KeyCode grabObject = KeyCode.E;
    public KeyCode grabObjectAlt = KeyCode.None;
    public KeyCode dropObject = KeyCode.D;
    public KeyCode dropObjectAlt = KeyCode.None;
    public KeyCode throwObject = KeyCode.G;
    public KeyCode throwObjectAlt = KeyCode.None;
    public KeyCode openJournal = KeyCode.J;
    public KeyCode openJournalAlt = KeyCode.None;
    public KeyCode openTeamMenu = KeyCode.Tab;
    public KeyCode openTeamMenuAlt = KeyCode.None;
    public KeyCode exitMenus = KeyCode.Escape;
    public KeyCode exitMenusAlt = KeyCode.None;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode crouchKeyAlt = KeyCode.None;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode jumpKeyAlt = KeyCode.None;
    public KeyCode primaryNext = KeyCode.RightArrow;
    public KeyCode primaryPrevious = KeyCode.LeftArrow;
    public KeyCode secondaryNext = KeyCode.E;
    public KeyCode secondaryPrevious = KeyCode.A;
    public KeyCode[] inventoryKeys = new KeyCode[6];
    public KeyCode[] inventoryKeysAlt = new KeyCode[6];
    public KeyCode worldInteractions = KeyCode.E;
    public KeyCode worldInteractionsAlt = KeyCode.None;

    [Header("Sensitivity Multipliers")]
    [Range(0.5f, 1.5f)] public float tpsLookSensitivityMultiplier = 1f;
    [Range(0.5f, 1.5f)] public float fpsLookSensitivityMultiplier = 1f;

    public struct BindingDefinition
    {
        public string id;
        public string label;
        public Func<KeyCode> getPrimary;
        public Action<KeyCode> setPrimary;
        public Func<KeyCode> getSecondary;
        public Action<KeyCode> setSecondary;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        Instance = this;

        if (inventoryKeys == null || inventoryKeys.Length != 6)
            inventoryKeys = new KeyCode[6];
        if (inventoryKeysAlt == null || inventoryKeysAlt.Length != 6)
            inventoryKeysAlt = new KeyCode[6];

        if (GameManager.Instance != null)
        {
            GameData data = GameManager.Instance.GetGameData();
            if (data != null)
                SaveManager.LoadInputBindings(data, this);
        }
    }

    public bool GetKey(KeyCode primary, KeyCode secondary)
    {
        return Input.GetKey(primary) || (secondary != KeyCode.None && Input.GetKey(secondary));
    }

    public bool GetKeyDown(KeyCode primary, KeyCode secondary)
    {
        return Input.GetKeyDown(primary) || (secondary != KeyCode.None && Input.GetKeyDown(secondary));
    }

    public bool GetKeyUp(KeyCode primary, KeyCode secondary)
    {
        return Input.GetKeyUp(primary) || (secondary != KeyCode.None && Input.GetKeyUp(secondary));
    }

    public bool GetMoveForward() => GetKey(forwardKey, forwardKeyAlt);
    public bool GetMoveBackward() => GetKey(backwardKey, backwardKeyAlt);
    public bool GetMoveLeft() => GetKey(leftKey, leftKeyAlt);
    public bool GetMoveRight() => GetKey(rightKey, rightKeyAlt);
    public bool GetSprint() => GetKey(sprintKey, sprintKeyAlt);
    public bool GetTurnLightDown() => GetKeyDown(turnLight, turnLightAlt);
    public bool GetGrabDown() => GetKeyDown(grabObject, grabObjectAlt);
    public bool GetDropDown() => GetKeyDown(dropObject, dropObjectAlt);
    public bool GetThrowDown() => GetKeyDown(throwObject, throwObjectAlt);
    public bool GetOpenJournalDown() => GetKeyDown(openJournal, openJournalAlt);
    public bool GetOpenTeamMenuDown() => GetKeyDown(openTeamMenu, openTeamMenuAlt);
    public bool GetExitMenusDown() => GetKeyDown(exitMenus, exitMenusAlt);
    public bool GetCrouchDown() => GetKeyDown(crouchKey, crouchKeyAlt);
    public bool GetJumpDown() => GetKeyDown(jumpKey, jumpKeyAlt);
    public bool GetWorldInteractionDown() => GetKeyDown(worldInteractions, worldInteractionsAlt);

    public bool GetNextDown() => GetKeyDown(primaryNext, secondaryNext);
    public bool GetPreviousDown() => GetKeyDown(primaryPrevious, secondaryPrevious);

    public bool GetInventoryDown(int index)
    {
        if (index < 0 || index >= inventoryKeys.Length)
            return false;

        KeyCode primary = inventoryKeys[index];
        KeyCode secondary = (inventoryKeysAlt != null && index < inventoryKeysAlt.Length) ? inventoryKeysAlt[index] : KeyCode.None;
        return GetKeyDown(primary, secondary);
    }

    public string GetKeyDisplay(KeyCode primary, KeyCode secondary)
    {
        if (secondary == KeyCode.None)
            return primary.ToString();
        return primary + " / " + secondary;
    }

    public List<BindingDefinition> GetBindingDefinitions()
    {
        var bindings = new List<BindingDefinition>
        {
            new BindingDefinition { id = "move_forward", label = "Move Forward", getPrimary = () => forwardKey, setPrimary = v => forwardKey = v, getSecondary = () => forwardKeyAlt, setSecondary = v => forwardKeyAlt = v },
            new BindingDefinition { id = "move_backward", label = "Move Backward", getPrimary = () => backwardKey, setPrimary = v => backwardKey = v, getSecondary = () => backwardKeyAlt, setSecondary = v => backwardKeyAlt = v },
            new BindingDefinition { id = "move_left", label = "Move Left", getPrimary = () => leftKey, setPrimary = v => leftKey = v, getSecondary = () => leftKeyAlt, setSecondary = v => leftKeyAlt = v },
            new BindingDefinition { id = "move_right", label = "Move Right", getPrimary = () => rightKey, setPrimary = v => rightKey = v, getSecondary = () => rightKeyAlt, setSecondary = v => rightKeyAlt = v },
            new BindingDefinition { id = "sprint", label = "Sprint", getPrimary = () => sprintKey, setPrimary = v => sprintKey = v, getSecondary = () => sprintKeyAlt, setSecondary = v => sprintKeyAlt = v },
            new BindingDefinition { id = "grab", label = "Grab", getPrimary = () => grabObject, setPrimary = v => grabObject = v, getSecondary = () => grabObjectAlt, setSecondary = v => grabObjectAlt = v },
            new BindingDefinition { id = "drop", label = "Drop", getPrimary = () => dropObject, setPrimary = v => dropObject = v, getSecondary = () => dropObjectAlt, setSecondary = v => dropObjectAlt = v },
            new BindingDefinition { id = "throw", label = "Throw", getPrimary = () => throwObject, setPrimary = v => throwObject = v, getSecondary = () => throwObjectAlt, setSecondary = v => throwObjectAlt = v },
            new BindingDefinition { id = "toggle_light", label = "Toggle Light", getPrimary = () => turnLight, setPrimary = v => turnLight = v, getSecondary = () => turnLightAlt, setSecondary = v => turnLightAlt = v },
            new BindingDefinition { id = "open_journal", label = "Open Journal", getPrimary = () => openJournal, setPrimary = v => openJournal = v, getSecondary = () => openJournalAlt, setSecondary = v => openJournalAlt = v },
            new BindingDefinition { id = "open_team", label = "Open Team", getPrimary = () => openTeamMenu, setPrimary = v => openTeamMenu = v, getSecondary = () => openTeamMenuAlt, setSecondary = v => openTeamMenuAlt = v },
            new BindingDefinition { id = "exit_menus", label = "Exit Menus", getPrimary = () => exitMenus, setPrimary = v => exitMenus = v, getSecondary = () => exitMenusAlt, setSecondary = v => exitMenusAlt = v },
            new BindingDefinition { id = "crouch", label = "Crouch", getPrimary = () => crouchKey, setPrimary = v => crouchKey = v, getSecondary = () => crouchKeyAlt, setSecondary = v => crouchKeyAlt = v },
            new BindingDefinition { id = "jump", label = "Jump", getPrimary = () => jumpKey, setPrimary = v => jumpKey = v, getSecondary = () => jumpKeyAlt, setSecondary = v => jumpKeyAlt = v },
            new BindingDefinition { id = "world_interaction", label = "World Interaction", getPrimary = () => worldInteractions, setPrimary = v => worldInteractions = v, getSecondary = () => worldInteractionsAlt, setSecondary = v => worldInteractionsAlt = v },
            new BindingDefinition { id = "next", label = "Next", getPrimary = () => primaryNext, setPrimary = v => primaryNext = v, getSecondary = () => secondaryNext, setSecondary = v => secondaryNext = v },
            new BindingDefinition { id = "previous", label = "Previous", getPrimary = () => primaryPrevious, setPrimary = v => primaryPrevious = v, getSecondary = () => secondaryPrevious, setSecondary = v => secondaryPrevious = v }
        };

        if (inventoryKeys == null || inventoryKeys.Length != 6)
            inventoryKeys = new KeyCode[6];
        if (inventoryKeysAlt == null || inventoryKeysAlt.Length != 6)
            inventoryKeysAlt = new KeyCode[6];

        for (int i = 0; i < inventoryKeys.Length; i++)
        {
            int index = i;
            bindings.Add(new BindingDefinition
            {
                id = $"inventory_{index + 1}",
                label = $"Inventory {index + 1}",
                getPrimary = () => inventoryKeys[index],
                setPrimary = v => inventoryKeys[index] = v,
                getSecondary = () => inventoryKeysAlt[index],
                setSecondary = v => inventoryKeysAlt[index] = v
            });
        }

        return bindings;
    }
}
