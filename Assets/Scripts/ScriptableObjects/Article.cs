using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Article", menuName = "Article")]
public class Article : ScriptableObject
{
    public Sprite image;
    public float winValueMultiplier = 2;
    public string articleName;
    public int value = 10;
    [Min(0)] public int weeklyHarvestReward = 0;

    public string GetLocalizedName()
    {
        return LocalizationManager.Get(LocalizationKeys.ArticleName(this), articleName);
    }

    public int GetWeeklyHarvestReward()
    {
        return Mathf.Max(0, weeklyHarvestReward);
    }
}
