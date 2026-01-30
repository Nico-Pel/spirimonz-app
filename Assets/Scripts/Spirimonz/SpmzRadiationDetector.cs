using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpmzRadiationDetector : Spirimonz
{
    public RadiationDetector radiationDetector;
    public GameObject radiationFeedback;

    private void Awake()
    {
        radiationDetector.OnDetectionStart.AddListener(TurnOnRadiationFeedback);
        radiationDetector.OnDetectionEnd.AddListener(TurnOffRadiationFeedback);

        radiationDetector.useSound = powerActiveInHands;
    }

    private void TurnOnRadiationFeedback()
    {
        Debug.Log("Radiations Ghost 1 IsOnMap: " + isOnTheMap);
        if (powerActiveInHands == false && isOnTheMap == false) return;
        Debug.Log("Radiations Ghost 2");

        radiationFeedback.SetActive(true);
        
        animator.SetBool("Radiations", true);
        animator.SetTrigger("RadiationsDetection");
    }

    private void TurnOffRadiationFeedback()
    {
        radiationFeedback.SetActive(false);
        animator.SetBool("Radiations", false);
    }

    protected override void SetCurrentRoom(Room room)
    {
        base.SetCurrentRoom(room);
        radiationDetector.SetCurrentRoom(room);
    }
    
    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        
        // If the detector wasn't active in hands, trigger the radiation feedbacks if it's necessary
        if (powerActiveInHands == false)
        {
            radiationDetector.useSound = true;
            radiationDetector.SetCurrentRoom(currentRoom);
        }
    }

    public override bool GoBackToHands(Transform handPos)
    {
        if (!base.GoBackToHands(handPos))
        {
            return false;
        }
        
        if (powerActiveInHands == false)
        {
            radiationDetector.StopUsingSound();
        }

        return true;
    }

    protected override void OnHuntStart()
    {
        base.OnHuntStart();
        radiationDetector.EndDetection();
    }
}