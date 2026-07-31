---
kind: configuration_system
name: Configuration System
category: configuration_system
scope:
    - '**'
source_files:
    - Infrastructure/JsonOptions.cs
    - Infrastructure/ServiceCollectionExtensions.cs
    - Client/AcpClient.cs
    - Models/SessionNewRequest.cs
    - Agentic.ACPLibrary.csproj
---

This repository is a .NET NuGet library implementing the Agent Client Protocol over JSON-RPC 2.0. It does **not** implement a general-purpose application configuration system (no appsettings.json, environment variable loading, feature flags, or secrets management). Configuration concerns are minimal and localized:

1. **JSON serialization options** — `Infrastructure/JsonOptions.cs` defines a shared `JsonSerializerOptions` singleton used throughout the library for deserializing JSON-RPC messages and ACP protocol models. It configures case-insensitive property names, null-ignoring on write, non-indented output, and out-of-order metadata properties, plus custom converters (`JsonRpcMessageConverter`, `JsonStringEnumConverter`).

2. **Dependency injection registration** — `Infrastructure/ServiceCollectionExtensions.cs` provides an `AddAcpClient()` extension that registers the client, dispatcher, and request tracker into Microsoft.Extensions.DependencyInjection. This is the only DI/configuration entry point.

3. **Runtime configuration via constructor parameters and properties** — `AcpClient` takes an `IAgentTransport`, `IJsonRpcDispatcher`, and optional `ILogger<AcpClient>` through its constructor. Pluggable behavior is configured by setting `PermissionHandler`, `FileSystemHandler`, and `TerminalHandler` properties before calling `InitializeAsync`. There is no file-based or env-var configuration.

4. **Model-driven configuration** — The `McpServerConfig` record in `Models/SessionNewRequest.cs` represents MCP server configuration passed as part of the `session/new` request payload (command, args, env variables). This is runtime data, not persistent configuration.

5. **Build/package metadata** — `Agentic.ACPLibrary.csproj` contains package metadata (version, authors, description) but no build-time configuration files.

There are no `.env`, `appsettings.*`, YAML/TOML config files, or configuration providers. The library is designed to be consumed by host applications that supply all configuration at construction time.