namespace BlinkTalk.Application;

/// <summary>
/// A language the app can run in — the unit that a keyboard layout, a word list and a database are
/// all chosen by. Only languages the app actually has those things for appear here, so an unsupported
/// culture can never reach the layout or the pack downloader.
/// <para>
/// The member names are load-bearing: each one names that language's pack in repo-root
/// <c>LanguagePacks/</c> (<c>English.zip</c>) and its writable database
/// (<c>BlinkTalk-English.db</c>), so renaming a member renames both files. The numbers are not — the
/// setting stores a culture code, not this enum — so commenting a member out is safe.
/// </para>
/// <para>
/// The commented-out members have a <c>.resx</c> but no letters in <c>Text/Layouts/</c> and no pack,
/// so they stay out until both exist. Their arms in <c>AppLanguage.GetNameForCode</c> are commented
/// out to match.
/// </para>
/// <para>
/// A region with its own <c>.resx</c> does not earn a member either: Brazilian Portuguese is offered
/// in the settings dropdown and translated in <c>Localization.pt-BR.resx</c>, but it types the same
/// letters and predicts from the same word list as <see cref="Portuguese"/>, so it resolves to that.
/// A region only belongs here once it needs a pack of its own.
/// </para>
/// </summary>
public enum Language
{
    Arabic,
    English,
    French,
    German,
    Portuguese,
    Spanish
}
