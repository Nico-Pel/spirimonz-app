using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIArticleShop : MonoBehaviour
{
    [Header("Data")]
    public Article article;

    [Header("Refs")] 
    public Image iArticle;
    public TextMeshProUGUI tName;
    public Button bReward;
    public TextMeshProUGUI tRewardAd;
    public Button bTicket;
    public TextMeshProUGUI tTicketCount;
    public TextMeshProUGUI tRewardTicket;
    public Button bGo;
    public TextMeshProUGUI tPrice;
    public GameObject iDone;
    public TextMeshProUGUI tNotFound;

    [Header("State Colors")]
    public Color claimedTextColor = new Color(0.7137255f, 0.9490196f, 0.78431374f, 1f);

    private Color _notFoundBaseColor;
    private bool _notFoundBaseColorCached;

    private void Awake()
    {
        EnsureReferences();
    }

    public Article Article => article;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    public void SetCallbacks(Action onBaseClaim, Action onRewardClaim, Action onTicketClaim)
    {
        EnsureReferences();

        if (bGo != null)
        {
            bGo.onClick.RemoveAllListeners();
            if (onBaseClaim != null)
                bGo.onClick.AddListener(() => onBaseClaim());
        }

        if (bReward != null)
        {
            bReward.onClick.RemoveAllListeners();
            if (onRewardClaim != null)
                bReward.onClick.AddListener(() => onRewardClaim());
        }

        if (bTicket != null)
        {
            bTicket.onClick.RemoveAllListeners();
            if (onTicketClaim != null)
                bTicket.onClick.AddListener(() => onTicketClaim());
        }
    }

    public void SetVisualState(
        string articleName,
        int baseReward,
        int doubledReward,
        bool found,
        bool claimed,
        bool canClaim,
        bool showStateText,
        string stateText,
        Color stateTextColor,
        string ticketCountText,
        bool showRewardButton,
        bool rewardInteractable,
        bool showTicketButton,
        bool ticketInteractable)
    {
        EnsureReferences();

        if (tName != null)
            tName.text = articleName;

        if (tPrice != null)
            tPrice.text = $"{baseReward}#";

        if (tRewardAd != null)
            tRewardAd.text = $"{doubledReward}#";

        if (tRewardTicket != null)
            tRewardTicket.text = $"{doubledReward}#";

        if (tTicketCount != null)
            tTicketCount.text = ticketCountText;

        if (iDone != null)
            iDone.SetActive(found);

        if (tNotFound != null)
        {
            CacheNotFoundBaseColor();
            tNotFound.gameObject.SetActive(showStateText);
            tNotFound.text = stateText;
            tNotFound.color = stateTextColor;
        }

        if (bGo != null)
        {
            bGo.gameObject.SetActive(!claimed && found);
            bGo.interactable = canClaim;
        }

        if (bReward != null)
        {
            bReward.gameObject.SetActive(showRewardButton);
            bReward.interactable = rewardInteractable;
        }

        if (bTicket != null)
        {
            bTicket.gameObject.SetActive(showTicketButton);
            bTicket.interactable = ticketInteractable;
        }
    }

    private void EnsureReferences()
    {
        if (tName == null)
            tName = FindText(transform, "tName");

        if (bReward == null)
            bReward = FindButton(transform, "BReward");
        if (tRewardAd == null && bReward != null)
            tRewardAd = FindText(bReward.transform, "tReward");

        if (bTicket == null)
            bTicket = FindButton(transform, "BTicket");
        if (tTicketCount == null && bTicket != null)
            tTicketCount = FindText(bTicket.transform, "tCount");
        if (tRewardTicket == null && bTicket != null)
            tRewardTicket = FindText(bTicket.transform, "tReward");

        if (bGo == null)
            bGo = FindButton(transform, "BGo");
        if (tPrice == null && bGo != null)
            tPrice = FindText(bGo.transform, "tPrice");

        if (iDone == null)
            iDone = FindObject(transform, "iDone");

        if (tNotFound == null)
            tNotFound = FindText(transform, "tNotFound");

        if (iArticle != null)
        {
            iArticle.sprite = article != null ? article.image : null;
        }
    }

    private void CacheNotFoundBaseColor()
    {
        if (_notFoundBaseColorCached || tNotFound == null)
            return;

        _notFoundBaseColor = tNotFound.color;
        _notFoundBaseColorCached = true;
    }

    public Color GetNotFoundBaseColor()
    {
        CacheNotFoundBaseColor();
        return _notFoundBaseColorCached ? _notFoundBaseColor : Color.white;
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
