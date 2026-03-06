using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogue == null) return;

        UIGame.Instance.uiDialogue.StartDialogue(dialogue);
    }

    private string GetLocalizedText(DialogueLine line)
    {
        switch (LanguageManager.CurrentLanguage)
        {
            case Language.French:
                return line.french;

            default:
                return line.english;
        }
    }
}