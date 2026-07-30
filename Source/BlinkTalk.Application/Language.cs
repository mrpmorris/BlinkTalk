namespace BlinkTalk.Application;

/// <summary>
/// A language the app can run in — the unit that a keyboard layout, an accent scheme, a word list
/// and a database are all chosen by. Only languages the app actually has those things for appear
/// here, so an unsupported culture can never reach the layout or the pack downloader.
/// <para>
/// The member names are load-bearing: each one names that language's pack in repo-root
/// <c>LanguagePacks/</c> (<c>English.zip</c>) and its writable database
/// (<c>BlinkTalk-English.db</c>), so renaming a member renames both files.
/// </para>
/// </summary>
public enum Language
{
    English,
    French,
    German,
    Spanish,
    Portuguese,
    Arabic
}
