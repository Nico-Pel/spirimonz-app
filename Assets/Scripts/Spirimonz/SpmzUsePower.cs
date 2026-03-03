using System;
using UnityEngine;

public class SpmzUsePower : Spirimonz
{
    [Header("Energy")]
    public float maxEnergy = 100f;
    [ReadOnly] public float currentEnergy = 100f;

    [Tooltip("Energy consumed per second while using the power")]
    public float usingEnergyForSec = 15f;

    [Tooltip("Energy regenerated per second when not using the power")]
    public float rechargeForSec = 20f;

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
        if (Input.GetMouseButtonDown(1))
            TryActivate();

        if (Input.GetMouseButtonUp(1))
            StopPower();
    }

    private void UpdateEnergy()
    {
        if (_isUsingPower)
        {
            // Consume energy per second
            currentEnergy -= usingEnergyForSec * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                StopPower();
            }
        }
        else
        {
            // Regenerate energy per second
            currentEnergy += rechargeForSec * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }
    }

    private void TryActivate()
    {
        if (CurrentEnergyFraction() < minPercentToUse)
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

        if (_timeDisabled > 0f)
        {
            float timeOff = Time.realtimeSinceStartup - _timeDisabled;

            // Regenerate based on time spent disabled
            float energyRecovered = rechargeForSec * timeOff;
            currentEnergy = Mathf.Min(currentEnergy + energyRecovered, maxEnergy);

            _timeDisabled = 0f;
        }
    }

    protected override void OnDisable()
    {
        StopPower();

        _timeDisabled = Time.realtimeSinceStartup;
        base.OnDisable();
    }

    public bool IsUsingPower() => _isUsingPower;
    public float CurrentEnergyFraction() => Mathf.Clamp01(currentEnergy / maxEnergy);
}