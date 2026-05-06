using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class InputManager : GameBehaviour
{
    public static InputManager Instance { get; private set; }

    private const string INPUT_TOKEN_COLOR = "#F9AB2D";

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
    public KeyCode dropSpirimonz = KeyCode.Mouse0;
    public KeyCode dropSpirimonzAlt = KeyCode.None;
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
    [Range(0.2f, 3f)] public float tpsLookSensitivityMultiplier = 1f;
    [Range(0.2f, 3f)] public float fpsLookSensitivityMultiplier = 1f;
    [Range(0.2f, 3f)] public float fpsLookVerticalSensitivityMultiplier = 1f;

    public const float DefaultLookSensitivityMultiplier = 1f;
    public const float DefaultMobileFpsLookHorizontalSensitivityMultiplier = 0.5f;
    public const float DefaultMobileFpsLookVerticalSensitivityMultiplier = 0.2f;

    public float GetDefaultFpsLookHorizontalSensitivityMultiplier()
    {
        if (MobileInput.Enabled || Application.isMobilePlatform)
            return DefaultMobileFpsLookHorizontalSensitivityMultiplier;

        return DefaultLookSensitivityMultiplier;
    }

    public float GetDefaultFpsLookVerticalSensitivityMultiplier()
    {
        return (MobileInput.Enabled || Application.isMobilePlatform)
            ? DefaultMobileFpsLookVerticalSensitivityMultiplier
            : DefaultLookSensitivityMultiplier;
    }

    public struct BindingDefinition
    {
        public string id;
        public string label;
        public string labelKey;
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

    public bool GetMoveForward() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKey(forwardKey, forwardKeyAlt);
    public bool GetMoveBackward() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKey(backwardKey, backwardKeyAlt);
    public bool GetMoveLeft() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKey(leftKey, leftKeyAlt);
    public bool GetMoveRight() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKey(rightKey, rightKeyAlt);
    public bool GetSprint() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKey(sprintKey, sprintKeyAlt);
    public bool GetTurnLightDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowSecondary) && GetKeyDown(turnLight, turnLightAlt);
    public bool GetGrabDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowGrab) && GetKeyDown(grabObject, grabObjectAlt);
    public bool GetDropDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowDrop) && GetKeyDown(dropObject, dropObjectAlt);
    public bool GetThrowDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowThrow) && GetKeyDown(throwObject, throwObjectAlt);
    public bool GetOpenJournalDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowJournal) && GetKeyDown(openJournal, openJournalAlt);
    public bool GetOpenTeamMenuDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowTeamMenu) && GetKeyDown(openTeamMenu, openTeamMenuAlt);
    public bool GetExitMenusDown() => GetKeyDown(exitMenus, exitMenusAlt);
    public bool GetCrouchDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKeyDown(crouchKey, crouchKeyAlt);
    public bool GetJumpDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement) && GetKeyDown(jumpKey, jumpKeyAlt);
    public bool GetWorldInteractionDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract) && GetKeyDown(worldInteractions, worldInteractionsAlt);
    public bool GetWorldInteractionDownRaw() => GetKeyDown(worldInteractions, worldInteractionsAlt);
    public bool GetDropSpirimonzDown() => TutorialInputGate.IsAllowed(TutorialInputGate.AllowDropSpmz) && GetKeyDown(dropSpirimonz, dropSpirimonzAlt);

    public bool GetNextDown() => GetKeyDown(primaryNext, secondaryNext);
    public bool GetPreviousDown() => GetKeyDown(primaryPrevious, secondaryPrevious);

    public bool GetInventoryDown(int index)
    {
        if (index < 0 || index >= inventoryKeys.Length)
            return false;

        KeyCode primary = inventoryKeys[index];
        KeyCode secondary = (inventoryKeysAlt != null && index < inventoryKeysAlt.Length) ? inventoryKeysAlt[index] : KeyCode.None;
        return TutorialInputGate.IsInventorySlotAllowed(index) && GetKeyDown(primary, secondary);
    }

    public string GetWorldInteractionDisplay()
    {
        return MobileInput.Enabled
            ? GetMobileLabel("input.mobile.a", "A")
            : GetKeyDisplay(worldInteractions, worldInteractionsAlt);
    }

    public string GetGrabDisplay()
    {
        return MobileInput.Enabled
            ? GetMobileLabel("input.mobile.a", "A")
            : GetKeyDisplay(grabObject, grabObjectAlt);
    }

    public string GetKeyDisplay(KeyCode primary, KeyCode secondary)
    {
        if (secondary == KeyCode.None)
            return GetKeyName(primary);
        return GetKeyName(primary) + " / " + GetKeyName(secondary);
    }

    public string GetKeyDisplayName(KeyCode key)
    {
        return GetKeyName(key);
    }

    private string GetKeyName(KeyCode key)
    {
        if (key == KeyCode.Mouse0)
            return LocalizationManager.Get("input.mouse.left", "Left Mouse Button");
        if (key == KeyCode.Mouse1)
            return LocalizationManager.Get("input.mouse.right", "Right Mouse Button");
        if (key == KeyCode.Mouse2)
            return LocalizationManager.Get("input.mouse.middle", "Middle Mouse Button");

        return key.ToString();
    }

    public string ReplaceInputTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return Regex.Replace(text, "\\[(.+?)\\]", match =>
        {
            string token = match.Groups[1].Value;
            if (TryGetTokenDisplay(token, out string display))
                return FormatInputToken(display);
            return match.Value;
        });
    }

    private string FormatInputToken(string display)
    {
        return $"<color={INPUT_TOKEN_COLOR}>[{display}]</color>";
    }

    private bool TryGetTokenDisplay(string token, out string display)
    {
        display = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        string key = token.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "_");

        switch (key)
        {
            case "dropspmz":
            case "drop_spmz":
            case "drop_spirimonz":
            case "dropspirimonz":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.tap", "Tap")
                    : GetKeyDisplay(dropSpirimonz, dropSpirimonzAlt);
                return true;
            case "secondary":
            case "rightclick":
            case "mouse1":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.b", "B")
                    : GetKeyDisplay(KeyCode.Mouse1, KeyCode.None);
                return true;
            case "drop":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.a", "A")
                    : GetKeyDisplay(dropObject, dropObjectAlt);
                return true;
            case "throw":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.b", "B")
                    : GetKeyDisplay(throwObject, throwObjectAlt);
                return true;
            case "grab":
                display = GetGrabDisplay();
                return true;
            case "pickupspmz":
            case "pickup_spmz":
            case "pickup_spirimonz":
            case "pickupspirimonz":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.a", "A")
                    : GetKeyDisplay(grabObject, grabObjectAlt);
                return true;
            case "interact":
            case "use":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.tap", "Tap")
                    : GetKeyDisplay(KeyCode.Mouse0, KeyCode.None);
                return true;
            case "interactspmz":
            case "interact_spmz":
            case "interact_spirimonz":
            case "interactspirimonz":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.tap", "Tap")
                    : GetKeyDisplay(KeyCode.Mouse0, KeyCode.None);
                return true;
            case "usewatch":
            case "use_watch":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.b", "B")
                    : GetKeyDisplay(KeyCode.Mouse0, KeyCode.None);
                return true;
            case "journal":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.journal", "Journal")
                    : GetKeyDisplay(openJournal, openJournalAlt);
                return true;
            case "team":
            case "teammenu":
                display = GetMobileInventoryDisplay();
                return true;
            case "sprint":
                display = GetKeyDisplay(sprintKey, sprintKeyAlt);
                return true;
            case "crouch":
                display = GetKeyDisplay(crouchKey, crouchKeyAlt);
                return true;
            case "jump":
                display = GetKeyDisplay(jumpKey, jumpKeyAlt);
                return true;
            case "light":
            case "togglelight":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.lamp", "Lamp")
                    : GetKeyDisplay(turnLight, turnLightAlt);
                return true;
            case "settings":
            case "menu":
            case "escape":
            case "esc":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.settings", "Settings")
                    : GetKeyDisplay(exitMenus, exitMenusAlt);
                return true;
            case "next":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.next", "Next")
                    : GetKeyDisplay(primaryNext, secondaryNext);
                return true;
            case "prev":
            case "previous":
                display = MobileInput.Enabled
                    ? GetMobileLabel("input.mobile.previous", "Previous")
                    : GetKeyDisplay(primaryPrevious, secondaryPrevious);
                return true;
            case "moveforward":
            case "forward":
                display = GetKeyDisplay(forwardKey, forwardKeyAlt);
                return true;
            case "moveback":
            case "back":
            case "backward":
                display = GetKeyDisplay(backwardKey, backwardKeyAlt);
                return true;
            case "moveleft":
            case "left":
                display = GetKeyDisplay(leftKey, leftKeyAlt);
                return true;
            case "moveright":
            case "right":
                display = GetKeyDisplay(rightKey, rightKeyAlt);
                return true;
        }

        if (key.StartsWith("inventory"))
        {
            if (int.TryParse(key.Replace("inventory", string.Empty), out int index))
            {
                return TryGetInventoryDisplay(index - 1, out display);
            }
        }

        if (key.StartsWith("slot"))
        {
            if (int.TryParse(key.Replace("slot", string.Empty), out int index))
            {
                return TryGetInventoryDisplay(index - 1, out display);
            }
        }

        return false;
    }

    private bool TryGetInventoryDisplay(int index, out string display)
    {
        display = string.Empty;
        if (inventoryKeys == null || index < 0 || index >= inventoryKeys.Length)
            return false;

        if (MobileInput.Enabled)
        {
            display = GetMobileSlotDisplay(index);
            return true;
        }

        KeyCode primary = inventoryKeys[index];
        KeyCode secondary = (inventoryKeysAlt != null && index < inventoryKeysAlt.Length)
            ? inventoryKeysAlt[index]
            : KeyCode.None;

        display = GetKeyDisplay(primary, secondary);
        return true;
    }

    private string GetMobileInventoryDisplay()
    {
        if (!MobileInput.Enabled)
            return GetKeyDisplay(openTeamMenu, openTeamMenuAlt);

        int selectedIndex = -1;
        if (InventoryManager.Instance != null)
            selectedIndex = InventoryManager.Instance.currentSelectedIndex;

        if (selectedIndex >= 0 && selectedIndex < 6 && TutorialInputGate.IsInventorySlotAllowed(selectedIndex))
            return GetMobileSlotDisplay(selectedIndex);

        return GetMobileLabel("input.mobile.inventory", "Inventory Footer");
    }

    private string GetMobileSlotDisplay(int index)
    {
        if (index == 0)
            return GetMobileLabel("input.mobile.lamp", "Lamp");

        return LocalizationManager.Format("input.mobile.slot", index + 1);
    }

    private string GetMobileLabel(string key, string fallback)
    {
        return LocalizationManager.Get(key, fallback);
    }

    public List<BindingDefinition> GetBindingDefinitions()
    {
        var bindings = new List<BindingDefinition>
        {
            new BindingDefinition { id = "move_forward", labelKey = "input.binding.move_forward", label = "Move Forward", getPrimary = () => forwardKey, setPrimary = v => forwardKey = v, getSecondary = () => forwardKeyAlt, setSecondary = v => forwardKeyAlt = v },
            new BindingDefinition { id = "move_backward", labelKey = "input.binding.move_backward", label = "Move Backward", getPrimary = () => backwardKey, setPrimary = v => backwardKey = v, getSecondary = () => backwardKeyAlt, setSecondary = v => backwardKeyAlt = v },
            new BindingDefinition { id = "move_left", labelKey = "input.binding.move_left", label = "Move Left", getPrimary = () => leftKey, setPrimary = v => leftKey = v, getSecondary = () => leftKeyAlt, setSecondary = v => leftKeyAlt = v },
            new BindingDefinition { id = "move_right", labelKey = "input.binding.move_right", label = "Move Right", getPrimary = () => rightKey, setPrimary = v => rightKey = v, getSecondary = () => rightKeyAlt, setSecondary = v => rightKeyAlt = v },
            new BindingDefinition { id = "sprint", labelKey = "input.binding.sprint", label = "Sprint", getPrimary = () => sprintKey, setPrimary = v => sprintKey = v, getSecondary = () => sprintKeyAlt, setSecondary = v => sprintKeyAlt = v },
            new BindingDefinition { id = "drop_spmz", labelKey = "input.binding.drop_spirimonz", label = "Drop Spirimonz", getPrimary = () => dropSpirimonz, setPrimary = v => dropSpirimonz = v, getSecondary = () => dropSpirimonzAlt, setSecondary = v => dropSpirimonzAlt = v },
            new BindingDefinition { id = "grab", labelKey = "input.binding.grab", label = "Grab", getPrimary = () => grabObject, setPrimary = v => grabObject = v, getSecondary = () => grabObjectAlt, setSecondary = v => grabObjectAlt = v },
            new BindingDefinition { id = "drop", labelKey = "input.binding.drop", label = "Drop", getPrimary = () => dropObject, setPrimary = v => dropObject = v, getSecondary = () => dropObjectAlt, setSecondary = v => dropObjectAlt = v },
            new BindingDefinition { id = "throw", labelKey = "input.binding.throw", label = "Throw", getPrimary = () => throwObject, setPrimary = v => throwObject = v, getSecondary = () => throwObjectAlt, setSecondary = v => throwObjectAlt = v },
            new BindingDefinition { id = "toggle_light", labelKey = "input.binding.toggle_light", label = "Toggle Light", getPrimary = () => turnLight, setPrimary = v => turnLight = v, getSecondary = () => turnLightAlt, setSecondary = v => turnLightAlt = v },
            new BindingDefinition { id = "open_journal", labelKey = "input.binding.open_journal", label = "Open Journal", getPrimary = () => openJournal, setPrimary = v => openJournal = v, getSecondary = () => openJournalAlt, setSecondary = v => openJournalAlt = v },
            new BindingDefinition { id = "open_team", labelKey = "input.binding.open_team", label = "Open Team", getPrimary = () => openTeamMenu, setPrimary = v => openTeamMenu = v, getSecondary = () => openTeamMenuAlt, setSecondary = v => openTeamMenuAlt = v },
            new BindingDefinition { id = "exit_menus", labelKey = "input.binding.exit_menus", label = "Exit Menus", getPrimary = () => exitMenus, setPrimary = v => exitMenus = v, getSecondary = () => exitMenusAlt, setSecondary = v => exitMenusAlt = v },
            new BindingDefinition { id = "crouch", labelKey = "input.binding.crouch", label = "Crouch", getPrimary = () => crouchKey, setPrimary = v => crouchKey = v, getSecondary = () => crouchKeyAlt, setSecondary = v => crouchKeyAlt = v },
            new BindingDefinition { id = "jump", labelKey = "input.binding.jump", label = "Jump", getPrimary = () => jumpKey, setPrimary = v => jumpKey = v, getSecondary = () => jumpKeyAlt, setSecondary = v => jumpKeyAlt = v },
            new BindingDefinition { id = "world_interaction", labelKey = "input.binding.world_interaction", label = "World Interaction", getPrimary = () => worldInteractions, setPrimary = v => worldInteractions = v, getSecondary = () => worldInteractionsAlt, setSecondary = v => worldInteractionsAlt = v },
            new BindingDefinition { id = "next", labelKey = "input.binding.next", label = "Next", getPrimary = () => primaryNext, setPrimary = v => primaryNext = v, getSecondary = () => secondaryNext, setSecondary = v => secondaryNext = v },
            new BindingDefinition { id = "previous", labelKey = "input.binding.previous", label = "Previous", getPrimary = () => primaryPrevious, setPrimary = v => primaryPrevious = v, getSecondary = () => secondaryPrevious, setSecondary = v => secondaryPrevious = v }
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
