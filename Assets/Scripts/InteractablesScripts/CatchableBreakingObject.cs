using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class CatchableBreakingObject : CatchableObject
{
    [Space] [Header("Break Special Settings")]
    public bool annoyTheGhosts;
    public float ghostAngerMultiplierOnBreak = 1f;
    public bool forbidInteractionOnBreak = true;
    
    [Space] [Header("Model components")] 
    public GameObject model;
    public GameObject fracturedModel;

    [Header("Breaking Sounds")] 
    public SoundParameters breakingSoundParameters;
    public float minForceToBreak = 5f;
    public float lockingFracturesDelay = 3f;

    [Header("Breaking Forces")]
    public float fractureExplosionForce = 2.5f;
    public float fractureExplosionRadius = 1f;
    public float fractureExplosionUpwards = 0.2f;
    public float fractureRandomTorque = 1.2f;

    private bool _canBreak = false;

    private void Start()
    {
        //Prevent object to break on start
        this.Invoke(3f, () => _canBreak = true);
        this.Invoke(0.1f, SetPriority);
    }

    private void SetPriority()
    {
        if (House.Instance.currentGhost.ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Trickster)
            priority = 1;
    }

    protected override void OnCollision(Transform other, float impactForce)
    {
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

        if (forbidInteractionOnBreak)
        {
            canBeGrabByPlayer = false;
            canBeThrownByGhost = false;
            canBeThrownByPlayer = false;
        }

        if (annoyTheGhosts)
        {
            House.Instance.currentGhost.MultiplyAnger(ghostAngerMultiplierOnBreak);
        }

        PlayBreakingSound(impactForce);

        if (model != null && fracturedModel != null)
            fracturedModel.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation);

        if (model != null)
            model.SetActive(false);
        if (fracturedModel != null)
            fracturedModel.SetActive(true);

        ApplyFractureForces(impactForce);

        foreach (Transform fracture in GetComponentsInChildren<Transform>())
        {
            if (fracture == transform) continue;

            Transform localFracture = fracture;

            localFracture.transform.parent = null;
            this.Invoke(lockingFracturesDelay, () =>
            {
                if (localFracture == null) return;

                if (localFracture.TryGetComponent(out Rigidbody rb) && rb.velocity.magnitude < 0.5f)
                {
                    rb.isKinematic = true;
                }
                else
                {
                    localFracture.DOScale(0.01f, 2f).OnComplete(() =>
                    {
                        if (localFracture != null)
                            Destroy(localFracture.gameObject);
                    });
                }
            });
        }
    }

    private void ApplyFractureForces(float impactForce)
    {
        if (fracturedModel == null)
            return;

        float impactScale = minForceToBreak > 0f ? Mathf.Clamp01(impactForce / minForceToBreak) : 1f;
        float explosionForce = fractureExplosionForce * Mathf.Lerp(0.75f, 1.5f, impactScale);
        float torqueForce = fractureRandomTorque * Mathf.Lerp(0.75f, 1.5f, impactScale);

        Rigidbody[] rigidbodies = fracturedModel.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb == null)
                continue;

            rb.isKinematic = false;
            if (explosionForce > 0f && fractureExplosionRadius > 0f)
                rb.AddExplosionForce(explosionForce, transform.position, fractureExplosionRadius, fractureExplosionUpwards, ForceMode.Impulse);

            if (torqueForce > 0f)
                rb.AddTorque(Random.onUnitSphere * torqueForce, ForceMode.Impulse);
        }
    }
    
    private void PlayBreakingSound(float impactForce)
    {
        if (breakingSoundParameters != null)
        {
            float volumeMultiplier = Mathf.Clamp01(impactForce / 10f); // normalise impactForce
            float volumeToUse = breakingSoundParameters.volume * volumeMultiplier;
            breakingSoundParameters.PlaySound(transform.position, volumeToUse);
        }
    }
}
