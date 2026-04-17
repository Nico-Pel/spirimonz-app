using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class StaticBreakableObject : GameBehaviour
{
    [Header("Break Special Settings")]
    public bool annoyTheGhosts;
    public float ghostAngerMultiplierOnBreak = 1f;
    public float percentageOfFracturesToBeRetained = 33f;
    
    [Header("Breaking Basic settings")]
    public float minForceToBreak = 5f;
    public float lockingFracturesDelay = 3f;
    public float breakProtectionDuration = 3f;
    public float fractureForwardForce = 2f;
    public float fractureForwardNoise = 0.35f;
    public float fractureExplosionForce = 2f;
    public float fractureExplosionRadius = 1f;
    public float fractureExplosionUpwards = 0.2f;
    public float fractureRandomTorque = 1f;
    public float ignoreOriginalCollisionsDuration = 0.2f;

    [Space] [Header("Break Components")] public GameObject model;
    public GameObject fracturedObject;
    public SoundParameters breakingSoundParameters;

    private List<Rigidbody> _fracturedRigidbodies;
    private bool _canBreak;
    
    public UnityEvent OnBreak;

    private void Awake()
    {
        _fracturedRigidbodies = new List<Rigidbody>();

        if (fracturedObject != null)
        {
            fracturedObject.SetActive(false);
            _fracturedRigidbodies.AddRange(fracturedObject.GetComponentsInChildren<Rigidbody>(true));
        }

        if (breakProtectionDuration > 0f)
            this.Invoke(breakProtectionDuration, () => _canBreak = true);
        else
            _canBreak = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!_canBreak)
            return;

        float impactForce = other.relativeVelocity.magnitude;
        if (minForceToBreak >= 0f && impactForce >= minForceToBreak)
            Break(impactForce);
    }

    public void Break(float impactForce = 0f)
    {
        if (_canBreak == false) return;

        _canBreak = false;
        if (model != null)
            model.SetActive(false);

        if (model != null && fracturedObject != null)
            fracturedObject.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation);

        if (fracturedObject != null)
            fracturedObject.SetActive(true);

        PlayBreakingSound(impactForce);

        if (annoyTheGhosts)
        {
            House.Instance.currentGhost.MultiplyAnger(ghostAngerMultiplierOnBreak);
        }

        if (_fracturedRigidbodies == null || _fracturedRigidbodies.Count == 0)
            return;

        Collider[] originalColliders = GetComponentsInChildren<Collider>(true);
        if (ignoreOriginalCollisionsDuration > 0f && originalColliders.Length > 0)
            StartCoroutine(TemporaryIgnoreOriginalCollisions(originalColliders, ignoreOriginalCollisionsDuration));

        int retainCount = Mathf.RoundToInt(_fracturedRigidbodies.Count * Mathf.Clamp01(percentageOfFracturesToBeRetained / 100f));
        HashSet<Rigidbody> retained = new HashSet<Rigidbody>();
        List<Rigidbody> shuffled = new List<Rigidbody>(_fracturedRigidbodies);

        for (int i = 0; i < retainCount && shuffled.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, shuffled.Count);
            retained.Add(shuffled[index]);
            shuffled.RemoveAt(index);
        }

        foreach (Rigidbody rb in _fracturedRigidbodies)
        {
            if (rb == null)
                continue;

            bool keep = retained.Contains(rb);
            Transform fracture = rb.transform;

            if (!keep)
                fracture.parent = null;

            if (keep)
            {
                rb.isKinematic = true;
                continue;
            }

            rb.isKinematic = false;
            Vector3 noisyDir = (transform.forward + UnityEngine.Random.insideUnitSphere * fractureForwardNoise).normalized;
            if (fractureForwardForce > 0f)
                rb.AddForce(noisyDir * fractureForwardForce, ForceMode.Impulse);

            if (fractureExplosionForce > 0f && fractureExplosionRadius > 0f)
                rb.AddExplosionForce(fractureExplosionForce, transform.position, fractureExplosionRadius, fractureExplosionUpwards, ForceMode.Impulse);

            if (fractureRandomTorque > 0f)
                rb.AddTorque(UnityEngine.Random.onUnitSphere * fractureRandomTorque, ForceMode.Impulse);

            this.Invoke(lockingFracturesDelay, () =>
            {
                if (fracture == null)
                    return;

                if (rb != null && rb.velocity.magnitude < 0.5f)
                {
                    rb.isKinematic = true;
                }
                else
                {
                    fracture.DOScale(0.01f, 2f).OnComplete(() =>
                    {
                        if (fracture != null)
                            Destroy(fracture.gameObject);
                    });
                }
            });
        }
        
        OnBreak?.Invoke();
    }

    private IEnumerator TemporaryIgnoreOriginalCollisions(Collider[] originalColliders, float duration)
    {
        List<Collider> fractureColliders = new List<Collider>();
        for (int i = 0; i < _fracturedRigidbodies.Count; i++)
        {
            Rigidbody rb = _fracturedRigidbodies[i];
            if (rb == null)
                continue;

            Collider[] cols = rb.GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < cols.Length; j++)
            {
                if (cols[j] != null)
                    fractureColliders.Add(cols[j]);
            }
        }

        if (fractureColliders.Count == 0)
            yield break;

        SetIgnoreCollisions(originalColliders, fractureColliders, true);
        yield return new WaitForSeconds(duration);
        SetIgnoreCollisions(originalColliders, fractureColliders, false);
    }

    private static void SetIgnoreCollisions(Collider[] originalColliders, List<Collider> fractureColliders, bool ignore)
    {
        for (int i = 0; i < originalColliders.Length; i++)
        {
            Collider original = originalColliders[i];
            if (original == null || original.isTrigger)
                continue;

            for (int j = 0; j < fractureColliders.Count; j++)
            {
                Collider fracture = fractureColliders[j];
                if (fracture == null || fracture.isTrigger)
                    continue;

                Physics.IgnoreCollision(original, fracture, ignore);
            }
        }
    }

    private void PlayBreakingSound(float impactForce)
    {
        if (breakingSoundParameters == null)
            return;

        if (impactForce <= 0f)
        {
            breakingSoundParameters.PlaySound(transform.position);
            return;
        }

        float volumeMultiplier = Mathf.Clamp01(impactForce / 10f);
        float volumeToUse = breakingSoundParameters.volume * volumeMultiplier;
        breakingSoundParameters.PlaySound(transform.position, volumeToUse);
    }
}
