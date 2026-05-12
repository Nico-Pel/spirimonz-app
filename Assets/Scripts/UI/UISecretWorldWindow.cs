using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISecretWorldWindow : GameBehaviour
{
    [Header("Worlds")]
    public SecretWorld[] secretWorlds;

    [Header("UI")]
    public TextMeshProUGUI tWorldName;
    public Image worldImage;
    public TextMeshProUGUI tConditions;
    public TextMeshProUGUI tTips;
    public TextMeshProUGUI tCountdown;
    public TextMeshProUGUI tPrice;
    public Button bTravel;

    [Header("Price Colors")]
    public Color priceOkColor = Color.white;
    public Color priceNotEnoughColor = Color.red;

    [Header("Texts")]
    [TextArea] public string conditionsEnglish;
    [TextArea] public string conditionsFrench;
    [TextArea] public string tipsEnglish;
    [TextArea] public string tipsFrench;
    [TextArea] public string freeTextEnglish = "Free";
    [TextArea] public string freeTextFrench = "Gratuit";

    private const string ConditionsKey = "ui.secret_world.conditions";
    private const string TipsKey = "ui.secret_world.tips";
    private const string FreeKey = "ui.common.free";

    [Header("Rotation")]
    [Range(0, 23)] public int rotationHourLocal = 0;
    [Range(0, 59)] public int rotationMinuteLocal = 0;
    public float countdownRefreshInterval = 0.25f;

    [Header("Travel")]
    public float travelDelayBeforeFade = 0.6f;
    public float fadeDuration = 0.5f;
    public float loadDelayAfterFade = 0f;

    [Header("Debug")]
    public bool debugCycleWithX = true;
    public KeyCode debugCycleKey = KeyCode.X;

    private GameManager _gameManager;
    private int _currentIndex = -1;
    private DateTime _currentStartLocal;
    private DateTime _nextChangeLocal;
    private float _nextCountdownRefresh;
    private NPC _sourceNpc;
#if UNITY_EDITOR
    private int _debugIndexOffset;
#endif

    private void OnEnable()
    {
        _gameManager = GameManager.Instance;
        if (bTravel != null)
        {
            bTravel.onClick.RemoveAllListeners();
            bTravel.onClick.AddListener(OnTravelPressed);
        }

        if (_gameManager != null)
            _gameManager.onMoneyUpdated.AddListener(UpdatePriceState);

        Player player = Player.Instance;
        _sourceNpc = player != null ? player.currentNPC : null;

        if (_gameManager != null)
        {
            _gameManager.SetInt(SaveKeys.SECRET_WORLD_ROTATION_HOUR, rotationHourLocal);
            _gameManager.SetInt(SaveKeys.SECRET_WORLD_ROTATION_MINUTE, rotationMinuteLocal);
        }

        RefreshWorld(forceUIUpdate: true);
        UpdateCountdown();
        UpdatePriceState();
    }

    private void OnDisable()
    {
        if (_gameManager != null)
            _gameManager.onMoneyUpdated.RemoveListener(UpdatePriceState);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (debugCycleWithX && Input.GetKeyDown(debugCycleKey))
        {
            CycleDebugWorld();
        }
#endif

        if (Time.unscaledTime < _nextCountdownRefresh)
            return;

        _nextCountdownRefresh = Time.unscaledTime + Mathf.Max(0.05f, countdownRefreshInterval);
        RefreshWorld(forceUIUpdate: false);
        UpdateCountdown();
    }

    private void RefreshWorld(bool forceUIUpdate)
    {
        bool changed = EnsureSchedule();
        if (forceUIUpdate || changed)
            UpdateWorldUI();
    }

    private bool EnsureSchedule()
    {
        if (_gameManager == null || secretWorlds == null || secretWorlds.Length == 0)
            return false;

        int baseIndex = Mathf.Clamp(_gameManager.GetInt(SaveKeys.SECRET_WORLD_INDEX, 0), 0, secretWorlds.Length - 1);
        DateTime now = DateTime.Now;
        DateTime todayRotation = new DateTime(now.Year, now.Month, now.Day, rotationHourLocal, rotationMinuteLocal, 0, DateTimeKind.Local);
        DateTime lastRotation = ReadLastRotationLocal();

        if (lastRotation == DateTime.MinValue)
        {
            lastRotation = now < todayRotation ? todayRotation.AddDays(-1) : todayRotation;
            SaveSchedule(baseIndex, lastRotation);
        }

        if (now < lastRotation)
        {
            lastRotation = now < todayRotation ? todayRotation.AddDays(-1) : todayRotation;
            SaveSchedule(baseIndex, lastRotation);
        }

        int steps = (int)Math.Floor((now - lastRotation).TotalDays);
        if (steps > 0)
        {
            baseIndex = (baseIndex + steps) % secretWorlds.Length;
            lastRotation = lastRotation.AddDays(steps);
            SaveSchedule(baseIndex, lastRotation);
        }

        int displayIndex = baseIndex;
#if UNITY_EDITOR
        if (_debugIndexOffset != 0)
            displayIndex = (baseIndex + _debugIndexOffset) % secretWorlds.Length;
#endif

        bool changed = _currentIndex != displayIndex || _currentStartLocal != lastRotation;
        _currentIndex = displayIndex;
        _currentStartLocal = lastRotation;
        _nextChangeLocal = lastRotation.AddDays(1);
        return changed;
    }

    private void UpdateWorldUI()
    {
        SecretWorld world = GetCurrentWorld();
        if (world == null)
        {
            SetEmptyUI();
            return;
        }

        if (tWorldName != null)
            tWorldName.text = world.GetLocalizedName();
        if (worldImage != null)
            worldImage.sprite = world.worldImage;
        if (tConditions != null)
            tConditions.text = LocalizationManager.Get(ConditionsKey, LocalizeFallback(conditionsEnglish, conditionsFrench));
        if (tTips != null)
            tTips.text = LocalizationManager.Get(TipsKey, LocalizeFallback(tipsEnglish, tipsFrench));

        if (_gameManager != null)
            _gameManager.SetSecretWorldPriceConfig(world.houseEntryPriceIncrease, world.maxHouseEntryPriceIncreases);

        UpdatePriceState();
    }

    private void UpdateCountdown()
    {
        if (tCountdown == null)
            return;

        if (secretWorlds == null || secretWorlds.Length == 0)
        {
            tCountdown.text = "--:--:--";
            return;
        }

        if (DateTime.Now >= _nextChangeLocal)
        {
            RefreshWorld(forceUIUpdate: true);
        }

        TimeSpan remaining = _nextChangeLocal - DateTime.Now;
        if (remaining.TotalSeconds < 0)
            remaining = TimeSpan.Zero;

        tCountdown.text = string.Format("{0:00}:{1:00}:{2:00}",
            Mathf.FloorToInt((float)remaining.TotalHours),
            remaining.Minutes,
            remaining.Seconds);
    }

    private void UpdatePriceState()
    {
        SecretWorld world = GetCurrentWorld();
        if (world == null)
        {
            if (bTravel != null)
                bTravel.interactable = false;
            if (tPrice != null)
                tPrice.text = "--";
            return;
        }

        int price = Mathf.Max(0, world.travelPrice);
        bool travelPaid = _gameManager != null && _gameManager.IsSecretWorldTravelPaid(_currentIndex);
        if (travelPaid)
            price = 0;

        bool canPay = _gameManager != null && (price == 0 || _gameManager.CanBuy(price));

        if (tPrice != null)
        {
            tPrice.text = price <= 0
                ? LocalizationManager.Get(FreeKey, LocalizeFallback(freeTextEnglish, freeTextFrench))
                : (price + "#");
            tPrice.color = canPay ? priceOkColor : priceNotEnoughColor;
        }

        if (bTravel != null)
            bTravel.interactable = canPay;
    }

    private void OnTravelPressed()
    {
        SecretWorld world = GetCurrentWorld();
        if (world == null || _gameManager == null)
            return;

        int price = Mathf.Max(0, world.travelPrice);
        bool travelPaid = _gameManager.IsSecretWorldTravelPaid(_currentIndex);
        if (travelPaid)
            price = 0;

        if (price > 0 && !_gameManager.Buy(price))
        {
            UpdatePriceState();
            return;
        }

        if (price > 0)
            _gameManager.MarkSecretWorldTravelPaid(_currentIndex);

        if (!string.IsNullOrWhiteSpace(world.sceneName))
            _gameManager.RegisterTemporaryWorldScene(world.sceneName);

        World currentWorld = World.Instance != null ? World.Instance : FindObjectOfType<World>();
        if (currentWorld != null)
            currentWorld.PlayTravelAnimation();

        UIGame ui = UIGame.Instance;
        if (ui != null)
            ui.CloseAllWindows();

        LockPlayerForTravel();

        GameBehaviour runner = _gameManager != null ? (GameBehaviour)_gameManager : (GameBehaviour)ui;
        if (runner == null)
            return;

        runner.Invoke(travelDelayBeforeFade, () =>
        {
            if (ui != null)
                ui.EnableOverlay(true, fadeDuration);

            float loadDelay = Mathf.Max(0f, fadeDuration + loadDelayAfterFade);
            runner.Invoke(loadDelay, () =>
            {
                if (!string.IsNullOrWhiteSpace(world.sceneName))
                    _gameManager.LoadScene(world.sceneName);
            });
        });
    }

    private SecretWorld GetCurrentWorld()
    {
        if (secretWorlds == null || secretWorlds.Length == 0)
            return null;

        if (_currentIndex < 0 || _currentIndex >= secretWorlds.Length)
            return null;

        return secretWorlds[_currentIndex];
    }

    private void SetEmptyUI()
    {
        if (tWorldName != null)
            tWorldName.text = "-";
        if (worldImage != null)
            worldImage.sprite = null;
        if (tConditions != null)
            tConditions.text = "";
        if (tTips != null)
            tTips.text = "";
        if (tCountdown != null)
            tCountdown.text = "--:--:--";
        if (tPrice != null)
            tPrice.text = "--";
        if (bTravel != null)
            bTravel.interactable = false;
    }

    private DateTime ReadLastRotationLocal()
    {
        if (_gameManager == null)
            return DateTime.MinValue;

        string raw = _gameManager.GetString(SaveKeys.SECRET_WORLD_LAST_ROTATION_LOCAL_TICKS, string.Empty);
        if (long.TryParse(raw, out long localTicks) && localTicks > 0)
        {
            return new DateTime(localTicks, DateTimeKind.Local);
        }

        // Fallback for older saves using UTC ticks
        string legacy = _gameManager.GetString(SaveKeys.SECRET_WORLD_START_UTC_TICKS, string.Empty);
        if (long.TryParse(legacy, out long utcTicks) && utcTicks > 0)
        {
            DateTime legacyUtc = new DateTime(utcTicks, DateTimeKind.Utc);
            return legacyUtc.ToLocalTime();
        }

        return DateTime.MinValue;
    }

    private void SaveSchedule(int index, DateTime lastRotationLocal)
    {
        if (_gameManager == null)
            return;

        _gameManager.SetInt(SaveKeys.SECRET_WORLD_INDEX, index);
        _gameManager.SetString(SaveKeys.SECRET_WORLD_LAST_ROTATION_LOCAL_TICKS, lastRotationLocal.Ticks.ToString());
    }

    private string LocalizeFallback(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }

    public void SetSourceNpc(NPC npc)
    {
        _sourceNpc = npc;
    }

    private void LockPlayerForTravel()
    {
        Player player = Player.Instance;
        if (player != null)
        {
            player.LockControls(true);
            player.currentNPC = null;
        }

        if (_sourceNpc != null)
        {
            _sourceNpc.InteractionLocked = true;
            _sourceNpc.CloseCTA();
        }
    }

#if UNITY_EDITOR
    private void CycleDebugWorld()
    {
        if (secretWorlds == null || secretWorlds.Length == 0)
            return;

        _debugIndexOffset = (_debugIndexOffset + 1) % secretWorlds.Length;
        RefreshWorld(forceUIUpdate: true);
        UpdateCountdown();
    }
#endif
}
