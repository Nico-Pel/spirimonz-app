using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PrintSource : GameBehaviour
{
    public SpriteRenderer spriteRenderer;

    private float _colorDecreasing = 0.1f;
    private bool _activated;
    private float _currentDuration;
    private float _colorPower;
    private float _powerMax = 3; //Full color is 1.5f
    private float invisibleMarge = 0.25f;

    public void Activate(float duration, Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        _colorPower = 0;
        _activated = true;
        _currentDuration = duration;
        
        Debug.Log("POUET UV PRINT: ", gameObject);
    }

    private void Update()
    {
        if (_currentDuration > 0)
        {
            _currentDuration -= Time.deltaTime;
        }
        else if (_activated)
        {
            Deactivate();
        }

        if (_activated)
        {
            HandlePrintColor();
            _colorPower -= _colorDecreasing * Time.deltaTime;
        }
    }

    public void ChargingColor(float value)
    {
        _colorPower += value;
        if (_colorPower > _powerMax)
        {
            _colorPower = _powerMax;
        }
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
    }

    private void Deactivate()
    {
        _activated = false;
        spriteRenderer.material.DOColor(new Color(1, 1, 1, 0), 1).SetSpeedBased();
    }

    public bool IsActivated()
    {
        return _activated;
    }
}
