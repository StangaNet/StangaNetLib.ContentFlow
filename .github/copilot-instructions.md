# StangaNetLib.ContentFlow — Copilot instructions

- Content lifecycle state machine: Draft → Pending → Approved → Published → Revoked, with audit trail and background scheduler.
- Prefer `Result` / `Result<T>` and `Guard` from `StangaNetLib.Core` for expected failures / validation.
- Keep public APIs documented (`///`) and open-generic (`IContentWorkflowService<T>`, `IContentRepository<T>`).
- DI: `AddStangaNetLibContentFlow` + per-type `AddStangaNetLibContentFlowType<T>`; scheduler supports Scoped repositories via DI scopes.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover `net8.0`, `net9.0`, and `net10.0` when changing shared behavior.
