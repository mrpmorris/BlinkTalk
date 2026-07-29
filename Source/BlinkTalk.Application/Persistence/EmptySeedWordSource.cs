using System.Collections.Generic;
using System.Linq;

namespace BlinkTalk.Application.Persistence;

/// <summary>
/// A seed source with no words. The default registration: word lists are no longer bundled with
/// the app, so a database opened outside the settings-page download flow is created empty and is
/// seeded later from a downloaded <see cref="InMemoryZipSeedWordSource"/>.
/// </summary>
public sealed class EmptySeedWordSource : ISeedWordSource
{
    public IEnumerable<(string Word, int LanguageUsageCount)> GetWords() =>
        Enumerable.Empty<(string, int)>();
}
