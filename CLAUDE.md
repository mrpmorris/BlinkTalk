# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

BlinkTalk is a single-switch AAC (augmentative communication) app for someone with locked-in
syndrome. A helper points the screen at the person and taps **anywhere** whenever they indicate
(blink, look up, whatever they can repeat) — or the device camera watches for a trained facial
gesture. The UI continuously **scans** — highlighting one option at a time on a timer — and the
indication "selects" whatever is highlighted. Through a hierarchy of scanners the person spells
letters, picks predicted words, and speaks sentences aloud. A SQLite dictionary plus an n-gram model
learns the person's vocabulary over time. This is a .NET MAUI Blazor Hybrid rewrite of an original
Unity3D app, localised into English/French/German/Spanish/Portuguese.

## Projects

Under `Source/` (all in `BlinkTalk.sln`):

- **`BlinkTalk.Application`** (`netstandard2.0`) — all the scanning/prediction/persistence logic.
  **Contains no MAUI or Blazor types** and is unit-tested on plain .NET. Platform concerns enter only
  through interfaces in `Abstractions/` (`IUIDispatcher`, `ITextToSpeechService`, `ISettingsStore`,
  `IClock`, `IIndicator`) and `Persistence/IDatabaseProvisioner`.
- **`BlinkTalk`** (MAUI Blazor Hybrid) — the host. `Components/` holds the Razor UI; `Services/`
  holds the MAUI implementations of the Application interfaces. Word lists (zipped
  `Word,LanguageUsageCount` CSVs) are **not bundled**: they live in repo-root `LanguagePacks/` and are
  downloaded on demand from GitHub raw (`LanguagePackDownloader.UrlFormat`) by the Settings page.
- **`BlinkTalk.Resources`** (`net10.0`) — `Localization.resx` + per-language satellites. **All
  user-visible strings go here**, accessed as `Localization.SomeKey`; `AppLanguage` drives
  `Localization.Culture`.
- **`BlinkTalk.Application.Tests`** (xUnit) — tests for the Application project. Links the real
  `LanguagePacks/English.zip` for parity tests.
- **`Source/Installer/BlinkTalk.wixproj`** — WiX MSI wrapping the self-contained Windows publish.

> Naming gotcha: assembly `BlinkTalk.Application` and the app's root namespace `BlinkTalk`. Because
> `BlinkTalk.Application` (a namespace) would shadow MAUI's `Application` type inside the `BlinkTalk`
> namespace, `App.xaml.cs` derives from the fully-qualified `Microsoft.Maui.Controls.Application`.

## Commands

Run from the repo root.

