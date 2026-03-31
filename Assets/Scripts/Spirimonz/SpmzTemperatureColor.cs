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

    public float FreezingThreshold => lowerTemperature;
    public float VisualFreezingPercent => _visualFreezingPercent;

    private float _percentageOnGradient;
    private Color _currentColor;
    private Color _colorVelocity;
    private float _visualFreezingPercent;
    private Color _warmColor;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();

        if (IsLocked()) return;
        
        _currentColor = gradientColor.Evaluate(0);
        _warmColor = _currentColor;
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
        UpdateVisualFreezingPercent();
        
        return true;
    }

    private void UpdateVisualFreezingPercent()
    {
        Vector3 warm = new Vector3(_warmColor.r, _warmColor.g, _warmColor.b);
        Vector3 freeze = new Vector3(freezingColor.r, freezingColor.g, freezingColor.b);
        Vector3 current = new Vector3(_currentColor.r, _currentColor.g, _currentColor.b);

        float maxDist = Vector3.Distance(warm, freeze);
        if (maxDist <= 0.0001f)
        {
            _visualFreezingPercent = 1f;
            return;
        }

        float dist = Vector3.Distance(current, freeze);
        _visualFreezingPercent = Mathf.Clamp01(1f - (dist / maxDist));
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
