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

    [Header("Drop Limit")]
    public int maxDrops = 20;

    [Header("Rotation Safety")]
    public bool lockRotationToYAxis = true;
    
    private float _basePercentageChancesToGiveFruit;
    private int _dropsCount;

    protected override void Start()
    {
        base.Start();
        _basePercentageChancesToGiveFruit = percentageChancesToGiveFruit;
    }

    private void LateUpdate()
    {
        if (!lockRotationToYAxis)
            return;

        // In hands, the hand anchor owns the pose. Do not flatten world rotation there.
        if (!isOnTheMap)
            return;

        Vector3 euler = transform.rotation.eulerAngles;
        if (Mathf.Abs(euler.x) > 0.01f || Mathf.Abs(euler.z) > 0.01f)
        {
            euler.x = 0f;
            euler.z = 0f;
            transform.rotation = Quaternion.Euler(euler);
        }
    }

    protected override void SwallowObject(CatchableObject catchableObject)
    {
        base.SwallowObject(catchableObject);
        TryToGiveFruit();
        Destroy(catchableObject);
    }
    
    private void TryToGiveFruit()
    {
        if (maxDrops > 0 && _dropsCount >= maxDrops)
            return;

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
            if (maxDrops > 0 && _dropsCount >= maxDrops)
                return;

            Vector3 force = transform.forward * giveFruitForceForward + Vector3.up * giveFruitForceUp;
            Fruit newFruit = SpmzDropUtility.SpawnDrop(fruitPrefab, spawnFruitPos.position, Quaternion.identity, null, force, Vector3.zero);
            if (newFruit != null)
            {
                _dropsCount++;
                newFruit.transform.DOScale(Vector3.one, 0.5f).From(Vector3.one * 0.01f);
            }
        });
    }

}
