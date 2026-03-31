# Agent Handoff Notes for a Unity tvOS Game Repo

## Goals and constraints

This repo should be optimized for a tight “agent loop” where the agent can change gameplay behavior (for example snake steering/turn rules), run automated checks, and verify an **end-state snapshot** deterministically—without needing to eyeball the game in the Editor. citeturn9view2turn10view1

Key constraints you gave:

- Development happens on Linux, while CI runs builds/tests and produces tvOS artifacts (and releases) on entity["company","GitHub","code hosting company"] Actions runners, including macOS for Xcode. citeturn5view1turn2search3turn4search36
- tvOS build pipeline is Unity → generate Xcode project → Xcode builds and uploads. citeturn5view1turn2search3
- The game is “snake-ish” but with **free-flowing movement** (not necessarily grid-locked), so tests need to focus on **simulation correctness** and **replayability** rather than tiled positions only. citeturn9view2turn2search17

## Toolchain overview: GameCI, fastlane, Unity Test Framework, Unity MCP

### GameCI vs fastlane

Treat these as complementary lanes, not substitutes:

- entity["organization","GameCI","unity ci open-source"] is primarily about making Unity behave predictably in CI: running tests, caching the Library folder, and building Unity targets through reusable CI actions. citeturn0search1turn0search5turn0search2turn4search12
- entity["organization","fastlane","mobile devops automation"] is best for the Apple distribution side: uploading builds to TestFlight (via pilot), App Store Connect API authentication, and release automation. citeturn0search3turn0search23turn0search19

A practical split that keeps the repo agent-friendly:

- GameCI handles **Unity Test Framework** runs (EditMode/PlayMode) and Unity builds in CI. citeturn0search1turn0search2turn10view1  
- fastlane handles **archive/export/upload** once you have an Xcode project output. citeturn0search3turn2search3turn0search23

### Unity Test Framework (UTF)

Unity’s baseline is Unity Test Framework, which supports **Edit mode** and **Play mode** tests and can be run from the Editor UI or from the command line. citeturn9view2turn10view0turn10view1turn2search17

This is the best foundation for an agent loop because the agent can run tests headlessly, parse NUnit-format XML results, and reason about failures from stack traces rather than visual guesses. citeturn10view1turn10view0

### Unity Input System testing utilities

If you use the newer Input System, Unity explicitly supports **driving input entirely from code** in automated tests (no physical devices or platform backends required). citeturn1search7turn1search3

This is the right way to test “controller-ish” behavior as part of PlayMode tests (even if CI doesn’t have a real Apple TV controller attached). citeturn1search7turn1search3turn2search17

### Unity MCP (not “MTP”) for agent/editor integration

Unity calls the AI bridge **Unity MCP** (Model Context Protocol). It describes a bridge inside the Editor and a relay process that exposes Editor capabilities as MCP tools to external AI clients. citeturn3search3turn3search6

For this project, Unity MCP is useful *only if* you want your agent to perform Editor-facing tasks (scene setup, asset manipulation, running editor workflows). citeturn3search3  
It is not a replacement for deterministic gameplay tests, which should still be done via Unity Test Framework and a stable simulation harness. citeturn9view2turn10view1

Security footnote (worth including in repo notes): recent academic work has shown MCP-based tool ecosystems can introduce meaningful risks (for example tool poisoning / prompt injection paths), so the safe baseline is to run MCP with least privilege and never expose signing credentials or App Store keys to an MCP tool surface. citeturn3academia40turn3academia41

## Repo conventions that make future agents effective

The theme: **make changes easy to test without opening Unity**, and make Unity project diffs mergeable.

### Version control settings that reduce “Unity repo pain”

In Unity 6.3 docs, “Visible meta files” is the mode intended for VCSes Unity doesn’t directly integrate with (like Git), and Unity explicitly notes that `.meta` files contain important identity/import info and must move with assets. citeturn11search0turn11search17

Agent-facing takeaway to bake into the repo:
- Ensure `.meta` files are committed and stable; they contain GUID + import settings, and references break if they go missing or mismatch. citeturn11search17turn11search25
- Use text serialization for mergeability (Unity stores many assets/scenes/prefabs in a YAML subset when configured). citeturn3search9turn11search5

### Build Profiles should be committed

Unity’s tvOS build flow in Unity 6.3 is oriented around **Build Profiles** (File → Build Profiles), including a “Create Xcode Project” toggle and selecting tvOS platform modules if missing. citeturn5view1turn7search19

