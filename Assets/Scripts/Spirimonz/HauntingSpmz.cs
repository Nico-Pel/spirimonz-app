using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HauntingSpmz : Spirimonz
{
    [Header("Haunting Settings")]
    [ReadOnly] public Room hauntedRoom;
    public bool useApproachSpeed = false;
    public float approachSpeed = 4f;
    public float waypointDistMin = 0.1f;
    public float waypointDistMax = 1f;
    public float waypointChangeDelayMin = 0.25f;
    public float waypointChangeDelayMax = 5f;
    public float forceChangeAfterTime = 15f;
    public float stuckTimeBeforeChange = 3f;
    public float stuckMoveDistance = 0.05f;

    private float _baseSpeed;
    private bool _isApproachingHauntedRoom;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();

        if (IsLocked()) return;

        hauntedRoom = ChooseHauntedRoom();
        SetRoamRoom(hauntedRoom);

        _baseSpeed = speed;
        if (useApproachSpeed && approachSpeed > 0f && hauntedRoom != null)
        {
            _isApproachingHauntedRoom = true;
            speed = approachSpeed;
        }
    }

    private void StopApproachSpeed()
    {
        speed = _baseSpeed;
        _isApproachingHauntedRoom = false;
    }

    protected override void OnRoamWaypointReached()
    {
        base.OnRoamWaypointReached();

        if (_isApproachingHauntedRoom)
        {
            StopApproachSpeed();
        }
    }

    protected override float GetRoamReachDistance()
    {
        float min = Mathf.Max(0.01f, waypointDistMin);
        float max = Mathf.Max(min, waypointDistMax);
        return Random.Range(min, max);
    }

    protected override float GetRoamWaypointChangeDelayMin()
    {
        return waypointChangeDelayMin;
    }

    protected override float GetRoamWaypointChangeDelayMax()
    {
        return waypointChangeDelayMax;
    }

    protected override float GetRoamForceChangeAfterTime()
    {
        return forceChangeAfterTime;
    }

    protected override float GetRoamStuckTime()
    {
        return stuckTimeBeforeChange;
    }

    protected override float GetRoamStuckMoveDistance()
    {
        return stuckMoveDistance;
    }

    private Room ChooseHauntedRoom()
    {
        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.rooms == null || _house.rooms.Length == 0)
            return null;

        Room favoriteRoom = _house.currentGhost != null ? _house.currentGhost.favoriteRoom : null;

        List<Room> allRooms = new List<Room>();
        foreach (Room room in _house.rooms)
        {
            if (room != null)
                allRooms.Add(room);
        }

        if (allRooms.Count == 0)
            return null;

        if (favoriteRoom == null || allRooms.Count == 1)
            return allRooms[Random.Range(0, allRooms.Count)];

        HashSet<Room> forbiddenRooms = new HashSet<Room> { favoriteRoom };
        if (favoriteRoom.neighborRooms != null)
        {
            foreach (Room neighbor in favoriteRoom.neighborRooms)
            {
                if (neighbor != null)
                    forbiddenRooms.Add(neighbor);
            }
        }

        List<Room> candidates = new List<Room>();
        foreach (Room room in allRooms)
        {
            if (!forbiddenRooms.Contains(room))
                candidates.Add(room);
        }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        List<Room> neighborCandidates = new List<Room>();
        if (favoriteRoom.neighborRooms != null)
        {
            foreach (Room neighbor in favoriteRoom.neighborRooms)
            {
                if (neighbor != null)
                    neighborCandidates.Add(neighbor);
            }
        }

        if (neighborCandidates.Count > 0)
            return neighborCandidates[Random.Range(0, neighborCandidates.Count)];

        return allRooms[Random.Range(0, allRooms.Count)];
    }
}
