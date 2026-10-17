# StangaNetLib.ContentFlow

[![.NET CI](https://github.com/StangaNet/StangaNetLib.ContentFlow/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.ContentFlow/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.1.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)

Content lifecycle state machine for .NET: Draft → Pending → Approved → Published → Revoked, with an immutable review audit trail and a background scheduler for automatic publish/expiry. Built on [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core) (`Result`, `Guard`).

## Design Philosophy

- **Domain-centric**: Workflow, Content, and Audit as capabilities.
- **Functional failures**: transitions return `Result` / `Result<T>`.
- **Background scheduling**: time-based publish/expiry via a hosted service (supports Scoped stores).
- **Modern .NET**: `net8.0` / `net9.0` with open generics and `TimeProvider`.

## Installation

The package is hosted on **GitHub Packages** (pulls Core from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.ContentFlow" Version="1.1.0" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.ContentFlow.Workflow` | `IContentWorkflowService<T>`, scheduler | Transitions + scheduling |
| `StangaNetLib.ContentFlow.Content` | `IContentRepository<T>`, `ContentItem<T>` | Persistence contracts |
| `StangaNetLib.ContentFlow.Auditing` | `IContentReviewLogRepository` | Immutable review trail |
| `StangaNetLib.ContentFlow.Configuration` | `ContentFlowSettings` | Options + validator |
| `StangaNetLib.ContentFlow.Extensions` | `AddStangaNetLibContentFlow` | DI |

## Project Structure

```
StangaNetLib.ContentFlow/
├── assets/
│   ├── package-icon.png
│   └── state_machine.svg
├── build/
│   └── StangaNetLib.ContentFlow.props
├── src/
│   └── StangaNetLib.ContentFlow/
│       ├── Auditing/
│       ├── Configuration/
│       ├── Content/
│       ├── Exceptions/
│       ├── Extensions/
│       └── Workflow/
└── tests/
    └── StangaNetLib.ContentFlow.Tests/
```

## Quick Start

```csharp
builder.Services.AddStangaNetLibContentFlow(builder.Configuration);
builder.Services.AddStangaNetLibContentFlowType<Article>();

// Optional: replace in-memory defaults with EF Core Scoped registrations
// services.AddScoped<IContentRepository<Article>, EfArticleRepository>();
// services.AddScoped<IContentUnitOfWork, EfContentUnitOfWork>();
```

## CI/CD

On push/PR to `main` and `workflow_dispatch`, the shared StangaNet workflow:

1. Restores, builds, and tests on .NET 8 and .NET 9
2. Packs the NuGet package (with symbols)
3. Publishes to GitHub Packages outside pull requests
4. Uploads the `.nupkg` as a build artifact
