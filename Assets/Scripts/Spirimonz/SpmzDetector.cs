using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class SpmzDetector : Spirimonz
{
    [FormerlySerializedAs("range")] [Header("Detector Settings")]
    public float detectionRange = 5f; // Distance de détection
    public float maxPathRange = 7.5f;
    public float detectionWalkSpeed = 4;
    public float activityDistanceToReach = 2f;
    
    public Transform detectorSourceTransform;
    public List<ActivitySource> activitySources = new List<ActivitySource>();

    [Header("Detector Feedbacks")] 
    public MeshRenderer[] mRenderers;

    public float blinkInterval = 0.5f;

    public Material detectionMatNull;
    public Material detectionMatBase;
    public Material detectionMatFive;
    
    private ActivitySource _currentActivitySourceDetected;
    private bool _newActivityReached;
    
    private bool _emissionEnabled = false;
    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        activitySources.AddRange(FindObjectsOfType<ActivitySource>());
    }
    public override void UpdateSpirimonzBehaviour()
    {
        base.UpdateSpirimonzBehaviour();
        
        if (_currentActivitySourceDetected != null && _currentActivitySourceDetected.activityValue == 0)
        {
            _currentActivitySourceDetected = null;
            UpdateDetectionFeedback();
        }
        
        foreach (ActivitySource activitySource in activitySources)
        {
            //If there is no spirit activity or the activity is already detected, ignore it
            if (activitySource.activityValue == 0 || activitySource == _currentActivitySourceDetected) continue;
            
            float dist = Vector3.Distance(detectorSourceTransform.position, activitySource.transform.position);
            if (dist <= detectionRange)
            {
                //If its in range but the path is too long, abort mission bro
                if (isOnTheMap && IsNearFromMyAgent(agent, activitySource.transform, maxPathRange) == false) return;
                
                if (_currentActivitySourceDetected == null || activitySource.activityValue >= _currentActivitySourceDetected.activityValue)
                {
                    NewActivityDetected(activitySource);
                }
            }
        }

        if (_currentActivitySourceDetected != null && CurrentBehaviour() != SpirimonzBehaviourState.Special)
        {
            ChangeBehaviour(SpirimonzBehaviourState.Special);
        }
        else if (_currentActivitySourceDetected == null && CurrentBehaviour() == SpirimonzBehaviourState.Special)
        {
            SwitchBehaviour();
        }
    }

    private void NewActivityDetected(ActivitySource activitySource)
    {
        _currentActivitySourceDetected = activitySource;
        UpdateDetectionFeedback();
        _newActivityReached = false;
    }

    private void UpdateDetectionFeedback()
    {
        int sourceValue = _currentActivitySourceDetected == null ? 0 : _currentActivitySourceDetected.activityValue;
        
        Material matToGive = detectionMatNull;
        if (sourceValue > 0)
        {
            matToGive = sourceValue == 5 ? detectionMatFive : detectionMatBase;
            
            if(sourceValue == 5)
                BlinkingDetection();
        }

        for (int i = 0; i < mRenderers.Length; i++)
        {
            if (i < sourceValue)
            {
                mRenderers[i].material = matToGive;
            }
            else
            {
                mRenderers[i].material = detectionMatNull;
            }
        }
    }

    public override void UpdateSpecialMovement()
    {
        if (_currentActivitySourceDetected == null || _newActivityReached) return;

        Vector3 targetPos = _currentActivitySourceDetected.transform.position;

        // Cherche le point atteignable le plus proche sur le NavMesh
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            float dist = Vector3.Distance(transform.position, hit.position);

            if (dist > activityDistanceToReach)
            {
                agent.speed = detectionWalkSpeed;
                agent.SetDestination(hit.position);
            }
            else
            {
                if (_newActivityReached == false)
                {
                    transform.DOLookAt(hit.position, 0.5f);
                    ActivityReached();
                }
            }
        }
    }

    private void ActivityReached()
    {
        _newActivityReached = true;
        if (animator != null)
        {
            animator.SetTrigger("ActivityReached");
            agent.speed = 0;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    public override void OnSpirimonzDisabled()
    {
        base.OnSpirimonzDisabled();
        
        if (powerActiveInHands == false)
        {
            _currentActivitySourceDetected = null;
        }
        
        _newActivityReached = false;
    }
    
    public override void InteractionStarted()
    {
        //You can't disturb the Spirimonz Detector during its hunt
        if (CurrentBehaviour() == SpirimonzBehaviourState.Special) return;
        
        base.InteractionStarted();
    }

    private void BlinkingDetection()
    {
        bool enableEmission = _currentActivitySourceDetected.activityValue == 5 && _emissionEnabled == false;
        foreach (MeshRenderer mr in mRenderers)
        {
            if (enableEmission)
            {
                mr.material.EnableKeyword("_EMISSION");
            }
            else
            {
                mr.material.DisableKeyword("_EMISSION");
            }
        }

        if (_currentActivitySourceDetected.activityValue == 5)
        {
            this.Invoke(blinkInterval, BlinkingDetection);
        }
    }
}
