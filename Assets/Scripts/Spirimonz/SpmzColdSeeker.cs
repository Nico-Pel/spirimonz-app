using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public class SpmzColdSeeker : Spirimonz
{
    [Header("Cold Room Selection")]
    [Range(0f, 1f)] public float coldestRoomsPercent = 0.3f;
    public int minColdestRooms = 1;
    public int maxColdestRooms = 3;
    [Min(0f)] public float coldestRoomTieMargin = 0.05f;
    public bool requireNoColderNeighborToWait = false;

    [Header("Freezing Sleep")]
    public bool useSpirimonzTemperatureThreshold = true;
    public float freezingTemperatureThreshold = 1f;
    [Min(0.01f)] public float freezingCheckInterval = 0.25f;

    [Header("Sleep Rewards")]
    public CatchableObject sleepRewardPrefab;
    public CatchableObject bathroomSleepRewardPrefab;
    public Transform sleepRewardSpawnPoint;
    public Vector3 sleepRewardOffset = new Vector3(0f, 0.3f, 0f);
    public bool attachRewardToSpirimonz = true;
    [Min(0f)] public float rewardSpawnScaleDuration = 0.25f;
    [Range(0.001f, 1f)] public float rewardSpawnScaleFrom = 0.01f;

    [Header("Animator Triggers")]
    public string nopTrigger = "Nop";
    [FormerlySerializedAs("sleepTrigger")] public string sleepBool = "Sleep";
    public string wakeUpTrigger = "WakeUp";

    private SpmzTemperatureColor _temperatureColor;
    private bool _isSleeping;
    private bool _sleepRewardSpawned;
    private float _nextFreezingCheckTime;
    private readonly List<Room> _roomBuffer = new List<Room>();
    private bool _lookAtWhileWaitingBase;
    private bool _lookAtBaseInitialized;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        _temperatureColor = GetComponent<SpmzTemperatureColor>();
        EnsureLookBase();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureLookBase();
        SyncSleepAnimator();
        ApplySleepLookState();
    }

    public override void InteractionStarted()
    {
        onInteract?.Invoke();
        HandleLookAtPlayerOnInteract();

        if (_isSleeping)
        {
            TriggerAnimator(wakeUpTrigger);
            SetSleeping(false);
            SwitchBehaviour();
            return;
        }

        if (_currentBehaviour == SpirimonzBehaviourState.FollowPlayer)
        {
            if (IsRoomEligibleForWaiting(currentRoom))
            {
                SwitchBehaviour();
            }
            else
            {
                TriggerAnimator(nopTrigger);
            }
            return;
        }

        if (_currentBehaviour == SpirimonzBehaviourState.Wait)
        {
            SwitchBehaviour();
            return;
        }

        SwitchBehaviour();
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        UpdateFreezingSleep();
        return true;
    }

    private void UpdateFreezingSleep()
    {
        if (!isOnTheMap)
            return;

        if (_currentBehaviour != SpirimonzBehaviourState.Wait)
            return;

        if (_isSleeping)
            return;

        if (Time.time < _nextFreezingCheckTime)
            return;

        _nextFreezingCheckTime = Time.time + Mathf.Max(0.01f, freezingCheckInterval);

        Room room = currentRoom;
        if (room == null)
            return;

        if (!IsRoomFreezing(room))
            return;

        SetSleeping(true);
        TrySpawnSleepReward(room);
    }

    private bool IsRoomEligibleForWaiting(Room room)
    {
        if (room == null)
            return false;

        if (_house == null)
            _house = House.Instance;

        if (_house == null || _house.rooms == null || _house.rooms.Length == 0)
            return false;

        _roomBuffer.Clear();
        foreach (Room r in _house.rooms)
        {
            if (r != null)
                _roomBuffer.Add(r);
        }

        if (_roomBuffer.Count == 0)
            return false;

        _roomBuffer.Sort((a, b) => a.GetTemperatureCelsius().CompareTo(b.GetTemperatureCelsius()));

        int coldestCount = ComputeColdestRoomCount(_roomBuffer.Count);
        coldestCount = Mathf.Clamp(coldestCount, 1, _roomBuffer.Count);

        float threshold = _roomBuffer[coldestCount - 1].GetTemperatureCelsius();
        float temp = room.GetTemperatureCelsius();
        bool isAmongColdestRooms = temp <= threshold + coldestRoomTieMargin;
        if (!isAmongColdestRooms)
            return false;

        if (!requireNoColderNeighborToWait)
            return true;

        return !HasColderNeighborRoom(room, temp);
    }

    private int ComputeColdestRoomCount(int roomCount)
    {
        float percent = Mathf.Clamp01(coldestRoomsPercent);
        int byPercent = Mathf.CeilToInt(roomCount * percent);
        int count = Mathf.Clamp(byPercent, minColdestRooms, maxColdestRooms);
        return Mathf.Clamp(count, 1, roomCount);
    }

    private bool IsRoomFreezing(Room room)
    {
        float threshold = freezingTemperatureThreshold;
        if (useSpirimonzTemperatureThreshold && _temperatureColor != null)
            threshold = _temperatureColor.FreezingThreshold;

        return room.GetTemperatureCelsius() < threshold;
    }

    private bool HasColderNeighborRoom(Room room, float currentTemperature)
    {
        if (room == null || room.neighborRooms == null || room.neighborRooms.Length == 0)
            return false;

        float allowedTemperature = currentTemperature - coldestRoomTieMargin;
        for (int i = 0; i < room.neighborRooms.Length; i++)
        {
            Room neighbor = room.neighborRooms[i];
            if (neighbor == null)
                continue;

            if (neighbor.GetTemperatureCelsius() < allowedTemperature)
                return true;
        }

        return false;
    }

    private void TrySpawnSleepReward(Room room)
    {
        if (_sleepRewardSpawned)
            return;

        CatchableObject prefab = IsBathroomRoom(room) ? bathroomSleepRewardPrefab : sleepRewardPrefab;
        if (prefab == null)
            return;

        Transform anchor = sleepRewardSpawnPoint != null ? sleepRewardSpawnPoint : transform;
        Vector3 worldPos = anchor.TransformPoint(sleepRewardOffset);
        Quaternion worldRot = anchor.rotation;

        Transform parent = null;
        if (attachRewardToSpirimonz)
        {
            parent = anchor;
        }
        else if (House.Instance != null)
        {
            parent = House.Instance.transform;
        }

        CatchableObject instance = parent != null
            ? Instantiate(prefab, worldPos, worldRot, parent)
            : Instantiate(prefab, worldPos, worldRot);

        if (attachRewardToSpirimonz && instance != null)
        {
            instance.transform.localPosition = sleepRewardOffset;
            instance.transform.localRotation = Quaternion.identity;
        }

        if (instance != null && rewardSpawnScaleDuration > 0f)
        {
            Vector3 targetScale = instance.transform.localScale;
            float fromFactor = Mathf.Clamp(rewardSpawnScaleFrom, 0.001f, 1f);
            instance.transform
                .DOScale(targetScale, rewardSpawnScaleDuration)
                .From(targetScale * fromFactor);
        }

        if (instance != null && instance.rb != null)
        {
            instance.rb.isKinematic = true;
        }

        _sleepRewardSpawned = true;
    }

    private bool IsBathroomRoom(Room room)
    {
        if (room == null)
            return false;

        return room.roomType == Room.RoomType.bathroom ||
               room.roomType == Room.RoomType.toilet;
    }

    private void TriggerAnimator(string triggerName)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }

    private void SetSleeping(bool sleeping)
    {
        EnsureLookBase();
        _isSleeping = sleeping;
        SyncSleepAnimator();
        ApplySleepLookState();
    }

    private void SyncSleepAnimator()
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(sleepBool))
            return;

        animator.SetBool(sleepBool, _isSleeping);
    }

    private void EnsureLookBase()
    {
        if (_lookAtBaseInitialized)
            return;

        _lookAtWhileWaitingBase = lookAtPlayerWhileWaiting;
        _lookAtBaseInitialized = true;
    }

    private void ApplySleepLookState()
    {
        if (!_lookAtBaseInitialized)
            return;

        lookAtPlayerWhileWaiting = _isSleeping ? false : _lookAtWhileWaitingBase;
    }
}
