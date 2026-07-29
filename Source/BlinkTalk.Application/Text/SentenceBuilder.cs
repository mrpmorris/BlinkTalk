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
    /// <summary>The word currently being typed (empty at a committed word boundary). Exposed as
    /// raw state because <see cref="ToString"/> can't distinguish it from a trailing space.</summary>
    public string CurrentWord { get; private set; } = "";
    public bool ShouldClearOnNextInput { get; private set; }
    public IReadOnlyList<string> SuggestedWords { get; private set; } = Array.Empty<string>();

    public event EventHandler? ViewModelChanged;

    private const int NumberOfSuggestedWords = 6;
    private readonly IPhraseService PhraseService;
    private readonly List<KeyValuePair<int, string>> Words = new List<KeyValuePair<int, string>>();
    private readonly IWordService WordService;

    public SentenceBuilder(IWordService wordService, IPhraseService phraseService)
    {
        WordService = wordService;
        PhraseService = phraseService;
    }

    public bool IsEmpty => string.IsNullOrEmpty(ToString());

    public string Commit()
    {
        if (!string.IsNullOrEmpty(CurrentWord))
            PushCurrentWord();
        PhraseService.IncrementPhraseUsage(Words.Select(x => x.Key));
        ShouldClearOnNextInput = true;
        DoViewModelChanged();
        return ToString();
    }

    /// <summary>Must be called once the database is available (loads the initial suggestions).</summary>
    public void Initialize()
    {
        GetWordSuggestions();
    }

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
                CurrentWord += KeyCharacters.TextOf(keyCode);
                break;
        }
        DoViewModelChanged();
    }

    /// <summary>
    /// Appends text that no single key types: an accented letter composed from a base letter and a
    /// diacritic. Some of those are two characters rather than one (an Arabic letter followed by its
    /// harakat), which is why this exists alongside <see cref="Input"/>.
    /// </summary>
    public void InputText(string text)
    {
        CheckForClearOnInput();
        CurrentWord += text;
        DoViewModelChanged();
    }

    public void PushWord(string word)
    {
        CheckForClearOnInput();
        CurrentWord = word;
        PushCurrentWord();
    }

    public override string ToString()
    {
        string result = string.Join(" ", Words.Select(x => x.Value));
        if (!string.IsNullOrEmpty(CurrentWord))
            result += " " + CurrentWord;
        return result;
    }

    /// <summary>
    /// Deletes one character, or the last committed word when the current word is already empty.
    /// One character means one code unit, so backspacing an Arabic letter that carries a harakat
    /// removes the harakat and leaves the letter — which is what someone correcting a mis-picked
    /// diacritic wants.
    /// </summary>
    private void Backspace()
    {
        if (CurrentWord.Length > 0)
            CurrentWord = CurrentWord.Substring(0, CurrentWord.Length - 1);
        else
            PopWord();
        DoViewModelChanged();
    }

    private void CheckForClearOnInput()
    {
        if (ShouldClearOnNextInput)
            Clear();
        ShouldClearOnNextInput = false;
    }

    private void Clear()
    {
        Words.Clear();
        CurrentWord = "";
        DoViewModelChanged();
    }

    private void DoViewModelChanged()
    {
        GetWordSuggestions();
        ViewModelChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GetWordSuggestions()
    {
        List<string> result = PhraseService.GetWordSuggestions(Words.Select(x => x.Key), CurrentWord, NumberOfSuggestedWords);
        if (result.Count < NumberOfSuggestedWords)
        {
            // Request twice as many words as we need. That way when we add to phrase words and
            // call Distinct we won't end up with fewer than the total number of words required.
            List<string> suggestionsFromDictionary = WordService.GetWordSuggestions(CurrentWord, NumberOfSuggestedWords * 2);
            result.AddRange(suggestionsFromDictionary);
        }
        SuggestedWords = result.Distinct(StringComparer.CurrentCultureIgnoreCase).Take(NumberOfSuggestedWords).ToList();
    }

    private void PopWord()
    {
        if (Words.Count == 0)
            return;
        KeyValuePair<int, string> wordInfo = Words[Words.Count - 1];
        Words.RemoveAt(Words.Count - 1);
        WordService.DecreaseWordUsage(wordInfo.Key);
    }

    private void PushCurrentWord()
    {
        if (!string.IsNullOrEmpty(CurrentWord))
        {
            WordService.IncreaseWordUsage(CurrentWord, out int wordId);
            Words.Add(new KeyValuePair<int, string>(wordId, CurrentWord));
            CurrentWord = "";
        }
        DoViewModelChanged();
    }
}