Practical repo rule:
- Create and commit at least two build profile assets (for example: `tvOS-Dev` and `tvOS-Release`). The CI pipeline can then select them using `-activeBuildProfile` or `-buildTarget` as appropriate. citeturn7search12turn7search16turn5view1

### Suggested folder structure that supports the agent loop

This structure is designed so “snake behavior changed” mostly touches deterministic core logic, and not scenes/prefabs.

- `Assets/Game/Core/`  
  Pure gameplay model + deterministic simulation (no `MonoBehaviour` dependency if possible).
- `Assets/Game/UnityGlue/`  
  Renderers, `MonoBehaviour` adaptors, UI wiring, Audio triggers.
- `Assets/Game/Input/`  
  Input Actions asset + a thin adapter that converts input into “commands” for the core simulation.
- `Assets/Tests/EditMode/`  
  Model/simulation tests (fast, deterministic). citeturn8search6turn10view1
- `Assets/Tests/PlayMode/`  
  A small number of integration tests: scene boot, input adapter wiring, and “smoke path”. citeturn2search17turn9view2
- `Assets/Editor/Build/`  
  One “build script entrypoint” callable via `-executeMethod`, to generate the tvOS Xcode project and enforce build invariants. citeturn7search0turn7search12turn2search6
- `Tools/ci/`  
  Cross-platform scripts (`bash`/`python`) that standardize how the agent runs tests locally and in CI (so the agent doesn’t rewrite pipeline logic each iteration). citeturn7search12turn10view1

## Testing strategy for a free-flowing snake-like game

### The core loop to enable: “Inputs → ticks → snapshot”

Unity Test Framework supports both normal NUnit tests and Unity-style tests that interact with the game loop (coroutines in Play mode; update loop integration in Edit mode). citeturn9view2turn2search17

For your game, the high-leverage pattern is:

- A deterministic simulation stepper that advances by **fixed ticks** (or fixed delta), producing a serializable snapshot (positions/segments, score, alive/dead, etc.).
- Tests define:
  - initial state + RNG seed
  - an ordered list of input commands (turn left/right, accelerate, etc.)
  - number of ticks to simulate
  - expected end snapshot (or assertions over it)

This makes “observe the ending point” a direct structured assertion rather than a visual check. citeturn9view2turn10view1

### EditMode tests should cover most behavior

Unity explicitly distinguishes EditMode tests as Editor-only tests that can reference both runtime and Editor code (but can’t do coroutines the same way as PlayMode), and PlayMode tests as runtime-focused tests often written as coroutines. citeturn8search6turn2search17

For an agent loop, the aim is:
- 80–95% of gameplay rules in EditMode tests (fast + deterministic). citeturn8search6turn10view1
- A thin layer of PlayMode tests for Unity integration risks. citeturn2search17turn9view2

### Use categories and deterministic ordering to keep the loop cheap

Unity Test Framework’s command-line reference supports:
- selecting EditMode vs PlayMode via `-testPlatform`
- filtering via `-testCategory` and `-testfilter`
- enforcing order via `-orderedTestListFile`
- writing NUnit XML via `-testResults`
- running EditMode tests synchronously with `-runSynchronously` (where applicable) citeturn10view1turn10view0

Agent-facing repo convention:
- Tag tests with categories like `Core`, `Integration`, `Slow`, `Determinism`.
- CI defaults to `Core` on PRs; `Integration` runs on main or nightly.
- Keep a text file in-repo listing “golden” deterministic tests to run in a stable order (useful when debugging nondeterminism). citeturn10view1

### Input testing without hardware

Unity’s Input System docs explicitly state you can generate input entirely from code for automated tests, without platform backends and physical devices. citeturn1search7  
The Input System provides `InputTestFixture` as a test fixture to structure such tests. citeturn1search3

Practical implication for tvOS:
- Don’t make CI dependent on real Apple TV Remote/controller hardware.
- Test “controller semantics” (turning rules, input buffering, deadzones) through Input System simulation in PlayMode tests. citeturn1search7turn2search17

### Optional: keep a “golden replay” format

For free-flowing movement, a golden replay format makes regression testing easier than hardcoding expected positions in code.

Minimal replay file contents:
- RNG seed
- tick rate / fixed delta
- list of input events with tick index
- expected final snapshot hash (plus optionally full JSON snapshot for debugging)

This integrates naturally with Unity Test Framework’s ability to output structured failures and with CI artifact uploads. citeturn10view1turn0search1

## CI design for Linux development and macOS release builds

### What should run on every PR

The fastest meaningful CI is:

- Unity EditMode tests in batch mode, filtered to “Core”.
- Optionally PlayMode “smoke tests” (scene boot + input adapter + one short simulation).

