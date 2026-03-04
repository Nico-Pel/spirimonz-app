using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PropDestructorTrigger : GameBehaviour
{
    public Transform propEndPos;

    public float attractionSpeed = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CatchableObject catchable))
        {
            AttractProp(catchable);
        }
    }

    private void AttractProp(CatchableObject catchable)
    {
        catchable.canBeGrabByPlayer = false;
        catchable.canBeThrownByGhost = false;
        
        catchable.transform.DOMove(propEndPos.position, attractionSpeed).SetSpeedBased();
        catchable.transform.DOScale(0.01f, attractionSpeed).SetSpeedBased().OnComplete(() =>
        {
            catchable.gameObject.SetActive(false);
        });
    }
}