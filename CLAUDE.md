# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

BlinkTalk is a single-switch AAC (augmentative communication) app for someone with locked-in
syndrome. A helper points the screen at the person and presses **one** large button whenever they
blink. The UI continuously **scans** — highlighting one option at a time on a timer — and the blink
"selects" whatever is highlighted. Through a hierarchy of scanners the person spells letters, picks
predicted words, and speaks sentences aloud. A bundled SQLite database plus an n-gram model learns the
person's vocabulary over time. This is a .NET MAUI Blazor Hybrid rewrite of an original Unity3D app.

## Projects

The solution (`BlinkTalk.sln`) has three projects under `Source/`:

- **`BlinkTalk.Application`** (`netstandard2.0`) — all the scanning/prediction/persistence logic.
  **Contains no MAUI or Blazor types** and is unit-tested on plain .NET. Platform concerns enter only
  through interfaces in `Abstractions/` (`IUiDispatcher`, `ITextToSpeechService`, `ISettingsStore`,
  `IClock`) and `Persistence/IDatabaseProvisioner`.
- **`BlinkTalk`** (MAUI Blazor Hybrid, multi-targets `net10.0-android;-ios;-maccatalyst;-windows`) —
  the host. `Components/` holds the Razor UI; `Services/` holds the MAUI implementations of the Core
  interfaces; `Resources/Raw/English.db` is the shipped database (a `MauiAsset`).
- **`BlinkTalk.Application.Tests`** (xUnit) — tests for Core. Links the real `English.db` for parity tests.

> Naming gotcha: assembly `BlinkTalk.Application` and the app's root namespace `BlinkTalk`. Because
> `BlinkTalk.Application` (a namespace) would shadow MAUI's `Application` type inside the `BlinkTalk`
> namespace, `App.xaml.cs` derives from the fully-qualified `Microsoft.Maui.Controls.Application`.

## Commands

Run from the repo root.

```bash
# Tests (Core) — the primary fast feedback loop
dotnet test Source/BlinkTalk.Application.Tests/BlinkTalk.Application.Tests.csproj

# A single test class / method
dotnet test Source/BlinkTalk.Application.Tests/BlinkTalk.Application.Tests.csproj --filter "FullyQualifiedName~FocusCyclerTests"

# Build the logic library on its own
dotnet build Source/BlinkTalk.Application/BlinkTalk.Application.csproj

# Build / run the app on Windows (primary dev target)
dotnet build Source/BlinkTalk/BlinkTalk.csproj -f net10.0-windows10.0.19041.0
dotnet run   --project Source/BlinkTalk/BlinkTalk.csproj -f net10.0-windows10.0.19041.0

# Deploy+run on a connected Android device/emulator
dotnet build Source/BlinkTalk/BlinkTalk.csproj -t:Run -f net10.0-android
```

Do **not** `dotnet build BlinkTalk.sln` on Windows: it tries every app TFM, and `-ios`/`-maccatalyst`
require a Mac. Always pass `-f <tfm>` when building the `BlinkTalk` app. iOS/Mac Catalyst builds need a
Mac host. `dotnet build-server shutdown` releases file locks if a rename/delete fails.

## Architecture (the parts that span files)

**Scanning is a stack of strategies.** `ScanController` (the logical port of the original Unity
`TypingController`) owns a `Stack<IInputStrategy>` and the `SentenceBuilder`. `Push<T>()` enters a
deeper scan level; `Pop()` returns to the parent. The strategies, in `Input/Strategies/`:
`SectionSelector` (top: WordSelector / Keyboard / Speak) → `KeyboardRowSelector` → `KeyboardColumnSelector`,
and `WordSuggestionSelector`. The single switch is `ScanController.Indicate()`, which routes to
`strategies.Peek().ReceiveIndication()`.

**`FocusCycler` is the timer.** Each strategy creates one via `controller.NewCycler(...)`. It advances a
focus index, calling `focusChanged` for each index where `mayFocus` is true, after a dwell; indices that
fail `mayFocus` are **skipped without consuming a dwell**. These behaviors are load-bearing and must be
preserved (they have tests): the first dwell is longer (`FirstCycleDelayMultiplier`), strategies auto-exit
(pop) after `FocusChangeCount` exceeds a threshold (rows `> n+1`, keys `> n+2`, words `> n+1`), and the
section selector never auto-exits. See `Input/Strategies/*` and `Consts`.

**UI-thread marshaling is the #1 correctness rule.** `FocusCycler`'s delay continuation runs on the
thread pool, so every scan callback is funnelled through `IUiDispatcher` (MAUI `MainThread` in the app,
inline in tests) — this keeps all mutation single-threaded, the guarantee Unity gave for free. In Razor,
subscribe to `ScanController.StateChanged` and call `InvokeAsync(StateHasChanged)`; **never** call
`StateHasChanged` directly from a scan callback. The whole screen is the indicate surface (`Type.razor`):
tap anywhere or press Space/Enter. The highlight is rendered purely in CSS (`wwwroot/css/blinktalk.css`),
with the pulse colour keyed to `ScanController.Depth`.

**Prediction is two layers of raw SQL over SQLite** (`Prediction/`). `Words(ID, Word, UserSelectionCount,
LanguageUsageCount)` is the dictionary; `WordSequences(PrecedingWord{1,2,3}Id, SuggestedWordId, UsageCount,
LastUsedDate)` stores a 4-word sliding-window n-gram. `PhraseService` scores next-word candidates by how
the last three words match the preceding slots (a weighted SQL `CASE`), then usage/recency; `WordService`
is the prefix-based dictionary fallback. **Keep the scoring/ordering SQL identical** — suggestion order is
behavioral. Null preceding-word ids are stored as the sentinel `-1`, not SQL NULL.

**Persistence conventions** (`Persistence/`): all SQL goes through `ISqliteDatabase`
(`MicrosoftDataSqliteDatabase`) returning a small `DataTable`/`DataRow` shim, so the services read almost
like the original. Two rules: **bind user-entered text as parameters** (the original interpolated it,
which broke on apostrophes and was injectable), and read integer columns with `Convert.ToInt32(...)`
because Microsoft.Data.Sqlite returns INTEGER as `long`. On startup the bundled read-only `English.db` is
copied to writable app-data by `IDatabaseProvisioner` (the package asset can't be written to, especially
on Android), then `AutoMigratingDatabase.Migrate()` prunes word sequences older than 30 days.

**Settings / scan speed:** `ScanController.CycleDelaySeconds` reads/writes `ISettingsStore`
(MAUI `Preferences`); the slider lives in `Components/Pages/Settings.razor`. Default and the longer
first-item dwell are in `Text/Consts`.

**TTS:** `MauiTtsService` uses MAUI `TextToSpeech` (en-GB, pitch/volume, flush-via-cancel). Known gap:
MAUI exposes no cross-platform speaking *rate*, so the original's slow rate (0.4) is not yet applied —
that needs a per-platform shim (Android `setSpeechRate`, iOS/Mac `AVSpeechUtterance.Rate`, Windows SSML).

## Adding to the keyboard or keys

The keyboard layout (rows of keys) is `Text/KeyboardLayout.CreateDefault()` — the single source for both
scanning and rendering. Valid keys are the `Text/KeyCode` enum; display labels are in `Text/KeyDisplay`.
The char map (KeyCode → character) is in `SentenceBuilder`; Space and Backspace are keys, not characters.
