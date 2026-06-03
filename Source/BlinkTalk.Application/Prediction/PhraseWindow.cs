using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Application.Prediction
{
    /// <summary>
    /// One row of the 4-word sliding window used to learn word sequences: three preceding
    /// word ids (nullable at the start of a sentence) and the word they led to. Ported
    /// verbatim from the original.
    /// </summary>
    public sealed class PhraseWindow
    {
        public int? PrecedingWord3Id { get; }
        public int? PrecedingWord2Id { get; }
        public int? PrecedingWord1Id { get; }
        public int SuggestedWordId { get; }

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
