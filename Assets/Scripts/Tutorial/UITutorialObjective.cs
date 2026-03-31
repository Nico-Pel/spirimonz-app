using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITutorialObjective : GameBehaviour
{
    public GameObject root;
    public RectTransform bounceTarget;
    public TextMeshProUGUI tTitle;
    public TextMeshProUGUI tProgress;
    public Graphic progressBox;

    [Header("Colors")]
    public Color successColor = new Color(0.35f, 1f, 0.35f, 1f);
    public bool lockProgressColor = true;

    [Header("Bounce")]
    public float bounceScale = 1.08f;
    public float bounceDuration = 0.25f;
    public float loopScale = 1.05f;
    public float loopDuration = 0.6f;
    public float loopIntervalMultiplier = 3f;

    private Vector3 _baseScale = Vector3.one;
    private Tween _attentionTween;
    private Color _baseTitleColor;
    private Color _baseProgressColor;
    private Color _baseProgressBoxColor;
    private bool _colorsCached;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (bounceTarget == null)
            bounceTarget = transform as RectTransform;

        if (bounceTarget != null)
            _baseScale = bounceTarget.localScale;

        ResolveProgressBox();
        CacheBaseColors();
    }

    public void ShowObjective(string title, int current, int goal)
    {
        if (root != null && !root.activeSelf)
            root.SetActive(true);

        if (tTitle != null)
            tTitle.text = title;

        if (tProgress != null)
        {
            tProgress.gameObject.SetActive(true);
            tProgress.text = $"{current}/{goal}";
        }

        SetSuccessStyle(false);
        StopAttentionLoop();
    }

    public void ShowMessage(string title, bool showProgress = false)
    {
        if (root != null && !root.activeSelf)
            root.SetActive(true);

        if (tTitle != null)
            tTitle.text = title;

        if (tProgress != null)
            tProgress.gameObject.SetActive(showProgress);

        SetSuccessStyle(false);
        StopAttentionLoop();
    }

    public void SetProgress(int current, int goal)
    {
        if (tProgress == null)
            return;

        tProgress.text = $"{current}/{goal}";
    }

    public void ShowReturnToNpc(string text)
    {
        ShowCompletionCTA(text, true);
    }

    public void ShowCompletionCTA(string text, bool loop)
    {
        if (root != null && !root.activeSelf)
            root.SetActive(true);

        if (tTitle != null)
            tTitle.text = text;

        if (tProgress != null)
            tProgress.gameObject.SetActive(true);

        SetSuccessStyle(true);

        if (loop)
            StartAttentionLoop();
        else
            StopAttentionLoop();
    }

    public void BounceOnce(bool resumeLoopAfter = false)
    {
        if (bounceTarget == null)
            return;

        bounceTarget.DOKill();
        bounceTarget.localScale = _baseScale;
        Tween tween = bounceTarget
            .DOScale(_baseScale * bounceScale, bounceDuration)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo);

        if (resumeLoopAfter)
            tween.OnComplete(StartAttentionLoop);
    }

    public void StartAttentionLoop()
    {
        if (bounceTarget == null)
            return;

        StopAttentionLoop();
        bounceTarget.localScale = _baseScale;
        float interval = Mathf.Max(0f, loopDuration * Mathf.Max(0.01f, loopIntervalMultiplier));
        Sequence seq = DOTween.Sequence();
        seq.Append(bounceTarget.DOScale(_baseScale * bounceScale, bounceDuration).SetEase(Ease.OutBack));
        seq.Append(bounceTarget.DOScale(_baseScale, bounceDuration).SetEase(Ease.OutBack));
        if (interval > 0f)
            seq.AppendInterval(interval);
        seq.SetLoops(-1);
        _attentionTween = seq;
    }

    public void StopAttentionLoop()
    {
        if (_attentionTween != null)
        {
            _attentionTween.Kill();
            _attentionTween = null;
        }

        if (bounceTarget != null)
            bounceTarget.localScale = _baseScale;
    }

    public void Hide()
    {
        StopAttentionLoop();
        if (root != null)
            root.SetActive(false);
    }

    private void CacheBaseColors()
    {
        if (_colorsCached)
            return;

        if (tTitle != null)
            _baseTitleColor = tTitle.color;

        if (tProgress != null)
            _baseProgressColor = tProgress.color;

        if (progressBox != null)
            _baseProgressBoxColor = progressBox.color;

        _colorsCached = true;
    }

    private void ResolveProgressBox()
    {
        if (tProgress == null)
            return;

        if (progressBox != null && progressBox != tProgress)
            return;

        Image parentImage = tProgress.GetComponentInParent<Image>();
        if (parentImage != null && parentImage != tProgress)
        {
            progressBox = parentImage;
            return;
        }

        if (progressBox == tProgress)
            progressBox = null;
    }

    private void LateUpdate()
    {
        if (!lockProgressColor || tProgress == null || !_colorsCached)
            return;

        if (tProgress.color != _baseProgressColor)
            tProgress.color = _baseProgressColor;
    }

    private void SetSuccessStyle(bool success)
    {
        CacheBaseColors();

        if (tTitle != null)
            tTitle.color = success ? successColor : _baseTitleColor;

        if (progressBox != null && progressBox != tProgress)
            progressBox.color = success ? successColor : _baseProgressBoxColor;
    }
}
