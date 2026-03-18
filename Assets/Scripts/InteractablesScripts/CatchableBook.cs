using DG.Tweening;
using UnityEngine;

public class CatchableBook : CatchableObject
{
    [Header("Book Visuals")]
    public SkinnedMeshRenderer bookRenderer;
    public string openBlendShapeName = "Open";
    public float closedBlendshapeWeight = 0f; // Unity blendshape weight is usually 0..100
    public float openBlendshapeWeight = 100f;
    public float openCloseDuration = 0.25f;
    public Ease openCloseEase = Ease.OutCubic;
    public bool startOpen;

    [Header("Collider Settings")]
    public BoxCollider bookCollider;
    public Vector3 closedColliderCenter = Vector3.zero;
    public Vector3 closedColliderSize = Vector3.one;
    public Vector3 openColliderCenter = Vector3.zero;
    public Vector3 openColliderSize = Vector3.one;
    public bool previewOpenCollider;
    public bool drawColliderGizmos = true;
    public Color closedGizmoColor = new Color(0.2f, 0.9f, 1f, 0.25f);
    public Color openGizmoColor = new Color(1f, 0.6f, 0.2f, 0.25f);

    private int _openBlendShapeIndex = -1;
    private bool _isOpen;
    private Tween _blendTween;

    private void Awake()
    {
        if (bookRenderer != null)
            _openBlendShapeIndex = bookRenderer.sharedMesh != null
                ? bookRenderer.sharedMesh.GetBlendShapeIndex(openBlendShapeName)
                : -1;

        _isOpen = startOpen;
        ApplyOpenState(_isOpen, instant: true);
    }

    private void OnValidate()
    {
        if (bookRenderer != null && bookRenderer.sharedMesh != null)
            _openBlendShapeIndex = bookRenderer.sharedMesh.GetBlendShapeIndex(openBlendShapeName);

        if (!Application.isPlaying)
            ApplyCollider(previewOpenCollider);
    }

    public override void SpecialActionInHandsOnClick()
    {
        ToggleOpen();
    }

    public override void OnThrow()
    {
        base.OnThrow();
        TryToggleOnThrow();
    }

    public void OnGhostThrow()
    {
        TryToggleOnThrow();
    }

    private void TryToggleOnThrow()
    {
        if (!_isOpen)
        {
            if (Random.value <= 0.5f)
                SetOpen(true);
        }
        else
        {
            if (Random.value <= 0.2f)
                SetOpen(false);
        }
    }

    private void ToggleOpen()
    {
        SetOpen(!_isOpen);
    }

    private void SetOpen(bool open)
    {
        if (_isOpen == open)
            return;

        _isOpen = open;
        ApplyOpenState(_isOpen, instant: false);
    }

    private void ApplyOpenState(bool open, bool instant)
    {
        float targetWeight = open ? openBlendshapeWeight : closedBlendshapeWeight;
        ApplyBlendshape(targetWeight, instant);
        ApplyCollider(open);
    }

    private void ApplyBlendshape(float targetWeight, bool instant)
    {
        if (bookRenderer == null || _openBlendShapeIndex < 0)
            return;

        _blendTween?.Kill();

        if (instant)
        {
            bookRenderer.SetBlendShapeWeight(_openBlendShapeIndex, targetWeight);
            return;
        }

        _blendTween = DOTween.To(
                () => bookRenderer.GetBlendShapeWeight(_openBlendShapeIndex),
                v => bookRenderer.SetBlendShapeWeight(_openBlendShapeIndex, v),
                targetWeight,
                openCloseDuration)
            .SetEase(openCloseEase);
    }

    private void ApplyCollider(bool open)
    {
        if (bookCollider == null)
            return;

        bookCollider.center = open ? openColliderCenter : closedColliderCenter;
        bookCollider.size = open ? openColliderSize : closedColliderSize;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawColliderGizmos || bookCollider == null)
            return;

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = bookCollider.transform.localToWorldMatrix;

        Gizmos.color = closedGizmoColor;
        Gizmos.DrawWireCube(closedColliderCenter, closedColliderSize);

        Gizmos.color = openGizmoColor;
        Gizmos.DrawWireCube(openColliderCenter, openColliderSize);

        Gizmos.matrix = previous;
    }
}
