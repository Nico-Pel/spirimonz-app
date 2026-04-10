using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIChoiceWindow : GameBehaviour
{
    public enum ChoicePolarity
    {
        Normal,
        Positive,
        Negative,
        Custom
    }

    [Header("UI")]
    public TextMeshProUGUI tQuestion;
    public Button[] buttons;
    public TextMeshProUGUI[] buttonTexts;
    public Image[] buttonBackgrounds;

    [Header("Defaults")]
    public Color defaultButtonColor = new Color(0.2f, 0.7f, 0.3f, 0.35f);
    public Color defaultTextColor = Color.white;
    public Color positiveButtonColor = new Color(0.2f, 0.7f, 0.3f, 0.35f);
    public Color positiveTextColor = Color.white;
    public Color negativeButtonColor = new Color(0.8f, 0.2f, 0.2f, 0.35f);
    public Color negativeTextColor = Color.white;

    private Action<int> _onChoice;

    public void Open(
        string questionEnglish,
        string questionFrench,
        string[] answersEnglish,
        string[] answersFrench,
        Color[] buttonColors,
        Color[] textColors,
        Action<int> onChoice)
    {
        OpenInternal(questionEnglish, questionFrench, answersEnglish, answersFrench, buttonColors, textColors, null, onChoice);
    }

    public void OpenWithPolarity(
        string questionEnglish,
        string questionFrench,
        string[] answersEnglish,
        string[] answersFrench,
        ChoicePolarity[] polarities,
        Action<int> onChoice)
    {
        OpenInternal(questionEnglish, questionFrench, answersEnglish, answersFrench, null, null, polarities, onChoice);
    }

    private void OpenInternal(
        string questionEnglish,
        string questionFrench,
        string[] answersEnglish,
        string[] answersFrench,
        Color[] buttonColors,
        Color[] textColors,
        ChoicePolarity[] polarities,
        Action<int> onChoice)
    {
        _onChoice = onChoice;

        if (tQuestion != null)
            tQuestion.text = Localize(questionEnglish, questionFrench);

        int count = answersEnglish != null ? answersEnglish.Length : 0;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active = i < count;
                Button button = buttons[i];
                if (button == null)
                    continue;

                button.gameObject.SetActive(active);
                button.onClick.RemoveAllListeners();

                if (!active)
                    continue;

                int index = i;
                button.onClick.AddListener(() => HandleChoice(index));

                if (buttonTexts != null && i < buttonTexts.Length && buttonTexts[i] != null)
                {
                    string answerEn = answersEnglish != null && i < answersEnglish.Length ? answersEnglish[i] : string.Empty;
                    string answerFr = answersFrench != null && i < answersFrench.Length ? answersFrench[i] : string.Empty;
                    buttonTexts[i].text = Localize(answerEn, answerFr);

                    Color textColor = ResolveTextColor(i, textColors, polarities);
                    buttonTexts[i].color = textColor;
                }

                if (buttonBackgrounds != null && i < buttonBackgrounds.Length && buttonBackgrounds[i] != null)
                {
                    Color bgColor = ResolveButtonColor(i, buttonColors, polarities);
                    buttonBackgrounds[i].color = bgColor;
                }
            }
        }
    }

    public void Close()
    {
        if (UIGame.Instance != null)
            UIGame.Instance.CloseAllWindows();
        else
            gameObject.SetActive(false);
    }

    private void HandleChoice(int index)
    {
        _onChoice?.Invoke(index);
    }

    private string Localize(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }

    private Color ResolveButtonColor(int index, Color[] buttonColors, ChoicePolarity[] polarities)
    {
        if (polarities != null && index < polarities.Length)
        {
            switch (polarities[index])
            {
                case ChoicePolarity.Normal:
                    return defaultButtonColor;
                case ChoicePolarity.Positive:
                    return positiveButtonColor;
                case ChoicePolarity.Negative:
                    return negativeButtonColor;
            }
        }

        if (buttonColors != null && index < buttonColors.Length)
            return buttonColors[index];

        return defaultButtonColor;
    }

    private Color ResolveTextColor(int index, Color[] textColors, ChoicePolarity[] polarities)
    {
        if (polarities != null && index < polarities.Length)
        {
            switch (polarities[index])
            {
                case ChoicePolarity.Normal:
                    return defaultTextColor;
                case ChoicePolarity.Positive:
                    return positiveTextColor;
                case ChoicePolarity.Negative:
                    return negativeTextColor;
            }
        }

        if (textColors != null && index < textColors.Length)
            return textColors[index];

        return defaultTextColor;
    }
}
