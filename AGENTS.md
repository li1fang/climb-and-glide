# Project rules

Read `docs/STATE.zh-CN.md` for current acceptance status and the source-of-truth map. Update current status there; keep historical trial reports intact. Package versions and gameplay defaults belong in their existing metadata/configuration files, not duplicated documentation tables.

This is a personal entertainment/demo project: climb a ladder, choose to launch from a platform, and freely paraglide. Windows desktop and Quest Pro PC VR are the first targets. Unity 6.3 LTS is selected; paid purchases require discussion.

## Current implementation scope
- A2 adds the plateau/valley, target navigation and terrain references to the rendered Windows synthetic-input trial. PC VR and physical devices remain unvalidated and out of this stage.
- No Bluetooth scanning, device connection, motor commands, automatic arming, or physical tests without a separate, explicit test instruction.
- Do not modify C:/projects/power. Reuse its documented evidence, not its manual permission flow as an automatic control API.
- Ladder v0: alternating meaningful pulls advance upward; inactivity stops. No precise grab/slip/fall detection, no direction-triggered resistance changes. Thresholds are configurable software defaults, not device calibration.
- Flight v0: tunable normal-flight approximation, not a validated aerodynamic or training simulator. Progressive PUMP resistance is deferred.

## Ownership and integration
- Root owns project-wide contracts, README/docs, main runtime presentation, editor bootstrap, and final integration.
- Flight agent owns Runtime/Core/Flight*.cs, Runtime/Core/Vec3.cs, tests/FlightChecks.cs.
- Ladder agent owns Runtime/Core/Ladder*.cs, tests/LadderChecks.cs.
- Environment agent owns Unity version/package metadata, tools bootstrap, scripts/test-core.ps1, tests/CoreChecks.csproj, docs/SETUP.md.
- Coordinate before changing shared files. Do not run multiple Unity Editors against one project directory.
- Commit .meta files once Unity actually creates them; never invent proof of package resolution, native compilation, headset execution, or builds.

## Technical boundaries
- Pure C# core has no Unity or hardware dependency. X right, Y up, Z forward; metres/seconds/radians.
- Sources are explicitly synthetic/replay/live. Unknown tension is never filled with a requested resistance value.
- Head look is independent of flight steering. Left/right normalized pull inputs remain independent.
- The only proposed separate runtime service is the PUMP bridge; game physics remains in-process.
- Preserve versioned configurations, meaningful regression checks, and clear known limitations.
- Do not claim M1/M2 complete until their actual acceptance gates pass. Source scaffolding and offline tests are distinct from Unity/VR execution.
