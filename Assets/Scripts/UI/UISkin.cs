using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UISkin : MonoBehaviour
{
    private const string PurchaseKeyPrefix = "ui.skin_pack.purchased.";

    [Header("Data")]
    public string packId;
    public string packName;
    [TextArea(2, 6)] public string packDescription;
    [Min(0)] public int price;
    public SpirimonzSettings[] spirimonzSkinsToUnlock;

    [Header("Refs")]
    public TextMeshProUGUI tTitle;
    public TextMeshProUGUI tDescription;
    public Image[] previewImages;
    public Button bBuy;
    public TextMeshProUGUI tPrice;
    public GameObject doneOverlay;

    [Header("Visuals")]
    public Color priceOkColor = new Color(0.13333334f, 0.2901961f, 0.23529412f, 1f);
    public Color priceNotEnoughColor = Color.red;

    [Header("Sound")]
    public SoundParameters purchaseSound;

    private GameManager _gameManager;
    private bool _subscribedToMoneyEvent;
    private bool _buttonHooked;
    private Color _priceBaseColor;
    private bool _priceBaseColorCached;

    private void Awake()
    {
        EnsureReferences();
        EnsureSoundDefaults();
        HookButton();
    }

    private void OnEnable()
    {
        _gameManager = GameManager.Instance;
        HookButton();
        SubscribeToMoneyUpdates();
        Refresh();
    }

    private void OnDisable()
    {
        if (_gameManager != null && _subscribedToMoneyEvent)
        {
            _gameManager.onMoneyUpdated.RemoveListener(Refresh);
            _subscribedToMoneyEvent = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
        EnsureSoundDefaults();
        CachePriceBaseColor();
    }
#endif

    public void Refresh()
    {
        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        EnsureReferences();
        CachePriceBaseColor();

        if (tTitle != null)
            tTitle.text = LocalizationManager.Get(GetPackNameLocalizationKey(), packName);
        if (tDescription != null)
            tDescription.text = LocalizationManager.Get(GetPackDescriptionLocalizationKey(), packDescription);

        bool purchased = IsPurchased();
        if (purchased && _gameManager != null)
            PersistPurchasedFlagIfNeeded();

        bool canAfford = _gameManager != null && _gameManager.CanBuy(price);

        if (tPrice != null)
        {
            tPrice.text = $"{Mathf.Max(0, price)}#";
            tPrice.color = purchased
                ? (_priceBaseColorCached ? _priceBaseColor : priceOkColor)
                : (canAfford ? priceOkColor : priceNotEnoughColor);
        }

        if (bBuy != null)
        {
            bBuy.gameObject.SetActive(!purchased);
            bBuy.interactable = !purchased && canAfford;
        }

        if (doneOverlay != null)
            doneOverlay.SetActive(purchased);
    }

    private void TryBuy()
    {
        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        if (_gameManager == null || IsPurchased())
        {
            Refresh();
            return;
        }

        if (!_gameManager.Buy(price))
        {
            Refresh();
            return;
        }

        UnlockConfiguredSkins();
        PersistPurchasedFlagIfNeeded(forceValue: true);
        purchaseSound?.PlaySound();
        Refresh();
    }

    private void UnlockConfiguredSkins()
    {
        if (_gameManager == null || spirimonzSkinsToUnlock == null)
            return;

        for (int i = 0; i < spirimonzSkinsToUnlock.Length; i++)
        {
            SpirimonzSettings settings = spirimonzSkinsToUnlock[i];
            if (settings == null || string.IsNullOrWhiteSpace(settings.spirimonzID))
                continue;

            _gameManager.UnlockSpirimonzSkin(settings.spirimonzID);
            _gameManager.player?.inventoryManager?.RefreshSpirimonzSkin(settings);
        }
    }

    private bool IsPurchased()
    {
        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        if (_gameManager == null)
            return false;

        if (_gameManager.GetBool(GetPurchaseKey(), false))
            return true;

        if (spirimonzSkinsToUnlock == null || spirimonzSkinsToUnlock.Length == 0)
            return false;

        bool hasConfiguredSkin = false;
        for (int i = 0; i < spirimonzSkinsToUnlock.Length; i++)
        {
            SpirimonzSettings settings = spirimonzSkinsToUnlock[i];
            if (settings == null || string.IsNullOrWhiteSpace(settings.spirimonzID))
                continue;

            hasConfiguredSkin = true;
            if (!_gameManager.IsSpirimonzSkinUnlocked(settings.spirimonzID))
                return false;
        }

        return hasConfiguredSkin;
    }

    private void PersistPurchasedFlagIfNeeded(bool? forceValue = null)
    {
        if (_gameManager == null)
            return;

        bool value = forceValue ?? IsPurchased();
        if (_gameManager.GetBool(GetPurchaseKey(), false) == value)
            return;

        _gameManager.SetBool(GetPurchaseKey(), value);
    }

    private string GetPurchaseKey()
    {
        string resolvedPackId = string.IsNullOrWhiteSpace(packId) ? gameObject.name : packId.Trim();
        return PurchaseKeyPrefix + resolvedPackId;
    }

    private string GetPackNameLocalizationKey()
    {
        string resolvedPackId = string.IsNullOrWhiteSpace(packId) ? gameObject.name : packId.Trim();
        return $"skin_pack.{resolvedPackId}.name";
    }

    private string GetPackDescriptionLocalizationKey()
    {
        string resolvedPackId = string.IsNullOrWhiteSpace(packId) ? gameObject.name : packId.Trim();
        return $"skin_pack.{resolvedPackId}.description";
    }

    private void SubscribeToMoneyUpdates()
    {
        if (_gameManager == null || _subscribedToMoneyEvent)
            return;

        _gameManager.onMoneyUpdated.AddListener(Refresh);
        _subscribedToMoneyEvent = true;
    }

    private void HookButton()
    {
        if (bBuy == null || _buttonHooked)
            return;

        bBuy.onClick.RemoveListener(TryBuy);
        bBuy.onClick.AddListener(TryBuy);
        _buttonHooked = true;
    }

    private void EnsureSoundDefaults()
    {
        UISoundDefaults.AssignIfNull(ref purchaseSound);
        UISoundDefaults.MarkAsUi(purchaseSound);
    }

    private void CachePriceBaseColor()
    {
        if (_priceBaseColorCached || tPrice == null)
            return;

        _priceBaseColor = tPrice.color;
        _priceBaseColorCached = true;

        if (priceOkColor == default)
            priceOkColor = _priceBaseColor;
    }

    private void EnsureReferences()
    {
        if (tTitle == null)
            tTitle = FindText(transform, "tTitle");

        if (tDescription == null)
            tDescription = FindText(transform, "tDescription");

        if (bBuy == null)
            bBuy = FindButton(transform, "BGo");

        if (tPrice == null && bBuy != null)
            tPrice = FindText(bBuy.transform, "tPrice");

        if (doneOverlay == null)
            doneOverlay = FindObject(transform, "Done");
    }

    private static Button FindButton(Transform root, string name)
    {
        GameObject go = FindObject(root, name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static TextMeshProUGUI FindText(Transform root, string name)
    {
        GameObject go = FindObject(root, name);
        return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
    }

    private static GameObject FindObject(Transform root, string name)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == name)
                return children[i].gameObject;
        }

        return null;
    }
}
