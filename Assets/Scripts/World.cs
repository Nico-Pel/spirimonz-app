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

    [Header("Tutorial Redirect")]
    public bool autoRedirectToTutorial = true;
    public string tutorialSceneName = "HouseTuto";
    public float tutorialRedirectDelay = 0.1f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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
}
