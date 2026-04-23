using UnityEngine;

public class SpmzColdRoamer : Spirimonz
{
    [Header("Cold Room Search")]
    [Min(0.1f)] public float minColdRoomCheckDelay = 5f;
    [Min(0.1f)] public float maxColdRoomCheckDelay = 10f;

    [Header("Roam Recovery")]
    [Min(0f)] public float forceChangeAfterTime = 15f;
    [Min(0f)] public float stuckTimeBeforeChange = 3f;
    [Min(0.001f)] public float stuckMoveDistance = 0.05f;

    [Header("Freezing Detection")]
    public bool useSpirimonzTemperatureThreshold = true;
    public float freezingTemperatureThreshold = 1f;
    public string detectionBoolName = "Detection";
    [Min(0f)] public float detectionSpeedMultiplier = 1.5f;

    private SpmzTemperatureColor _temperatureColor;
    private Room _targetColdRoom;
    private float _nextColdRoomCheckTime;
    private float _baseSpeed;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        _temperatureColor = GetComponent<SpmzTemperatureColor>();
        _baseSpeed = speed;
        UpdateDetectionAnimator(false);
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        MoveToColdestRoom(force: true);
        ScheduleNextColdRoomCheck();
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        if (!isOnTheMap)
        {
            UpdateDetectionAnimator(false);
            return true;
        }

        if (Time.time >= _nextColdRoomCheckTime)
        {
            MoveToColdestRoom(force: false);
            ScheduleNextColdRoomCheck();
        }

        bool isDetecting = IsRoomFreezing(currentRoom);
        UpdateDetectionAnimator(isDetecting);
        UpdateMovementSpeed(isDetecting);
        return true;
    }

    public override void InteractionStarted()
    {
        onInteract?.Invoke();
        HandleLookAtPlayerOnInteract();
    }

    private void MoveToColdestRoom(bool force)
    {
        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.rooms == null || _house.rooms.Length == 0)
            return;

        Room coldestRoom = FindColdestRoom();
        if (coldestRoom == null)
            return;

        bool hasValidWaypoint = HasWayPointInRoom(coldestRoom);
        if (!hasValidWaypoint)
        {
            Debug.LogError($"{name}: no waypoint found in coldest room '{coldestRoom.name}'. This room should have at least one WayPoint.", coldestRoom);
            return;
        }

        bool shouldKeepCurrentTarget = !force &&
                                       coldestRoom == _targetColdRoom &&
                                       currentRoom == coldestRoom;
        if (shouldKeepCurrentTarget)
            return;

        _targetColdRoom = coldestRoom;
        SetRoamRoom(coldestRoom);
        ChangeBehaviour(SpirimonzBehaviourState.Roam);
    }

    private Room FindColdestRoom()
    {
        Room coldestRoom = null;
        float coldestTemperature = float.MaxValue;

        for (int i = 0; i < _house.rooms.Length; i++)
        {
            Room room = _house.rooms[i];
            if (room == null)
                continue;

            if (!HasWayPointInRoom(room))
                continue;

            float temperature = room.GetTemperatureCelsius();
            if (coldestRoom == null || temperature < coldestTemperature)
            {
                coldestRoom = room;
                coldestTemperature = temperature;
            }
        }

        return coldestRoom;
    }

    private bool HasWayPointInRoom(Room room)
    {
        if (_house == null || room == null || _house.wayPoints == null)
            return false;

        for (int i = 0; i < _house.wayPoints.Count; i++)
        {
            WayPoint wayPoint = _house.wayPoints[i];
            if (wayPoint != null && wayPoint.linkedRoom == room)
                return true;
        }

        return false;
    }

    private void ScheduleNextColdRoomCheck()
    {
        float minDelay = Mathf.Max(0.1f, minColdRoomCheckDelay);
        float maxDelay = Mathf.Max(minDelay, maxColdRoomCheckDelay);
        _nextColdRoomCheckTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    private bool IsRoomFreezing(Room room)
    {
        if (room == null)
            return false;

        float threshold = freezingTemperatureThreshold;
        if (useSpirimonzTemperatureThreshold && _temperatureColor != null)
            threshold = _temperatureColor.FreezingThreshold;

        return room.GetTemperatureCelsius() < threshold;
    }

    private void UpdateDetectionAnimator(bool active)
    {
        if (animator == null || string.IsNullOrEmpty(detectionBoolName))
            return;

        animator.SetBool(detectionBoolName, active);
    }

    private void UpdateMovementSpeed(bool isDetecting)
    {
        speed = isDetecting
            ? _baseSpeed * Mathf.Max(1f, detectionSpeedMultiplier)
            : _baseSpeed;
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
}
