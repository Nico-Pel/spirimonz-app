using UnityEngine;

[DisallowMultipleComponent]
public class MobileLightOptimizedLight : MonoBehaviour
{
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
    private bool _gameplayLight;
    private bool _disabledByOptimizer;

    public bool IsGameplayLight => _gameplayLight;

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

    public void Initialize(bool gameplayLight)
    {
        _gameplayLight = gameplayLight;
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

        if (_disabledByOptimizer || (allowDisableWhenFar && !_gameplayLight))
            targetLight.enabled = _baseEnabled;

        _isOverriding = false;
        _disabledByOptimizer = false;
    }

    public void Apply(MobileLightOptimizerManager manager, Vector3 targetPos, Vector3 targetForward)
    {
        if (targetLight == null)
            return;
        if (!targetLight.gameObject.activeInHierarchy)
            return;

        if (!_initialized)
        {
            Initialize(gameplayLight: false);
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

        float near = useCustomDistances ? nearDistance : manager.nearDistance;
        float shadowDist = useCustomDistances ? shadowDisableDistance : manager.shadowDisableDistance;
        float far = useCustomDistances ? farDistance : manager.farDistance;
        float disable = useCustomDistances ? disableDistance : manager.disableDistance;

        Vector3 toLight = targetLight.transform.position - targetPos;
        float distSqr = toLight.sqrMagnitude;
        float nearSqr = near * near;
        float shadowSqr = shadowDist * shadowDist;
        float farSqr = far * far;
        float disableSqr = disable * disable;

        bool isNear = distSqr <= nearSqr;

        if (manager != null && !_gameplayLight && manager.useLightBudget)
        {
            bool budgetApplies = manager.budgetAffectsNearLights || !isNear;
            if (budgetApplies && !manager.IsLightBudgetAllowed(this))
            {
                targetLight.enabled = false;
                _disabledByOptimizer = true;
                return;
            }
        }

        if (isNear)
        {
            _baseEnabled = targetLight.enabled;
            Restore();
            return;
        }

        _isOverriding = true;

        if (distSqr > shadowSqr)
            targetLight.shadows = LightShadows.None;
        else
            targetLight.shadows = _baseShadows;

        if (distSqr > farSqr)
            targetLight.renderMode = LightRenderMode.Auto;
        else
            targetLight.renderMode = _baseRenderMode;

        bool canDisable = allowDisableWhenFar && !_gameplayLight;
        if (canDisable)
        {
            float dist = Mathf.Sqrt(distSqr);
            bool inViewCone = false;
            if (dist > 0.001f)
            {
                Vector3 dir = toLight / dist;
                inViewCone = Vector3.Dot(targetForward, dir) >= manager.viewDotThreshold;
            }

            if (distSqr > disableSqr && !inViewCone)
            {
                targetLight.enabled = false;
                _disabledByOptimizer = true;
                return;
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
}
