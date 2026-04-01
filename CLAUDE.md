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

- **test.yml** — EditMode + PlayMode tests on Linux using `unityci/editor` container directly
- **deploy.yml** — Unity build → Xcode project → Fastlane → TestFlight (macOS runner)

### Unity License in CI

**Do NOT use GameCI's built-in license handling** (`game-ci/unity-test-runner`, etc.).
GameCI v4 doesn't support Unity 6's new `UnityEntitlementLicense.xml` format, and Unity
killed manual `.alf`→`.ulf` activation for Personal licenses. Instead, we:
1. Run inside the `unityci/editor` Docker container directly
2. Activate via `Unity.Licensing.Client --activate-all --include-personal --username --password`
3. Call `unity-editor -runTests` directly

This requires `UNITY_EMAIL` and `UNITY_PASSWORD` secrets (no `UNITY_LICENSE` needed
for activation, though it's still set as a secret). Docker image: `ubuntu-6000.3.11f1-base-3.2.2`.

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

## No Broken Windows Policy

We track every warning, error, and rough edge as a GitHub issue — nothing gets ignored or
swept under the rug. If you see something wrong (CI warning, deprecation notice, flaky test,
TODO in code, ugly workaround), file a GitHub issue immediately. We may not fix it today,
but we will not lose track of it.

When fixing issues, check the open issues list first — there may already be a ticket for it.
Close issues with a commit reference when resolved.

## Dev Environment

- Arch Linux, headless (no GUI/X11 — Unity Editor not usable locally)
- dotnet SDK 10.0 installed for local test runs
- Unity Hub installed but requires X11 forwarding to use (flaky)
- Git LFS configured for binary assets
