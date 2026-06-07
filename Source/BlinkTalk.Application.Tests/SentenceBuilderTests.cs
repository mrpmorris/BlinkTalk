using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Tests;

public class SentenceBuilderTests
{
    private static SentenceBuilder Build()
    {
        var sb = new SentenceBuilder(new FakeWordService(), new FakePhraseService());
        sb.Initialize();
        return sb;
    }

    [Fact]
    public void TypesCharactersIntoTheCurrentWord()
    {
        var sb = Build();
        sb.Input(KeyCode.H);
        sb.Input(KeyCode.I);
        Assert.Equal("HI", sb.ToString().Trim());
    }

    [Fact]
    public void SpacePushesTheCurrentWord()
    {
        var sb = Build();
        sb.Input(KeyCode.H);
        sb.Input(KeyCode.I);
        sb.Input(KeyCode.Space);
        sb.Input(KeyCode.U);
        Assert.Equal("HI U", sb.ToString().Trim());
    }

    [Fact]
    public void BackspaceDeletesCharThenPopsWord()
    {
        var sb = Build();
        sb.Input(KeyCode.H);
        sb.Input(KeyCode.I);
        sb.Input(KeyCode.Backspace); // removes 'I'
        Assert.Equal("H", sb.ToString().Trim());

        sb.Input(KeyCode.I);          // "HI"
        sb.Input(KeyCode.Space);      // push "HI"
        sb.Input(KeyCode.Backspace);  // current empty -> pop "HI"
        Assert.Equal("", sb.ToString().Trim());
    }

    [Fact]
    public void CommitReturnsSentenceAndFlagsClearOnNextInput()
    {
        var sb = Build();
        sb.Input(KeyCode.B);
        sb.Input(KeyCode.Y);
        sb.Input(KeyCode.E);

        string committed = sb.Commit();

        Assert.Equal("BYE", committed.Trim());
        Assert.True(sb.ShouldClearOnNextInput);
    }

    [Fact]
    public void NextInputAfterCommitClearsTheSentence()
    {
        var sb = Build();
        sb.Input(KeyCode.B);
        sb.Commit();
        Assert.True(sb.ShouldClearOnNextInput);

        sb.Input(KeyCode.A);

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
    public void IsEmptyReflectsContent()
    {
        var sb = Build();
        Assert.True(sb.IsEmpty);
        sb.Input(KeyCode.A);
        Assert.False(sb.IsEmpty);
    }

    [Fact]
    public void RaisesViewModelChangedOnInput()
    {
        var sb = Build();
        int changes = 0;
        sb.ViewModelChanged += (s, e) => changes++;
        sb.Input(KeyCode.A);
        Assert.True(changes > 0);
    }
}
