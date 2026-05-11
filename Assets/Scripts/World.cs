using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : GameBehaviour
{
    public static World Instance { get; private set; }

    public string worldName;
    public Transform[] spawnPoints;
    public Transform startPosTuto;
    public Transform spawnTaxiPos;
    public Animator travelAnimator;
    public string travelTrigger = "Travel";

    [Header("Tutorial Redirect")]
    public bool autoRedirectToTutorial = true;
    public string tutorialSceneName = "HouseTuto";
    public float tutorialRedirectDelay = 0.1f;

    [Header("Mobile NPC Optimization")]
    [Tooltip("NPCs in this list are considered optional and may be hidden on mobile to reduce CPU / skinning cost.")]
    public GameObject[] optionalMobileNpcs;
    [Range(0f, 1f)] public float optionalMobileNpcVisibleRatio = 0.5f;

    private bool _mobileNpcOptimizationApplied;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ApplyMobileNpcOptimization();

        if (!autoRedirectToTutorial)
            return;

        this.Invoke(tutorialRedirectDelay, TryAutoRedirectToTutorial);
    }

    private void TryAutoRedirectToTutorial()
    {
        if (!autoRedirectToTutorial)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        if (SaveManager.IsTemporarySlot)
            return;

        if (TutorialManager.TutorialDoorUnlockedRuntime)
            return;

        if (gm.GetBool(SaveKeys.TUTORIAL_DOOR_UNLOCKED, false))
            return;

        if (string.IsNullOrWhiteSpace(tutorialSceneName))
            return;

        gm.LoadHouseSceneWithMode(tutorialSceneName, GameManager.HouseSceneMode.Tutorial);
    }

    public void PlayTravelAnimation()
    {
        if (travelAnimator == null)
            return;

        if (string.IsNullOrWhiteSpace(travelTrigger))
            return;

        travelAnimator.SetTrigger(travelTrigger);
    }

    private void ApplyMobileNpcOptimization()
    {
        if (_mobileNpcOptimizationApplied)
            return;

        _mobileNpcOptimizationApplied = true;

        if (!ShouldOptimizeOptionalNpcsForMobile())
            return;

        if (optionalMobileNpcs == null || optionalMobileNpcs.Length == 0)
            return;

        List<GameObject> validNpcs = new List<GameObject>();
        for (int i = 0; i < optionalMobileNpcs.Length; i++)
        {
            GameObject npc = optionalMobileNpcs[i];
            if (npc != null)
                validNpcs.Add(npc);
        }

        if (validNpcs.Count == 0)
            return;

        int visibleCount = Mathf.Clamp(Mathf.RoundToInt(validNpcs.Count * optionalMobileNpcVisibleRatio), 0, validNpcs.Count);
        if (visibleCount >= validNpcs.Count)
            return;

        System.Random rng = new System.Random(BuildOptionalNpcSeed(validNpcs.Count));
        for (int i = validNpcs.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            GameObject temp = validNpcs[i];
            validNpcs[i] = validNpcs[swapIndex];
            validNpcs[swapIndex] = temp;
        }

        for (int i = visibleCount; i < validNpcs.Count; i++)
            validNpcs[i].SetActive(false);
    }

    private bool ShouldOptimizeOptionalNpcsForMobile()
    {
        if (Application.isMobilePlatform)
            return true;

        return GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled;
    }

    private int BuildOptionalNpcSeed(int npcCount)
    {
        int saveSlotSeed = SaveManager.CurrentSlot;
        int worldSeed = string.IsNullOrWhiteSpace(worldName) ? 0 : worldName.GetHashCode();
        return worldSeed ^ (saveSlotSeed * 397) ^ (npcCount * 17);
    }
}