Unity explicitly supports running tests from the command line with `-runTests`, `-testPlatform`, and outputting results via `-testResults`. citeturn10view0turn10view1

Implementation options:

- Use GameCI’s Unity Test Runner action and cache the `Library` folder (GameCI claims caching can cut test/build time dramatically). citeturn0search1turn0search5
- If you go DIY, still stay aligned with Unity’s own documentation for “batchmode” + test CLI arguments. citeturn7search12turn10view1

### Unity license handling in CI

GameCI’s activation docs describe a pattern of storing a Unity license (`.ulf`) and Unity credentials as GitHub secrets for CI use. citeturn4search0turn4search25

If you’re on a personal license and it expires/rotates, GameCI also maintains tooling intended to (re)automate personal license activation flows. citeturn4search1

Agent-facing repo note:
- Put **exact secret names and where they’re used** in `docs/ci-secrets.md`, because future agents need to know what the pipeline expects without spelunking workflows. citeturn4search0turn4search25

### Where to build tvOS artifacts

Unity’s tvOS build doc is explicit: it’s a two-step pipeline—Unity creates an Xcode project, then Xcode builds it to device. citeturn5view1

Given your “Linux dev + macOS CI” constraint, there are two viable patterns:

- **All-on-macOS**: run Unity on a macOS runner to generate the Xcode project, then immediately `xcodebuild` and upload. Fewer cross-runner artifacts. citeturn5view1turn2search3
- **Split build**: generate the Xcode project using GameCI Builder on Linux (as an artifact), then compile/sign/upload on macOS. If you do this, GameCI documents a `dockerWorkspacePath` knob to avoid path-related issues when moving iOS/Xcode projects between OSes/runners. citeturn0search2

For a small game, “all-on-macOS” is simplest; “split build” is for cost optimization and advanced flows. citeturn0search2turn5view1

### Pin Xcode versions in GitHub Actions

Apple’s App Store Connect upload guidance includes Xcode version requirements; for example, it states tvOS apps must be built using Xcode 16 or later, and includes a note that starting in 2026 you must use at least Xcode 14 to upload to App Store Connect. citeturn2search3

In parallel, GitHub-hosted macOS runner images can change their Xcode availability/policy, and runner-images discussions recommend explicitly selecting the desired Xcode version each run. citeturn4search36turn4search3

Agent-facing instruction:
- Make “select Xcode version” a first-class CI step (either via `xcode-select` or a setup action that switches among preinstalled versions). citeturn4search3turn4search36

## TestFlight deployment lane for tvOS

### The minimal release chain

Apple documents multiple ways to upload binaries to App Store Connect (Xcode UI workflows, `altool`, Transporter, and App Store Connect API-based approaches). citeturn2search3turn2search14  
Use fastlane to make this reproducible in CI:

- fastlane `upload_to_testflight` (pilot) uploads a new build and can manage testers/build distribution workflows. citeturn0search3turn0search11
- fastlane can use App Store Connect API keys (JWT-based) rather than interactive Apple ID sessions, which is typically the most CI-stable approach. citeturn0search23turn2search14

### Where Unity-specific logic should live

Unity recommends build automation via a build pipeline script using `BuildPipeline.BuildPlayer`, which you can invoke from CI using `-executeMethod`; command-line build docs list `-projectPath`/`-quit` as required and recommend `-batchmode`, `-logFile`, and setting `-buildTarget` or `-activeBuildProfile`. citeturn7search0turn7search12turn7search2

Practical repo approach:
- One Unity build entrypoint that:
  - verifies the active Build Profile (tvOS dev or release)
  - enables Create Xcode Project
  - outputs the Xcode project into a deterministic path in the repo workspace
  - emits build logs to a file that CI uploads as an artifact citeturn5view1turn7search12

### Speed knobs to note early

Unity documents “scripts-only builds” (reusing prior content to avoid rebuilding data) and also notes platforms with incremental build pipeline automatically reuse content when possible. citeturn7search3turn7search8turn5view1

This matters for agent iteration:
- CI can be made faster by caching and by structuring builds so “code-only changes” don’t trigger full content rebuild paths. citeturn0search1turn7search3

### tvOS-specific gotcha to keep in repo notes

Unity’s documentation and community discussions indicate the Xcode project generated for tvOS is a separately configured build output, and Unity does not generate a single combined Xcode project containing both iOS and tvOS targets for a single App Store bundle. (This matters only if you later decide to ship a “universal purchase” or unified project structure.) citeturn0search6turn4search15