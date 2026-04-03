using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIFilterButton : GameBehaviour
{
    [Header("Components")]
    public Button bFilter;
    public Image filterImage;
    public Image filterSecondaryImage;

    [Header("Colors")]
    public Color[] buttonColors;
    public Color[] buttonSecondaryColors;

    [Header("Settings")] 
    public int maxState = 2;
    public int startState = 0;
    public bool resetOnEnable = true;

    [Header("Sounds")]
    public SoundParameters changeSound;

    public UnityEvent onStateChanged;
    private int _currentState;

    private void Start()
    {
        _currentState = startState;
        if (bFilter != null)
            bFilter.onClick.AddListener(NextState);

        if (changeSound != null)
            UISoundDefaults.MarkAsUi(changeSound);
    }

    private void OnEnable()
    {
        if (resetOnEnable)
        {
            ChangeFilterState(startState);
        }
    }

    private void NextState()
    {
        _currentState++;
        if (_currentState > maxState)
            _currentState = 0;
        
        ChangeFilterState(_currentState);
        PlayChangeSound();
    }

    private void ChangeFilterState(int state)
    {
        filterImage.color = buttonColors[state];
        filterSecondaryImage.color = buttonSecondaryColors[state];
        
        onStateChanged?.Invoke();
        _currentState = state;
    }

    public int GetState() => _currentState;

    private void PlayChangeSound()
    {
        if (changeSound != null)
            changeSound.PlaySound();
    }
}
