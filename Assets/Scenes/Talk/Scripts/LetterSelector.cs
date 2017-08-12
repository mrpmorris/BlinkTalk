using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class LetterSelector
    {
        public event EventHandler<LetterSelectedEventArgs> letterSelected;

        private readonly Stack<Selection> selectionHistory;
        private readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly Selection defaultSelection;

        public LetterSelector()
        {
            selectionHistory = new Stack<Selection>();
            defaultSelection = new Selection(0, alphabet.Length - 1);
            DrillUpCompletely();
        }

        public bool CanDrillUp
        {
            get { return selectionHistory.Count > 1; }
        }

        public string GetLeftRange()
        {
            if (currentSelection.lowestIndex == currentSelection.highestIndex)
                return alphabet[currentSelection.lowestIndex] + "";
            return alphabet.Substring(currentSelection.lowestIndex, currentSelection.GetCenterPoint() - currentSelection.lowestIndex + 1);
        }

        public string GetRightRange()
        {
            if (currentSelection.lowestIndex == currentSelection.highestIndex)
                return alphabet[currentSelection.lowestIndex] + "";
            return alphabet.Substring(currentSelection.GetCenterPoint() + 1, currentSelection.highestIndex - currentSelection.GetCenterPoint());
        }

        public string GetFullRange()
        {
            string result = alphabet.Substring(currentSelection.lowestIndex, currentSelection.highestIndex - currentSelection.lowestIndex + 1);
            return result;
        }

        public void DrillDownLeft()
        {
            if (GetLeftRange().Length == 1)
                OnLetterSelected(alphabet[currentSelection.lowestIndex]);
            else
            {
                var newSelection = currentSelection.GetLeftSubSelection();
                selectionHistory.Push(newSelection);
            }
        }

        public void DrillDownRight()
        {
            if (GetRightRange().Length == 1)
                OnLetterSelected(alphabet[currentSelection.highestIndex]);
            else
            {
                var newSelection = currentSelection.GetRightSubSelection();
                selectionHistory.Push(newSelection);
            }
        }
    
        public void DrillUp()
        {
            if (selectionHistory.Count > 1)
                selectionHistory.Pop();
        }

        public void DrillUpCompletely()
        {
            selectionHistory.Clear();
            selectionHistory.Push(defaultSelection);
        }

        private void OnLetterSelected(char letter)
        {
            selectionHistory.Clear();
            selectionHistory.Push(defaultSelection);
            var letterSelected = this.letterSelected;
            if (letterSelected != null)
                letterSelected(this, new LetterSelectedEventArgs(letter));
        }

        private Selection currentSelection
        {
            get { return selectionHistory.Peek(); }
        }


        struct Selection
        {
            public int lowestIndex;
            public int highestIndex;


            public Selection(int lowestIndex, int highestIndex)
            {
                this.lowestIndex = lowestIndex;
                this.highestIndex = highestIndex;
            }

            public int GetCenterPoint()
            {
                return (lowestIndex + highestIndex) / 2;
            }

            public Selection GetLeftSubSelection()
            {
                return new Selection(lowestIndex, GetCenterPoint());
            }

            public Selection GetRightSubSelection()
            {
                return new Selection(GetCenterPoint() + 1, highestIndex);
            }
        }

        public class LetterSelectedEventArgs : EventArgs
        {
            public readonly char letter;
            public LetterSelectedEventArgs(char letter)
            {
                this.letter = letter;
            }
        }
    }
}
