using UnityEngine;
using TMPro;

public class UILootRecap : GameBehaviour
{
    public TextMeshProUGUI tName;
    public TextMeshProUGUI tValue;

    public void Init(Article article, int quantity, int totalValue, Color valueTextColor)
    {
        string articleName = article.GetLocalizedName();
        tName.text = quantity > 1
            ? $"{articleName} x{quantity}"
            : articleName;

        tValue.text = totalValue + "#";
        tValue.color = valueTextColor;
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
