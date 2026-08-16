using System.Collections.Generic;
using System.Text;

namespace BlinkTalk.Application.Text;

/// <summary>
/// Every character a language's keyboard can produce, as the set to test a word against. A word holding
/// anything else can never be reached: prediction is the only way a word surfaces, and it is reached by
/// typing a prefix, so a prefix nobody can type never matches. Such a word is dead weight in the
/// database and in the download.
/// <para>
/// The decorator marks belong in the set as much as the keys do. They are not in
/// <see cref="KeyboardLayout.Rows"/> — the person reaches them through the popup instead — so a set built
/// from the rows alone would reject every Arabic word carrying a haraka, which is one in eight of them.
/// </para>
/// <para>
/// Built from the alphabetical arrangement, and the speed one would do just as well: a test pins the two
/// to the same characters in a different order, so which the person has chosen cannot change the answer.
/// </para>
/// </summary>
public sealed class TypeableCharacters
{
	/// <summary>
	/// Runes rather than chars, so an astral character counts once rather than as the two halves of its
	/// surrogate pair — and so a lone half, which is what a truncated word list has in it, decodes to the
	/// replacement character and is rejected rather than matching one of them.
	/// </summary>
	private readonly HashSet<Rune> Characters;

	private TypeableCharacters(HashSet<Rune> characters)
	{
		Characters = characters;
	}

	/// <summary>The characters <paramref name="language"/>'s keyboard can produce.</summary>
	public static TypeableCharacters For(Language language)
	{
		KeyboardLayout layout = KeyboardLayout.Create(language, KeyboardLayoutStyle.Alphabetical);
		var characters = new HashSet<Rune>();

		foreach (IReadOnlyList<KeyboardKey> row in layout.Rows)
			foreach (KeyboardKey key in row)
				// Text is null on the keys that act rather than type — Space, Backspace and the one that
				// opens the decorator popup. Space typing nothing is what keeps a space out of the set,
				// which is right while the seeded words are words: a phrase would need it back.
				if (key.Kind == KeyboardKeyKind.Character && key.Text is not null)
					Add(characters, key.Text);

		foreach (string decorator in layout.Decorators)
			Add(characters, decorator);

		return new TypeableCharacters(characters);
	}

	/// <summary>
	/// Whether every character of <paramref name="text"/> is one this keyboard can produce. A key that
	/// types more than one character contributes each of them separately, so a word is judged on the
	/// characters it is spelled with rather than on how many dwells spelling it would take.
	/// </summary>
	public bool CanType(string text)
	{
		foreach (Rune rune in text.EnumerateRunes())
			if (!Characters.Contains(rune))
				return false;
		return true;
	}

	private static void Add(HashSet<Rune> characters, string text)
	{
		foreach (Rune rune in text.EnumerateRunes())
			characters.Add(rune);
	}
}
