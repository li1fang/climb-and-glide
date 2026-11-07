# Contributing

Start with [current project state](docs/STATE.zh-CN.md), [setup](docs/SETUP.md), and [project rules](AGENTS.md).

- Keep physics and gameplay rules in the Unity-independent core. Keep input provenance explicit; synthetic input is not device evidence.
- Change defaults in their source configuration, not copies in documentation. Update STATE when acceptance or scope changes.
- Run `scripts/test-core.ps1` for core changes; add `-Trial` for session/flow changes. Unity changes need a native build and rendered trial, including screenshot review.
- Run only one Editor against a project directory. Coordinate shared files before parallel work.
- Preserve Unity-generated `.meta` files and the package lockfile. Do not commit Library, tools, builds, logs, credentials, or personal device records.
- Run `scripts/check-source.ps1` before proposing a public snapshot. This is a heuristic inventory check, not a proof that files contain no sensitive information.
- Report what ran, which binary/source was tested, and remaining limitations. VR and device claims require separate actual validation.

Project source is licensed under [MIT](LICENSE). External dependencies retain their own terms; record imported material in THIRD_PARTY_NOTICES.md.
