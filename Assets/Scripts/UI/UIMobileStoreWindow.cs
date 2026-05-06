using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMobileStoreWindow : MonoBehaviour
{
    [Serializable]
    public class OfferBinding
    {
        public MobileStoreOfferType offerType;
        public Button button;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI stateText;
        public GameObject ownedObject;
        public GameObject unavailableObject;
    }

    public OfferBinding[] offers;
    public Button closeButton;

    private MobileMonetizationManager _store;

    private void Awake()
    {
        _store = MobileMonetizationManager.Instance;
        BindButtons();
    }

    private void OnEnable()
    {
        _store = MobileMonetizationManager.Instance;
        _store.OnStoreStateChanged += RefreshOffers;
        RefreshOffers();
        CancelInvoke(nameof(RefreshOffers));
        InvokeRepeating(nameof(RefreshOffers), 1f, 1f);
    }

    private void OnDisable()
    {
        if (_store != null)
            _store.OnStoreStateChanged -= RefreshOffers;

        CancelInvoke(nameof(RefreshOffers));
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                if (UIGame.Instance != null && UIGame.Instance.tablet != null)
                    UIGame.Instance.tablet.TurnOffTablet();
                else
                    gameObject.SetActive(false);
            });
        }

        if (offers == null)
            return;

        for (int i = 0; i < offers.Length; i++)
        {
            OfferBinding binding = offers[i];
            if (binding == null || binding.button == null)
                continue;

            MobileStoreOfferType capturedType = binding.offerType;
            binding.button.onClick.AddListener(() => OnOfferPressed(capturedType));
        }
    }

    public void RefreshOffers()
    {
        if (_store == null)
            _store = MobileMonetizationManager.Instance;

        if (offers == null)
            return;

        for (int i = 0; i < offers.Length; i++)
        {
            OfferBinding binding = offers[i];
            if (binding == null)
                continue;

            MobileMonetizationManager.OfferViewData data = _store.GetOfferViewData(binding.offerType);

            if (binding.titleText != null)
                binding.titleText.text = data.title;

            if (binding.descriptionText != null)
                binding.descriptionText.text = data.description;

            if (binding.priceText != null)
                binding.priceText.text = data.priceText;

            if (binding.valueText != null)
                binding.valueText.text = data.valueText;

            if (binding.stateText != null)
            {
                if (data.owned)
                    binding.stateText.text = "Owned";
                else if (!data.available)
                    binding.stateText.text = "Unavailable";
                else
                    binding.stateText.text = string.Empty;
            }

            if (binding.ownedObject != null)
                binding.ownedObject.SetActive(data.owned);

            if (binding.unavailableObject != null)
                binding.unavailableObject.SetActive(!data.available);

            if (binding.button != null)
                binding.button.interactable = data.canPurchase;
        }
    }

    private void OnOfferPressed(MobileStoreOfferType offerType)
    {
        if (_store == null)
            _store = MobileMonetizationManager.Instance;

        _store.PurchaseOffer(offerType);
        RefreshOffers();
    }
}
