using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    public string key;
    [TextArea] public string fallback;

    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    public void Refresh()
    {
        if (_text == null)
            return;

        string value = LocalizationManager.Get(key, fallback);
        InputManager input = InputManager.Instance;
        if (input != null)
            value = input.ReplaceInputTokens(value);

        _text.text = value;
    }

    private void HandleLanguageChanged(Language lang)
    {
        Refresh();
    }
}
