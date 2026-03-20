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

    [Header("Performance")]
    public int lightsPerFrame = 25;
    public bool includeInactiveLights = true;
    
    [Header("Scene Filtering")]
    public string[] sceneNamePrefixes = { "world", "house" };

    private readonly List<MobileLightOptimizedLight> _lights = new List<MobileLightOptimizedLight>();
    private int _lightIndex;
    private bool _enabled;

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
