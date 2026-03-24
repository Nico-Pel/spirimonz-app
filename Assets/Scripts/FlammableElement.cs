using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlammableElement : GameBehaviour
{
    public enum FlammableType
    {
        None,
        Candle,
        Chimney
    }

    public FlammableType type;
    public bool startOnFire;
    public bool turnOffOnThrow;
    public Room optionalLinkedRoom;
    
    public GameObject[] fireObjects;
    public ParticleSystem[] fireOffParticles;

    [Header("Ghost sound settings")]
    public AudioClip blowUpByGhostSoundClip;
    public float ghostVolume = 1f;
    public float ghostPitch = 1f;

    public bool canBeTurnedOn = true;

    [Header("Room Heating")]
    public bool heatRoom = false;
    [Min(0f)] public float heatPerSecond = 0.5f;
    [Min(0f)] public float maxRoomHeatDelta = 8f;

    private bool _isOnFire;
    private Room _cachedRoom;

    public UnityEvent<bool> onChangeFireState;

    private bool _usedForAQuest;

    private void Start()
    {
        EnableFire(startOnFire, false);
    }

    private void Update()
    {
        if (!heatRoom || !_isOnFire)
            return;

        if (heatPerSecond <= 0f || maxRoomHeatDelta <= 0f)
            return;

        Room room = GetLinkedRoom();
        if (room == null)
            return;

        float maxAllowed = room.GetStartTemperature() + maxRoomHeatDelta;
        maxAllowed = Mathf.Min(maxAllowed, room.maxTemperature);

        room.AddHeatingClamped(heatPerSecond * Time.deltaTime, maxAllowed);
    }

    public void EnableFire(bool enable, bool useParticlesOff = true, bool useGhostSoundClip = false, bool forced = false)
    {
        if (enable == true && (canBeTurnedOn == false && forced == false)) return;
        
        _isOnFire = enable;
        
        foreach (GameObject fire in fireObjects)
        {
            fire.SetActive(enable);
        }

        if (enable == false && useParticlesOff && fireOffParticles.Length > 0)
        {
            foreach (ParticleSystem particles in fireOffParticles)
            {
                if (particles != null)
                {
                    particles.Play();
                }
            }

            if (useGhostSoundClip && blowUpByGhostSoundClip != null)
            {
                SoundManager.Instance.PlaySound(blowUpByGhostSoundClip, transform.position, ghostVolume, ghostPitch, -1f, 15f);
            }
        }

        if (enable && type != FlammableType.Candle && _usedForAQuest == false)
        {
            _usedForAQuest = true;
            House house = House.Instance;
            foreach (Quest quest in house.map.quests)
            {
                if(quest.type == Quest.QuestType.LightACandle)
                    GameManager.Instance.UpdateQuestProgress(quest, house.map.houseID, 1);
            }
        }
        
        onChangeFireState?.Invoke(_isOnFire);
    }
    
    public bool IsOnFire()
    {
        return _isOnFire;
    }

    private Room GetLinkedRoom()
    {
        if (optionalLinkedRoom != null)
            return optionalLinkedRoom;

        if (_cachedRoom == null)
            _cachedRoom = GetComponentInParent<Room>();

        return _cachedRoom;
    }
}
