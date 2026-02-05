using System.Collections;
using UnityEngine;

public class BlinkingLight : MonoBehaviour
{
    public bool isLinkedToPlayer;
    
    [Header("Light Settings")]
    public Light light; // la light à gérer
    private float baseIntensity = 1f; // intensité normale
    private float blinkMultiplier = 0.5f; // fraction d'intensité lors du blink
    private float blinkSpeed = 0.1f; // vitesse du blink en secondes
    private float blinkSpeedVoltaic = 0.08f; // vitesse du clignotement en secondes si le fantome est voltaic
    private float blinkDistance = 10f; // distance du ghost pour déclencher le blink

    private Ghost _ghost;
    private bool _isBlinking;
    private float _targetIntensity;
    private GamePlayer _player;

    private void Start()
    {
        if (light == null)
            light = GetComponent<Light>();

        baseIntensity = light.intensity;

        _ghost = House.Instance.currentGhost;
        if (_ghost.ghostParameters.ghostType == GhostParameters.GhostType.Voltaic)
        {
            blinkSpeed = blinkSpeedVoltaic;
        }

        _player = (GamePlayer)Player.Instance;
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy && _ghost != null && _ghost.currentState != Ghost.GhostState.hideState)
        {
            float dist = Vector3.Distance(transform.position, _ghost.transform.position);

            if (!_isBlinking && dist <= blinkDistance)
            {
                StartCoroutine(BlinkRoutine());
                if (_ghost.currentWayPoint.linkedRoom != _player.currentRoom)
                {
                    _player.AlertTheHuntingGhost();
                }
            }
            else if (_isBlinking && dist > blinkDistance)
            {
                _isBlinking = false; // stoppe le blink à la fin de la coroutine
            }
        }
        else
        {
            _isBlinking = false; // pas actif → pas de blink
        }
    }

    private IEnumerator BlinkRoutine()
    {
        _isBlinking = true;
        while (_isBlinking)
        {
            // Intensité faible
            _targetIntensity = baseIntensity * blinkMultiplier;
            while (_isBlinking && light.intensity > _targetIntensity + 0.01f)
            {
                light.intensity = Mathf.Lerp(light.intensity, _targetIntensity, Time.deltaTime * 10f);
                yield return null;
            }

            // Intensité normale
            _targetIntensity = baseIntensity;
            while (_isBlinking && light.intensity < _targetIntensity - 0.01f)
            {
                light.intensity = Mathf.Lerp(light.intensity, _targetIntensity, Time.deltaTime * 10f);
                yield return null;
            }

            yield return new WaitForSeconds(blinkSpeed); // pause entre les clignos
        }

        // Restore à l'intensité normale
        light.intensity = baseIntensity;
    }
}