using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class UIInAppButton : MonoBehaviour
{
    public InApp inApp;
    public Button button;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI stateText;
    public GameObject ownedObject;
    public GameObject unavailableObject;

    private MobileMonetizationManager _store;
    private readonly List<Button> _wiredButtons = new List<Button>();

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        WireButtons();

#if UNITY_EDITOR
        Debug.Log($"Pouet UIInAppButton.Awake name='{name}' wiredButtons={_wiredButtons.Count} hasAssignedButton={button != null}");
#endif
    }

    private void OnEnable()
    {
        _store = MobileMonetizationManager.Instance;
        if (_store != null)
        {
            _store.RegisterInApp(inApp);
            _store.OnStoreStateChanged += RefreshView;
        }

#if UNITY_EDITOR
        Debug.Log($"Pouet UIInAppButton.OnEnable name='{name}' hasStore={_store != null} hasInApp={inApp != null}");
#endif

        RefreshView();
    }

    private void OnDisable()
    {
        if (_store != null)
            _store.OnStoreStateChanged -= RefreshView;
    }

    public void RefreshView()
    {
        if (_store == null)
            _store = MobileMonetizationManager.Instance;

        if (_store == null || inApp == null)
            return;

        MobileMonetizationManager.OfferViewData data = _store.GetInAppViewData(inApp);

        if (titleText != null)
            titleText.text = data.title;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (priceText != null)
            priceText.text = data.priceText;

        if (valueText != null)
            valueText.text = data.valueText;

        if (stateText != null)
        {
            if (data.owned)
                stateText.text = "Owned";
            else if (!data.available)
                stateText.text = "Unavailable";
            else
                stateText.text = string.Empty;
        }

        if (ownedObject != null)
            ownedObject.SetActive(data.owned);

        if (unavailableObject != null)
            unavailableObject.SetActive(!data.available);

        for (int i = 0; i < _wiredButtons.Count; i++)
        {
            if (_wiredButtons[i] != null)
                _wiredButtons[i].interactable = data.canPurchase;
        }
    }

    private void WireButtons()
    {
        _wiredButtons.Clear();
        AddWiredButton(button);

        Button[] childButtons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < childButtons.Length; i++)
            AddWiredButton(childButtons[i]);
    }

    private void AddWiredButton(Button candidate)
    {
        if (candidate == null || _wiredButtons.Contains(candidate))
            return;

        candidate.onClick.RemoveListener(OnPressed);
        candidate.onClick.AddListener(OnPressed);
        _wiredButtons.Add(candidate);
    }

    private void OnPressed()
    {
        if (_store == null)
            _store = MobileMonetizationManager.Instance;

#if UNITY_EDITOR
        Debug.Log($"Pouet UIInAppButton.OnPressed name='{name}' hasStore={_store != null} hasInApp={inApp != null}");
#endif

        if (_store == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Pouet UIInAppButton store instance is missing.");
#endif
            return;
        }

        if (inApp == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Pouet UIInAppButton no InApp asset assigned on button '{name}'.");
#endif
            return;
        }

        bool purchaseTriggered = _store.PurchaseInApp(inApp);
#if UNITY_EDITOR
        Debug.Log($"Pouet UIInAppButton purchaseTriggered={purchaseTriggered} inApp='{inApp.name}'");
#endif
        RefreshView();
    }
}
