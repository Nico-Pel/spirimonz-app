using UnityEngine;

public class SpmzGhostRoomSensor : Spirimonz
{
    [Header("Ghost Room Sensor")]
    public GameObject sameRoomFeedback;
    [Min(0f)] public float requiredSameRoomDuration = 5f;

    [Header("Sounds")]
    public SoundParameters onActivatedSound;
    public SoundParameters onDeactivatedSound;
    public SoundParameters activeLoopSound;

    private float _sameRoomTimer;
    private SoundManager.SoundInstance _activeLoopSoundInstance;

    protected override void OnEnable()
    {
        base.OnEnable();
        SyncFeedback();
    }

    protected override void OnDisable()
    {
        SetFeedbackState(false);
        base.OnDisable();
    }

    public override void SetCurrentRoom(Room room)
    {
        base.SetCurrentRoom(room);
        SyncFeedback();
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        SyncFeedback();
        return true;
    }

    private void SyncFeedback()
    {
        if (sameRoomFeedback == null)
            return;

        Ghost ghost = _house != null ? _house.currentGhost : House.Instance != null ? House.Instance.currentGhost : null;
        bool isSameRoom = ghost != null && currentRoom != null && ghost.currentRoom == currentRoom;

        if (isSameRoom)
        {
            _sameRoomTimer += Time.deltaTime;
        }
        else
        {
            _sameRoomTimer = 0f;
        }

        bool shouldEnableFeedback = isSameRoom && _sameRoomTimer >= requiredSameRoomDuration;
        SetFeedbackState(shouldEnableFeedback);
    }

    private void SetFeedbackState(bool enabled)
    {
        if (sameRoomFeedback == null || sameRoomFeedback.activeSelf == enabled)
            return;

        sameRoomFeedback.SetActive(enabled);

        if (enabled)
        {
            onActivatedSound?.PlaySound(transform.position);
            PlayLoopSound();
        }
        else
        {
            onDeactivatedSound?.PlaySound(transform.position);
            StopLoopSound();
        }
    }

    private void PlayLoopSound()
    {
        if (activeLoopSound == null)
            return;

        if (_activeLoopSoundInstance != null && _activeLoopSoundInstance.IsPlaying)
            return;

        _activeLoopSoundInstance = activeLoopSound.PlayManagedSound(transform.position);
    }

    private void StopLoopSound()
    {
        if (_activeLoopSoundInstance == null)
            return;

        _activeLoopSoundInstance.Stop(false);
        _activeLoopSoundInstance = null;
    }
}
