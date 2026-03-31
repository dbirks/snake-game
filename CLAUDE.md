# CLAUDE.md

## Project Overview

Unity Snake game for Apple TV (tvOS). Free-flowing movement, not grid-locked.
Bundle ID: `dev.birks.snakegame`. Development happens on headless Arch Linux;
builds and deploys happen in GitHub Actions CI.

## Architecture

- `Assets/Game/Core/` — **Pure C# simulation, zero Unity dependency** (`noEngineReferences: true`). This is the heart of the game. All gameplay logic lives here so it can be tested without Unity.
- `Assets/Game/UnityGlue/` — MonoBehaviour adapters that render `SnakeState` to Unity visuals. Orange snake, flat aesthetic.
- `Assets/Game/Input/` — Input System adapter for Apple TV remote/gamepad.
- `Assets/Editor/Build/` — CI build script (`-executeMethod SnakeGame.Editor.BuildScript.BuildTvOS`).
- `Tests.Local/` — dotnet test project that runs Core tests locally without Unity.

## Running Tests Locally

```bash
./Tools/ci/run-local-tests.sh
```

This runs the EditMode tests against the Core simulation using `dotnet test`. No Unity installation needed. Uses net10.0 (Arch has dotnet SDK 10).

## CI/CD

- **test.yml** — EditMode + PlayMode tests on Linux via GameCI
- **deploy.yml** — Unity build → Xcode project → Fastlane → TestFlight (macOS runner)
- **activation.yml** — One-time workflow to get Unity license `.ulf` file

## Fastlane

Adapted from the CAWCAW iOS app (`~/dev/cawcaw/ios/fastlane/`). Key differences for this project:
- Platform is `tvos` (not `ios`) in all match/sigh calls
- Builds from Unity-generated Xcode project at `build/tvOS/` (not a workspace)
- Reuses CAWCAW's match certificate repo

## Conventions

- Keep gameplay logic in `Game.Core` with no Unity dependency
- Write EditMode tests for all gameplay rules (fast, deterministic)
- PlayMode tests only for Unity integration concerns (scene boot, rendering)
- Timestamp-based build numbers (`YYYYMMDDHHMMSS`)
- Commit often with descriptive messages

## Dev Environment

- Arch Linux, headless (no GUI/X11 — Unity Editor not usable locally)
- dotnet SDK 10.0 installed for local test runs
- Unity Hub installed but requires X11 forwarding to use (flaky)
- Git LFS configured for binary assets
