# AGENTS.md

## Environment

- .NET SDK 10.0.x (`net10.0` target framework)

## Local Validation

```bash
dotnet build
dotnet test tests/Agentic.ACPLibrary.Tests/Agentic.ACPLibrary.Tests.csproj
dotnet pack Agentic.ACPLibrary.csproj -c Release -o ./nupkg
```

## Public API Compatibility

This repository is a public NuGet library (`ShihaoShen.Agentic.ACPLibrary`).
Changes to public types, members, or signatures are breaking changes for
downstream consumers. Preserve backward compatibility unless a major version
bump is explicitly planned.

## CI/CD Pipeline

The CI/CD pipeline is defined in `.github/workflows/nuget.yml`.

### Trigger

- Runs on every **push to the `main` branch**.

### Version Suffix

CI builds append a nightly pre-release suffix:

```
nightly.<github-sha>
```

The full package version becomes e.g. `0.1.0-nightly.abc123def`.
The suffix is passed via `-p:VersionSuffix=nightly.${{ github.sha }}`.

### Publish Targets

Packages are published to **two** registries:

| Registry | Source URL | Authentication |
|---|---|---|
| NuGet.org | `https://api.nuget.org/v3/index.json` | `NuGet/login@v1` (OIDC, user from `vars.NUGET_USER`) |
| GitHub Packages | `https://nuget.pkg.github.com/AgenticDesktop/index.json` | PAT via `secrets.BOT_TOKEN` |

### Local vs CI Pack Commands

| Context | Command |
|---|---|
| **Local** | `dotnet pack Agentic.ACPLibrary.csproj -c Release -o ./nupkg` |
| **CI** | `dotnet pack Agentic.ACPLibrary.csproj -c Release -o ./nupkg -p:VersionSuffix=nightly.${{ github.sha }}` |

The only difference is the CI-specific `-p:VersionSuffix=...` which stamps every
build with a unique nightly identifier. Local packs produce a stable version
without any suffix.

### Runtime Environment

- Runner: `windows-latest`
- Permissions: `id-token: write` (required for NuGet OIDC login)

### Full CI Flow

1. Authenticate to NuGet.org via OIDC (`NuGet/login@v1`, user from `vars.NUGET_USER`)
2. Authenticate to GitHub Packages via PAT (`secrets.BOT_TOKEN`)
3. Checkout code (`actions/checkout@v7`)
4. Setup .NET 10.0.x (`actions/setup-dotnet@v6`)
5. `dotnet pack` with nightly version suffix
6. `dotnet nuget push` to both registries

> **Note:** The CI pipeline does **not** run `dotnet build` or `dotnet test`.
> All build and test validation must pass locally before pushing to `main`.
> This differs from the local validation commands listed above.

## Effect Boundary

Modifying `.github/workflows/nuget.yml` affects the CI/CD pipeline that
publishes packages. Any change to this file can alter what gets published,
when, and where.
