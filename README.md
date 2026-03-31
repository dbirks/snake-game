# Snake Game (tvOS)

A snake game for Apple TV, built with Unity. The snake has free-flowing movement (not grid-locked) with a flat, orange aesthetic.

## Prerequisites

- **Unity Hub** + **Unity 6** (latest) with the tvOS module
- **Apple Developer Program** membership ($99/year) for TestFlight deployment
- **Git LFS** for binary assets

## Getting Started

### 1. Install Unity

```bash
# Install Unity Hub (Arch Linux)
yay -S unityhub

# Or download from https://unity.com/download
```

Open Unity Hub, sign in, and install the latest Unity 6 release. When installing, add these modules:
- **tvOS Build Support** (required for Apple TV builds)
- **Linux Build Support (IL2CPP)** if you want local Linux builds

### 2. Clone and open the project

```bash
git clone git@github.com:YOUR_USERNAME/snake-game.git
cd snake-game
git lfs install
git lfs pull
```

Open Unity Hub → Add → Browse to the `snake-game` folder → Open.

Unity will import all assets and generate the Library folder on first open (this takes a few minutes).

### 3. Run in the Editor

1. Open `Assets/Scenes/Main.unity` (you'll need to create this scene — see below)
2. Press Play

### Creating the main scene

Since Unity scenes aren't generated from code, you'll need to set up the initial scene:

1. Create a new Scene: File → New Scene → Save as `Assets/Scenes/Main.unity`
2. Create an empty GameObject named "Game"
3. Add the `GameManager` component to it
4. Create a child GameObject named "SnakeRenderer" with the `SnakeRenderer` component
5. Create a child GameObject named "Input" with `InputAdapter` and `PlayerInput` components
6. Wire the references in GameManager's inspector (snakeRenderer, inputAdapter)
7. Set up the camera: position at `(10, 6, -15)`, looking at `(10, 6, 0)`, orthographic size ~7
8. Add the scene to Build Settings (File → Build Settings → Add Open Scenes)

## Project Structure

```
Assets/
  Game/
    Core/           # Pure C# simulation — no Unity dependency
      SnakeSimulation.cs   # Deterministic tick-based sim
      SnakeState.cs        # Serializable state snapshots
      InputCommand.cs      # Input command enum
    UnityGlue/      # MonoBehaviour adapters, renderers
      GameManager.cs       # Wires sim to Unity game loop
      SnakeRenderer.cs     # Renders state to flat-look quads
    Input/          # Input System adapter
      InputAdapter.cs      # Converts input events to commands
  Editor/
    Build/          # CI build automation
      BuildScript.cs       # -executeMethod entrypoint
  Tests/
    EditMode/       # Fast deterministic tests (no Unity runtime)
    PlayMode/       # Integration tests (scene boot, rendering)
  Scenes/           # Unity scenes
```

**Key design choice**: The simulation (`Game.Core`) has zero Unity dependency (`noEngineReferences: true`). This means:
- EditMode tests run fast (no game loop needed)
- Simulation is deterministic (same seed + inputs = same output)
- Easy to reason about gameplay without visual inspection

## Running Tests

### In Unity Editor

Window → General → Test Runner → EditMode tab → Run All

### From command line (for CI)

```bash
# EditMode tests
Unity -batchmode -nographics -runTests -testPlatform EditMode -testResults results.xml -projectPath .

# PlayMode tests
Unity -batchmode -nographics -runTests -testPlatform PlayMode -testResults results.xml -projectPath .
```

### Test categories

- `Core` — Fast simulation tests (run on every PR)
- `Integration` — PlayMode tests (scene boot, rendering)
- `Determinism` — Verify same-seed reproducibility

## CI/CD

The project uses two GitHub Actions workflows:

### `test.yml` — Runs on every PR
- EditMode + PlayMode tests via [GameCI](https://game.ci) on Linux runners (cheap)
- Test results appear as GitHub status checks on PRs

### `deploy.yml` — Runs on push to main and v* tags
1. Runs EditMode tests (Linux)
2. Builds Unity → Xcode project for tvOS (macOS runner via GameCI)
3. Signs and archives with Fastlane + match
4. Uploads to TestFlight

**Push to `main`** → internal TestFlight build
**Push a `v*` tag** → external TestFlight release (notifies testers)

### Required secrets

See [docs/ci-secrets.md](docs/ci-secrets.md) for the full list. Summary:

| Category | Secrets |
|----------|---------|
| Unity | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` |
| App Store Connect | `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_PRIVATE_KEY` |
| Match | `FASTLANE_MATCH_GIT_URL`, `FASTLANE_MATCH_PASSWORD`, `FASTLANE_MATCH_DEPLOY_KEY` |

## First-Time Apple TV Setup

These steps only need to be done once:

1. **Register Bundle ID**: Apple Developer portal → Identifiers → Register `dev.birks.snakegame` (tvOS)
2. **Create App Record**: App Store Connect → Apps → New App → select tvOS, use the bundle ID above
3. **Generate tvOS certificates**: Run locally:
   ```bash
   cd snake-game
   MATCH_GIT_URL=git@github.com:YOUR/certs-repo.git \
   bundle exec fastlane certificates_distribution
   ```
   This creates tvOS distribution certificates and profiles in your match repo.
4. **Add GitHub secrets**: See [docs/ci-secrets.md](docs/ci-secrets.md)

### tvOS-specific notes

- **App icons**: tvOS requires layered icons (2-5 layers) for the parallax effect. Sizes: 400x240 (small), 1280x768 (large). The bottom layer must be fully opaque.
- **Top Shelf images**: Required. Standard: 1920x720, Wide: 2320x720.
- **Launch image**: 1920x1080 static PNG.
- **Tester feedback**: tvOS TestFlight testers cannot submit feedback through the app — they must email directly.

## Local Fastlane Commands

```bash
# Install Ruby dependencies
bundle install

# Download development certificates
bundle exec fastlane certificates_development

# Download distribution certificates
bundle exec fastlane certificates_distribution

# Build + upload to TestFlight (requires Xcode project from Unity build)
bundle exec fastlane beta
```
