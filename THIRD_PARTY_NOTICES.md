# Third-party components

This source snapshot contains project code, procedural primitive-based scenery, a project shader, Unity-authored serialized settings, and dependency declarations. No Asset Store art, downloaded music, fonts, textures, editor binaries, SDK binaries, or package-cache sources are intentionally vendored.

| Component | Source / authoritative inventory | Treatment |
|---|---|---|
| Unity Editor and generated project settings | `game/ProjectSettings/ProjectVersion.txt`; [Unity](https://unity.com/download) | Users obtain and license the Editor separately; its terms are not replaced by the project's future license. |
| Unity packages and transitive dependencies | `game/Packages/manifest.json` and `packages-lock.json` | Resolved by Package Manager. Preserve each resolved package's LICENSE and Third Party Notices when distributing material covered by them. PackageCache is excluded. |
| Unity CLI | [Official CLI guide](https://docs.unity.com/en-us/unity-cli/use-unity-cli) | External tool; not redistributed. |
| .NET SDK | `scripts/setup-dotnet.ps1`; [dotnet source](https://github.com/dotnet/sdk) | Downloaded separately with pinned integrity check; tools directory excluded. |
| Node.js | [Node.js](https://nodejs.org/) | External analyzer runtime; not redistributed. |

The pinned Unity package graph includes Unity and third-party components; do not assume it is all covered by the project's source license. The locally resolved packages carry their own license files. This inventory covers a **source-only** publication; a future downloadable Player needs its own dependency/notice review. No third-party device protocol implementation was copied from the separate hardware project.

Add source URL, version, author and applicable license here before importing external assets or code. Project source uses the MIT license in LICENSE; this does not relicense external tools or dependencies.
