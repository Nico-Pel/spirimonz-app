using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpmzTemperatureValue : Spirimonz
{
    [Header("Components")]
    public TextMeshPro tTemperature;
    public ClickableObject clickableObject;

    [Header("Settings")]
    [SerializeField] private float temperatureLerpSpeed = 5f;
    [SerializeField] private float displayUpdateInterval = 0.2f;
    [SerializeField] private float unitSwitchCooldown = 0.5f; // cooldown pour éviter spam clics

    [Header("Colors")]
    [SerializeField] private Color celsiusColor = new Color(0.3f, 1f, 0.9f);
    [SerializeField] private Color fahrenheitColor = new Color(1f, 0.9f, 0.3f);

    private bool useFahrenheit = true;
    private float _printedTemperature; // stockée en Celsius
    private float _displayTimer;
    private float _lastUnitSwitchTime = -999f;

    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        
        clickableObject.onClick.AddListener(ChangeUnit);

        if (currentRoom == null)
        {
            GamePlayer gamePlayer = Player.Instance as GamePlayer;
            if(gamePlayer != null)
                currentRoom = gamePlayer.currentRoom;
        }

        if (currentRoom != null)
        {
            _printedTemperature = currentRoom.GetTemperatureCelsius();
        }
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        tTemperature.gameObject.SetActive(isOnTheMap || powerActiveInHands);

        if (!base.UpdateSpirimonzBehaviour())
            return false;

        // lerp fluide vers la température réelle
        float currentTemperature = currentRoom.GetTemperatureCelsius();
        _printedTemperature = Mathf.Lerp(
            _printedTemperature,
            currentTemperature,
            Time.deltaTime * temperatureLerpSpeed
        );

        // update texte seulement toutes les X secondes
        _displayTimer -= Time.deltaTime;
        if (_displayTimer > 0f)
            return true;

        _displayTimer = displayUpdateInterval;
        UpdateTemperatureText();

        return true;
    }

    private void UpdateTemperatureText()
    {
        float displayTemp = useFahrenheit ? ConvertToFahrenheit(_printedTemperature) : _printedTemperature;
        string unit = useFahrenheit ? "°F" : "°C";

        tTemperature.text = displayTemp.ToString("F2") + unit;
        tTemperature.color = useFahrenheit ? fahrenheitColor : celsiusColor;
    }

    private void ChangeUnit()
    {
        // cooldown pour éviter le spam clic
        if (Time.time - _lastUnitSwitchTime < unitSwitchCooldown)
            return;

        _lastUnitSwitchTime = Time.time;

        useFahrenheit = !useFahrenheit;
        UpdateTemperatureText(); // changement instantané visuel, mais pas de lerp hack
    }

    private float ConvertToFahrenheit(float celsius)
    {
        return celsius * 9f / 5f + 32f;
    }
}
