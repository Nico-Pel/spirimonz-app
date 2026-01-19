using System;
using UnityEngine;
using UnityEngine.Events;

public class RadiationDetector : GameBehaviour
{
    private bool _radiation = false;

    private Room _currentRoom;
    
    public UnityEvent OnDetectionStart;
    public UnityEvent OnDetectionEnd;

    [Header("Sound settings")] 
    public bool useSound = true;
    public AudioClip radiationSoundClip;
    public float volume = 1f;
    public float range = 15f;
    
    private SoundManager.SoundInstance _radiationSound;
    private float _currentDuration;
    
    public void TriggerDetection(float duration)
    {
        if (duration > _currentDuration)
        {
            _currentDuration += duration - _currentDuration;
            _radiation = true;
            OnDetectionStart?.Invoke();

            if (useSound == true && radiationSoundClip != null)
            {
                _radiationSound = SoundManager.Instance.PlaySound(
                    radiationSoundClip,
                    transform.position,
                    volume: volume,
                    range: range,
                    loop: true,
                    sourceParent: transform
                );
            }
        }
    }

    public void StopUsingSound()
    {
        useSound = false;
        if (_radiationSound != null)
        {
            _radiationSound.Stop(false);
        }
    }

    private void Update()
    {
        if (_currentDuration > 0)
        {
            _currentDuration -= Time.deltaTime;
        }
        else if(_radiation == true)
        {
            EndDetection();
        }
    }

    public void EndDetection()
    {
        _radiation = false;
        _currentDuration = 0;
        OnDetectionEnd?.Invoke();

        if (radiationSoundClip != null && _radiationSound != null)
        {
            _radiationSound.Stop(false);
        }
    }

    public bool IsDetectingRadiation()
    {
        return _radiation;
    }

    private void OnDisable()
    {
        EndDetection();
    }

    public void SetCurrentRoom(Room room)
    {
        if (_currentRoom != null)
        {
            _currentRoom.OnRadiationStart.RemoveListener(TriggerDetection);
            _currentRoom.OnRadiationEnd.RemoveListener(EndDetection);
        }
        _currentRoom = room;
        if (_currentRoom.radiationInTheRoom)
        {
            TriggerDetection(_currentRoom.radiationDuration);
        }

        _currentRoom.OnRadiationStart.AddListener(TriggerDetection);
        _currentRoom.OnRadiationEnd.AddListener(EndDetection);
    }
}
