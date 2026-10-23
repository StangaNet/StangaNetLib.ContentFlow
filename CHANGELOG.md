# Changelog

All notable changes to **StangaNetLib.ContentFlow** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]


## [1.1.1] - 2026-10-23

### Changed
- Add `net10.0` target framework alongside `net8.0` and `net9.0`.
- Require .NET SDK 10 (global.json) to multi-target `net8.0` / `net9.0` / `net10.0`; opt in Microsoft.Testing.Platform as the `dotnet test` runner.

## [1.1.0] - 2026-10-17

### Added
- Scoped repository support in the content scheduler (opens a DI scope each tick).
- Dual targeting `net8.0` / `net9.0`, XML docs, symbols (`snupkg`), package icon, and MSBuild consumer metadata stamp.
- Depends on `StangaNetLib.Core` 1.0.0; Microsoft.Extensions.* pinned to 8.x.
- CI via shared StangaNet reusable workflow (push / PR / manual; publish outside PRs).

### Changed
- Packaging and repo hygiene aligned to StangaNet org standard (source key `StangaNet`, gated publish).

## [1.0.0] - 2026-10-16

### Added
- Content lifecycle state machine: Draft → Pending → Approved → Published → Revoked.
- Immutable review audit trail (`IContentReviewLogRepository`).
- Background scheduler for automatic publish and expiry revocation.
- DI registration (`AddStangaNetLibContentFlow` / `AddStangaNetLibContentFlowType<T>`).
- In-memory repositories for development and tests.

[Unreleased]: https://github.com/StangaNet/StangaNetLib.ContentFlow/compare/v1.1.1...HEAD
[1.1.1]: https://github.com/StangaNet/StangaNetLib.ContentFlow/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/StangaNet/StangaNetLib.ContentFlow/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/StangaNet/StangaNetLib.ContentFlow/releases/tag/v1.0.0
