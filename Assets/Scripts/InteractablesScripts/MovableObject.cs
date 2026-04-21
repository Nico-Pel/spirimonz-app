using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class MovableObject : ClickableObject
{
    [Space]
    
    [Header("Movable Object Settings")]
    public bool movePosition;
    public bool moveRotation;
    
    [Header("Move on click!")]
    public Vector3 offsetPosition;
    public float moveSpeed = 1.5f;
    public Ease moveEase = Ease.OutBack;
    public Ease moveBackEase = Ease.Linear;
    [Tooltip("Optional: move via Rigidbody.MovePosition instead of Transform. Useful for physics collisions.")]
    public Rigidbody moveRigidbody;

    [Header("Rotate on click!")]
    public Vector3 offsetRotation;
    public float rotateSpeed = 100f;
    public float rotateBackSpeed = 250f;
    public Ease rotateEase = Ease.OutBack;
    public Ease rotateBackEase = Ease.Linear;

    [Header("Sounds")] 
    public AudioClip moveSound;
    public AudioClip moveSoundBack;
    public float volume = 0.5f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    
    private bool _isActivated;
    private bool _canBeClickedByPlayer = true;
    
    private Vector3 _startPosition;
    private Vector3 _startRotation;
    private Tween _moveTween;

    private void Start()
    {
        Transform startTransform = moveRigidbody != null ? moveRigidbody.transform : transform;
        _startPosition = startTransform.localPosition;
        _startRotation = transform.localEulerAngles;
    }

    public override void OnClick()
    {
        base.OnClick();
        
        transform.DOKill();
        _moveTween?.Kill();
        
        if (movePosition)
        {
            Vector3 newPos = _isActivated ? _startPosition : _startPosition + offsetPosition;
            Ease ease = _isActivated ? moveBackEase : moveEase;

            if (moveRigidbody != null)
            {
                Transform targetTransform = moveRigidbody.transform;
                Vector3 targetWorld = targetTransform.parent != null
                    ? targetTransform.parent.TransformPoint(newPos)
                    : newPos;

                float speed = Mathf.Max(0.0001f, moveSpeed);
                float duration = Vector3.Distance(moveRigidbody.position, targetWorld) / speed;

                if (duration <= 0f)
                {
                    moveRigidbody.MovePosition(targetWorld);
                }
                else
                {
                    _moveTween = DOTween.To(
                            () => moveRigidbody.position,
                            value => moveRigidbody.MovePosition(value),
                            targetWorld,
                            duration
                        )
                        .SetEase(ease)
                        .SetUpdate(UpdateType.Fixed);
                }
            }
            else
            {
                transform.DOLocalMove(newPos, moveSpeed).SetSpeedBased().SetEase(ease);
            }
        }

        if (moveRotation)
        {
            Vector3 newPos = _isActivated ? _startRotation : _startRotation + offsetRotation;
            float speed = _isActivated ? rotateBackSpeed : rotateSpeed;
            Ease ease = _isActivated ? rotateBackEase : rotateEase;
            transform.DOLocalRotate(newPos, speed).SetSpeedBased().SetEase(ease);
        }
        
        PlaySound();

        _isActivated = !_isActivated;
    }

    private void PlaySound()
    {
        if (moveSound == null) return;
        
        AudioClip clip = _isActivated && moveSoundBack != null ? moveSoundBack : moveSound;
        SoundManager.Instance?.PlaySound(clip, activitySource.transform.position, volume, Random.Range(pitchMin, pitchMax));
    }

    public bool IsActivated()
    {
        return _isActivated;
    }

    public void SetActivatedState(bool active)
    {
        if (_isActivated == active)
            return;

        OnClick();
    }
    
    protected override void GhostClickedDuringAHunt()
    {
        if (!_isActivated)
        {
            _canBeClickedByPlayer = false;
            this.Invoke(1f, () =>
            {
                _canBeClickedByPlayer = true;
            });
            OnClick();
        }
    }
}
