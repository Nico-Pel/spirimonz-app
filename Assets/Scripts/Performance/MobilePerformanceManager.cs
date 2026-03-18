using UnityEngine;

public class MobilePerformanceManager : MonoBehaviour
{
    public static MobilePerformanceManager Instance { get; private set; }

    public enum PerfLevel
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    [Header("State")]
    public bool autoAdjust = true;
    public PerfLevel startLevel = PerfLevel.Medium;

    [Header("FPS Detection")]
    public float checkInterval = 0.5f;
    public float fpsSmoothing = 0.1f;
    public float downgradeHoldTime = 2f;
    public float upgradeHoldTime = 4f;
    public float changeCooldown = 4f;

    [Header("Thresholds (FPS)")]
    public float downThresholdHigh = 52f;
    public float upThresholdHigh = 58f;
    public float downThresholdMed = 42f;
    public float upThresholdMed = 50f;
    public float downThresholdLow = 30f;
    public float upThresholdLow = 40f;

    [Header("Quality - High")]
    public int highTargetFps = 60;
    public int highPixelLightCount = 2;
    public int highAntiAliasing = 2;
    public int highTextureLimit = 0;
    public float highShadowDistance = 30f;
    public ShadowResolution highShadowResolution = ShadowResolution.Medium;
    public int highShadowCascades = 2;
    public float highLodBias = 1.0f;
    public AnisotropicFiltering highAniso = AnisotropicFiltering.Enable;
    public bool highRealtimeReflectionProbes = false;
    public bool highSoftParticles = false;

    [Header("Quality - Medium")]
    public int medTargetFps = 50;
    public int medPixelLightCount = 1;
    public int medAntiAliasing = 0;
    public int medTextureLimit = 1;
    public float medShadowDistance = 18f;
    public ShadowResolution medShadowResolution = ShadowResolution.Low;
    public int medShadowCascades = 1;
    public float medLodBias = 0.85f;
    public AnisotropicFiltering medAniso = AnisotropicFiltering.Enable;
    public bool medRealtimeReflectionProbes = false;
    public bool medSoftParticles = false;

    [Header("Quality - Low")]
    public int lowTargetFps = 30;
    public int lowPixelLightCount = 0;
    public int lowAntiAliasing = 0;
    public int lowTextureLimit = 2;
    public float lowShadowDistance = 10f;
    public ShadowResolution lowShadowResolution = ShadowResolution.Low;
    public int lowShadowCascades = 0;
    public float lowLodBias = 0.7f;
    public AnisotropicFiltering lowAniso = AnisotropicFiltering.Disable;
    public bool lowRealtimeReflectionProbes = false;
    public bool lowSoftParticles = false;

    private struct Baseline
    {
        public int vSyncCount;
        public int targetFps;
        public int pixelLightCount;
        public int antiAliasing;
        public int textureLimit;
        public float shadowDistance;
        public ShadowResolution shadowResolution;
        public int shadowCascades;
        public float lodBias;
        public AnisotropicFiltering aniso;
        public bool realtimeReflectionProbes;
        public bool softParticles;
    }

    private Baseline _baseline;
    private bool _baselineCaptured;
    private bool _enabled;
    private PerfLevel _currentLevel;
    private float _emaFps = 60f;
    private float _nextCheckTime;
    private float _lowTimer;
    private float _highTimer;
    private float _lastChangeTime;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("MobilePerformanceManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MobilePerformanceManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!_enabled || !autoAdjust)
            return;

        float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        _emaFps = Mathf.Lerp(_emaFps, fps, fpsSmoothing);

        if (Time.unscaledTime < _nextCheckTime)
            return;

        _nextCheckTime = Time.unscaledTime + checkInterval;

        float downThreshold = GetDownThreshold(_currentLevel);
        float upThreshold = GetUpThreshold(_currentLevel);

        if (_emaFps < downThreshold)
        {
            _lowTimer += checkInterval;
            _highTimer = 0f;
        }
        else if (_emaFps > upThreshold)
        {
            _highTimer += checkInterval;
            _lowTimer = 0f;
        }
        else
        {
            _lowTimer = Mathf.Max(0f, _lowTimer - checkInterval);
            _highTimer = Mathf.Max(0f, _highTimer - checkInterval);
        }

        if (Time.unscaledTime - _lastChangeTime < changeCooldown)
            return;

        if (_lowTimer >= downgradeHoldTime && _currentLevel < PerfLevel.Low)
        {
            SetLevel(_currentLevel + 1);
        }
        else if (_highTimer >= upgradeHoldTime && _currentLevel > PerfLevel.High)
        {
            SetLevel(_currentLevel - 1);
        }
    }

    public void SetEnabled(bool enable)
    {
        _enabled = enable;
        if (_enabled)
        {
            CaptureBaseline();
            SetLevel(startLevel, force: true);
        }
        else
        {
            RestoreBaseline();
        }
    }

