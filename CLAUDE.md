# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.3.15f1** (Unity 6.3) project, created from the Universal 3D (URP) template. It is still close to the template: no game code yet, one scene (`Assets/Scenes/SampleScene.unity`), and the template's `TutorialInfo` readme scripts. The folder is not a git repository.

- **Rendering:** URP 17.3. There are two pipeline/renderer pairs in `Assets/Settings/`: `PC_RPAsset` + `PC_Renderer`, and `Mobile_RPAsset` + `Mobile_Renderer`. Quality levels choose which pair is used. If you change rendering settings, update both pairs unless the change is meant for one platform only.
- **Input:** `activeInputHandler: 1` means only the new Input System is enabled, so the legacy `UnityEngine.Input` API will throw. Actions are defined in `Assets/InputSystem_Actions.inputactions`.
- **Scripts:** go in `Assets/`. Runtime code compiles into `Assembly-CSharp` and anything under an `Editor/` folder compiles into `Assembly-CSharp-Editor`, because there are no `.asmdef` files yet. Unity regenerates the `.csproj` and `.slnx` files at the root, so never edit them.
- **Do not edit:** `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are generated or local-only.
- **`.meta` files:** whenever you create, move, or delete an asset, create, move, or delete its `.meta` file with it, or GUID references will break. Prefer letting the Editor create assets.

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

The Unity Test Framework (1.6.0) is installed, but the project has no test assemblies yet. To add tests, create an `.asmdef` that references `UnityEngine.TestRunner` and `UnityEditor.TestRunner` and lists `nunit.framework.dll` under precompiled references.
