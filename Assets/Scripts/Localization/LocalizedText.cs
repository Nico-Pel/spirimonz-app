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

        _text.text = LocalizationManager.Get(key, fallback);
    }

    private void HandleLanguageChanged(Language lang)
    {
        Refresh();
    }
}
