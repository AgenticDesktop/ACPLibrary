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

## Effect Boundary

Modifying `.github/workflows/nuget.yml` affects the CI/CD pipeline that
publishes packages. The current workflow triggers on every push to `main` and
publishes to both NuGet.org and GitHub Packages. Any change to this file can
alter what gets published, when, and where.
