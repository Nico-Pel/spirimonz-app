using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Article", menuName = "Article")]
public class Article : ScriptableObject
{
    public float winValueMultiplier = 2;
    public string articleName;
    public int value = 10;
}
