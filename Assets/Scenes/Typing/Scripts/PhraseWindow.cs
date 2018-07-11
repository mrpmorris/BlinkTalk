using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Typing
{
    public class PhraseWindow
    {
        public readonly int? PrecedingWord3Id;
        public readonly int? PrecedingWord2Id;
        public readonly int? PrecedingWord1Id;
        public readonly int SuggestedWordId;

        public PhraseWindow(int wordId)
        {
            SuggestedWordId = wordId;
        }

        public PhraseWindow(int? precedingWord3Id, int? precedingWord2Id, int? precedingWord1Id, int suggestedWordId)
        {
            PrecedingWord3Id = precedingWord3Id;
            PrecedingWord2Id = precedingWord2Id;
            PrecedingWord1Id = precedingWord1Id;
            SuggestedWordId = suggestedWordId;
        }

        public PhraseWindow Next(int wordId)
        {
            return new PhraseWindow(
                precedingWord3Id: PrecedingWord2Id,
                precedingWord2Id: PrecedingWord1Id,
                precedingWord1Id: SuggestedWordId,
                suggestedWordId: wordId);
        }

        public static IEnumerable<PhraseWindow> CreatePhraseWindows(IEnumerable<int> wordIds)
        {
            var result = new List<PhraseWindow>();
            var suggestedWordId = wordIds.First();
            wordIds = wordIds.Skip(1);
            var wordWindow = new PhraseWindow(suggestedWordId);
            result.Add(wordWindow);
            foreach (int wordId in wordIds)
            {
                wordWindow = wordWindow.Next(wordId);
                result.Add(wordWindow);
            }
            return result;
        }
    }
}
