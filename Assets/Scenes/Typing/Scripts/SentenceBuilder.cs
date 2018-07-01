using System.Collections.Generic;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SentenceBuilder
    {
        private List<string> Words = new List<string>();
        private string CurrentWord = "";
        private Dictionary<KeyCode, char> CharsByKeyCode;

        public SentenceBuilder()
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
            //TODO: PeteM - DB
            {
                Words = new List<string>
                {
                    "THIS", "IS", "A"
                };
                CurrentWord = "TEST";
            }
        }

        public void Clear()
        {
            Words.Clear();
            CurrentWord = "";
        }

        public void Input(KeyCode keyCode)
        {
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
        }

        public void PushWord(string word)
        {
            CurrentWord = word;
            PushCurrentWord();
        }

        public override string ToString()
        {
            string result = string.Join(" ", Words);
            if (!string.IsNullOrEmpty(CurrentWord))
                result += " " + CurrentWord;
            return result;
        }

        private void PushCurrentWord()
        {
            if (!string.IsNullOrEmpty(CurrentWord))
            {
                Words.Add(CurrentWord);
                CurrentWord = "";
            }
        }

        private void Backspace()
        {
            if (CurrentWord.Length > 0)
                CurrentWord = CurrentWord.Substring(0, CurrentWord.Length - 1);
            else
                PopWord();
        }

        private void PopWord()
        {
            if (Words.Count == 0)
                return;
            Words.RemoveAt(Words.Count - 1);
        }
    }
}
