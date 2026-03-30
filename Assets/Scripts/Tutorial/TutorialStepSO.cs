using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step")]
public class TutorialStepSO : ScriptableObject
{
    public string id;
    public Dialogue dialogue;
    public TutorialObjective objective = new TutorialObjective();
    public TutorialInputMask inputMask = new TutorialInputMask();
    public TutorialGhostOverride ghostOverride = new TutorialGhostOverride();

    [Header("Completion Flow")]
    public bool requireNpcReturn = true;
    [Min(0f)] public float autoAdvanceDelay = 3f;
}
