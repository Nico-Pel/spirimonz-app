using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SpmzZoneUV : Spirimonz
{
    public float range = 5f; // Distance de détection
    public float chargeSpeed = 0.1f; // Valeur ajoutée par seconde
    
    public Transform UVsourceTransform;

    [FormerlySerializedAs("useEnergy")] [Header("Energy")] 
    public bool canUseEnergy;
    [ReadOnly] public float currentEnergy = 100f;
    public float percentageEnergyToStartUsingIt = 25f;
    public float usingEnergyMaxSec = 6f;
    public float rechargeMaxSec = 25f;

    public GameObject energyFeedbackObject;
    public Light energyFeedbackLight;
    public Vector3 minFeedbackScale = Vector3.one * 0.23f;
    public float minFeedbackLightIntensity = 0.3f;

    private float _maxEnergy;
    private bool _isUsingEnergy;
    private Vector3 _energyFeedbackBaseScale;
    private float _energyFeedbackBaseIntensity;

    private float _lastUtilisationTime;

    private void Awake()
    {
        _maxEnergy = currentEnergy;
        
        if(energyFeedbackObject != null)
            _energyFeedbackBaseScale = energyFeedbackObject.transform.localScale;
        
        if(energyFeedbackLight != null)
            _energyFeedbackBaseIntensity = energyFeedbackLight.intensity;
    }

    public List<PrintSource> printSources = new List<PrintSource>();
    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        
        if (IsLocked()) return;

        printSources.AddRange(FindObjectsOfType<PrintSource>());
    }
    
    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;

        if (canUseEnergy && _isUsingEnergy == false)
        {
            if (currentEnergy < _maxEnergy)
            {
                currentEnergy += Time.deltaTime * (_maxEnergy / rechargeMaxSec);
                if (currentEnergy > _maxEnergy)
                    currentEnergy = _maxEnergy;
            }

            return false;
        }

        if (currentEnergy <= 0)
        {
            _isUsingEnergy = false;
            animator.SetBool("CanUsePower", false);
            currentEnergy = 0;
            if(energyFeedbackLight != null)
                energyFeedbackLight.intensity = 0;
            if(energyFeedbackObject != null)
                energyFeedbackObject.transform.localScale = Vector3.zero;
            return false;
        }

        UpdateEnergyFeedback();
        currentEnergy -= Time.deltaTime * (_maxEnergy / usingEnergyMaxSec);
        
        foreach (PrintSource ps in printSources)
        {
            float dist = Vector3.Distance(UVsourceTransform.position, ps.transform.position);
            if (dist <= range)
            {
                ps.ChargingColor(chargeSpeed * Time.deltaTime);
            }
        }

        return true;
    }

    private void UpdateEnergyFeedback()
    {
        if (energyFeedbackObject != null)
        {
            float t = Mathf.Clamp01(currentEnergy / _maxEnergy);
            t = Mathf.Pow(t, 0.4f);
            energyFeedbackObject.transform.localScale =
                Vector3.Lerp(minFeedbackScale, _energyFeedbackBaseScale, t);
        }

        if (energyFeedbackLight != null)
        {
            float t = Mathf.Clamp01(currentEnergy / _maxEnergy);
            t = Mathf.Pow(t, 0.4f);
            energyFeedbackLight.intensity =
                Mathf.Lerp(minFeedbackLightIntensity, _energyFeedbackBaseIntensity, t);
        }
    }

    public override void ActionOnEnabled()
    {
        base.ActionOnEnabled();
        
        float chargingTime = Time.time - _lastUtilisationTime;
        currentEnergy += (_maxEnergy / rechargeMaxSec) * chargingTime;
        
        TryToActivate();
    }

    private void TryToActivate()
    {
        if (currentEnergy > _maxEnergy)
            currentEnergy = _maxEnergy;
        
        if ((currentEnergy / _maxEnergy) >= percentageEnergyToStartUsingIt / 100f)
        {
            animator.SetBool("CanUsePower", true);
            _isUsingEnergy = true;
        }
        else
        {
            animator.SetTrigger("Nop");
        }
    }

    private void OnDisable()
    {
        _isUsingEnergy = false;
        animator.SetBool("CanUsePower", false);
        _lastUtilisationTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (UVsourceTransform != null)
            Gizmos.DrawWireSphere(UVsourceTransform.position, range);
    }

    public override void OnClickInHands()
    {
        base.OnClickInHands();

        if (_isUsingEnergy) return;
        
        TryToActivate();
    }
}