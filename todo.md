# BlinkTalk → Fluxor migration plan

## Goal

Replace the controller-based scan architecture — `ScanController`, the `IInputStrategy`
stack, `FocusCycler`, and the `IIndicator` event sources all talking to each other — with a
descriptive **Fluxor** model: actions, reducers, effects and state. The Fluxor model becomes
the application's logic (an "API" you can read), the UI becomes a thin consumer that only
dispatches actions and renders state, and a different UI could be swapped in later without
touching the logic.

## What is being replaced

| Today (in `BlinkTalk.Application`) | Tomorrow (in `BlinkTalk.Fluxor`) |
| --- | --- |
| `ScanController` + `IScanController` (owns strategy stack, subscribes to indicators) | State + reducers + effects. Deleted. |
| `FocusCycler` (the timer) | A **stateful effect**: `ScanTimerEffect` (the timer is the effect's own state). |
| `IInputStrategy` + `Input/Strategies/*` (section/row/column/word/decorator selectors) | Pure reducers (the scan-level state machine). Deleted. |
| `IIndicator`, `IndicatorBase`, `PointerIndicator`, `KeyboardIndicator`, `CameraGestureIndicator` (events) | Actions: `IndicationDetectedAction`, `IndicatorStartedAction`, `IndicatorStoppedAction`. Deleted. |
| `StateChanged` event → `InvokeAsync(StateHasChanged)` | Fluxor `IState<T>.Changed` → `StateHasChanged`. |

`SentenceBuilder`, prediction, persistence, keyboards and `AppLanguage` stay in
`BlinkTalk.Application` (domain services). The scanning *state machine* moves out of it.

## Architecture decisions (agreed up front)

1. **New project `Source/BlinkTalk.Fluxor`** (net10.0) holds the whole Fluxor model — state,
   actions, reducers, effects, selectors. It references `BlinkTalk.Application` +
   `BlinkTalk.Resources`. `BlinkTalk` (the MAUI app) and the test project reference it.
   `BlinkTalk.Application` keeps its "no DI/framework" nature: it remains the pure domain
   layer. This is what makes the UI swappable — a future UI references the same two projects.

2. **The Fluxor model is descriptive, not a state holder.** Actions are meaningfully named
   intents (`TypeCharacterAction`, `ChooseSuggestedWordAction`, `SpeakSentenceAction`…), so
   the action stream reads as a story. Reducers are pure `(state, action) → state`. Effects
   own every side effect (timer, speech, DB download/seed, sentence prediction calls).

3. **Settings pages are the only place a "set all state" action is permitted.** There is
   exactly one such action, `ApplySettingsAction`, carrying a full snapshot; it is dispatched
   **only** from the Settings page's Done flow. Every other page dispatches granular actions.

4. **The scan timer is a stateful effect.** `ScanTimerEffect` owns the dwell loop as its own
   field state. It listens to:
   - `EnableScannerAction` → start the loop
   - `StopScanningAction` → cancel the loop
   - `IndicatorStartedAction` / `IndicatorStoppedAction` → pause / resume (dwell resumes with
     the remaining time)
   - level-entry actions → re-read the level's item count and reset the first-dwell multiplier
   On each dwell it dispatches `ScanTickAction`; the reducer advances the highlight.
   Load-bearing `FocusCycler` behaviour is ported, not dropped: longer first dwell
   (`Consts.FirstCycleDelayMultiplier`), skip-unfocusable-without-consuming-a-dwell, paused
   span excluded from the dwell, strategies auto-exit (pop) after `FocusChangeCount` exceeds
   the threshold (rows `> n+1`, keys `> n+2`, words `> n+1`), section selector never auto-exits.

5. **Indicators become actions.** The pointer/keyboard surface dispatches
   `IndicationDetectedAction` (with an `IndicationSource`); the camera bridge dispatches
   `IndicatorStartedAction` / `IndicatorStoppedAction` (held-gesture edges) and
   `IndicationDetectedAction`. No indicator objects, no events, no subscriptions.

6. **UI-thread marshalling rule continues.** The timer effect dispatches through
   `IUIDispatcher` (MAUI `MainThread` in the app, inline in tests) because its continuations
   run on the thread pool. Everything that touches `SentenceBuilder` (SQLite) runs on the UI
   thread. Components react to state via `IState<T>` and call `InvokeAsync(StateHasChanged)`.

7. **Behaviour parity is the contract.** The existing behaviour tests are ported to the
   store+effects harness, not deleted: `ScanFlowTests`, `DecoratorScanFlowTests`,
   `ScanPauseOnDwellTests`, `FocusCyclerTests`, `FocusCyclerPauseTests`.

8. **Incremental, not big-bang.** The Fluxor model is built alongside the old code; the UI is
   flipped in Phase 4; the old `Input/` layer is deleted only in Phase 5. No intermediate
   commit breaks the build.

## Proposed action catalogue (refine in Task 3)

- **Scanning:** `EnableScannerAction`, `StopScanningAction`, `ScanTickAction`,
  `PauseScanningAction`, `ResumeScanningAction`, `RestartScanningAction`
- **Indication:** `IndicationDetectedAction(IndicationSource)`, `IndicatorStartedAction`,
  `IndicatorStoppedAction`
- **Levels:** `EnterSectionSelectionAction`, `EnterRowScanningAction`,
  `EnterColumnScanningAction`, `EnterWordSelectionAction`, `EnterDecoratorSelectionAction`,
  `PopScanLevelAction(int levels)`
- **Input:** `TypeCharacterAction(string)`, `TypeSpaceAction`, `BackspaceAction`,
  `ChooseSuggestedWordAction(string)`, `ApplyDecoratorAction(string)`, `SpeakSentenceAction`,
  `SentenceUpdatedAction(SentenceView)`
- **Settings (granular — used everywhere else):** `SetLanguageAction`,
  `SetKeyboardLayoutStyleAction`, `SetScanSpeedAction`, `SetCameraDwellSecondsAction`,
  `EnableCameraIndicatorAction`, `SaveCameraTrainingAction`
- **Settings (set-all-state — Settings page only):** `ApplySettingsAction`
- **Language pack:** `DownloadLanguagePackAction`, `LanguagePackDownloadedAction`,
  `LanguagePackDownloadFailedAction`, `CancelLanguagePackDownloadAction`

## Tasks

### Phase 1 — Foundation

- [ ] **1. Add Fluxor and scaffold the project.** Add `Fluxor` (and `Fluxor.Blazor.Web`)
      packages to the app project; create `Source/BlinkTalk.Fluxor` (net10.0) referencing
      `BlinkTalk.Application` + `BlinkTalk.Resources`; add it to `BlinkTalk.sln`, the app
      project and the test project; register `builder.Services.AddFluxor(o =>
      o.ScanAssemblies(typeof(BlinkTalk.Fluxor.AssemblyMarker).Assembly))` in `MauiProgram`.
      *Done: solution builds and the app launches with Fluxor initialised (a no-op store).*

- [ ] **2. Define the state model and selectors.** Plain C# records (immutable), one per
      concern: `ScanState` (level, highlight, `FocusChangeCount`, `SkipWordSelection`,
      `IsChoosingDecorator`, `IsPaused`, `IsScanning`, `ActiveRow`, focused section/key/row/
      word/decorator index), `SentenceState` (text, current word, suggestions,
      `ShouldClearOnNextInput`), `CameraState` (session-only enabled, trained, signal,
      threshold, dwell seconds), `SettingsState` (language, keyboard layout style, scan
      speed) — initialised from `ISettingsStore` via the feature's state constructor.
      `ScanLevel` enum + `HighlightTarget`/`Section`/`KeyboardKey` reused from Application.
      *Done: `InitialState` tests pass — persisted settings seed the store at startup; session
      camera flag starts false.*

### Phase 2 — Actions & reducers (the descriptive API)

- [ ] **3. Define the action catalogue.** One file per group under `Actions/`, named exactly
      like the catalogue above. `IndicationSource` enum. Every action is a small record with
      the data it needs and a `///` summary of the intent. *Done: the file reads as a spec;
      `IndicationDetectedAction` is dispatched by the UI, `ScanTickAction` only by the timer
      effect, `ApplySettingsAction` only by the Settings page.*

- [ ] **4. Implement the reducers.** Pure `[ReducerMethod]`s in `Reducers/`:
      scan-level state machine (what `ScanTickAction` does per level incl. skip logic,
      first-dwell multiplier is effect-owned), level transitions on indication, auto-exit
      thresholds, sentence, camera, settings. `ApplySettingsAction` replaces the whole state
      snapshot. *Done: reducer unit tests pass (pure `state → state`), including the
      skip-without-dwell and auto-exit rules.*

### Phase 3 — Effects (side effects + the timer)

- [ ] **5. Implement `ScanTimerEffect` (the stateful timer).** Singleton effect owning the
      dwell loop (`CancellationTokenSource` + remaining-dwell state as fields). Listens to the
      actions in decision 4; dispatches `ScanTickAction` on each dwell, marshalled through
      `IUIDispatcher`. Port the pause-excludes-elapsed-time behaviour from `FocusCycler`.
      *Done: `FocusCyclerTests` + `FocusCyclerPauseTests` rewritten against
      `Dispatcher.Dispatch` + `StepDelay`/`GatedDelay` pass with identical assertions.*

- [ ] **6. Implement the indication router + input effects.** An effect observes
      `IndicationDetectedAction`, reads current scan state, and dispatches the concrete
      action (`TypeCharacterAction`, `EnterRowScanningAction`, `ChooseSuggestedWordAction`,
      `EnterDecoratorSelectionAction`, `SpeakSentenceAction`, `BackspaceAction`…). A
      `SentenceEffect` applies input actions through `SentenceBuilder` and dispatches
      `SentenceUpdatedAction`; `SpeechEffect` commits + speaks. `SentenceBuilder` becomes a
      singleton (one sentence per app session, as today). *Done: `ScanFlowTests` and
      `DecoratorScanFlowTests` pass against the store+effects harness.*

- [ ] **7. Implement the language-pack + camera effects.** `LanguagePackEffect` handles
      `DownloadLanguagePackAction` (progress via `DownloadProgressChangedAction`), downloads,
      seeds via `AppDatabase.OpenForCurrentLanguage(new InMemoryZipSeedWordSource(zip))`,
      then dispatches `LanguagePackDownloadedAction`; failures/cancel dispatch the matching
      action. Camera config is written through `ISettingsStore` by reducers/effects.
      *Done: settings flow (download → seed → Done) works; downloader tests still pass.*

### Phase 4 — UI rewrite (thin consumer)

- [ ] **8. Rewrite `Type.razor` / `.razor.cs`.** Inject `IDispatcher` + `IState<ScanState>`/
      `IState<SentenceState>` (Fluxor `IState` / `FluxorComponent`). `OnPointerDown` →
      `Dispatch(new IndicationDetectedAction(IndicationSource.Pointer))`, `Space`/`Enter` →
      `...Keyboard`. Highlight/depth/decorator popup render from state. Remove
      `ScanController`, `PointerIndicator`, `KeyboardIndicator` injections and the
      `StateChanged` subscription. *Done: typing page scans and selects from store state; no
      controller or indicator references remain.*

- [ ] **9. Rewrite `Settings.razor` / `.razor.cs`.** Read current values from state; dispatch
      granular `Set*Action`s as the person edits; on Done dispatch `ApplySettingsAction` (the
      only set-all-state call site), the download lives in the effect. `ReturnToTyping` logic
      (language changed → clear sentence, restart scan) becomes reducers/effects on
      `ApplySettingsAction`. *Done: language/layout/scan-speed/camera choices persist and the
      typing page picks them up; the only `ApplySettingsAction` dispatch in the codebase is
      here.*

- [ ] **10. Rewrite `CameraIndicator.razor.cs` and `Camera.razor.cs`.** JS callbacks dispatch
       `IndicationDetectedAction` / `IndicatorStartedAction` / `IndicatorStoppedAction`
       instead of calling `Indicator.Trigger()`. Camera page reads/writes `CameraState` via
       actions; the live meter stays local UI state. Remove `CameraGestureIndicator` usage.
       *Done: camera training + live detection + scan pause/resume work through the store.*

### Phase 5 — Cleanup & parity

- [ ] **11. Delete the old input layer.** Remove `ScanController`, `IScanController`,
       `FocusCycler`, `IInputStrategy`, `Input/Strategies/*`, `IIndicator`/`IndicatorBase`,
       `Services/Indicators/Indicators.cs`, and their `MauiProgram` registrations
       (`ScanController`, indicators). Keep `HighlightTarget`, `Section`, `Consts`.
       `IUIDispatcher` stays (the timer effect uses it). *Done: no references to the deleted
       types anywhere; `dotnet build` clean.*

- [ ] **12. Port the remaining behaviour tests.** Port `ScanPauseOnDwellTests` and any other
       controller-facing tests to the store+effects harness; add reducer/effect/selector unit
       tests; add a test asserting `ApplySettingsAction` is only dispatched by the Settings
       page (or at least that it is the only set-all-state action in the action set). Keep
       the parity assertions intact. *Done: full `dotnet test` green.*

- [ ] **13. Update documentation.** Rewrite the Architecture section of `AGENTS.md` (scanning
       stack → Fluxor state machine, timer effect, indicator actions, settings-only
       set-all-state rule, UI-thread marshalling). *Done: docs match the code.*

### Phase 6 — Verification

- [ ] **14. Final verification.** `dotnet test`, `dotnet build Source/BlinkTalk/BlinkTalk.csproj
       -f net10.0-windows10.0.19041.0`, run the app on Windows: type a sentence (pointer +
       keyboard switch), pick a suggested word, speak, open the decorator popup (Arabic),
       change language/layout/scan speed and confirm the typing page restarts correctly, and
       check the camera page still trains/pauses the scan. *Done: all green, no
       controller/indicator references left, old code removed.*

## Definition of done

- `ScanController`, `FocusCycler`, `IInputStrategy`, the strategies and all indicator types
  are gone from the codebase.
- The Fluxor action catalogue is the single vocabulary; every action name states its intent;
  `ApplySettingsAction` is the only set-all-state action and lives only in Settings.
- The scan timer is a stateful effect driven by actions; all legacy timer behaviours are
  preserved and tested.
- UI pages only dispatch actions and render state — a different UI could be dropped in.
- Full test suite green; Windows app builds and the scanning flows work in the running app.
- `AGENTS.md` documents the new architecture.
