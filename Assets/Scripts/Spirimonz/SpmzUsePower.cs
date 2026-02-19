using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SpmzUsePower : Spirimonz
{
    [Header("Energy")]
    public float maxEnergy = 100f;
    [ReadOnly] public float currentEnergy = 100f;
    public float usingEnergyMaxSec = 6f;
    public float rechargeMaxSec = 25f;
    public float minPercentToUse = 0.25f;

    private float _timeDisabled;
    
    [Header("Power")]
    public PowerActivator powerActivator;

    private bool _isUsingPower;

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;
        
        if (IsLocked()) return false;

        HandleInput();
        UpdateEnergy();

        return true;
    }

    private void HandleInput()
    {
        // Activation du pouvoir tant que clic droit maintenu
        if (Input.GetMouseButtonDown(1))
        {
            TryActivate();
        }

        if (Input.GetMouseButtonUp(1))
        {
            StopPower();
        }
    }

    private void UpdateEnergy()
    {
        if (_isUsingPower)
        {
            // Consomme l'énergie
            currentEnergy -= Time.deltaTime * (maxEnergy / usingEnergyMaxSec);
            if (currentEnergy <= 0)
            {
                currentEnergy = 0;
                StopPower();
            }
        }
        else
        {
            // Recharge l'énergie
            currentEnergy += Time.deltaTime * (maxEnergy / rechargeMaxSec);
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }
    }

    private void TryActivate()
    {
        if (currentEnergy / maxEnergy < minPercentToUse)
        {
            animator.SetTrigger("Nop");
            return;
        }

        _isUsingPower = true;
        powerActivator.Activate();
        animator.SetBool("CanUsePower", true);
    }

    private void StopPower()
    {
        if (!_isUsingPower) return;

        _isUsingPower = false;
        powerActivator.Deactivate();
        animator.SetBool("CanUsePower", false);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Si le Spirimonz avait été désactivé un moment
        if (_timeDisabled > 0f)
        {
            float timeOff = Time.realtimeSinceStartup - _timeDisabled;

            // Recharge en fonction du temps passé off
            float energyRecovered = timeOff * (maxEnergy / rechargeMaxSec);
            currentEnergy = Mathf.Min(currentEnergy + energyRecovered, maxEnergy);
        
            _timeDisabled = 0f; // reset
        }
    }

    private void OnDisable()
    {
        StopPower();
    
        // Enregistrer le moment où il est désactivé
        _timeDisabled = Time.realtimeSinceStartup;
    }
    
    public bool IsUsingPower() => _isUsingPower;
    public float CurrentEnergyFraction() => Mathf.Clamp01(currentEnergy / maxEnergy);
}