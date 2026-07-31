---
kind: logging_system
name: Logging System
category: logging_system
scope:
    - '**'
source_files:
    - Client/AcpClient.cs
    - Infrastructure/ServiceCollectionExtensions.cs
---

The repository uses Microsoft.Extensions.Logging as the logging abstraction, with no custom logging framework or infrastructure. Logging is injected via constructor injection into `AcpClient` and falls back to `NullLogger<T>` when no logger is provided.

**Framework and usage pattern**
- The only logging consumer is `Client/AcpClient.cs`, which holds a private `ILogger<AcpClient>` field.
- The constructor accepts an optional `ILogger<AcpClient>? logger` parameter and defaults to `NullLogger<AcpClient>.Instance` when null, making logging opt-in for consumers.
- No `ServiceCollectionExtensions.AddAcpClient` registration configures logging; consumers must supply their own `ILoggerFactory`/`ILogger` setup externally.

**Log levels and structured fields**
- Uses standard log levels: `Information` for lifecycle events (initialization, session creation/loading, shutdown), `Warning` for agent process exit and protocol version mismatch, and `Debug` for high-frequency operations like sending prompts.
- All messages use structured logging with named placeholders (e.g., `{ExitCode}`, `{AgentName}`, `{SessionId}`, `{Cwd}`) so sinks can capture fields without string interpolation overhead.

**What is logged**
- Client initialization and transport start
- Agent process exit codes
- Protocol version mismatches between client and agent
- Session lifecycle (create, load, cancel)
- Prompt dispatching
- Client shutdown

**DI integration**
- `Infrastructure/ServiceCollectionExtensions.cs` registers the ACP client and its dependencies but does not configure logging. Consumers are expected to call `services.AddLogging(...)` and register their preferred sink before invoking `AddAcpClient()`.

**Conventions for developers**
- Use `ILogger<T>` constructor injection wherever new components are added.
- Prefer `Information` for operational milestones, `Warning` for recoverable anomalies, and `Debug` for verbose diagnostic traces.
- Always use structured message templates with named placeholders rather than interpolated strings.
- Do not throw exceptions from logging calls; rely on the DI-provided logger being present since `NullLogger` is the safe default.