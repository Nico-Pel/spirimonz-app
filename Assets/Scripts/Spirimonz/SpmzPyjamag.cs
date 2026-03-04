using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpmzPyjamag : SpmzPropEater
{
    [Header("Fruit settings")]
    public Fruit fruitPrefab;
    public Transform spawnFruitPos;
    public float giveFruitForceForward = 5f;
    public float giveFruitForceUp = 1f;
    public float percentageChancesToGiveFruit = 10f;
    public float percentageChancesUpOnFail = 15f;
    public float delayBeforeGivingFruit = 0.25f;
    
    private float _basePercentageChancesToGiveFruit;

    protected override void Start()
    {
        base.Start();
        _basePercentageChancesToGiveFruit = percentageChancesToGiveFruit;
    }

    protected override void SwallowObject(CatchableObject catchableObject)
    {
        base.SwallowObject(catchableObject);
        TryToGiveFruit();
        Destroy(catchableObject);
    }
    
    private void TryToGiveFruit()
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= percentageChancesToGiveFruit)
        {
            GiveFruit();
        }
        else
        {
            FailToGiveFruit();
        }
    }

    private void FailToGiveFruit()
    {
        percentageChancesToGiveFruit += percentageChancesUpOnFail;
    }

    private void GiveFruit()
    {
        percentageChancesToGiveFruit = _basePercentageChancesToGiveFruit;
        animator.SetTrigger("DropFruit");
        this.Invoke(delayBeforeGivingFruit, () =>
        {
            Fruit newFruit = Instantiate(fruitPrefab, spawnFruitPos.position, Quaternion.identity);
            newFruit.transform.DOScale(Vector3.one, 0.5f).From(Vector3.one * 0.01f);
            newFruit.rb.isKinematic = false;
            newFruit.rb.AddForce(transform.forward * giveFruitForceForward + Vector3.up * giveFruitForceUp);
        });
    }

}