```bash
# Tests — the primary fast feedback loop
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

`BlinkTalk.csproj` computes `TargetFrameworks` conditionally: Android always, plus iOS/MacCatalyst
off Linux, plus Windows on Windows. Passing `-p:WindowsPublish=true` narrows it to the Windows TFM
alone (a deliberately non-reserved property name, so it does not flow into project references) —
that is how CI publishes Windows on a runner with only the `maui-windows` workload. Always pass
`-f <tfm>` when building the app project; iOS/Mac Catalyst need a Mac host.
`dotnet build-server shutdown` releases file locks if a rename/delete fails.

**Releasing** is tag-driven (`.github/workflows/release.yml`): pushing a bare `N.N.N` tag whose
commit is on `master` validates, builds the MSI + APK + AAB, and creates the GitHub release. The
version code is `major*10000 + minor*100 + patch`. Android signing needs the four `ANDROID_*`
repo secrets or the job fails deliberately (rather than shipping a debug-signed package).

## Code style

`.editorconfig` is authoritative and unusual: **hard tabs**, Allman braces, `System.*` usings *not*
sorted first. Private fields are PascalCase (`private readonly IClock Clock;`) — match that, not
`_camelCase`. Razor components keep their logic in a **code-behind `.razor.cs`** partial class, not
in an inline `@code` block.

## Architecture (the parts that span files)

**Scanning is a stack of strategies.** `ScanController` (the logical port of the original Unity
`TypingController`) owns a `Stack<IInputStrategy>` and the `SentenceBuilder`. `Push<T>()` enters a
deeper scan level; `Pop()` returns to the parent. The strategies, in `Input/Strategies/`:
`SectionSelector` (top: WordSelector / Keyboard / Speak — the `Section` enum order *is* the scan
order) → `KeyboardRowSelector` → `KeyboardColumnSelector`, and `WordSuggestionSelector`. The single
switch is inverted: each input source implements `IIndicator` and raises `Indicated`; the concrete
sources derive from `IndicatorBase` in `Services/Indicators/Indicators.cs` (`PointerIndicator`,
`KeyboardIndicator`, `CameraGestureIndicator`). `ScanController` subscribes to every registered
`IIndicator` in its constructor and routes the signal via `strategies.Peek().ReceiveIndication()`.

**`FocusCycler` is the timer.** Each strategy creates one via `controller.NewCycler(...)`. It advances
a focus index, calling `focusChanged` for each index where `mayFocus` is true, after a dwell; indices
that fail `mayFocus` are **skipped without consuming a dwell**. These behaviors are load-bearing and
must be preserved (they have tests): the first dwell is longer (`FirstCycleDelayMultiplier`),
strategies auto-exit (pop) after `FocusChangeCount` exceeds a threshold (rows `> n+1`, keys `> n+2`,
words `> n+1`), and the section selector never auto-exits. A cycler can also `Pause()`/`Resume()` —
`ScanController` pauses the active cycler while a camera gesture is being held, and the paused span is
excluded so the dwell resumes with the time that was actually remaining. See `Input/Strategies/*`,
`Text/Consts`, and `FocusCyclerPauseTests`.

**UI-thread marshaling is the #1 correctness rule.** `FocusCycler`'s delay continuation runs on the
thread pool, so every scan callback is funnelled through `IUIDispatcher` (MAUI `MainThread` in the app,
inline in tests) — this keeps all mutation single-threaded, the guarantee Unity gave for free. In Razor,
subscribe to `ScanController.StateChanged` and call `InvokeAsync(StateHasChanged)`; **never** call
`StateHasChanged` directly from a scan callback. The whole screen is the indicate surface (`Type.razor`):
tap anywhere or press Space/Enter. The highlight is rendered purely in CSS (`wwwroot/css/blinktalk.css`),
with the pulse colour keyed to `ScanController.Depth`.

**Camera indicator.** `wwwroot/js/blinktalk-camera.js` runs MediaPipe FaceLandmarker (WASM) over
`getUserMedia` and reports per-frame "blendshape" scores; training picks the blendshape that separates
best between relaxed and indicating, and detection fires when it crosses the learned threshold *and*
has been held for `DwellSeconds` (reflex blinks are ~0.1–0.2s, so the hold time is what rejects them).
The JS calls back into `CameraIndicator.razor.cs`, which drives `CameraGestureIndicator`
(`TriggerDwellStarted`/`TriggerDwellEnded` pause and resume the scan while a gesture is held).
`CameraIndicatorConfig` stores signal/threshold/dwell/trained via `ISettingsStore`, but **whether the
camera is enabled is session-only** — always false at launch, by design. Android needs the WebView
tweaks applied in `MauiProgram` (`BlinkTalkWebChromeClient`, autoplay). Known gap: the MediaPipe
runtime and model load from a CDN on first use, so first-run camera setup needs a network connection.

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
because Microsoft.Data.Sqlite returns INTEGER as `long`. On first use `IDatabaseProvisioner` resolves the
writable app-data path, then `AutoMigratingDatabase.Migrate()` creates the schema with SQL (idempotent
`CREATE ... IF NOT EXISTS`), seeds the `Words` dictionary via `ISeedWordSource` when the table is empty,
and prunes word sequences older than 30 days. The registered seed source is `EmptySeedWordSource`; real
seeding happens when the Settings page's Done button finds no database for the selected language
(`IDatabaseProvisioner.DatabaseExists()`): it downloads the pack from GitHub raw
(`Persistence/LanguagePackDownloader`, progress + cancel in a modal) and passes a one-shot
`InMemoryZipSeedWordSource` (parsed by `WordListZipReader`) to
`AppDatabase.OpenForCurrentLanguage(seedOverride)`.

**Language is global state, set before DI exists.** `MauiProgram` hand-builds a
`MauiPreferencesSettings` and calls `AppLanguage.RestorePersisted` *before* the builder, because the
culture decides both resource lookups and the database filename (`BlinkTalk-French.db`) — one database
per language, so a French UI never inherits an English dictionary. `AppLanguage.SetCurrent` sets the
`DefaultThreadCurrent*` pair as well as the current thread's, because scan callbacks arrive on
thread-pool threads; read `AppLanguage.Current`, never `CultureInfo.CurrentUICulture`. Supported
languages are the switch in `AppLanguage.GetNameForCode` — adding one means a `.resx`, a
`LanguagePacks/<Name>.zip`, and an arm there.

**Settings:** keys live in one place, `Abstractions/ISettingsStore.cs` (`SettingsKeys`), backed by MAUI
`Preferences`. `ScanController.CycleDelaySeconds` is the scan speed; its default and the longer
first-item dwell are in `Text/Consts`.

**TTS:** `MauiTtsService` uses MAUI `TextToSpeech` — low pitch, full volume, flush-via-cancel, voice
resolved from the current culture (exact tag → same language → language-only, cached per culture).
All-caps words are lower-cased before speaking, because engines read a short all-caps token as an
initialism. Known gap: MAUI exposes no cross-platform speaking *rate*, so the original's slow rate
(0.4) is not applied — that needs a per-platform shim (Android `setSpeechRate`, iOS/Mac
`AVSpeechUtterance.Rate`, Windows SSML).

## Adding to the keyboard or keys

The keyboard layout (rows of keys) is `Text/KeyboardLayout.CreateDefault()` — the single source for both
scanning and rendering. Valid keys are the `Text/KeyCode` enum; display labels are in `Text/KeyDisplay`.
The char map (KeyCode → character) is in `SentenceBuilder`; Space and Backspace are keys, not characters.
