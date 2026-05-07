using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Random = UnityEngine.Random;

public class UIDialogue : GameBehaviour
{
    [SerializeField] private float dialogueSpeed = 0.05f;
    [SerializeField] private float dialogueSoundVolume = 0.075f;
    [SerializeField] [Range(0f, 1f)] private float letterSoundRate = 1f;
    [SerializeField] private float inputIgnoreDuration = 0.15f;
    [SerializeField] private int dialogueSortingOrder = 1200;

    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI boxText;
    [SerializeField] private Button bNext;
    [SerializeField] private Button bBox;
    [SerializeField] private TextMeshProUGUI tNext;

    private int _currentLine;
    private Dialogue _currentDialogue;

    private bool _dialogueActive = false;
    public bool IsDialogueActive => _dialogueActive || (dialogueBox != null && dialogueBox.activeSelf);
    private float _inputIgnoreUntil;
    private InputManager _inputManager;
    private SoundManager _soundManager;
    private Player _player;
    
    // Clip réutilisable
    private AudioClip _letterBeepClip;
    private float _minPitch = 0.9f;
    private float _maxPitch = 1.1f;
    private bool _suppressLetterSounds;

    private void Awake()
    {
        _letterBeepClip = GenerateBeep(440f, 0.05f);
        EnsureDialogueCanvasPriority();
    }

    private void Start()
    {
        bNext.onClick.AddListener(NextDialogue);
        bBox.onClick.AddListener(SkipTexting);
    }

    private void OnEnable()
    {
        this.Invoke(0.1f, () =>
        {
            if(_inputManager == null)
                _inputManager = InputManager.Instance;
            
            if(_soundManager == null)
                _soundManager = SoundManager.Instance;
            
            if (_player == null)
                _player = Player.Instance;

            if(_inputManager != null)
                tNext.text = _inputManager.GetWorldInteractionDisplay();
        });

        bNext.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Press "E" to go to next line
        bool rawInteractionDown = false;
        if (_inputManager != null)
        {
            bool mobileDialogueAdvanceDown = MobileInput.GrabDown || MobileInput.ConsumeGrabDown();
            rawInteractionDown =
                (!MobileInput.Enabled &&
                 (_inputManager.GetKeyDown(_inputManager.worldInteractions, _inputManager.worldInteractionsAlt) ||
                  _inputManager.GetKeyDown(_inputManager.grabObject, _inputManager.grabObjectAlt)))
                || (MobileInput.Enabled && mobileDialogueAdvanceDown);
        }

        if (_dialogueActive &&
            Time.unscaledTime >= _inputIgnoreUntil &&
            rawInteractionDown)
        {
            NextDialogue();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (_dialogueActive) return;

        EnsureDialogueCanvasPriority();
        _currentLine = 0;
        dialogueBox.SetActive(true);
        dialogueBox.transform.SetAsLastSibling();
        _currentDialogue = dialogue;
        _dialogueActive = true;
        _inputIgnoreUntil = Time.unscaledTime + Mathf.Max(0.01f, inputIgnoreDuration);

        titleText.text = dialogue.GetLocalizedNpcName();

        // --- On initialise le son des lettres ---
        SetLetterSoundProfile(dialogue);

        SetText();
    }

    private void NextDialogue()
    {
        if (_writingText)
        {
            SkipTexting();
            return;
        }
        
        _currentLine++;

        if (_currentDialogue.lines.Count > _currentLine)
        {
            SetText();
        }
        else
        {
            EndDialogue();
        }
    }

    private void SkipTexting()
    {
        if (_writingText == false) return;

        _suppressLetterSounds = true;
        boxText.DOKill(true);
        _suppressLetterSounds = false;
        EnableNextButton();
    }

    private void EnableNextButton()
    {
        bNext.gameObject.SetActive(true);
        bNext.transform.DOScale(1, 0.2f).SetEase(Ease.OutBack).From(0);
    }

    private bool _writingText;
    private void SetText()
    {
        bNext.gameObject.SetActive(false);

        DialogueLine line = _currentDialogue.lines[_currentLine];

        boxText.text = "";

        string text = _currentDialogue.GetLocalizedLine(_currentLine);
        if (_inputManager != null)
            text = _inputManager.ReplaceInputTokens(text);
        _writingText = true;

        int previousLength = 0;
        float soundAccumulator = 0f;

        // DOTween DOText
        boxText.DOText(text, text.Length * dialogueSpeed, richTextEnabled: true)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                int currentLength = boxText.text.Length;
                if (currentLength > previousLength)
                {
                    if (_suppressLetterSounds)
                    {
                        previousLength = currentLength;
                        return;
                    }

                    int addedChars = currentLength - previousLength;
                    for (int i = 0; i < addedChars; i++)
                    {
                        soundAccumulator += Mathf.Clamp01(letterSoundRate);
                        if (soundAccumulator < 1f)
                            continue;

                        soundAccumulator -= 1f;

                        if(_player == null) _player = Player.Instance;
                    
                        Vector3 pos = _player != null ? _player.characterController.transform.position : Camera.main.transform.position;
                        PlayLetterSoundUI(pos);
                    }
                }
                previousLength = currentLength;
            })
            .OnComplete(() =>
            {
                _suppressLetterSounds = false;
                EnableNextButton();
                _writingText = false;
            });
    }
    
    private void SetLetterSoundProfile(Dialogue dialogue)
    {
        if (dialogue.letterSoundProfile != null && dialogue.letterSoundProfile.baseBeep != null)
        {
            _letterBeepClip = dialogue.letterSoundProfile.baseBeep;
            _minPitch = dialogue.letterSoundProfile.minPitch;
            _maxPitch = dialogue.letterSoundProfile.maxPitch;
        }
        else
        {
            // Fallback si pas de profil : génère un beep classique
            _letterBeepClip = GenerateBeep(440f, 0.05f);
            _minPitch = 0.9f;
            _maxPitch = 1.1f;
        }
    }

    private void PlayLetterSoundUI(Vector3 position)
    {
        if (_soundManager == null) _soundManager = SoundManager.Instance;

        float pitch = Random.Range(_minPitch, _maxPitch);

        _soundManager.PlaySound(
            _letterBeepClip,
            position,
            volume: dialogueSoundVolume,
            pitch: pitch,
            duration: 0.05f
        );
    }

    private AudioClip GenerateBeep(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleLength = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleLength];

        for (int i = 0; i < sampleLength; i++)
        {
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate);
        }

        AudioClip clip = AudioClip.Create("LetterBeep", sampleLength, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void EnsureDialogueCanvasPriority()
    {
        if (dialogueBox == null)
            return;

        Canvas dialogueCanvas = dialogueBox.GetComponent<Canvas>();
        if (dialogueCanvas == null)
            dialogueCanvas = dialogueBox.AddComponent<Canvas>();

        dialogueCanvas.overrideSorting = true;
        dialogueCanvas.sortingOrder = dialogueSortingOrder;

        if (dialogueBox.GetComponent<GraphicRaycaster>() == null)
            dialogueBox.AddComponent<GraphicRaycaster>();
    }

    private void EndDialogue()
    {
        dialogueBox.SetActive(false);
        _dialogueActive = false;

        Player.Instance.EndDialogue();
    }
}
