using UnityEngine;
using DG.Tweening;
using System.Collections;

public class EyesBlinker : GameBehaviour
{
    [Header("References")]
    [SerializeField] private Transform eyes;

    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.1f;
    [SerializeField] private float minBlinkDelay = 0.5f;
    [SerializeField] private float maxBlinkDelay = 7f;

    private Tween _blinkTween;
    private Coroutine _blinkRoutine;
    private Vector3 _initialEyeScale;

    private void Awake()
    {
        if (eyes == null)
        {
            Debug.LogError($"{nameof(EyesBlinker)}: Eyes reference is missing.");
            enabled = false;
            return;
        }

        _initialEyeScale = eyes.localScale;
    }

    private void OnEnable()
    {
        if (eyes != null)
            eyes.localScale = _initialEyeScale;

        _blinkRoutine = StartCoroutine(BlinkLoop());
    }

    private void OnDisable()
    {
        if (_blinkRoutine != null)
            StopCoroutine(_blinkRoutine);

        _blinkTween?.Kill();
        _blinkTween = null;
        _blinkRoutine = null;

        if (eyes != null)
            eyes.localScale = _initialEyeScale;
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            float delay = Random.Range(minBlinkDelay, maxBlinkDelay);
            yield return new WaitForSeconds(delay);

            DoBlink();
        }
    }

    private void DoBlink()
    {
        _blinkTween?.Kill();

        _blinkTween = eyes
            .DOScaleY(0.01f, blinkDuration)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo);
    }
}
