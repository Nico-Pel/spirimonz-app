using UnityEngine;
using UnityEngine.Events;

public class RadiationDetector : GameBehaviour
{
    private bool _radiation = false;

    private Room _currentRoom;
    
    public UnityEvent OnDetectionStart;
    public UnityEvent OnDetectionEnd;

    private float _currentDuration;
    
    public void TriggerDetection(float duration)
    {
        if (duration > _currentDuration)
        {
            _currentDuration += duration - _currentDuration;
            _radiation = true;
            OnDetectionStart?.Invoke();
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

    private void EndDetection()
    {
        _radiation = false;
        _currentDuration = 0;
        OnDetectionEnd?.Invoke();
    }

    public bool IsDetectingRadiation()
    {
        return _radiation;
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
