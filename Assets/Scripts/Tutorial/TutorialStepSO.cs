using UnityEngine;
using System.Text.RegularExpressions;

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

    public string GetLocalizedObjectiveTitle()
    {
        string fallback = LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(objective.titleFrench)
            ? objective.titleFrench
            : objective.titleEnglish;

        string text = LocalizationManager.Get(LocalizationKeys.TutorialObjectiveTitle(this), fallback);
        text = ReplaceEvidenceTitleIfNeeded(text);

        InputManager input = InputManager.Instance;
        if (input != null)
            text = input.ReplaceInputTokens(text);

        return text;
    }

    private string ReplaceEvidenceTitleIfNeeded(string text)
    {
        if (objective == null || objective.type != TutorialObjectiveType.CheckEvidence || string.IsNullOrEmpty(text))
            return text;

        string evidenceTitle = LocalizationManager.GetEvidenceTitle(objective.evidenceType);
        if (string.IsNullOrWhiteSpace(evidenceTitle))
            return text;

        string quotedPattern = "([\"“”«])([^\"“”»]+)([\"“”»])";
        Match match = Regex.Match(text, quotedPattern);
        if (match.Success)
        {
            string replacement = $"{match.Groups[1].Value}{evidenceTitle}{match.Groups[3].Value}";
            return text.Substring(0, match.Index) + replacement + text.Substring(match.Index + match.Length);
        }

        return text;
    }
}
