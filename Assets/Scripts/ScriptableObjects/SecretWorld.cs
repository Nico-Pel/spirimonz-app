using UnityEngine;

[CreateAssetMenu(fileName = "SecretWorld", menuName = "Worlds/Secret World")]
public class SecretWorld : ScriptableObject
{
    public string worldName;
    public Sprite worldImage;
    public int travelPrice;
    public string sceneName;

    [Header("House Entry Pricing")]
    public int houseEntryPriceIncrease = 10;
    public int maxHouseEntryPriceIncreases = 5;
}
