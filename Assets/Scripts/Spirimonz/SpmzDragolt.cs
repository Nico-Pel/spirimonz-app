using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpmzDragolt : SpmzPropEater
{
    [Header("Detector Feedbacks")] 
    public MeshRenderer[] mRenderers;

    public float feedbackDuration = 5f;
    public float blinkInterval = 0.5f;

    public Material detectionMatNull;
    public Material detectionMatBase;
    public Material detectionMatFive;

    public float chancesToDetectActivity5OnActivity4 = 50f;

    [Header("Candle Giver Settings")] 
    public Transform candlePos;
    public float candleAnimationDelay = 1f;
    public float candleAnimationDuration = 0.25f;

    [Header("Sounds Settings")] 
    public SoundParameters activitySoundParameters;
    public SoundParameters activityFiveSoundParameters;

    private int _currentValue;
    private bool _emissionEnabled = false;
    
    private float _feedbackEndTime;
    
    protected override void PlayerGrabbedObject(CatchableObject grabbedObject)
    {
        if (_currentFireObject != null) return;
        
        base.PlayerGrabbedObject(grabbedObject);
    }
    
    protected override void SwallowObject(CatchableObject catchableObject)
    {
        base.SwallowObject(catchableObject);
        if (catchableObject.activitySource != null)
        {
            UpdateDetectionFeedback(catchableObject.activitySource);
        }

        if (catchableObject is not CatchableFireObject)
        {
            Destroy(catchableObject);
        }
        else
        {
            GiveBackCandle(catchableObject as CatchableFireObject);
        }
    }

    private CatchableFireObject _currentFireObject;
    private Vector3 _currentFireObjectScale;
    private SpirimonzBehaviourState _lastBehaviourState;
    private void GiveBackCandle(CatchableFireObject candle)
    {
        canBeTakenBackIntoHands = false;
        canInteract = false;

        _lastBehaviourState = _currentBehaviour;
        _currentBehaviour = SpirimonzBehaviourState.Wait;
        
        _currentFireObject = candle;
        
        animator.SetBool("GivingBackItem", true);
        
        candle.rb.isKinematic = true;
        candle.onGrab.AddListener(PlayerTookCandle);
        candle.transform.parent = candlePos;
        candle.turnOffFireOnBigRotation = false;

        candle.transform.localEulerAngles = Vector3.zero;
        
        this.Invoke(candleAnimationDelay, () =>
        {
            candle.transform.DOLocalMove(Vector3.zero, candleAnimationDuration);
            candle.transform.DOScale(1, candleAnimationDuration).SetEase(Ease.OutBack).OnComplete(() =>
            {
                candle.linkedFlammableElement.EnableFire(true, forced: true);
                candle.canBeGrabByPlayer = true;
            });
        });
    }

    private void PlayerTookCandle()
    {
        _currentFireObject.onGrab.RemoveListener(PlayerTookCandle);
        _currentFireObject.turnOffFireOnBigRotation = true;

        animator.SetBool("GivingBackItem", false);
        
        _currentFireObject = null;
        
        canBeTakenBackIntoHands = true;
        _currentBehaviour = _lastBehaviourState;
    }
    
    private void UpdateDetectionFeedback(ActivitySource activitySource)
    {
        int sourceValue = activitySource == null ? 0 : activitySource.activityValue;

        if (sourceValue == 4 && House.Instance.currentGhost.ghostParameters.HighSpiritActivities)
        {
            float roll = Random.Range(0f, 100f);
            if (roll < chancesToDetectActivity5OnActivity4)
            {
                sourceValue = 5;
            }
        }

        _currentValue = sourceValue;
        
        Material matToGive = detectionMatNull;
        if (sourceValue > 0)
        {
            matToGive = sourceValue == 5 ? detectionMatFive : detectionMatBase;

            if (sourceValue == 5)
            {
                BlinkingDetection();
                PlaySound(activityFiveSoundParameters);
            }
            else
            {
                PlaySound(activitySoundParameters);
            }
            
            _feedbackEndTime = Time.time + feedbackDuration;

            CancelInvoke(nameof(ResetDetectionFeedback));
            Invoke(nameof(ResetDetectionFeedback), feedbackDuration);
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

    private void ResetDetectionFeedback()
    {
        if (Time.time < _feedbackEndTime)
            return;

        UpdateDetectionFeedback(null);
    }

    private void PlaySound(SoundParameters soundParameters)
    {
        if(soundParameters != null)
            soundParameters.PlaySound(transform.position);
    }
    
    private void BlinkingDetection()
    {
        bool enableEmission = _currentValue == 5 && _emissionEnabled == false;
        foreach (MeshRenderer mr in mRenderers)
        {
            if (enableEmission)
            {
                mr.material.EnableKeyword("_EMISSION");
                _emissionEnabled = true;
            }
            else
            {
                mr.material.DisableKeyword("_EMISSION");
                _emissionEnabled = false;
            }
        }

        if (_currentValue == 5)
        {
            this.Invoke(blinkInterval, BlinkingDetection);
        }
    }
}
