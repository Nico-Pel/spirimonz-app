using System;
using UnityEngine;

public class SpmzUsePower : Spirimonz
{
    public enum PowerUseMode
    {
        Hold,
        SingleShot
    }

    [Header("Energy")]
    public float maxEnergy = 100f;
    [ReadOnly] public float currentEnergy = 100f;

    [Tooltip("Energy consumed per second while using the power")]
    public float usingEnergyForSec = 15f;

    [Tooltip("Energy regenerated per second when not using the power")]
    public float rechargeForSec = 20f;

    public float minPercentToUse = 0.25f;

    [Header("Power Use Mode")]
    public PowerUseMode useMode = PowerUseMode.Hold;
    [Tooltip("Energy consumed per use (SingleShot mode)")]
    public float energyCostPerUse = 0f;
    [Tooltip("How long the power stays active for feedback in SingleShot mode")]
    public float singleShotActiveDuration = 0.15f;

    private float _timeDisabled;

    [Header("Power")]
    public PowerActivator powerActivator;

    private bool _isUsingPower;
    private const string SINGLE_SHOT_STOP_INVOKE = "SpmzUsePower.SingleShotStop";

    protected override void Start()
    {
        useSecondaryButton = true;
        base.Start();
    }

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
        if ((!MobileInput.Enabled && Input.GetMouseButtonDown(1)) || MobileInput.SecondaryDown)
            TryActivate();

        if (useMode == PowerUseMode.Hold)
        {
            if ((!MobileInput.Enabled && Input.GetMouseButtonUp(1)) || MobileInput.SecondaryUp)
                StopPower();
        }
    }

    private void UpdateEnergy()
    {
        if (_isUsingPower && useMode == PowerUseMode.Hold)
        {
            // Consume energy per second
            currentEnergy -= usingEnergyForSec * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                StopPower();
            }
        }
        else if (!_isUsingPower)
        {
            // Regenerate energy per second
            currentEnergy += rechargeForSec * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }
    }

    protected virtual bool TryActivate()
    {
        if (!CanUsePower())
        {
            animator.SetTrigger("Nop");
            return false;
        }

        _isUsingPower = true;
        if (useMode == PowerUseMode.SingleShot && energyCostPerUse > 0f)
            SpendEnergy(energyCostPerUse);

        if (powerActivator != null)
            powerActivator.Activate();
        animator.SetBool("CanUsePower", true);

        if (useMode == PowerUseMode.SingleShot && animator != null)
            animator.SetTrigger("UsePower");

        OnPowerActivated();

        if (useMode == PowerUseMode.SingleShot)
        {
            CancelInvoke(SINGLE_SHOT_STOP_INVOKE);
            float duration = Mathf.Max(0f, singleShotActiveDuration);
            if (duration > 0f)
                this.Invoke(SINGLE_SHOT_STOP_INVOKE, duration, StopPower);
            else
                StopPower();
        }

        return true;
    }

    protected virtual void StopPower()
    {
        if (!_isUsingPower) return;

        _isUsingPower = false;
        if (powerActivator != null)
            powerActivator.Deactivate();
        animator.SetBool("CanUsePower", false);

        OnPowerDeactivated();
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

    protected virtual bool CanUsePower()
    {
        float required = GetRequiredEnergy();
        return currentEnergy >= required;
    }

    protected virtual float GetRequiredEnergy()
    {
        float requiredByPercent = maxEnergy * minPercentToUse;
        float requiredByCost = useMode == PowerUseMode.SingleShot ? Mathf.Max(0f, energyCostPerUse) : 0f;
        return Mathf.Max(requiredByPercent, requiredByCost);
    }

    protected bool SpendEnergy(float amount)
    {
        if (amount <= 0f) return true;
        if (currentEnergy < amount) return false;
        currentEnergy -= amount;
        return true;
    }

    public void RestoreEnergy(float amount)
    {
        if (amount <= 0f) return;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }

    protected virtual void OnPowerActivated() { }
    protected virtual void OnPowerDeactivated() { }
}
