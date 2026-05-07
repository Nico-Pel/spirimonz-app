using System;
using UnityEngine;
using UnityEngine.Events;

public class RadiationDetector : GameBehaviour
{
    public Spirimonz linkedSpirimonz;

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

    private void Awake()
    {
        if (linkedSpirimonz != null)
        {
            linkedSpirimonz.onSetRoom.AddListener(SetCurrentRoom);
            //linkedSpirimonz.onDisable.AddListener(StopUsingSound);
        }
    }

    public void TriggerDetection(float duration)
    {
        if (duration > _currentDuration)
        {
            _currentDuration += duration - _currentDuration;
            _radiation = true;
            OnDetectionStart?.Invoke();

            if (useSound == true && radiationSoundClip != null)
            {
                PlaySound();
            }
        }
    }

    private void PlaySound()
    {
        if (_radiationSound != null && _radiationSound.IsPlaying)
            return;

        _radiationSound = SoundManager.Instance.PlaySound(
            radiationSoundClip,
            transform.position,
            volume: volume,
            range: range,
            loop: true,
            sourceParent: transform
        );
    }

    public void StopUsingSound()
    {
        if (_radiationSound != null)
        {
            _radiationSound.Stop(false);
            _radiationSound = null;
        }
    }

    public void SetUseSound(bool enabled)
    {
        useSound = enabled;
        if (!useSound)
            StopUsingSound();
        else if (_radiation && radiationSoundClip != null)
            PlaySound();
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

    private void OnEnable()
    {
        if(linkedSpirimonz != null && linkedSpirimonz.currentRoom != null)
            SetCurrentRoom(linkedSpirimonz.currentRoom);
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

        if (_currentRoom == null)
        {
            EndDetection();
            return;
        }
        
        _currentRoom.OnRadiationStart.AddListener(TriggerDetection);
        _currentRoom.OnRadiationEnd.AddListener(EndDetection);
        
        if (_currentRoom.radiationDuration > 1f)
        {
            TriggerDetection(_currentRoom.radiationDuration);
        }
        else
        {
            EndDetection();
        }
    }
    
    public void PlaySoundManuallyIfNeeded()
    {
        // Si le son n'est pas déjà joué, le lancer
        if (useSound && radiationSoundClip != null && (_radiationSound == null || !_radiationSound.IsPlaying))
        {
            PlaySound();
        }
    }
}
