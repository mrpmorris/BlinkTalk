using System.Collections.Generic;

namespace BlinkTalk.Application.Prediction;

public interface IWordService
{
    void IncreaseWordUsage(string word, out int wordId);
    void DecreaseWordUsage(int wordId);
    List<string> GetWordSuggestions(string? currentWord, int numberOfWords);
}
