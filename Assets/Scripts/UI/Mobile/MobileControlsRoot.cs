using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MobileControlsRoot : MonoBehaviour
{
    public bool hideWhenTablet = true;
    public bool hideWhenDialogue = true;
    public bool hideWhenEndGame = true;
    public bool alwaysVisibleWhenMobile;

    private CanvasGroup _group;
    private bool _lastEnabled;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        ApplyState(MobileInput.Enabled);
        _lastEnabled = MobileInput.Enabled;
    }

    private void Update()
    {
        bool tabletActive = false;
        if (!alwaysVisibleWhenMobile && hideWhenTablet && UIGame.Instance != null)
        {
            if (UIGame.Instance.tablet != null && UIGame.Instance.tablet.gameObject.activeSelf)
                tabletActive = true;
        }

        bool shouldShow = MobileInput.Enabled && !tabletActive;
        if (_lastEnabled == shouldShow)
            return;

        _lastEnabled = shouldShow;
        ApplyState(shouldShow);
    }

    private void ApplyState(bool enabled)
    {
        _group.alpha = enabled ? 1f : 0f;
        _group.interactable = enabled;
        _group.blocksRaycasts = enabled;
    }
}
