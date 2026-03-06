using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Animations;
using DG.Tweening;

public class CTA : GameBehaviour
{
    [SerializeField] private GameObject ctaBox;
    [SerializeField] private TextMeshPro tInput;
    [SerializeField] private LookAtConstraint lookAtConstraint;

    private Vector3 _ctaBaseScale;

    private void Awake()
    {
        _ctaBaseScale = ctaBox.transform.localScale;
    }

    public void SetCallToAction(bool enable, Player player)
    {
        if (enable)
        {
            ctaBox.SetActive(true);
            ctaBox.transform.DOScale(_ctaBaseScale, 0.25f).SetEase(Ease.OutBack).From(0);
        }
        else
        {
            ctaBox.transform.DOScale(0, 0.25f).SetEase(Ease.InOutBack).OnComplete(() =>
            {
                ctaBox.SetActive(false);
            });
        }

        if (player != null)
        {
            tInput.text = player.inputManager.worldInteractions.ToString();
            
            lookAtConstraint.constraintActive = enable;

            if (enable)
            {
                ConstraintSource cs = lookAtConstraint.GetSource(0);
                cs.sourceTransform = player.camera.transform;
                lookAtConstraint.SetSource(0, cs);
            }
        }
    }
}
