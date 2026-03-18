using UnityEngine;

[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("If null, uses this RectTransform.")]
    public RectTransform target;

    [Tooltip("Extra padding (pixels) added to the safe area's min side.")]
    public Vector2 extraPaddingMin;

    [Tooltip("Extra padding (pixels) added to the safe area's max side.")]
    public Vector2 extraPaddingMax;

    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.width == 0 || Screen.height == 0)
            return;

        if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height || _lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (target == null)
            target = transform as RectTransform;

        if (target == null)
            return;

        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f)
            return;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x = (min.x + extraPaddingMin.x) / Screen.width;
        min.y = (min.y + extraPaddingMin.y) / Screen.height;
        max.x = (max.x - extraPaddingMax.x) / Screen.width;
        max.y = (max.y - extraPaddingMax.y) / Screen.height;

        min = Vector2.Max(Vector2.zero, min);
        max = Vector2.Min(Vector2.one, max);

        target.anchorMin = min;
        target.anchorMax = max;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;

        _lastSafeArea = safe;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
