using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpmzTemperatureColor : Spirimonz
{
    [Header("Temperature Color : Components")]
    public Gradient gradientColor;
    public Color freezingColor;

    public Light[] lightsToChange;
    public ParticleSystem[] particlesToChange;

    [Header("Temperature Color : Settings")]
    [SerializeField] private float higherTemperature = 25f;
    [SerializeField] private float lowerTemperature = 1f;
    [SerializeField] private float colorLerpSpeed = 5f;

    private float _percentageOnGradient;
    private Color _currentColor;
    private Color _colorVelocity;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();

        if (IsLocked()) return;
        
        _currentColor = gradientColor.Evaluate(0);
        SetTemperatureColors(_currentColor);
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour())
            return false;
        
        base.UpdateSpirimonzBehaviour();
        
        float currentTemperature = currentRoom.GetTemperatureCelsius();

        Color targetColor = freezingColor;

        if (currentTemperature >= lowerTemperature)
        {
            _percentageOnGradient = 1f - Mathf.InverseLerp(lowerTemperature, higherTemperature, currentTemperature);
            targetColor = gradientColor.Evaluate(_percentageOnGradient);
        }

        _currentColor = Color.Lerp(
            _currentColor,
            targetColor,
            Time.deltaTime * colorLerpSpeed
        );

        SetTemperatureColors(_currentColor);
        
        return true;
    }

    private void SetTemperatureColors(Color colorToSet)
    {
        foreach (Light light in lightsToChange)
        {
            light.color = colorToSet;
        }

        foreach (ParticleSystem particle in particlesToChange)
        {
            var main = particle.main;
            main.startColor = colorToSet;
        }
    }
}