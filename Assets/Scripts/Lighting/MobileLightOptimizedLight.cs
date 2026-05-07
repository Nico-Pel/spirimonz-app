using UnityEngine;

[DisallowMultipleComponent]
public class MobileLightOptimizedLight : MonoBehaviour
{
    public enum LightPriority
    {
        PlayerCritical = 0,
        ClickableHigh = 1,
        OtherMedium = 2,
        SpirimonzLow = 3,
        FlammableLow = 4
    }

    public Light targetLight;

    [Header("Overrides")]
    public bool ignoreOptimization;
    public bool allowDisableWhenFar = true;
    public bool useCustomDistances;
    public float nearDistance = 10f;
    public float shadowDisableDistance = 14f;
    public float farDistance = 22f;
    public float disableDistance = 30f;

    private LightShadows _baseShadows;
    private LightRenderMode _baseRenderMode;
    private float _baseRange;
    private bool _baseEnabled;
    private bool _initialized;
    private bool _isOverriding;
    private LightPriority _priority = LightPriority.OtherMedium;
    private bool _disabledByOptimizer;
    private float _budgetDisallowSince = -1f;
    private float _distanceDisableSince = -1f;
    private float _shadowDowngradeSince = -1f;
    private float _renderDowngradeSince = -1f;

    public LightPriority Priority => _priority;
    public bool IsBudgetCritical => _priority == LightPriority.PlayerCritical;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        if (MobileLightOptimizerManager.Instance != null)
            MobileLightOptimizerManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (MobileLightOptimizerManager.Instance != null)
            MobileLightOptimizerManager.Instance.Unregister(this);
    }

    public void Initialize(LightPriority priority)
    {
        _priority = priority;
        CacheBase();
        _initialized = true;
    }

    private void CacheBase()
    {
        if (targetLight == null)
            return;

        _baseShadows = targetLight.shadows;
        _baseRenderMode = targetLight.renderMode;
        _baseRange = targetLight.range;
        _baseEnabled = targetLight.enabled;
    }

    public void SetBaseEnabledState(bool enabled)
    {
        _baseEnabled = enabled;

        if (targetLight == null)
            return;

        if (!_disabledByOptimizer || !_isOverriding)
            targetLight.enabled = enabled;
    }

    public void Restore()
    {
        if (targetLight == null)
            return;

        targetLight.shadows = _baseShadows;
        targetLight.renderMode = _baseRenderMode;
        targetLight.range = _baseRange;

        if (_disabledByOptimizer || allowDisableWhenFar)
            targetLight.enabled = _baseEnabled;

        _isOverriding = false;
        _disabledByOptimizer = false;
        ResetTransitionTimers();
    }

    public void Apply(MobileLightOptimizerManager manager, Vector3 targetPos, Vector3 targetForward)
    {
        if (targetLight == null)
            return;
        if (!targetLight.gameObject.activeInHierarchy)
            return;

        if (!_initialized)
        {
            Initialize(LightPriority.OtherMedium);
        }
        else if (_baseRange <= 0f && targetLight.range > 0f)
        {
            // Some lights get their range set after Awake/OnEnable in House scenes.
            CacheBase();
        }

        if (ignoreOptimization || targetLight.type != LightType.Point)
        {
            Restore();
            return;
        }

        float near = useCustomDistances ? nearDistance : manager.GetNearDistance();
        float shadowDist = useCustomDistances ? shadowDisableDistance : manager.GetShadowDisableDistance();
        float far = useCustomDistances ? farDistance : manager.GetFarDistance();
        float disable = useCustomDistances ? disableDistance : manager.GetDisableDistance();

        Vector3 toLight = targetLight.transform.position - targetPos;
        float distSqr = toLight.sqrMagnitude;
        float nearSqr = near * near;
        float shadowSqr = shadowDist * shadowDist;
        float farSqr = far * far;
        float disableSqr = disable * disable;

        bool isNear = distSqr <= nearSqr;
        float downgradeDelay = manager != null ? manager.GetDowngradeDelay() : 0f;
        float disableDelay = manager != null ? manager.GetDisableOutOfViewDelay() : 0f;

        if (manager != null && !IsBudgetCritical && manager.useLightBudget)
        {
            bool budgetApplies = manager.GetBudgetAffectsNearLights() || !isNear;
            if (budgetApplies && !manager.IsLightBudgetAllowed(this))
            {
                if (HasConditionPersisted(ref _budgetDisallowSince, downgradeDelay))
                {
                    targetLight.enabled = false;
                    _disabledByOptimizer = true;
                    return;
                }
            }
            else
            {
                _budgetDisallowSince = -1f;
            }
        }
        else
        {
            _budgetDisallowSince = -1f;
        }

        if (isNear)
        {
            _baseEnabled = targetLight.enabled;
            Restore();
            return;
        }

        _isOverriding = true;

        if (distSqr > shadowSqr)
        {
            if (HasConditionPersisted(ref _shadowDowngradeSince, downgradeDelay))
                targetLight.shadows = GetOptimizedShadowMode(manager);
            else
                targetLight.shadows = _baseShadows;
        }
        else
        {
            targetLight.shadows = _baseShadows;
            _shadowDowngradeSince = -1f;
        }

        bool keepBaseRenderMode = manager != null && manager.GetKeepBaseRenderMode();
        if (!keepBaseRenderMode && distSqr > farSqr)
        {
            if (HasConditionPersisted(ref _renderDowngradeSince, downgradeDelay))
                targetLight.renderMode = LightRenderMode.Auto;
            else
                targetLight.renderMode = _baseRenderMode;
        }
        else
        {
            targetLight.renderMode = _baseRenderMode;
            _renderDowngradeSince = -1f;
        }

        targetLight.range = GetTargetRange(manager, farSqr, disableSqr, distSqr);

        bool canDisable = allowDisableWhenFar && !IsBudgetCritical && (manager == null || manager.ShouldDisableFarLights());
        if (canDisable)
        {
            float dist = Mathf.Sqrt(distSqr);
            bool inViewCone = false;
            if (dist > 0.001f)
            {
                Vector3 dir = toLight / dist;
                inViewCone = Vector3.Dot(targetForward, dir) >= manager.GetViewDotThreshold();
            }

            if (distSqr > disableSqr && !inViewCone)
            {
                if (HasConditionPersisted(ref _distanceDisableSince, disableDelay))
                {
                    targetLight.enabled = false;
                    _disabledByOptimizer = true;
                    return;
                }
            }
            else
            {
                _distanceDisableSince = -1f;
            }

            targetLight.enabled = _baseEnabled;
            _disabledByOptimizer = false;
        }
        else if (_disabledByOptimizer)
        {
            targetLight.enabled = _baseEnabled;
            _disabledByOptimizer = false;
        }
    }

    private LightShadows GetOptimizedShadowMode(MobileLightOptimizerManager manager)
    {
        if (_baseShadows == LightShadows.None)
            return LightShadows.None;

        if (manager != null && manager.IsHouseScene())
            return _baseShadows;

        return LightShadows.None;
    }

    private float GetTargetRange(MobileLightOptimizerManager manager, float farSqr, float disableSqr, float distSqr)
    {
        if (_baseRange <= 0f)
            return targetLight.range;
        if (manager != null && !manager.ShouldReduceRange())
            return _baseRange;

        if (distSqr <= farSqr || disableSqr <= farSqr || manager == null)
            return _baseRange;

        float dist = Mathf.Sqrt(distSqr);
        float far = Mathf.Sqrt(farSqr);
        float disable = Mathf.Sqrt(disableSqr);
        float t = Mathf.InverseLerp(far, disable, dist);
        float multiplier = manager.GetFarRangeMultiplier();
        return Mathf.Lerp(_baseRange, _baseRange * multiplier, t);
    }

    private bool HasConditionPersisted(ref float sinceTime, float delay)
    {
        if (delay <= 0f)
            return true;

        if (sinceTime < 0f)
        {
            sinceTime = Time.unscaledTime;
            return false;
        }

        return Time.unscaledTime - sinceTime >= delay;
    }

    private void ResetTransitionTimers()
    {
        _budgetDisallowSince = -1f;
        _distanceDisableSince = -1f;
        _shadowDowngradeSince = -1f;
        _renderDowngradeSince = -1f;
    }
}
