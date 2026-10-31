# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

An original farming life-sim in the classic genre (inspired by *Harvest Moon: Back to Nature*, but with no copied characters, story or item lists), built in Unity **6000.3.15f1** (Unity 6.3) with URP. Placeholder visuals only: coloured tiles, a capsule player, cubes for the bed/bin/shop.

**Play it:** open `Assets/_Game/Scenes/FarmView.unity` (the first scene in Build Settings) and press Play. WASD move, Space/click use, 1/2 cycle the hotbar, E interact (harvest, ship at the blue bin, sleep at the red bed, shop at the yellow counter), N debug sleep, F9 new game. The game saves to `Application.persistentDataPath/save_slot1.json` whenever a day ends.

**Words:** `GLOSSARY.md` defines the game's terms (Night, Withered, Mature, Seed packet, Ship…). Use them in code, tests and PRs, and add new terms when a rule introduces one.

### Folders

Everything the game owns lives under `Assets/_Game/`, grouped by **asset type**, not by feature. Put new files in the matching folder; don't create a feature folder (like `Art/Scenes/`) that duplicates one of these.

- `Scenes/`: **every** scene (`FarmView.unity`, `ArtSandbox.unity`, …).
- `Scripts/Runtime/<Area>/`, `Scripts/Editor/` (editor-only code; art tools in `Scripts/Editor/Art/`).
- `Tests/EditMode/`: tests.
- `Data/`: game data ScriptableObjects (`Crops/`, `Items/`, `Seeds/`, `Tools/`, `GameDatabase.asset`).
- `Art/`: visual assets: `Shaders/`, `Materials/`, `Meshes/`, and volume profiles (later `Textures/`, `Models/`, `Prefabs/`).

If a new kind of asset doesn't fit any of these, ask before adding a top-level folder.

### Architecture

Rules are plain C# with no scene dependencies; MonoBehaviours only wire them to input and the screen.

- `Assets/_Game/Scripts/Runtime/` (`Game.Runtime.asmdef`, namespaces `Farm.*`):
  - `Core/`: `GameState` builds and owns every system from a `GameSetup`, and `Capture()`/`Restore()` convert to `SaveData` (`SaveSerializer` = JsonUtility). `GameDatabase` finds items and crops by id for loading.
  - `Time/` (`GameCalendar`, `GameClock`), `Weather/` (`WeatherSystem`), `Farming/` (`FarmGrid`, `CropDefinition`, `CropInstance`, `Night`), `Items/` (`Inventory`, `ItemDefinition`, `SeedDefinition`), `Tools/` (`ToolUser`, `ToolDefinition`), `Player/` (`Stamina`), `Economy/` (`Wallet`, `ShippingBin`, `SeedShop`), `Day/` (`DayCycleService`).
  - `View/`: MonoBehaviours. `FarmGame` (owns the `GameState`, loads/saves), `PlayerController`, `PlayerActions`, `Hotbar`, `TimeController`, `ShopPanel`, `FarmHud`, `FarmGridView`, `FollowCamera`.
- **The night** is one ordered sequence. `DayCycleService.EndDay` sells the bin, then advances the calendar, resets the clock and advances the weather, then calls `FarmGrid.PassNight(Night)`, then restores stamina and fires `DayEnded`. `PassNight` grows watered crops, dries tiles, withers out-of-season crops, waters tilled tiles if it rains, and sprouts weeds. New night rules go inside `PassNight`, and new night facts go in `Night`.
- `FarmGrid` is the only thing that changes tiles. `FarmTile` setters are `internal`.
- Randomness is an injected `System.Random`. Tests use `FixedRandom`.
- `Farm.Time` shadows `UnityEngine.Time` inside `Farm.*` namespaces, so write `UnityEngine.Time.deltaTime` in view code.
- **Editor menus** (`Scripts/Editor/`, `Game.Editor.asmdef`):
  - `Farm > Create Sample Data` builds the six crops, items and seed packets plus `Data/GameDatabase.asset`. The balance table is in `SampleDataBuilder`.
  - `Farm > Build Farm View Scene` rebuilds the scene, UI and Build Settings.
  - When code creates a scene, reload asset references after `EditorSceneManager.NewScene`, because it unloads unused assets.

### Tests and the build loop

- **Tests:** EditMode NUnit tests in `Assets/_Game/Tests/EditMode/` (`Game.Tests.EditMode.asmdef`). `TestWorld` wraps a `GameState`, `TestData` builds throwaway ScriptableObjects, and `FixedRandom` makes chance rules predictable (by default it never rains and no weeds sprout).
- **Test-first** (`tdd` skill) for game rules: write a failing test at an agreed seam, then the minimum code. Scene and UI code is checked by playing.
- **With the Editor open** (works unfocused):

  ```powershell
  unity command script recompile; unity command script recompile_status   # poll until completed/up_to_date
  unity recompile --strict --project-path "<project>"                      # zero errors and warnings
  unity command test run --mode EditMode --filter Farm.Tests
  ```
- **Play-mode checks:** use `unity command editor.playmode start|stop`, `unity command script eval "<C#>"` (reflection reaches private handlers), and `unity command capture game_view --save_path "Assets/Temp/x.png"`. Captures must be saved inside the project, so move them out and delete `Assets/Temp` afterwards.
  - The Game view only repaints while Unity is focused. Use `editor.playmode set_run_in_background --enable true` for captures, and turn it back off.
  - Always stop play mode before recompiling.
  - **Never lose the user's save.** Before any check that can end a day or touch saves, copy `%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\save_slot1.json` (if it exists) to the scratchpad, and copy it back afterwards. Never delete it; if a check needs a fresh game, move the save aside and restore it.
