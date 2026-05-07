using UnityEngine;

public static class MobileInput
{
    public static bool Enabled { get; private set; }

    private static Vector2 _move;
    private static Vector2 _lookDelta;
    private static int _lookFrame = -1;
    private static Vector2 _lookAxis;
    private static Vector2 _lookPanDelta;
    private static int _lookPanFrame = -1;

    private static int _primaryDownFrame = -1;
    private static int _primaryUpFrame = -1;
    private static bool _primaryHeld;
    private static bool _primaryDownPending;

    private static int _secondaryDownFrame = -1;
    private static int _secondaryUpFrame = -1;
    private static bool _secondaryHeld;

    private static bool _sprintHeld;

    private static int _grabDownFrame = -1;
    private static int _grabUpFrame = -1;
    private static bool _grabHeld;
    private static bool _grabDownPending;
    private static int _dropDownFrame = -1;
    private static int _throwDownFrame = -1;
    private static int _crouchDownFrame = -1;
    private static int _jumpDownFrame = -1;
    private static int _toggleLightDownFrame = -1;
    private static int _openJournalDownFrame = -1;
    private static int _openTeamDownFrame = -1;
    private static int _exitMenusDownFrame = -1;
    private static int _nextDownFrame = -1;
    private static int _previousDownFrame = -1;
    private static int[] _inventoryDownFrames = new int[6];
    private static int _yDownFrame = -1;
    private static bool _yDownPending;
    private static Vector2 _primaryScreenPos;
    private static bool _primaryScreenPosValid;

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
            ResetState();
    }

    private static void ResetState()
    {
        _move = Vector2.zero;
        _lookDelta = Vector2.zero;
        _lookFrame = -1;
        _lookAxis = Vector2.zero;
        _lookPanDelta = Vector2.zero;
        _lookPanFrame = -1;

        _primaryDownFrame = -1;
        _primaryUpFrame = -1;
        _primaryHeld = false;
        _primaryDownPending = false;

        _secondaryDownFrame = -1;
        _secondaryUpFrame = -1;
        _secondaryHeld = false;

        _sprintHeld = false;

        _grabDownFrame = -1;
        _grabUpFrame = -1;
        _grabHeld = false;
        _grabDownPending = false;
        _dropDownFrame = -1;
        _throwDownFrame = -1;
        _crouchDownFrame = -1;
        _jumpDownFrame = -1;
        _toggleLightDownFrame = -1;
        _openJournalDownFrame = -1;
        _openTeamDownFrame = -1;
        _exitMenusDownFrame = -1;
        _nextDownFrame = -1;
        _previousDownFrame = -1;
        _yDownFrame = -1;
        _yDownPending = false;
        _primaryScreenPosValid = false;
        for (int i = 0; i < _inventoryDownFrames.Length; i++)
            _inventoryDownFrames[i] = -1;
    }

    public static Vector2 Move => Enabled ? _move : Vector2.zero;

    public static void SetMove(Vector2 value)
    {
        if (!Enabled) return;
        _move = Vector2.ClampMagnitude(value, 1f);
    }

    public static void AddLookDelta(Vector2 delta)
    {
        if (!Enabled) return;

        if (_lookFrame != Time.frameCount)
        {
            _lookDelta = Vector2.zero;
            _lookFrame = Time.frameCount;
        }

        _lookDelta += delta;
    }

    public static void SetLookAxis(Vector2 value)
    {
        if (!Enabled) return;
        _lookAxis = Vector2.ClampMagnitude(value, 1f);
    }

    public static void AddLookPanDelta(Vector2 delta)
    {
        if (!Enabled) return;

        if (_lookPanFrame != Time.frameCount)
        {
            _lookPanDelta = Vector2.zero;
            _lookPanFrame = Time.frameCount;
        }

        _lookPanDelta += delta;
    }

    public static Vector2 GetLookDelta()
    {
        if (!Enabled)
            return Vector2.zero;

        Vector2 delta = _lookFrame == Time.frameCount ? _lookDelta : Vector2.zero;
        return delta + _lookAxis;
    }

    public static Vector2 GetLookPanDelta()
    {
        if (!Enabled)
            return Vector2.zero;

        return _lookPanFrame == Time.frameCount ? _lookPanDelta : Vector2.zero;
    }

    // Primary action (equivalent to mouse left)
    public static void SetPrimaryHeld(bool held)
    {
        if (!Enabled) return;
        if (held && !_primaryHeld)
        {
            _primaryDownFrame = Time.frameCount;
            _primaryDownPending = true;
        }
        if (!held && _primaryHeld) _primaryUpFrame = Time.frameCount;
        _primaryHeld = held;
    }

    public static bool PrimaryHeld => Enabled && _primaryHeld;
    public static bool PrimaryDown => Enabled && _primaryDownFrame == Time.frameCount;
    public static bool PrimaryUp => Enabled && _primaryUpFrame == Time.frameCount;

    public static void PressPrimary()
    {
        if (!Enabled) return;
        _primaryDownFrame = Time.frameCount;
        _primaryUpFrame = Time.frameCount;
        _primaryDownPending = true;
    }

    public static bool ConsumePrimaryDown()
    {
        if (!Enabled) return false;
        if (!_primaryDownPending) return false;
        _primaryDownPending = false;
        return true;
    }

    // Secondary action (equivalent to mouse right)
    public static void SetSecondaryHeld(bool held)
    {
        if (!Enabled) return;
        if (held && !_secondaryHeld) _secondaryDownFrame = Time.frameCount;
        if (!held && _secondaryHeld) _secondaryUpFrame = Time.frameCount;
        _secondaryHeld = held;
    }

    public static bool SecondaryHeld => Enabled && _secondaryHeld;
    public static bool SecondaryDown => Enabled && _secondaryDownFrame == Time.frameCount;
    public static bool SecondaryUp => Enabled && _secondaryUpFrame == Time.frameCount;

    // Sprint
    public static void SetSprintHeld(bool held)
    {
        if (!Enabled) return;
        _sprintHeld = held;
    }

    public static bool SprintHeld => Enabled && _sprintHeld;

    // One-shot actions
    public static void PressGrab()
    {
        if (!Enabled) return;
        _grabDownFrame = Time.frameCount;
        _grabDownPending = true;
    }

    public static void SetGrabHeld(bool held)
    {
        if (!Enabled) return;
        if (held && !_grabHeld)
        {
            _grabDownFrame = Time.frameCount;
            _grabDownPending = true;
        }
        if (!held && _grabHeld)
            _grabUpFrame = Time.frameCount;

        _grabHeld = held;
    }

    public static bool GrabDown => Enabled && _grabDownFrame == Time.frameCount;
    public static bool GrabHeld => Enabled && _grabHeld;
    public static bool GrabUp => Enabled && _grabUpFrame == Time.frameCount;

    public static bool ConsumeGrabDown()
    {
        if (!Enabled) return false;
        if (!_grabDownPending) return false;
        _grabDownPending = false;
        return true;
    }

    public static void PressDrop() { if (Enabled) _dropDownFrame = Time.frameCount; }
    public static bool DropDown => Enabled && _dropDownFrame == Time.frameCount;

    public static void PressThrow() { if (Enabled) _throwDownFrame = Time.frameCount; }
    public static bool ThrowDown => Enabled && _throwDownFrame == Time.frameCount;

    public static void PressCrouch() { if (Enabled) _crouchDownFrame = Time.frameCount; }
    public static bool CrouchDown => Enabled && _crouchDownFrame == Time.frameCount;

    public static void PressJump() { if (Enabled) _jumpDownFrame = Time.frameCount; }
    public static bool JumpDown => Enabled && _jumpDownFrame == Time.frameCount;

    public static void PressToggleLight() { if (Enabled) _toggleLightDownFrame = Time.frameCount; }
    public static bool ToggleLightDown => Enabled && _toggleLightDownFrame == Time.frameCount;

    public static void PressOpenJournal() { if (Enabled) _openJournalDownFrame = Time.frameCount; }
    public static bool OpenJournalDown => Enabled && _openJournalDownFrame == Time.frameCount;

    public static void PressOpenTeamMenu() { if (Enabled) _openTeamDownFrame = Time.frameCount; }
    public static bool OpenTeamMenuDown => Enabled && _openTeamDownFrame == Time.frameCount;

    public static void PressExitMenus() { if (Enabled) _exitMenusDownFrame = Time.frameCount; }
    public static bool ExitMenusDown => Enabled && _exitMenusDownFrame == Time.frameCount;

    public static void PressNext() { if (Enabled) _nextDownFrame = Time.frameCount; }
    public static bool NextDown => Enabled && _nextDownFrame == Time.frameCount;

    public static void PressPrevious() { if (Enabled) _previousDownFrame = Time.frameCount; }
    public static bool PreviousDown => Enabled && _previousDownFrame == Time.frameCount;

    public static void PressY()
    {
        if (!Enabled) return;
        _yDownFrame = Time.frameCount;
        _yDownPending = true;
    }
    public static bool YDown => Enabled && _yDownFrame == Time.frameCount;

    // Use this when a one-shot must not be missed due to Update order.
    public static bool ConsumeYDown()
    {
        if (!Enabled) return false;
        if (!_yDownPending) return false;
        _yDownPending = false;
        return true;
    }

    public static void SetPrimaryScreenPos(Vector2 pos)
    {
        if (!Enabled) return;
        _primaryScreenPos = pos;
        _primaryScreenPosValid = true;
    }

    public static void ClearPrimaryScreenPos()
    {
        _primaryScreenPosValid = false;
    }

    public static bool HasPrimaryScreenPos => Enabled && _primaryScreenPosValid;
    public static Vector2 PrimaryScreenPos => _primaryScreenPos;

    public static void PressInventorySlot(int index)
    {
        if (!Enabled) return;
        if (index < 0 || index >= _inventoryDownFrames.Length) return;
        _inventoryDownFrames[index] = Time.frameCount;
    }

    public static bool InventoryDown(int index)
    {
        if (!Enabled) return false;
        if (index < 0 || index >= _inventoryDownFrames.Length) return false;
        return _inventoryDownFrames[index] == Time.frameCount;
    }
}
