using System.Collections;
using UnityEngine;

public class SpmzFireBreather : Spirimonz
{
    [Header("Fire Breath")]
    public FireTrigger fireTrigger;
    public Collider fireColl;
    public ParticleSystem flameFx;
    public string fireAnimationTrigger = "Fire";
    public float fireStartDelay = 0.5f;
    public float fireDuration = 0.6f;
    public float fireCooldown = 0.2f;
    public SoundParameters soundParameters;

    private bool _isBreathing;
    private float _nextAllowedFireTime;
    private Coroutine _fireRoutine;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        canBeDroppedOnMap = false;
        if (fireTrigger != null)
            fireTrigger.canCurseFlammables = true;
        SetFireActive(false);
    }

    public override void OnClickInHands()
    {
        if (IsLocked() || isOnTheMap)
            return;

        TryBreathFire();
    }

    private void TryBreathFire()
    {
        if (_isBreathing)
            return;

        if (Time.time < _nextAllowedFireTime)
            return;

        if (_fireRoutine != null)
            StopCoroutine(_fireRoutine);

        _fireRoutine = StartCoroutine(BreathFireRoutine());
    }

    private IEnumerator BreathFireRoutine()
    {
        _isBreathing = true;
        _nextAllowedFireTime = Time.time + Mathf.Max(0f, fireCooldown);

        if (animator != null && !string.IsNullOrEmpty(fireAnimationTrigger))
        {
            animator.SetTrigger(fireAnimationTrigger);
        }

        float startDelay = Mathf.Max(0f, fireStartDelay);
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        SetFireActive(true);

        yield return new WaitForSeconds(Mathf.Max(0f, fireDuration));

        SetFireActive(false);
        _isBreathing = false;
        _fireRoutine = null;
    }

    private void SetFireActive(bool active)
    {
        if (fireTrigger != null)
            fireTrigger.canGiveFire = active;
        
        if(fireColl != null)
            fireColl.enabled = active;

        if (flameFx != null && active == true)
            flameFx.Play();
        
        if(soundParameters != null && active == true)
            soundParameters.PlaySound(transform.position);
    }

    protected override void OnDisable()
    {
        if (_fireRoutine != null)
        {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }

        _isBreathing = false;
        SetFireActive(false);
        base.OnDisable();
    }
}
