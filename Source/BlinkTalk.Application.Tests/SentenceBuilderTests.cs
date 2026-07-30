using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

public class SentenceBuilderTests
{
    [Fact]
    public void BackspaceDeletesCharThenPopsWord()
    {
        var sb = Build();
        sb.Input(Key("H"));
        sb.Input(Key("I"));
        sb.Input(KeyboardKey.Backspace); // removes 'I'
        Assert.Equal("H", sb.ToString().Trim());

        sb.Input(Key("I"));              // "HI"
        sb.Input(KeyboardKey.Space);     // push "HI"
        sb.Input(KeyboardKey.Backspace); // current empty -> pop "HI"
        Assert.Equal("", sb.ToString().Trim());
    }

    [Fact]
    public void ClearThrowsAwayEverything()
    {
        var sb = Build();
        sb.Input(Key("H"));
        sb.Input(KeyboardKey.Space);
        sb.Input(Key("I"));

        sb.Clear();

        Assert.True(sb.IsEmpty);
        Assert.Equal("", sb.CurrentWord);
    }

    [Fact]
    public void ClearAfterCommitDoesNotLeaveTheSentencePrimedToClearAgain()
    {
        var sb = Build();
        sb.Input(Key("B"));
        sb.Commit();

        sb.Clear();

        // Without resetting the flag, the first letter typed in the new language would clear itself
        // out from under the person.
        Assert.False(sb.ShouldClearOnNextInput);
        sb.Input(Key("A"));
        Assert.Equal("A", sb.ToString().Trim());
    }

    [Fact]
    public void CommitReturnsSentenceAndFlagsClearOnNextInput()
    {
        var sb = Build();
        sb.Input(Key("B"));
        sb.Input(Key("Y"));
        sb.Input(Key("E"));

        string committed = sb.Commit();

        Assert.Equal("BYE", committed.Trim());
        Assert.True(sb.ShouldClearOnNextInput);
    }

    [Fact]
    public void DecoratorTextIsAppendedToTheLetterItFollows()
    {
        var sb = Build();
        sb.Input(Key("ش"));

        sb.InputText("\u064E"); // ARABIC FATHA

        Assert.Equal("ش\u064E", sb.CurrentWord);
    }

    [Fact]
    public void IsEmptyReflectsContent()
    {
        var sb = Build();
        Assert.True(sb.IsEmpty);
        sb.Input(Key("A"));
        Assert.False(sb.IsEmpty);
    }

    [Fact]
    public void NextInputAfterCommitClearsTheSentence()
    {
        var sb = Build();
        sb.Input(Key("B"));
        sb.Commit();
        Assert.True(sb.ShouldClearOnNextInput);

        sb.Input(Key("A"));

        Assert.Equal("A", sb.ToString().Trim());
        Assert.False(sb.ShouldClearOnNextInput);
    }

    [Fact]
    public void PushWordAppendsASuggestedWord()
    {
        var sb = Build();
        sb.PushWord("hello");
        sb.PushWord("world");
        Assert.Equal("hello world", sb.ToString().Trim());
    }

    [Fact]
    public void RaisesViewModelChangedOnInput()
    {
        var sb = Build();
        int changes = 0;
        sb.ViewModelChanged += (s, e) => changes++;
        sb.Input(Key("A"));
        Assert.True(changes > 0);
    }

    [Fact]
    public void SpacePushesTheCurrentWord()
    {
        var sb = Build();
        sb.Input(Key("H"));
        sb.Input(Key("I"));
        sb.Input(KeyboardKey.Space);
        sb.Input(Key("U"));
        Assert.Equal("HI U", sb.ToString().Trim());
    }

    [Fact]
    public void TheDecoratorKeyTypesNothingAndSayingOtherwiseIsABug()
    {
        var sb = Build();

        // The column selector opens the popup instead of handing this key over, so getting here at
        // all means the scanning is wrong — fail loudly rather than swallow a keypress.
        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Input(KeyboardKey.Decorators));
    }

    [Fact]
    public void TypesCharactersIntoTheCurrentWord()
    {
        var sb = Build();
        sb.Input(Key("H"));
        sb.Input(Key("I"));
        Assert.Equal("HI", sb.ToString().Trim());
    }

    private static SentenceBuilder Build()
    {
        var sb = new SentenceBuilder(new FakeWordService(), new FakePhraseService());
        sb.Initialize();
        return sb;
    }

    private static KeyboardKey Key(string text) => KeyboardKey.Character(text);
}
