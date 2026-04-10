using UnityEngine;

public class LeaveSecretWorldTrigger : GameBehaviour
{
    public int choiceWindowId = -1;
    public UIChoiceWindow choiceWindow;

    [Header("Texts")]
    [TextArea] public string questionEnglish = "Are you sure you want to leave?";
    [TextArea] public string questionFrench = "Es-tu sûr de vouloir partir ?";
    [TextArea] public string yesEnglish = "Yes";
    [TextArea] public string yesFrench = "Oui";
    [TextArea] public string noEnglish = "No";
    [TextArea] public string noFrench = "Non";

    [Header("Button Style")]
    public UIChoiceWindow.ChoicePolarity yesPolarity = UIChoiceWindow.ChoicePolarity.Positive;
    public UIChoiceWindow.ChoicePolarity noPolarity = UIChoiceWindow.ChoicePolarity.Negative;

    [Header("Travel")]
    public float fadeDuration = 0.5f;
    public float loadDelayAfterFade = 0f;

    private bool _canTrigger;
    private float _securityTime = 0.5f;

    private void Start()
    {
        this.Invoke(_securityTime, () => _canTrigger = true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_canTrigger)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsTemporaryWorldScene(gameObject.scene.name))
            return;

        _canTrigger = false;
        OpenConfirm();
    }

    private void OpenConfirm()
    {
        UIGame ui = UIGame.Instance;
        if (ui != null && choiceWindowId >= 0)
        {
            ui.OpenPrivateTabletWindow(choiceWindowId);
        }

        if (choiceWindow == null && ui != null && ui.tablet != null)
            choiceWindow = ui.tablet.GetComponentInChildren<UIChoiceWindow>(true);

        if (choiceWindow == null)
            return;

        string[] answersEn = { yesEnglish, noEnglish };
        string[] answersFr = { yesFrench, noFrench };
        UIChoiceWindow.ChoicePolarity[] polarities = { yesPolarity, noPolarity };

        choiceWindow.OpenWithPolarity(questionEnglish, questionFrench, answersEn, answersFr, polarities, HandleChoice);

        Player player = Player.Instance;
        if (player != null)
            player.LockControls(true);
    }

    private void HandleChoice(int index)
    {
        if (index == 0)
        {
            ConfirmLeave();
        }
        else
        {
            CancelLeave();
        }
    }

    private void ConfirmLeave()
    {
        UIGame ui = UIGame.Instance;
        if (ui != null)
            ui.CloseAllWindows();

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        gm.MarkSecretWorldReturnToTaxi();

        if (ui != null)
            ui.EnableOverlay(true, fadeDuration);

        float loadDelay = Mathf.Max(0f, fadeDuration + loadDelayAfterFade);
        gm.Invoke(loadDelay, () =>
        {
            gm.LoadScene(gm.defaultWorldSceneName);
        });
    }

    private void CancelLeave()
    {
        if (choiceWindow != null)
            choiceWindow.Close();

        this.Invoke(_securityTime, () => _canTrigger = true);
    }
}
