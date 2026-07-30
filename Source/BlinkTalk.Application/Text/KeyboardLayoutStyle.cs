namespace BlinkTalk.Application.Text;

/// <summary>
/// Which arrangement of a language's letters the keyboard uses. Chosen on the settings page and
/// persisted, because which one is faster depends on the person: one is easier to find a letter in,
/// the other is quicker once the positions are known.
/// </summary>
public enum KeyboardLayoutStyle
{
	/// <summary>The language's letters in its own alphabetical order.</summary>
	Alphabetical,

	/// <summary>Most-used letters earliest in the scan, so common words cost fewer dwells.</summary>
	Speed
}
