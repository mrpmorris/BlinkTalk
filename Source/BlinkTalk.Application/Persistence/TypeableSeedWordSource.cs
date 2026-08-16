using System;
using System.Collections.Generic;
using System.Text;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// Wraps a word source and lets through only the words the language's keyboard can spell. The word
/// lists are already built that way — the pack builder drops what its own layout cannot type — so on a
/// pack that came from there this changes nothing. It is here for the ones that did not: a list edited
/// by hand, or a keyboard that lost a letter after the pack that used it was published.
/// </summary>
public sealed class TypeableSeedWordSource : ISeedWordSource
{
    private readonly ISeedWordSource Inner;
    private readonly Language Language;

    public TypeableSeedWordSource(ISeedWordSource inner, Language language)
    {
        Inner = inner;
        Language = language;
    }

    public IEnumerable<(string Word, int LanguageUsageCount)> GetWords()
    {
        TypeableCharacters typeable = TypeableCharacters.For(Language);
        int offered = 0;
        int kept = 0;

        foreach ((string Word, int LanguageUsageCount) word in Inner.GetWords())
        {
            offered++;

            if (typeable.CanType(word.Word))
            {
                // The path every word of every shipped pack takes: yielded exactly as it was stored.
                kept++;
                yield return word;
                continue;
            }

            // Only now, having failed, is the word worth composing. A list written in decomposed form
            // spells é as e plus a combining acute, which no keyboard here has a key for, so every one
            // of its accented words would otherwise be thrown away. Composing only what has already
            // failed means a word that types as it stands is never rewritten on its way to the
            // database — and some scripts do reorder under composition, Arabic's marks among them.
            string composed = word.Word.Normalize(NormalizationForm.FormC);
            if (composed != word.Word && typeable.CanType(composed))
            {
                kept++;
                yield return (composed, word.LanguageUsageCount);
            }
        }

        // A pack whose every word is unspellable is not a pack with nothing to offer; it is the wrong
        // pack, or a right one built wrongly — a word list left in lower case, say, against a keyboard
        // that is upper case throughout. Seeding it would leave a dictionary that silently predicts
        // nothing, so it fails instead: the seed transaction rolls back, the half-built file is
        // deleted, and the person is told the pack could not be installed.
        if (offered > 0 && kept == 0)
            throw new InvalidOperationException(
                $"None of the {offered:N0} words in the {Language} word list can be typed on the " +
                $"{Language} keyboard, so the dictionary would predict nothing.");
    }
}
