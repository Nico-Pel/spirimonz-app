using System;
using UnityEngine;

public class SpmzRadiationDetector : Spirimonz
{
    public RadiationDetector radiationDetector;
    public GameObject radiationFeedback;

    private bool _eventsInitialized;

    private void InitEvents()
    {
        if (_eventsInitialized) return;
        
        radiationDetector.OnDetectionStart.AddListener(OnRadiationChanged);
        radiationDetector.OnDetectionEnd.AddListener(OnRadiationChanged);
        RefreshSoundUsage();

        _eventsInitialized = true;
    }

    private void OnRadiationChanged()
    {
        // Force le feedback en fonction de l'état actuel du détecteur
        //if (!isOnTheMap && !powerActiveInHands) return;

        bool isDetecting = radiationDetector.IsDetectingRadiation();
        if (isDetecting)
            TurnOnRadiationFeedback();
        else
            TurnOffRadiationFeedback();
    }

    private void TurnOnRadiationFeedback()
    {
        if (radiationFeedback == null)
            return;

        if (radiationFeedback.activeSelf) return;

        radiationFeedback.SetActive(true);
        if (animator != null)
        {
            animator.SetBool("Radiations", true);
            animator.SetTrigger("RadiationsDetection");
        }
    }

    private void TurnOffRadiationFeedback()
    {
        if (radiationFeedback != null && radiationFeedback.activeSelf)
            radiationFeedback.SetActive(false);

        if (animator != null)
            animator.SetBool("Radiations", false);

        if (radiationDetector != null)
            radiationDetector.StopUsingSound();
    }

    protected override void OnDisable()
    {
        // On stop aussi le feedback si jamais il est encore actif
        TurnOffRadiationFeedback();
        base.OnDisable();
    }

    public override void SetCurrentRoom(Room room)
    {
        base.SetCurrentRoom(room);

        if (radiationDetector != null)
            radiationDetector.SetCurrentRoom(room);

        SyncRadiationState();
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();

        isOnTheMap = true;

        if (radiationDetector != null)
        {
            RefreshSoundUsage();
            radiationDetector.SetCurrentRoom(currentRoom);
            SyncRadiationState();
        }
    }

    public override bool GoBackToHands(Transform handPos)
    {
        bool success = base.GoBackToHands(handPos);
        if (success)
        {
            RefreshSoundUsage();
            SyncRadiationState();
        }

        return success;
    }

    protected override void OnHuntStart()
    {
        base.OnHuntStart();
        if (radiationDetector != null)
            radiationDetector.EndDetection();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!_eventsInitialized)
            InitEvents();

        if (currentRoom == null && Player.Instance != null)
        {
            GamePlayer gamePlayer = Player.Instance as GamePlayer;
            if(gamePlayer != null)
                currentRoom = gamePlayer.currentRoom;
        }

        if (radiationDetector != null)
            radiationDetector.SetCurrentRoom(currentRoom);

        RefreshSoundUsage();
        SyncRadiationState();
    }

    protected override void UpdateMovementBehaviour()
    {
        base.UpdateMovementBehaviour();
        
        if (radiationDetector == null) return;

        SyncRadiationState();
    }

    private void RefreshSoundUsage()
    {
        if (radiationDetector == null)
            return;

        bool shouldUseSound = powerActiveInHands || isOnTheMap;
        radiationDetector.SetUseSound(shouldUseSound);
    }

    private void SyncRadiationState()
    {
        if (radiationDetector == null)
            return;

        bool isDetecting = radiationDetector.IsDetectingRadiation();
        if (isDetecting)
        {
            TurnOnRadiationFeedback();
            radiationDetector.PlaySoundManuallyIfNeeded();
        }
        else
        {
            TurnOffRadiationFeedback();
        }
    }
}
