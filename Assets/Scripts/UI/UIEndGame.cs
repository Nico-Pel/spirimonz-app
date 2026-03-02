using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIEndGame : GameBehaviour
{
    public enum EndTypes
    {
        Win,
        Escape,
        Lose
    }
    
    [Header("Spirit info text")]
    public string spiritVictoryText = "You have captured a {0} Spirimonz!";
    public string spiritEscapeText = "You have fled from a {0} spirit...";
    public string spiritLoseText = "A {0} spirit knocked you out...";
    
    [Header("Loot info text")]
    public string lootVictoryText = "Your discoveries have gained value thanks to your achievements!";
    public string lootEscapeText = "Your loot did not increase in value following a victory...";
    public string lootLoseText = "The items found on site were lost...";
    public Color normalTextColor = Color.white;
    public Color winTextColor = Color.yellow;
    
    [Header("Components")]
    public TextMeshProUGUI tHouseName;
    public TextMeshProUGUI tSubtitle;
    public TextMeshProUGUI tTotal;

    public TextMeshProUGUI tLootInfo;

    public Transform uiLootPos;
    public UILootRecap uiLootPrefab;
    
    public Button bContinue;
    public float uninteractableTime = 3f;

    public void SetTexts(EndTypes endType, House house)
    {
        tHouseName.text = house.map.houseName;
        
        string victoryText = null;
        switch (endType)
        {
            case EndTypes.Win:
                victoryText = spiritVictoryText;
                tLootInfo.text = lootVictoryText;
                tLootInfo.color = winTextColor;
                break;
            case EndTypes.Escape:
                victoryText = spiritEscapeText;
                tLootInfo.text = lootEscapeText;
                tLootInfo.color = normalTextColor;
                break;
            case EndTypes.Lose:
                victoryText = spiritLoseText;
                tLootInfo.text = lootLoseText;
                tLootInfo.color = normalTextColor;
                break;
        }
        tSubtitle.text = string.Format(victoryText, house.currentGhost.ghostParameters.ghostTypeData.ghostType.ToString());

        int totalValue = 0;
        bool isWin = endType == EndTypes.Win;

        List<Article> articlesFound = house.currentPlayer.inventoryManager.articlesFoundInGame;

// 1️⃣ Regroup article
        Dictionary<Article, int> groupedArticles = new Dictionary<Article, int>();

        foreach (var article in articlesFound)
        {
            if (groupedArticles.ContainsKey(article))
                groupedArticles[article]++;
            else
                groupedArticles.Add(article, 1);
        }
        
        foreach (var pair in groupedArticles)
        {
            Article article = pair.Key;
            int quantity = pair.Value;

            UILootRecap newUILoot = Instantiate(uiLootPrefab, uiLootPos);

            Color valueColorToUse =
                isWin && article.winValueMultiplier > 1
                    ? winTextColor
                    : normalTextColor;

            int unitValue = isWin
                ? (int)(article.value * article.winValueMultiplier)
                : article.value;

            int totalArticleValue = unitValue * quantity;

            if (totalArticleValue == -1) //Is the Victory article
            {
                totalArticleValue = house.map.victoryReward;
            }

            newUILoot.Init(article, quantity, totalArticleValue, valueColorToUse);

            totalValue += totalArticleValue;
        }

        tTotal.text = totalValue + "$";
        GameManager.Instance.AddMoney(totalValue);

        bContinue.interactable = false;
        this.Invoke(uninteractableTime, () => bContinue.interactable = true);
        bContinue.onClick.AddListener(() =>
        {
            house.houseEntry.Entry(house.currentPlayer, endType == EndTypes.Lose);
            house.currentPlayer.inventoryManager.articlesFoundInGame.Clear();
            UIGame.Instance.CloseAllWindows();
        });
    }
}