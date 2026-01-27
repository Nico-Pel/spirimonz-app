using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    [Header("Rotate on click!")]
    public Vector3 offsetRotation;
    public float rotateSpeed = 100f;
    public float rotateBackSpeed = 150f;
    public Ease rotateEase = Ease.OutBack;
    public Ease rotateBackEase = Ease.Linear;
    
    private bool _isActivated;
    
    private Vector3 _startPosition;
    private Vector3 _startRotation;

    private void Start()
    {
        _startPosition = transform.localPosition;
        _startRotation = transform.localEulerAngles;
    }

    public override void OnClick()
    {
        base.OnClick();
        
        transform.DOKill();
        
        if (movePosition)
        {
            Vector3 newPos = _isActivated ? _startPosition : _startPosition + offsetPosition;
            Ease ease = _isActivated ? moveBackEase : moveEase;
            transform.DOLocalMove(newPos, moveSpeed).SetSpeedBased().SetEase(ease);
        }

        if (moveRotation)
        {
            Vector3 newPos = _isActivated ? _startRotation : _startRotation + offsetRotation;
            float speed = _isActivated ? rotateBackSpeed : rotateSpeed;
            Ease ease = _isActivated ? rotateBackEase : rotateEase;
            transform.DOLocalRotate(newPos, speed).SetSpeedBased().SetEase(ease);
        }

        _isActivated = !_isActivated;
    }
}
