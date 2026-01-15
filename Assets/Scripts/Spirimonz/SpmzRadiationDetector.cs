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
    }

    private void TurnOnRadiationFeedback()
    {
        if (powerActiveInHands == false && isOnTheMap == false) return;
        
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
}
