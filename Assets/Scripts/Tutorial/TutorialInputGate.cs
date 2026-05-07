using UnityEngine;

public static class TutorialInputGate
{
    public static bool Enabled;

    public static bool AllowMovement = true;
    public static bool AllowLook = true;
    public static bool AllowInteract = true;
    public static bool AllowInteractSpmz = true;
    public static bool AllowUseWatch = true;
    public static bool AllowGrab = true;
    public static bool AllowPickupSpmz = true;
    public static bool AllowLight = true;
    public static bool AllowSecondary = true;
    public static bool AllowJournal = true;
    public static bool AllowTeamMenu = true;
    public static bool AllowDrop = true;
    public static bool AllowThrow = true;
    public static bool AllowDropSpmz = true;

    public static bool[] AllowInventorySlots = new bool[6] { true, true, true, true, true, true };

    public static void ResetAll(bool allow)
    {
        AllowMovement = allow;
        AllowLook = allow;
        AllowInteract = allow;
        AllowInteractSpmz = allow;
        AllowUseWatch = allow;
        AllowGrab = allow;
        AllowPickupSpmz = allow;
        AllowLight = allow;
        AllowSecondary = allow;
        AllowJournal = allow;
        AllowTeamMenu = allow;
        AllowDrop = allow;
        AllowThrow = allow;
        AllowDropSpmz = allow;

        if (AllowInventorySlots == null || AllowInventorySlots.Length != 6)
            AllowInventorySlots = new bool[6];

        for (int i = 0; i < AllowInventorySlots.Length; i++)
            AllowInventorySlots[i] = allow;
    }

    public static bool IsInventorySlotAllowed(int index)
    {
        if (!Enabled)
            return true;

        if (AllowInventorySlots == null || index < 0 || index >= AllowInventorySlots.Length)
            return true;

        return AllowInventorySlots[index];
    }

    public static bool HasAnyInventorySlotAllowed()
    {
        if (!Enabled)
            return true;

        if (AllowInventorySlots == null || AllowInventorySlots.Length == 0)
            return true;

        for (int i = 0; i < AllowInventorySlots.Length; i++)
        {
            if (AllowInventorySlots[i])
                return true;
        }

        return false;
    }

    public static bool IsAllowed(bool value)
    {
        return !Enabled || value;
    }
}
