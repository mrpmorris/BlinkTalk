﻿using BlinkTalk.Typing.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SentenceBuilder
    {
        public event EventHandler<EventArgs> ViewModelChanged;
        public bool ShouldClearOnNextInput { get; private set; }
        public IEnumerable<string> SuggestedWords { get; private set; }

        private const int NumberOfSuggestedWords = 6;
        private string CurrentWord = "";
        private List<KeyValuePair<int, string>> Words = new List<KeyValuePair<int, string>>();
        private Dictionary<KeyCode, char> CharsByKeyCode;

        public void Initialize()
        {
            CharsByKeyCode = new Dictionary<KeyCode, char>
            {
                { KeyCode.A, 'A' },
                { KeyCode.B, 'B' },
                { KeyCode.C, 'C' },
                { KeyCode.D, 'D' },
                { KeyCode.E, 'E' },
                { KeyCode.F, 'F' },
                { KeyCode.G, 'G' },
                { KeyCode.H, 'H' },
                { KeyCode.I, 'I' },
                { KeyCode.J, 'J' },
                { KeyCode.K, 'K' },
                { KeyCode.L, 'L' },
                { KeyCode.M, 'M' },
                { KeyCode.N, 'N' },
                { KeyCode.O, 'O' },
                { KeyCode.P, 'P' },
                { KeyCode.Q, 'Q' },
                { KeyCode.R, 'R' },
                { KeyCode.S, 'S' },
                { KeyCode.T, 'T' },
                { KeyCode.U, 'U' },
                { KeyCode.V, 'V' },
                { KeyCode.W, 'W' },
                { KeyCode.X, 'X' },
                { KeyCode.Y, 'Y' },
                { KeyCode.Z, 'Z' },
                { KeyCode.Alpha0, '0' },
                { KeyCode.Alpha1, '1' },
                { KeyCode.Alpha2, '2' },
                { KeyCode.Alpha3, '3' },
                { KeyCode.Alpha4, '4' },
                { KeyCode.Alpha5, '5' },
                { KeyCode.Alpha6, '6' },
                { KeyCode.Alpha7, '7' },
                { KeyCode.Alpha8, '8' },
                { KeyCode.Alpha9, '9' },
                { KeyCode.Comma, ',' },
                { KeyCode.Period, '.' },
                { KeyCode.Exclaim, '!' },
                { KeyCode.Question, '?' }
            };
            GetWordSuggestions();
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(ToString()); }
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
                    CurrentWord += CharsByKeyCode[keyCode];
                    break;
            }
            DoViewModelChanged();
        }

        public string Commit()
        {
            if (!string.IsNullOrEmpty(CurrentWord))
                PushCurrentWord();
            PhraseService.IncrementPhraseUsage(Words.Select(x => x.Key));
            ShouldClearOnNextInput = true;
            DoViewModelChanged();
            return ToString();
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

        private void Clear()
        {
            Words.Clear();
            CurrentWord = "";
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
            if (!string.IsNullOrEmpty(CurrentWord))
            {
                int wordId;
                WordService.IncreaseWordUsage(CurrentWord, out wordId);
                Words.Add(new KeyValuePair<int, string>(wordId, CurrentWord));
                CurrentWord = "";
            }
            DoViewModelChanged();
        }

        private void Backspace()
        {
            if (CurrentWord.Length > 0)
                CurrentWord = CurrentWord.Substring(0, CurrentWord.Length - 1);
            else
                PopWord();
            DoViewModelChanged();
        }

        private void PopWord()
        {
            if (Words.Count == 0)
                return;
            KeyValuePair<int, string> wordInfo = Words[Words.Count - 1];
            Words.RemoveAt(Words.Count - 1);
            WordService.DecreaseWordUsage(wordInfo.Key);
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
                // Request twice as many words than we need. That way when we add to phrase words and call Distinct we won't
                // end up with fewer than the total number of words required
                List<string> suggestionsFromDictionary = WordService.GetWordSuggestions(CurrentWord, NumberOfSuggestedWords * 2);
                result.AddRange(suggestionsFromDictionary);
            }
            SuggestedWords = result.Distinct().Take(NumberOfSuggestedWords);
        }
    }
}
