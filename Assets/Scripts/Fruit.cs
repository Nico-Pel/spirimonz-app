using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Fruit : ThrowableObject
{
    public bool disappearAfterEat;
    public MeshFilter meshFilter;
    public Mesh[] eatSteps;

    private float elevationMin = 0.5f;
    private float elevationMax = 2;

    public bool canBeEaten { get; set; } = true;
    private int _currentStep = 0;

    private Ghost _ghost;

    public void EatFruit(Ghost ghost)
    {
        if (!canBeEaten) return;

        canBeEaten = false;
        _ghost = ghost;
        
        rb.isKinematic = true;
        float elevation = Random.Range(elevationMin, elevationMax);
        this.transform.DOMove(transform.position + Vector3.up * elevation, 2).OnComplete(() =>
        {
            Crunch();
        });
    }

    private void Crunch()
    {
        _currentStep++;
        this.transform.DOScale(0.9f, 0.2f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutBack);
        ApplyBiteRotation();
        this.Invoke(0.2f, () =>
        {
            if (eatSteps.Length < _currentStep)
            {
                if(disappearAfterEat)
                    gameObject.SetActive(false);
            }
            else
            {
                meshFilter.mesh = eatSteps[_currentStep - 1];
                if (eatSteps.Length >= _currentStep + 1 || disappearAfterEat)
                {
                    this.Invoke(0.25f, Crunch);
                }
                else
                {
                    this.Invoke(0.25f, StopEating);
                }
            }
        });
    }
    
    public void ApplyBiteRotation(float maxAngle = 25f, float duration = 0.2f)
    {
        // Générer un vecteur de rotation aléatoire sur x, y, z
        Vector3 randomRotation = new Vector3(
            Random.Range(-maxAngle, maxAngle),
            Random.Range(-maxAngle, maxAngle),
            Random.Range(-maxAngle, maxAngle)
        );

        // Appliquer la rotation locale avec un tween
        this.transform.DOLocalRotate(randomRotation, duration)
            .SetRelative()       // rotation relative à la rotation actuelle
            .SetEase(Ease.OutQuad) // interpolation plus naturelle
            .OnComplete(() =>
            {
                // Revenir doucement à la rotation initiale
                this.transform.DOLocalRotate(Vector3.zero, duration).SetEase(Ease.OutQuad);
            });
    }

    private void StopEating()
    {
        rb.isKinematic = false;
        _ghost.ThrowObject(this);
    }
}
