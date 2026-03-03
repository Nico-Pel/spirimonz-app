using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzAttackDetector : Spirimonz
{
    [Header("Stop attack settings")] 
    
    public float feedbackDuration = 2;

    public float range = 5f;
    public ParticleSystem detectionParticles;
    public ParticleSystem specialDetectionParticles;
    
    public SoundParameters detectionSoundParameters;
    public SoundParameters specialDetectionSoundParameters;

    public GameObject lightDetection;
    public GameObject specialLightDetection;
    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        _house.currentGhost.onGhostCallForAHunt.AddListener(TryToDetectAnAttack);
    }

    private void TryToDetectAnAttack()
    {
        if (gameObject.activeInHierarchy == false) return;
        
        float dist = Vector3.Distance(transform.position, _house.currentGhost.transform.position);
        if (dist < range)
        {
            DetectAttack();
        }
    }

    private void DetectAttack()
    {
        canInteract = false;
        canBeTakenBackIntoHands = false;
        speed = 0;
        agent.speed = 0;
        
        bool radioactivity = _house.currentGhost.ghostParameters.Radioactivity;
        
        ParticleSystem particlesToUse = radioactivity ? specialDetectionParticles : detectionParticles;
        
        particlesToUse.transform.parent = _house.transform;
        particlesToUse.Play();
        
        SoundParameters soundParametersToUse = radioactivity ? specialDetectionSoundParameters : detectionSoundParameters;
        soundParametersToUse.PlaySound(transform.position);
        
        GameObject lightToUse = radioactivity ? specialLightDetection : lightDetection;
        lightToUse.transform.parent = _house.transform;
        lightToUse.SetActive(true);
        _house.Invoke(feedbackDuration, () =>
        {
            lightToUse.SetActive(false);
        });
            
        _house.currentGhost.CancelHuntCompletely();
        
        animator.SetTrigger("Detection");
    }

    public void IgnoreSpirimonz()
    {
        gameObject.SetActive(false);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Only draw if range is positive
        if (range <= 0f) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}