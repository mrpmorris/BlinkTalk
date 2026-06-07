using System;
using System.Collections.Generic;
using System.Linq;
using BlinkTalk.Application.Prediction;

namespace BlinkTalk.Application.Text;

/// <summary>
/// Builds up the sentence being composed: the current (unfinished) word plus the list of
/// committed words, and the live list of suggested next words. Ported from the original
/// SentenceBuilder; the static WordService/PhraseService calls are now injected services,
/// and UnityEngine.KeyCode is replaced by the Core KeyCode enum. Speaking is left to the
/// caller (the section strategy), exactly as in the original.
/// </summary>
public sealed class SentenceBuilder
{
    public event EventHandler? ViewModelChanged;
    public bool ShouldClearOnNextInput { get; private set; }
    public IReadOnlyList<string> SuggestedWords { get; private set; } = Array.Empty<string>();

    private const int NumberOfSuggestedWords = 6;
    private string _currentWord = "";
    private readonly List<KeyValuePair<int, string>> _words = new List<KeyValuePair<int, string>>();
    private readonly Dictionary<KeyCode, char> _charsByKeyCode;
    private readonly IWordService _wordService;
    private readonly IPhraseService _phraseService;

    public SentenceBuilder(IWordService wordService, IPhraseService phraseService)
    {
        _wordService = wordService;
        _phraseService = phraseService;
        _charsByKeyCode = BuildCharMap();
    }

    /// <summary>Must be called once the database is available (loads the initial suggestions).</summary>
    public void Initialize()
    {
        GetWordSuggestions();
    }

    public bool IsEmpty => string.IsNullOrEmpty(ToString());

    /// <summary>The word currently being typed (empty at a committed word boundary). Exposed as
    /// raw state because <see cref="ToString"/> can't distinguish it from a trailing space.</summary>
    public string CurrentWord => _currentWord;

    public void Input(KeyCode keyCode)
    {
        CheckForClearOnInput();
        switch (keyCode)
        {
            case KeyCode.Space:
                PushCurrentWord();
                break;
            case KeyCode.Backspace:
                Backspace();
                break;
            default:
                _currentWord += _charsByKeyCode[keyCode];
                break;
        }
        DoViewModelChanged();
    }

    public string Commit()
    {
        if (!string.IsNullOrEmpty(_currentWord))
            PushCurrentWord();
        _phraseService.IncrementPhraseUsage(_words.Select(x => x.Key));
        ShouldClearOnNextInput = true;
        DoViewModelChanged();
        return ToString();
    }

    public void PushWord(string word)
    {
        CheckForClearOnInput();
        _currentWord = word;
        PushCurrentWord();
    }

    public override string ToString()
    {
        string result = string.Join(" ", _words.Select(x => x.Value));
        if (!string.IsNullOrEmpty(_currentWord))
            result += " " + _currentWord;
        return result;
    }

    private void Clear()
    {
        _words.Clear();
        _currentWord = "";
        DoViewModelChanged();
    }

    private void CheckForClearOnInput()
    {
        if (ShouldClearOnNextInput)
            Clear();
        ShouldClearOnNextInput = false;
    }

    private void PushCurrentWord()
    {
        if (!string.IsNullOrEmpty(_currentWord))
        {
            _wordService.IncreaseWordUsage(_currentWord, out int wordId);
            _words.Add(new KeyValuePair<int, string>(wordId, _currentWord));
            _currentWord = "";
        }
        DoViewModelChanged();
    }

    private void Backspace()
    {
        if (_currentWord.Length > 0)
            _currentWord = _currentWord.Substring(0, _currentWord.Length - 1);
        else
            PopWord();
        DoViewModelChanged();
    }

    private void PopWord()
    {
        if (_words.Count == 0)
            return;
        KeyValuePair<int, string> wordInfo = _words[_words.Count - 1];
        _words.RemoveAt(_words.Count - 1);
        _wordService.DecreaseWordUsage(wordInfo.Key);
    }

    private void DoViewModelChanged()
    {
        GetWordSuggestions();
        ViewModelChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GetWordSuggestions()
    {
        List<string> result = _phraseService.GetWordSuggestions(_words.Select(x => x.Key), _currentWord, NumberOfSuggestedWords);
        if (result.Count < NumberOfSuggestedWords)
        {
            // Request twice as many words as we need. That way when we add to phrase words and
            // call Distinct we won't end up with fewer than the total number of words required.
            List<string> suggestionsFromDictionary = _wordService.GetWordSuggestions(_currentWord, NumberOfSuggestedWords * 2);
            result.AddRange(suggestionsFromDictionary);
        }
        SuggestedWords = result.Distinct(StringComparer.CurrentCultureIgnoreCase).Take(NumberOfSuggestedWords).ToList();
    }

    private static Dictionary<KeyCode, char> BuildCharMap()
    {
        return new Dictionary<KeyCode, char>
        {
            { KeyCode.A, 'A' }, { KeyCode.B, 'B' }, { KeyCode.C, 'C' }, { KeyCode.D, 'D' },
            { KeyCode.E, 'E' }, { KeyCode.F, 'F' }, { KeyCode.G, 'G' }, { KeyCode.H, 'H' },
            { KeyCode.I, 'I' }, { KeyCode.J, 'J' }, { KeyCode.K, 'K' }, { KeyCode.L, 'L' },
            { KeyCode.M, 'M' }, { KeyCode.N, 'N' }, { KeyCode.O, 'O' }, { KeyCode.P, 'P' },
            { KeyCode.Q, 'Q' }, { KeyCode.R, 'R' }, { KeyCode.S, 'S' }, { KeyCode.T, 'T' },
            { KeyCode.U, 'U' }, { KeyCode.V, 'V' }, { KeyCode.W, 'W' }, { KeyCode.X, 'X' },
            { KeyCode.Y, 'Y' }, { KeyCode.Z, 'Z' },
            { KeyCode.Number0, '0' }, { KeyCode.Number1, '1' }, { KeyCode.Number2, '2' },
            { KeyCode.Number3, '3' }, { KeyCode.Number4, '4' }, { KeyCode.Number5, '5' },
            { KeyCode.Number6, '6' }, { KeyCode.Number7, '7' }, { KeyCode.Number8, '8' },
            { KeyCode.Number9, '9' },
            { KeyCode.Comma, ',' }, { KeyCode.Period, '.' }, { KeyCode.Exclaim, '!' },
            { KeyCode.Question, '?' }
        };
    }
}
