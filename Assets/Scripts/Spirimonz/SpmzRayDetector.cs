using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzRayDetector : Spirimonz
{
    [Header("Detection settings")]
    public float detectionDistance = 3f;
    public float delayWithoutSourceBeforeTurningOff = 0.2f;

    private bool _delayStarted;

    [Header("Detection materials settings")]
    public MeshRenderer[] meshRenderers;
    public Material detectionMatNull;
    public Material detectionMatBase;
    public Material detectionMatFive;

    [Header("Detection Audio Settings")]
    public AudioClip detectionSound;
    public AudioClip detectionFiveSound;
    public float soundVolume = 0.8f;
    public float basePitch = 0.7f;
    public float bonusPitchPerDetectionLevel = 0.2f;
    
    [Header("Blink settings")]
    public bool enableBlink = true;
    public float blinkSpeed = 3f; // vitesse du blink
    private Dictionary<MeshRenderer, Coroutine> _blinkingCoroutines = new Dictionary<MeshRenderer, Coroutine>();

    private Camera _cam;
    private ActivitySource _targetedActivitySource;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private float _raycastTimer = 0f;
    public float raycastInterval = 0.2f; // 5 raycasts par seconde max

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour()) return false;

        _raycastTimer -= Time.deltaTime;
        if (_raycastTimer <= 0f)
        {
            _raycastTimer = raycastInterval;
            PerformRaycast();
        }

        return true;
    }

    private void PerformRaycast()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        RaycastHit hit;
        ActivitySource source = null;
        bool noSourceFound = true;

        if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out hit, detectionDistance))
        {
            source = GetActivitySourceFromHit(hit.transform);
            noSourceFound = source == null || source.activityValue <= 0;
        }

        if (noSourceFound && !_delayStarted)
        {
            _delayStarted = true;
            Invoke(nameof(ResetSource), delayWithoutSourceBeforeTurningOff);
        }

        if (source != null && source != _targetedActivitySource && source.activityValue > 0)
        {
            TargetNewActivitySource(source);
        }
    }

    private ActivitySource GetActivitySourceFromHit(Transform hitTransform)
    {
        if (hitTransform.TryGetComponent(out CatchableObject catchable) && catchable.activitySource != null)
            return catchable.activitySource;

        if (hitTransform.TryGetComponent(out ClickableObject clickable) && clickable.activitySource != null)
            return clickable.activitySource;

        if (hitTransform.TryGetComponent(out Door door) && door.activitySource != null)
            return door.activitySource;

        return null;
    }

    private void ResetSource()
    {
        _targetedActivitySource = null;
        UpdateVisuals(0);
        _delayStarted = false;
    }

    private void CancelResetInvoke()
    {
        CancelInvoke(nameof(ResetSource));
        _delayStarted = false;
    }

    private int _lastActivityValue = 0;
    private void TargetNewActivitySource(ActivitySource newActivitySource)
    {
        if (newActivitySource == null || newActivitySource.activityValue <= 0) return;
        
        CancelResetInvoke();

        bool isNewSource = newActivitySource != _targetedActivitySource;
        bool activityChanged = newActivitySource.activityValue != _lastActivityValue;

        if (!isNewSource && !activityChanged) return; // pas de trigger inutile

        _targetedActivitySource = newActivitySource;
        _lastActivityValue = newActivitySource.activityValue;

        CancelResetInvoke();
        UpdateVisuals(newActivitySource.activityValue);

        
        if (animator != null)
            animator.SetTrigger("Detection");

        PlayActivitySound(newActivitySource.activityValue);
    }

    private void UpdateVisuals(int activityValue)
    {
        activityValue = Mathf.Clamp(activityValue, 0, meshRenderers.Length);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null) continue;

            Material matToUse = detectionMatNull;

            if (activityValue > 0)
            {
                matToUse = activityValue == 5 ? detectionMatFive : detectionMatBase;
                if (i + 1 > activityValue)
                    matToUse = detectionMatNull;
            }

            meshRenderers[i].material = matToUse;
            StartBlinking(matToUse, meshRenderers[i]);
        }
    }
    
    private void StartBlinking(Material mat, MeshRenderer renderer)
    {
        // Stopper le blink précédent si nécessaire
        if (_blinkingCoroutines.TryGetValue(renderer, out var oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
            _blinkingCoroutines.Remove(renderer);
        }

        if (mat == detectionMatBase || mat == detectionMatFive)
        {
            Coroutine coroutine = StartCoroutine(BlinkEmission(mat));
            _blinkingCoroutines[renderer] = coroutine;
        }
        else
        {
            // Reset emission si ce n'est pas un material actif
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
        }
    }

    private IEnumerator BlinkEmission(Material mat)
    {
        if (!mat.HasProperty("_EmissionColor")) yield break;

        Color baseColor = mat.color;

        while (true)
        {
            // PingPong entre 0.5 et 1
            float intensity = Mathf.PingPong(Time.time * blinkSpeed, 0.5f) + 0.5f;
            mat.SetColor("_EmissionColor", baseColor * intensity);
            yield return null;
        }
    }
    
    private void PlayActivitySound(int activityValue)
    {
        if (detectionSound == null) return;

        AudioClip clipToUse = activityValue == 5 ? detectionFiveSound : detectionSound;
        float pitch = activityValue == 5 ? 1f : Mathf.Max(0.1f, basePitch + bonusPitchPerDetectionLevel * (activityValue - 1));

        SoundManager.Instance.PlaySound(clipToUse, transform.position, soundVolume, pitch, -1f, 15f, false, transform);
    }
}