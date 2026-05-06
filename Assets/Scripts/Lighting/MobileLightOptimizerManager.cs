using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileLightOptimizerManager : MonoBehaviour
{
    public static MobileLightOptimizerManager Instance { get; private set; }

    [Header("Target")]
    public Transform targetOverride;
    public bool useCameraAsTarget = true;

    [Header("Distances")]
    public float nearDistance = 10f;
    public float shadowDisableDistance = 14f;
    public float farDistance = 22f;
    public float disableDistance = 30f;
    public float viewDotThreshold = 0.2f;

    [Header("House Overrides")]
    public bool useHouseOverrides = true;
    public float houseNearDistance = 14f;
    public float houseShadowDisableDistance = 24f;
    public float houseFarDistance = 34f;
    public float houseDisableDistance = 42f;
    public float houseViewDotThreshold = -0.1f;

    [Header("Performance")]
    public int lightsPerFrame = 25;
    public bool includeInactiveLights = true;
    
    [Header("Scene Filtering")]
    public string[] sceneNamePrefixes = { "world", "house" };

    [Header("Budget")]
    public bool useLightBudget = true;
    public int maxNonGameplayLights = 10;
    public float budgetRefreshInterval = 0.25f;
    public float outOfViewPenalty = 5f;
    public bool budgetAffectsNearLights = false;
    public int houseMaxNonGameplayLights = 18;
    public float houseOutOfViewPenalty = 2f;
    public bool houseBudgetAffectsNearLights = false;
    public bool disableHouseLightBudget = true;

    [Header("Stability")]
    public float downgradeDelay = 0.45f;
    public float disableOutOfViewDelay = 0.75f;
    [Range(0.1f, 1f)] public float farRangeMultiplier = 0.88f;
    public float houseDowngradeDelay = 1.5f;
    public float houseDisableOutOfViewDelay = 2.5f;
    [Range(0.1f, 1f)] public float houseFarRangeMultiplier = 1f;
    public bool houseKeepBaseRenderMode = true;
    public bool disableHouseFarLightDisable = true;
    public bool disableHouseRangeReduction = true;

    private readonly List<MobileLightOptimizedLight> _lights = new List<MobileLightOptimizedLight>();
    private readonly HashSet<MobileLightOptimizedLight> _budgetAllowed = new HashSet<MobileLightOptimizedLight>();
    private int _lightIndex;
    private bool _enabled;
    private float _nextBudgetTime;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("MobileLightOptimizerManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileLightOptimizerManager>();
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_enabled)
            RefreshSceneLights();
    }

    private void Update()
    {
        if (!_enabled)
            return;

        UpdateLights();
    }

    public void SetEnabled(bool enable)
    {
        _enabled = enable;
        if (_enabled)
        {
            RefreshSceneLights();
        }
        else
        {
            RestoreAll();
        }
    }

    public void Register(MobileLightOptimizedLight light)
    {
        if (light == null)
            return;
        if (_lights.Contains(light))
            return;

        _lights.Add(light);
    }

    public void Unregister(MobileLightOptimizedLight light)
    {
        if (light == null)
            return;
        _lights.Remove(light);
    }

    public void RefreshSceneLights()
    {
        _lights.Clear();
        _budgetAllowed.Clear();
        _nextBudgetTime = 0f;

        if (!IsOptimizedScene())
            return;

        HashSet<Light> gameplayLights = CollectGameplayLights();
        Light[] allLights = Resources.FindObjectsOfTypeAll<Light>();

        for (int i = 0; i < allLights.Length; i++)
        {
            Light l = allLights[i];
            if (l == null)
                continue;
            if (!l.gameObject.scene.IsValid())
                continue;
            if (!includeInactiveLights && !l.gameObject.activeInHierarchy)
                continue;
            if (l.type != LightType.Point)
                continue;
            bool isBaked = false;
#if UNITY_EDITOR
            isBaked = l.lightmapBakeType == LightmapBakeType.Baked;
#else
#if UNITY_2021_2_OR_NEWER
            isBaked = l.bakingOutput.lightmapBakeType == LightmapBakeType.Baked;
#endif
#endif
            if (isBaked)
                continue;

            MobileLightOptimizedLight opt = l.GetComponent<MobileLightOptimizedLight>();
            if (opt == null)
                opt = l.gameObject.AddComponent<MobileLightOptimizedLight>();

            bool gameplay = gameplayLights.Contains(l);
            if (!gameplay)
            {
                if (l.GetComponentInParent<ActivableObject>() != null)
                    gameplay = true;
                else if (l.GetComponentInParent<BlinkingLight>() != null)
                    gameplay = true;
                else if (l.GetComponentInParent<ElectricLight>() != null)
                    gameplay = true;
            }
            opt.Initialize(gameplay);
            Register(opt);
        }
    }

    private bool IsOptimizedScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        string name = scene.name.ToLower();
        if (sceneNamePrefixes == null || sceneNamePrefixes.Length == 0)
            return name.StartsWith("world");

        for (int i = 0; i < sceneNamePrefixes.Length; i++)
        {
            string prefix = sceneNamePrefixes[i];
            if (string.IsNullOrWhiteSpace(prefix))
                continue;

            if (name.StartsWith(prefix.Trim().ToLower()))
                return true;
        }

        return false;
    }

    private void UpdateLights()
    {
        Transform target = ResolveTarget();
        if (target == null || _lights.Count == 0)
            return;

        Vector3 targetPos = target.position;
        Vector3 targetForward = target.forward;

        UpdateBudget(targetPos, targetForward);

        int count = Mathf.Clamp(lightsPerFrame, 1, _lights.Count);
        for (int i = 0; i < count; i++)
        {
            if (_lights.Count == 0)
                break;

            if (_lightIndex >= _lights.Count)
                _lightIndex = 0;

            MobileLightOptimizedLight light = _lights[_lightIndex];
            _lightIndex++;

            if (light == null)
            {
                _lights.RemoveAt(_lightIndex - 1);
                _lightIndex--;
                continue;
            }

            light.Apply(this, targetPos, targetForward);
        }
    }

    public bool IsLightBudgetAllowed(MobileLightOptimizedLight light)
    {
        if (!useLightBudget || light == null)
            return true;
        if (light.IsGameplayLight)
            return true;
        if (disableHouseLightBudget && UseHouseOverrideValues())
            return true;

        return _budgetAllowed.Contains(light);
    }

    public bool IsHouseScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        return scene.name.StartsWith("House");
    }

    public float GetNearDistance() => UseHouseOverrideValues() ? houseNearDistance : nearDistance;
    public float GetShadowDisableDistance() => UseHouseOverrideValues() ? houseShadowDisableDistance : shadowDisableDistance;
    public float GetFarDistance() => UseHouseOverrideValues() ? houseFarDistance : farDistance;
    public float GetDisableDistance() => UseHouseOverrideValues() ? houseDisableDistance : disableDistance;
    public float GetViewDotThreshold() => UseHouseOverrideValues() ? houseViewDotThreshold : viewDotThreshold;
    public int GetMaxNonGameplayLights() => UseHouseOverrideValues() ? houseMaxNonGameplayLights : maxNonGameplayLights;
    public float GetOutOfViewPenalty() => UseHouseOverrideValues() ? houseOutOfViewPenalty : outOfViewPenalty;
    public bool GetBudgetAffectsNearLights() => disableHouseLightBudget && UseHouseOverrideValues() ? false : (UseHouseOverrideValues() ? houseBudgetAffectsNearLights : budgetAffectsNearLights);
    public float GetDowngradeDelay() => UseHouseOverrideValues() ? houseDowngradeDelay : downgradeDelay;
    public float GetDisableOutOfViewDelay() => UseHouseOverrideValues() ? houseDisableOutOfViewDelay : disableOutOfViewDelay;
    public float GetFarRangeMultiplier() => UseHouseOverrideValues() ? houseFarRangeMultiplier : farRangeMultiplier;
    public bool GetKeepBaseRenderMode() => UseHouseOverrideValues() && houseKeepBaseRenderMode;
    public bool ShouldDisableFarLights() => !UseHouseOverrideValues() || !disableHouseFarLightDisable;
    public bool ShouldReduceRange() => !UseHouseOverrideValues() || !disableHouseRangeReduction;

    private void RestoreAll()
    {
        for (int i = 0; i < _lights.Count; i++)
        {
            if (_lights[i] != null)
                _lights[i].Restore();
        }
    }

    private Transform ResolveTarget()
    {
        if (targetOverride != null)
            return targetOverride;

        if (useCameraAsTarget && Camera.main != null)
            return Camera.main.transform;

        if (Player.Instance != null && Player.Instance.camera != null)
            return Player.Instance.camera.transform;

        return null;
    }

    private void UpdateBudget(Vector3 targetPos, Vector3 targetForward)
    {
        int lightBudget = GetMaxNonGameplayLights();
        if (!useLightBudget || lightBudget <= 0 || (disableHouseLightBudget && UseHouseOverrideValues()))
        {
            _budgetAllowed.Clear();
            return;
        }

        if (budgetRefreshInterval > 0f && Time.unscaledTime < _nextBudgetTime)
            return;

        _nextBudgetTime = Time.unscaledTime + Mathf.Max(0.05f, budgetRefreshInterval);
        _budgetAllowed.Clear();

        List<LightScore> scores = new List<LightScore>(_lights.Count);
        for (int i = 0; i < _lights.Count; i++)
        {
            MobileLightOptimizedLight light = _lights[i];
            if (light == null || light.targetLight == null)
                continue;
            if (!light.targetLight.gameObject.activeInHierarchy)
                continue;
            if (light.ignoreOptimization || light.IsGameplayLight)
                continue;

            Vector3 toLight = light.targetLight.transform.position - targetPos;
            float dist = toLight.magnitude;
            float maxDistance = GetDisableDistance();
            if (maxDistance > 0f && dist > maxDistance)
                continue;

            float score = dist;
            if (dist > 0.001f)
            {
                float dot = Vector3.Dot(targetForward, toLight / dist);
                if (dot < GetViewDotThreshold())
                    score += GetOutOfViewPenalty();
            }

            scores.Add(new LightScore(light, score));
        }

        scores.Sort((a, b) => a.Score.CompareTo(b.Score));

        int count = Mathf.Min(lightBudget, scores.Count);
        for (int i = 0; i < count; i++)
            _budgetAllowed.Add(scores[i].Light);
    }

    private bool UseHouseOverrideValues()
    {
        return useHouseOverrides && IsHouseScene();
    }

    private readonly struct LightScore
    {
        public readonly MobileLightOptimizedLight Light;
        public readonly float Score;

        public LightScore(MobileLightOptimizedLight light, float score)
        {
            Light = light;
            Score = score;
        }
    }

    private HashSet<Light> CollectGameplayLights()
    {
        HashSet<Light> result = new HashSet<Light>();

        ElectricLight[] electricLights = Resources.FindObjectsOfTypeAll<ElectricLight>();
        for (int i = 0; i < electricLights.Length; i++)
        {
            ElectricLight el = electricLights[i];
            if (el == null || !el.gameObject.scene.IsValid())
                continue;

            foreach (GameObject go in el.objectsToEnable)
            {
                if (go == null)
                    continue;

                Light[] lights = go.GetComponentsInChildren<Light>(true);
                for (int j = 0; j < lights.Length; j++)
                    result.Add(lights[j]);
            }
        }

        BlinkingLight[] blinking = Resources.FindObjectsOfTypeAll<BlinkingLight>();
        for (int i = 0; i < blinking.Length; i++)
        {
            BlinkingLight bl = blinking[i];
            if (bl == null || !bl.gameObject.scene.IsValid())
                continue;

            Light l = bl.GetComponentInChildren<Light>(true);
            if (l != null)
                result.Add(l);
        }

        if (Player.Instance != null)
        {
            Transform playerRoot = Player.Instance.transform;
            Light[] playerLights = playerRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < playerLights.Length; i++)
                result.Add(playerLights[i]);
        }

        return result;
    }
}