    public void SetLevel(PerfLevel level, bool force = false)
    {
        if (!force && level == _currentLevel)
            return;

        _currentLevel = level;
        _lastChangeTime = Time.unscaledTime;
        _lowTimer = 0f;
        _highTimer = 0f;

        ApplyLevel(level);
    }

    private void ApplyLevel(PerfLevel level)
    {
        // Always disable vSync to honor targetFrameRate
        QualitySettings.vSyncCount = 0;

        switch (level)
        {
            case PerfLevel.High:
                ApplyQuality(highTargetFps, highPixelLightCount, highAntiAliasing, highTextureLimit,
                    highShadowDistance, highShadowResolution, highShadowCascades, highLodBias,
                    highAniso, highRealtimeReflectionProbes, highSoftParticles);
                break;
            case PerfLevel.Medium:
                ApplyQuality(medTargetFps, medPixelLightCount, medAntiAliasing, medTextureLimit,
                    medShadowDistance, medShadowResolution, medShadowCascades, medLodBias,
                    medAniso, medRealtimeReflectionProbes, medSoftParticles);
                break;
            case PerfLevel.Low:
                ApplyQuality(lowTargetFps, lowPixelLightCount, lowAntiAliasing, lowTextureLimit,
                    lowShadowDistance, lowShadowResolution, lowShadowCascades, lowLodBias,
                    lowAniso, lowRealtimeReflectionProbes, lowSoftParticles);
                break;
        }
    }

    private void ApplyQuality(int targetFps, int pixelLights, int aa, int textureLimit,
        float shadowDistance, ShadowResolution shadowResolution, int shadowCascades, float lodBias,
        AnisotropicFiltering aniso, bool realtimeReflections, bool softParticles)
    {
        Application.targetFrameRate = targetFps;
        QualitySettings.pixelLightCount = Mathf.Max(0, pixelLights);
        QualitySettings.antiAliasing = Mathf.Max(0, aa);
        QualitySettings.globalTextureMipmapLimit = Mathf.Max(0, textureLimit);
        QualitySettings.shadowDistance = Mathf.Max(0f, shadowDistance);
        QualitySettings.shadowResolution = shadowResolution;
        QualitySettings.shadowCascades = Mathf.Clamp(shadowCascades, 0, 4);
        QualitySettings.lodBias = Mathf.Max(0.1f, lodBias);
        QualitySettings.anisotropicFiltering = aniso;
        QualitySettings.realtimeReflectionProbes = realtimeReflections;
        QualitySettings.softParticles = softParticles;
    }

    private void CaptureBaseline()
    {
        if (_baselineCaptured)
            return;

        _baseline = new Baseline
        {
            vSyncCount = QualitySettings.vSyncCount,
            targetFps = Application.targetFrameRate,
            pixelLightCount = QualitySettings.pixelLightCount,
            antiAliasing = QualitySettings.antiAliasing,
            textureLimit = QualitySettings.globalTextureMipmapLimit,
            shadowDistance = QualitySettings.shadowDistance,
            shadowResolution = QualitySettings.shadowResolution,
            shadowCascades = QualitySettings.shadowCascades,
            lodBias = QualitySettings.lodBias,
            aniso = QualitySettings.anisotropicFiltering,
            realtimeReflectionProbes = QualitySettings.realtimeReflectionProbes,
            softParticles = QualitySettings.softParticles
        };

        _baselineCaptured = true;
    }

    private void RestoreBaseline()
    {
        if (!_baselineCaptured)
            return;

        QualitySettings.vSyncCount = _baseline.vSyncCount;
        Application.targetFrameRate = _baseline.targetFps;
        QualitySettings.pixelLightCount = _baseline.pixelLightCount;
        QualitySettings.antiAliasing = _baseline.antiAliasing;
        QualitySettings.globalTextureMipmapLimit = _baseline.textureLimit;
        QualitySettings.shadowDistance = _baseline.shadowDistance;
        QualitySettings.shadowResolution = _baseline.shadowResolution;
        QualitySettings.shadowCascades = _baseline.shadowCascades;
        QualitySettings.lodBias = _baseline.lodBias;
        QualitySettings.anisotropicFiltering = _baseline.aniso;
        QualitySettings.realtimeReflectionProbes = _baseline.realtimeReflectionProbes;
        QualitySettings.softParticles = _baseline.softParticles;
    }

    private float GetDownThreshold(PerfLevel level)
    {
        switch (level)
        {
            case PerfLevel.High: return downThresholdHigh;
            case PerfLevel.Medium: return downThresholdMed;
            default: return downThresholdLow;
        }
    }

    private float GetUpThreshold(PerfLevel level)
    {
        switch (level)
        {
            case PerfLevel.High: return upThresholdHigh;
            case PerfLevel.Medium: return upThresholdMed;
            default: return upThresholdLow;
        }
    }
}
