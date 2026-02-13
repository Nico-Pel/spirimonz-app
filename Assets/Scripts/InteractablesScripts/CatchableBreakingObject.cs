using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class CatchableBreakingObject : CatchableObject
{
    [Space] [Header("Model components")] 
    public GameObject model;
    public GameObject fracturedModel;
    
    [Header("Breaking Sounds")] 
    public AudioClip[] breakingSounds;
    public float breakingSoundVolume = 0.5f;
    public float breakingSoundAveragePitch = 1f;
    public float breakingSoundVariationPitch = 0.1f;
    public float breakingSoundRange = 15f;
    public float minForceToBreak = 5f;
    public float lockingFracturesDelay = 3f;

    private bool _canBreak = false;

    private void Start()
    {
        //Prevent object to break on start
        this.Invoke(3f, () => _canBreak = true);
    }

    protected override void OnCollision(Transform other, float impactForce)
    {
        Debug.Log("FORCE : " + impactForce);
        if (impactForce > minForceToBreak)
        {
            BreakObject(impactForce);
        }
        else
        {
            base.OnCollision(other, impactForce);
        }
    }

    private void BreakObject(float impactForce)
    {
        if(_canBreak == false) return;

        _canBreak = false;

        canBeGrabByPlayer = false;
        canBeThrownByGhost = false;
        canBeThrownByPlayer = false;
        PlayBreakingSound(impactForce);

        model.SetActive(false);
        fracturedModel.SetActive(true);

        foreach (Transform fracture in GetComponentsInChildren<Transform>())
        {
            //Ignore parent
            if(fracture == transform) continue;
            
            this.Invoke(lockingFracturesDelay, () =>
            {
                if (fracture.gameObject != null)
                {
                    if (fracture.TryGetComponent(out Rigidbody rb) && rb.velocity.magnitude < 0.5f)
                    {
                        rb.isKinematic = true;
                    }
                    else
                    {
                        fracture.DOScale(0.01f, 2f).OnComplete(() =>
                        {
                            if(fracture.gameObject != null)
                                Destroy(fracture.gameObject);
                        });
                    }
                }
            });
        }
    }
    
    private void PlayBreakingSound(float impactForce)
    {
        if (breakingSounds.Length == 0) return;

        AudioClip clipToUse = null;
        clipToUse = breakingSounds[Random.Range(0, breakingSounds.Length)];
        if (clipToUse == null)
        {
            //If selected clip is null, select first viable sound
            foreach (AudioClip clip in breakingSounds)
            {
                if (clip != null)
                {
                    clipToUse = clip;
                    break;
                }
            }
        }

        //No viable audio clip
        if (clipToUse == null)
            return;
        
        float volumeMultiplier = Mathf.Clamp01(impactForce / 10f); // normalise impactForce
        float pitch = breakingSoundAveragePitch + Random.Range(-breakingSoundVariationPitch, breakingSoundVariationPitch);

        SoundManager.Instance.PlaySound(
            clipToUse,
            position: transform.position,
            volume: breakingSoundVolume * volumeMultiplier,
            pitch: pitch,
            range: breakingSoundRange
        );
    }
}