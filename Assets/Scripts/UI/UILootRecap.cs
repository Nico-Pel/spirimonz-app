using UnityEngine;
using TMPro;

public class UILootRecap : GameBehaviour
{
    public TextMeshProUGUI tName;
    public TextMeshProUGUI tValue;

    public void Init(Article article, int quantity, int totalValue, Color valueTextColor)
    {
        tName.text = quantity > 1
            ? $"{article.articleName} x{quantity}"
            : article.articleName;

        tValue.text = totalValue + "$";
        tValue.color = valueTextColor;
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}