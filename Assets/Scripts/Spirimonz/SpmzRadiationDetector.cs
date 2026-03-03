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

        radiationDetector.useSound = powerActiveInHands;

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
        if (radiationFeedback.activeSelf) return;

        radiationFeedback.SetActive(true);
        animator.SetBool("Radiations", true);
        animator.SetTrigger("RadiationsDetection");
    }

    private void TurnOffRadiationFeedback()
    {
        if (!radiationFeedback.activeInHierarchy) return;

        radiationFeedback.SetActive(false);
        animator.SetBool("Radiations", false);

        if (radiationDetector != null)
            radiationDetector.StopUsingSound(); // stop le son ici
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

        // Mettre à jour immédiatement le feedback si la radiation est déjà active
        if (isOnTheMap && radiationDetector.IsDetectingRadiation())
        {
            TurnOnRadiationFeedback();
        }
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();

        isOnTheMap = true;

        if (powerActiveInHands == false && radiationDetector != null)
        {
            // Activer le son avant de connecter la room
            radiationDetector.useSound = true;
            radiationDetector.SetCurrentRoom(currentRoom);

            // Forcer feedback visuel
            if (radiationDetector.IsDetectingRadiation())
            {
                TurnOnRadiationFeedback();

                // Jouer le son manuellement si nécessaire
                radiationDetector.PlaySoundManuallyIfNeeded();
            }
        }
    }

    public override bool GoBackToHands(Transform handPos)
    {
        bool success = base.GoBackToHands(handPos);

        /*if (success && powerActiveInHands == false && radiationDetector != null)
            radiationDetector.StopUsingSound();*/

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
    }

    protected override void UpdateMovementBehaviour()
    {
        base.UpdateMovementBehaviour();
        
        if (radiationDetector == null) return;

        bool isDetecting = radiationDetector.IsDetectingRadiation();

        if (isDetecting && !radiationFeedback.activeSelf)
            TurnOnRadiationFeedback();
        else if (!isDetecting && radiationFeedback.activeSelf)
            TurnOffRadiationFeedback(); // ici on stop aussi le son
    }
}
