using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzRayDetector : Spirimonz
{
    [Header("Detection Settings")]
    public float detectionDistance = 3f;

    [Header("Visuals")]
    public MeshRenderer[] meshRenderers;
    public Material detectionMatNull;
    public Material detectionMatBase;
    public Material detectionMatFive;
    public float visualDuration = 4f; // durée fixe du visuel et blink

    [Header("Audio")]
    public AudioClip detectionSound;
    public AudioClip detectionFiveSound;
    public float soundVolume = 0.8f;
    public float basePitch = 0.7f;
    public float bonusPitchPerDetectionLevel = 0.2f;

    [Header("Blink")]
    public bool enableBlink = true;
    public float blinkSpeed = 3f;
    private Dictionary<MeshRenderer, Coroutine> _blinkingCoroutines = new Dictionary<MeshRenderer, Coroutine>();

    private Camera _cam;
    private bool _isVisualPlaying = false;
    private float _raycastTimer = 0f;
    public float raycastInterval = 0.2f;

    private void Awake()
    {
        _cam = Camera.main;
    }

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
        if (_isVisualPlaying) return; // si animation visuelle en cours, ne rien trigger

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        RaycastHit hit;
        ActivitySource source = null;

        if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out hit, detectionDistance))
        {
            source = GetActivitySourceFromHit(hit.transform);
        }

        if (source != null && source.activityValue > 0 && source.GetActivityTimer() >= 1f)
        {
            StartCoroutine(PlayVisualAndSound(source));
        }
    }

    private ActivitySource GetActivitySourceFromHit(Transform t)
    {
        if (t.TryGetComponent(out CatchableObject c) && c.activitySource != null) return c.activitySource;
        if (t.TryGetComponent(out ClickableObject cl) && cl.activitySource != null) return cl.activitySource;
        if (t.TryGetComponent(out Door d) && d.activitySource != null) return d.activitySource;
        return null;
    }

    private IEnumerator PlayVisualAndSound(ActivitySource source)
    {
        _isVisualPlaying = true;

        // Visuel
        UpdateVisuals(source.activityValue);

        // Son
        PlayActivitySound(source.activityValue);

        // Animator
        if (animator != null) animator.SetTrigger("Detection");

        // Attente de la durée du visuel
        float timer = 0f;
        while (timer < visualDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Fin du visuel
        UpdateVisuals(0);
        _isVisualPlaying = false;
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
                if (i + 1 > activityValue) matToUse = detectionMatNull;
            }

            meshRenderers[i].material = matToUse;
            StartBlinking(matToUse, meshRenderers[i]);
        }
    }

    private void StartBlinking(Material mat, MeshRenderer renderer)
    {
        if (!enableBlink) return;

        if (_blinkingCoroutines.TryGetValue(renderer, out var oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
            _blinkingCoroutines.Remove(renderer);
        }

        if (mat == detectionMatBase || mat == detectionMatFive)
        {
            Coroutine c = StartCoroutine(BlinkEmission(mat));
            _blinkingCoroutines[renderer] = c;
        }
        else
        {
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
            float intensity = Mathf.PingPong(Time.time * blinkSpeed, 0.5f) + 0.5f;
            mat.SetColor("_EmissionColor", baseColor * intensity);
            yield return null;
        }
    }

    private void PlayActivitySound(int activityValue)
    {
        if (detectionSound == null) return;

        AudioClip clip = activityValue == 5 ? detectionFiveSound : detectionSound;
        float pitch = activityValue == 5 ? 1f : Mathf.Max(0.1f, basePitch + bonusPitchPerDetectionLevel * (activityValue - 1));

        SoundManager.Instance.PlaySound(clip, transform.position, soundVolume, pitch, -1f, 15f, false, transform);
    }
}
