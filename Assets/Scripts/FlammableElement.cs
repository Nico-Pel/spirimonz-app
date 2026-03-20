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

    private bool _isOnFire;

    public UnityEvent<bool> onChangeFireState;

    private bool _usedForAQuest;

    private void Start()
    {
        EnableFire(startOnFire, false);
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
                SoundManager.Instance.PlaySound(blowUpByGhostSoundClip, transform.position, ghostVolume, ghostPitch, -1f, 10f);
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
}