- **Delegating:** the `unity-coder` agent (Sonnet) implements briefed tasks. It never edits tests and stops after 2 failed attempts.

### Unity basics

- **Rendering:** URP 17.3 with two pipeline/renderer pairs in `Assets/Settings/` (`PC_*`, `Mobile_*`), chosen by quality level. Change both unless the change is platform-specific.
- **Input:** only the new Input System is enabled (`activeInputHandler: 1`), so `UnityEngine.Input` throws. Actions live in `Assets/InputSystem_Actions.inputactions`; Space is bound to `Attack`.
- **UI:** uGUI + TextMeshPro. `Assets/TextMesh Pro/` is Unity's imported essentials; don't edit it.
- **Do not edit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or the generated `.csproj`/`.slnx` files.
- **`.meta` files:** whenever you create, move or delete an asset, do the same to its `.meta` file, or GUID references break.

## Git

Remote: `https://github.com/SwiftkeyX/Harvest-Moon-Clone` (public), branch `main`. Commit only when the user asks, and push only when asked. Use the `git-commit` skill (its workflow and safety rules, but this project's message format below) and the `git-workflow-and-versioning` skill (branches, PRs), with these project rules on top:

- **Branches and PRs:**
  - Each game build step gets its own branch, `feature/step-<n>-<short-name>`, and its own PR.
  - Branches are **stacked**: step N branches from step N-1's branch, and its PR targets step N-1's branch. When the user merges a PR, GitHub moves the next PR to the new base.
  - Open a step's PR only after the user has reviewed and approved that step.
  - Write PR titles and bodies with the `pr-writer` skill (PR titles use the same `ADD:` / `UPDATE:` / `FIX:` prefixes as commits): draft PRs, a short reviewer-facing body, and the Claude Code attribution line at the end.
  - **PR body layout (overrides pr-writer's prose-only style):** three bold-labelled bullet sections, one fact per bullet, no paragraphs:
    - `**What changes in the game**`: player-visible behaviour, or the rules now enforced.
    - `**Not yet**`: what a reviewer might expect but this PR leaves out. Omit the section if there's nothing.
    - `**Where to look**`: the key classes, the tests, and review order for stacked PRs (e.g. "Review after #1").

    Keep the rest of pr-writer's rules: no command output, logs, or file lists, and no process wording like "step 3".
- **Setup/tooling changes** (`.claude/`, `CLAUDE.md`, `skills-lock.json`) go straight to `main` as their own commits, never inside a step branch.
- **Staging:** run `git reset -q` before staging so only the intended files go in. Something in this setup has staged files on its own before.
- **Never discard work:** don't use `git reset --hard`, `git checkout -- <file>`, or force-push without asking, even if a skill suggests it.
- **Commit message format (overrides Conventional Commits):** start the summary with one of these prefixes, then a short imperative description; no scopes.
  - `ADD:` something new: a feature, rule, test, file, or skill. Example: `ADD: game glossary`.
  - `UPDATE:` a change to something that exists: refactor, docs, settings, tuning. Example: `UPDATE: move overnight tile rules into FarmGrid.PassNight`.
  - `FIX:` a bug fix. Example: `FIX: reload turnip after opening the new scene`.
  - Earlier commits use the old `feat(farm):` style; leave them as they are.
- **`.meta` files:** every new, moved, or deleted asset or folder must be committed together with its `.meta`. Before committing, check that `git status` shows no `.meta` file without its asset and no asset without its `.meta`.
- **Generated changes:** Unity rewrites `ProjectSettings/*.asset`, `Packages/packages-lock.json`, and scene files when the Editor is open. Only include those files if the change was intended; otherwise leave them out and mention them.
- **Before committing code:** run `unity recompile --strict` (Editor open) and make sure it reports zero errors. Run the EditMode tests if logic changed. Don't commit code that doesn't compile.
- **Don't commit:** anything the `.gitignore` excludes (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx`).

## Talking to the Editor

There are two ways to control the running Editor:

1. **Unity CLI** (`unity`, installed at `%LOCALAPPDATA%\Unity\bin\unity.exe`). It connects through the experimental `com.unity.pipeline` package, which is already in `Packages/manifest.json`. The Editor must be open and finished compiling. If you get exit code 12 (EDITOR_UNREACHABLE), the Editor is usually in the background or reloading. Focus it, or pass `--focus` to `recompile`.
2. **Coplay MCP tools** (`mcp__coplay-mcp__*`). Use these for scene, GameObject, and asset edits, reading logs, and running C# in the Editor.

## Common commands

Set `UNITY_NO_BANNER=1` to keep the CLI output clean.

```powershell
unity status                       # connected Editors and their state
unity pipeline editors             # check that the Pipeline package/server is reachable
unity command                      # list commands the running Editor exposes
unity command eval ...             # run C# inside the running Editor
unity recompile --focus --strict   # recompile and report compile errors (warnings fail with --strict)
unity test --mode EditMode         # run EditMode tests (NUnit XML report)
unity test --mode PlayMode --filter <TestName>   # run a single test / matching tests
unity build                        # batch-mode build
```

`unity test`, `unity build`, and `unity run` start a separate batch-mode Editor. Unity can't open a project that is already open, so close the interactive Editor first with `unity close "<project path>"`, or use `unity recompile` or `unity command` against the running Editor instead.

For the game's own tests, prefer the in-Editor loop above (`unity command test run`).
