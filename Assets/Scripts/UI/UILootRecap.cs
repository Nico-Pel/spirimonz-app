using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UILootRecap : GameBehaviour
{
    public TextMeshProUGUI tName;
    public TextMeshProUGUI tValue;

    public void Init(Article article, Color valueTextColor)
    {
        tName.text = article.articleName;
        tValue.text = article.value + "$";
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}