using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class PrintSource : GameBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Header("Tutorial / Debug")]
    public bool activateOnStart = false;
    public bool neverDeactivate = false;
    [Min(0f)] public float startActiveDuration = 30f;

    private float _colorDecreasing = 0.1f;
    private bool _activated;
    private float _currentDuration;
    private float _colorPower;
    private float _powerMax = 3; //Full color is 1.5f
    private float invisibleMarge = 0.5f;
    public float delayBeforeEnergyDecay = 3f;
    private float _lastEnergyChargeTime = -999f;
    private bool _wasVisible;
    
    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;
    public UnityEvent OnFirstReveal;

    private void Start()
    {
        if (activateOnStart && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float duration = neverDeactivate ? float.PositiveInfinity : Mathf.Max(0.01f, startActiveDuration);
            Activate(duration, spriteRenderer.sprite);
        }
    }

    public void Activate(float duration, Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        _colorPower = 0;
        _activated = true;
        _currentDuration = duration;
        _lastEnergyChargeTime = Time.time;
        _wasVisible = false;
        OnActivate?.Invoke();
    }

    private void Update()
    {
        if (_currentDuration > 0)
        {
            _currentDuration -= Time.deltaTime;
        }

        if (_activated)
        {
            if (Time.time - _lastEnergyChargeTime >= delayBeforeEnergyDecay && _colorPower > 0)
            {
                _colorPower -= _colorDecreasing * Time.deltaTime;
                if (_colorPower < 0)
                    _colorPower = 0;
            }

            HandlePrintColor();

            if (_currentDuration <= 0 && _colorPower <= 0)
            {
                Deactivate();
            }
        }
    }

    public void ChargingColor(float value)
    {
        _colorPower += value;
        if (_colorPower > _powerMax)
        {
            _colorPower = _powerMax;
        }
        _lastEnergyChargeTime = Time.time;
    }

    private void HandlePrintColor()
    {
        float colorPower = _colorPower - invisibleMarge;
        if (colorPower < 0)
        {
            colorPower = 0;
        }
        else if (colorPower > 1)
        {
            colorPower = 1;
        }
        spriteRenderer.material.color = new Color(1, 1, 1, colorPower);

        bool isVisible = colorPower > 0f;
        if (isVisible && !_wasVisible)
        {
            _wasVisible = true;
            OnFirstReveal?.Invoke();
        }
        else if (!isVisible && _wasVisible)
        {
            _wasVisible = false;
        }
    }

    private void Deactivate()
    {
        if (neverDeactivate)
            return;

        _activated = false;
        _wasVisible = false;
        spriteRenderer.material.DOColor(new Color(1, 1, 1, 0), 1).SetSpeedBased();
        OnDeactivate?.Invoke();
    }

    public bool IsActivated()
    {
        return _activated;
    }
}
