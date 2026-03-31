using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialObjectiveType
{
    None,
    ActivateActivablesSimultaneous,
    GrabCatchable,
    GrabSpirimonz,
    DropSpirimonz,
    LightFlammables,
    RevealPrints,
    DetectActivity,
    CheckEvidence,
    OpenJournal,
    WaitSpirimonz,
    PlaceObjectInZone,
    DetectFreezing,
    GhostEatFruit,
    DetectRadiation,
    SurviveHunt,
    DetectSpiritOrbs,
    LeaveHouse
}

[Serializable]
public class TutorialObjective
{
    public TutorialObjectiveType type = TutorialObjectiveType.None;
    public int goal = 1;

    [TextArea(2, 4)] public string titleEnglish;
    [TextArea(2, 4)] public string titleFrench;

    [Header("Activables")]
    public ActivableObject.ActivationSpecialType activableTypeFilter = ActivableObject.ActivationSpecialType.none;
    public ActivableObject[] activables;

    [Header("Catchables")]
    public CatchableObject[] catchables;
    public bool requireCatchableFireObject;

    [Header("Flammables")]
    public FlammableElement[] flammables;
    public bool requireCandleFlammables = true;
    public bool requireCandlePlacedAndUpright = false;
    [Min(0f)] public float candleUprightMaxAngle = 15f;

    [Header("Prints")]
    public PrintSource[] printSources;

    [Header("Detectors")]
    public SpmzDetector[] detectors;

    [Header("Spirit Orbs")]
    public GhostOrbsParticles[] orbsParticles;
    [Min(0.1f)] public float orbsHoldDuration = 2f;
    [Min(0f)] public float orbsMaxDistance = 12f;
    [Range(0f, 90f)] public float orbsMaxAngle = 8f;

    [Header("Evidence")]
    public GhostInvestigator.EvidenceType evidenceType = GhostInvestigator.EvidenceType.FreezingTemperature;
    public GhostInvestigator.EvidenceState evidenceState = GhostInvestigator.EvidenceState.Present;

    [Header("Fruits")]
    public Fruit[] fruits;

    [Header("Radiations")]
    public RadiationDetector[] radiationDetectors;

    [Header("Spirimonz Filters")]
    [Tooltip("1-5 slots. 0 = ignore.")]
    public int requiredTeamSlotIndex = 0;
    public bool requireSpirimonzInHands = false;
    public bool requireSpirimonzOnMap = false;

    [Header("Wait Behaviour")]
    [Min(0.1f)] public float waitDuration = 2f;

    [Header("Freezing Temperature")]
    public bool useSpirimonzTemperatureThreshold = true;
    public float freezingTemperatureThreshold = 1f;
    [Range(0f, 1f)] public float freezingVisualPercent = 0.8f;

    [Header("Drop Zone")]
    public string dropZoneId;
    public Collider[] dropZones;
    public CatchableObject[] dropZoneObjects;
    public bool dropZoneRequireCatchableFireObject;
    public FlammableElement.FlammableType dropZoneFlammableType = FlammableElement.FlammableType.None;
    public bool requireStableRotation;
    [Min(0f)] public float maxUprightAngle = 15f;
    [Min(0f)] public float stableRotationDuration = 0.2f;
}

[Serializable]
public class TutorialInputMask
{
    public bool overrideInputs = true;
    public bool allowMovement = true;
    public bool allowLook = true;
    public bool allowInteract = true;
    public bool allowInteractSpmz = true;
    public bool allowUseWatch = true;
    public bool allowGrab = true;
    public bool allowPickupSpmz = true;
    public bool allowSecondary = true;
    public bool allowJournal = true;
    public bool allowTeamMenu = true;
    public bool allowDrop = true;
    public bool allowThrow = true;
    public bool allowDropSpmz = true;

    [HideInInspector] public bool useSeparateDropThrow = false;
    [HideInInspector] public bool allowThrowDrop = true;

    public bool overrideInventorySlots = true;
    public bool[] allowInventorySlots = new bool[6] { true, true, true, true, true, true };
}

[Serializable]
public class TutorialGhostOverride
{
    public bool enabled;
    public bool blockAllActivities;
    public bool forceRoom;
    public Room forcedRoom;
    [Tooltip("Fallback if forcedRoom is not assigned.")]
    public string forcedRoomName;
    [Tooltip("Fallback if forcedRoom is not assigned. Uses House.rooms index (0-based).")]
    public int forcedRoomIndex = -1;
    public bool allowRoomChange = false;
    public bool restrictActivities;
    public List<Ghost.GhostActivities> allowedActivities = new List<Ghost.GhostActivities>();
    public bool forceActivity;
    public Ghost.GhostActivities forcedActivity = Ghost.GhostActivities.Nothing;
    public bool allowHunt = true;

    public void Apply(Ghost ghost)
    {
        if (ghost == null)
            return;

        if (!enabled)
        {
            ghost.ClearTutorialOverride();
            return;
        }

        Room resolvedRoom = ResolveForcedRoom();

        ghost.ApplyTutorialOverride(
            enabled,
            blockAllActivities,
            forceRoom,
            resolvedRoom,
            restrictActivities,
            allowedActivities,
            forceActivity,
            forcedActivity,
            allowHunt,
            allowRoomChange);
    }

    private Room ResolveForcedRoom()
    {
        if (forcedRoom != null)
            return forcedRoom;

        House house = House.Instance;
        if (house == null || house.rooms == null)
            return null;

        if (!string.IsNullOrWhiteSpace(forcedRoomName))
        {
            string target = forcedRoomName.Trim();
            for (int i = 0; i < house.rooms.Length; i++)
            {
                Room room = house.rooms[i];
                if (room == null)
                    continue;

                if (string.Equals(room.name, target, StringComparison.OrdinalIgnoreCase))
                    return room;
            }
        }

        if (forcedRoomIndex >= 0 && forcedRoomIndex < house.rooms.Length)
            return house.rooms[forcedRoomIndex];

        return null;
    }
}
