# Climb and Glide

Personal Unity demo: alternate pulls to climb a ladder, choose a platform, then freely glide to a landing area.

## Current status

**Canonical state and acceptance gates: [STATE (中文)](docs/STATE.zh-CN.md).** This README is an entry point; dated reports are historical evidence. Project source is licensed under [MIT](LICENSE); external tools and dependencies retain their own terms. Climb and Glide is a working title.

- Selected: Unity 6.3 LTS, Windows desktop and Quest Pro PC VR.
- Implemented source: pure C# normal-flight approximation, alternating-stroke ladder model, generated Unity test scene, platform-to-flight transition and desktop/XR synthetic controls.
- Verified: offline core checks, native Unity compilation and actual rendered A2 Windows trials with plateau/valley terrain and target navigation. See the A2 review for evidence and limits.
- Pending: manual keyboard/mouse play, PC VR runtime/loader configuration, Quest execution and physical PUMP integration.
- There is no BLE client or motor-control path in this demo. Private device records are not part of this repository.

This is progress toward M1/M2, not a claim that either milestone has passed all acceptance gates.

## Documents

- [Development plan in Chinese](docs/DEVELOPMENT_PLAN.zh-CN.md)
- [Environment and downloads](docs/SETUP.md)
- [Executed checks and remaining acceptance](docs/VALIDATION.zh-CN.md)
- [A0 automated trial and retrospective](docs/A0_AUTOMATED_TRIAL.zh-CN.md)
- [A1 rendered trial and retrospective](docs/A1_REVIEW.zh-CN.md)
- [A2 terrain/navigation trial and retrospective](docs/A2_REVIEW.zh-CN.md)
- [Module ownership](AGENTS.md)
- [Contributing](CONTRIBUTING.md)
- [Architecture and efficiency review](docs/ARCHITECTURE_REVIEW.zh-CN.md)
- [Open-source preparation](docs/OPEN_SOURCE.zh-CN.md)
- [Third-party dependencies](THIRD_PARTY_NOTICES.md)

## Offline checks

```powershell
.\scripts\test-core.ps1
# Shared-session automated playthroughs, CSV traces and HTML report:
.\scripts\test-core.ps1 -Trial
```

If the local SDK is absent, follow SETUP. The script uses no external test packages and does not connect to devices. Run a single copy at a time.

## Unity entry

Open the `game` folder with the pinned Editor version. Wait for package import, resolve any reported native compilation errors, then use **Climb > Create or Open Demo Scene** and press Play. **Climb > Build Windows Demo** is the proposed Windows build entry. The scene builder and Windows build entry have passed native batch execution through Unity CLI.

The first world is generated from primitives. URP is pinned as a package; a render pipeline asset and its actual rendering still require Editor setup/verification. The graybox uses a shader explicitly included in Resources; its Windows rendering was visually inspected.

PC VR additionally requires Meta Horizon Link, an active OpenXR runtime, and Standalone OpenXR configuration in Unity. Headset tracking and controller-trigger simulation are separate from final hand-gesture UI and from real PUMP input.

## Synthetic controls

| Control | Action |
|---|---|
| Q / E held | Pull left / right simulated rope; release to return |
| Right mouse button + move | Look without changing flight steering |
| F | Start a flight directly from altitude |
| C | Return to ladder start |
| Space at a platform | Request launch; wait for both simulated ropes to return to neutral |
| R | Reset current activity |
| P | Pause/resume the demo |
| Tab | Select landing target A / B / C |
| XR controller triggers | Alternative simulated left/right rope input, when XR is active |

The simulation does not measure tension, support real body weight, or claim calibrated paraglider behavior. The ladder thresholds are configurable placeholder values. Original precise-grip, slip/fall and direction-triggered load switching are outside the first implementation.

## Rendered A2 trials

```powershell
.\scripts\run-a2.ps1
.\scripts\run-a2.ps1 -SkipBuild -LeftTarget
```

The 12 m ladder now starts on an 80 m plateau. HUD distance is horizontal; COURSE bearing is relative to the flight heading; AGL uses local terrain height. Auto steering is enabled only for automated trials.
