using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MobileControlsRoot : MonoBehaviour
{
    public bool hideWhenTablet = true;
    public bool hideWhenDialogue = true;
    public bool hideWhenEndGame = true;
    public bool hideWhenSettings = true;
    public bool alwaysVisibleWhenMobile;
    public bool showOnTitleScreen;

    private CanvasGroup _group;
    private bool _lastEnabled;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        bool mobileUiActive = IsMobileUiActive();
        ApplyState(mobileUiActive);
        _lastEnabled = mobileUiActive;
    }

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.IsTitleScreenActive())
        {
            bool shouldShowOnTitle = IsMobileUiActive() && showOnTitleScreen;
            if (_lastEnabled != shouldShowOnTitle)
            {
                _lastEnabled = shouldShowOnTitle;
                ApplyState(shouldShowOnTitle);
            }

            return;
        }

        bool shouldHide = false;
        if (UIGame.Instance != null)
        {
            if (UIGame.Instance.IsBlockingHouseLoadingScreenActive)
                shouldHide = true;

            if (UIGame.Instance.IsCaptureUiHidden)
                shouldHide = true;

            if (!alwaysVisibleWhenMobile && hideWhenTablet && UIGame.Instance.tablet != null && UIGame.Instance.tablet.gameObject.activeSelf)
                shouldHide = true;

            if (hideWhenDialogue && UIGame.Instance.uiDialogue != null && UIGame.Instance.uiDialogue.IsDialogueActive)
                shouldHide = true;

            if (hideWhenEndGame &&
                UIGame.Instance.tablet != null &&
                UIGame.Instance.tablet.endGame != null &&
                UIGame.Instance.tablet.endGame.gameObject.activeSelf)
            {
                shouldHide = true;
            }

            if (!alwaysVisibleWhenMobile &&
                hideWhenSettings &&
                UIGame.Instance.settingsMenu != null &&
                UIGame.Instance.settingsMenu.IsOpen)
            {
                shouldHide = true;
            }
        }

        bool shouldShow = IsMobileUiActive() && !shouldHide;
        if (_lastEnabled == shouldShow)
            return;

        _lastEnabled = shouldShow;
        ApplyState(shouldShow);
    }

    private static bool IsMobileUiActive()
    {
        return MobileInput.Enabled ||
               Application.isMobilePlatform ||
               (GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled);
    }

    private void ApplyState(bool enabled)
    {
        _group.alpha = enabled ? 1f : 0f;
        _group.interactable = enabled;
        _group.blocksRaycasts = enabled;
    }
}
