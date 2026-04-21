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
        bool shouldHide = false;
        if (UIGame.Instance != null)
        {
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
        }

        bool shouldShow = MobileInput.Enabled && !shouldHide;
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
