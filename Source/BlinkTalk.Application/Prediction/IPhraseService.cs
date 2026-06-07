using System.Collections.Generic;

namespace BlinkTalk.Application.Prediction;

public interface IPhraseService
{
    void IncrementPhraseUsage(IEnumerable<int> wordIds);
    List<string> GetWordSuggestions(IEnumerable<int> wordIds, string? currentWord, int numberOfWords);
}
