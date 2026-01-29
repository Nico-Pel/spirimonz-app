using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ApparitionChances : MonoBehaviour
{
    public float apparitionPercentageChances = 15f;

    private void Start()
    {
        if(!ShouldAppears())
            gameObject.SetActive(false);
    }

    private bool ShouldAppears()
    {
        float roll = Random.Range(0f, 100f);
        return roll <= apparitionPercentageChances;
    }
}
