# Troubleshooting Guide

<cite>
**Referenced Files in This Document**
- [README.md](file://README.md)
- [Agentic.ACPLibrary.csproj](file://Agentic.ACPLibrary.csproj)
- [AcpClient.cs](file://Client/AcpClient.cs)
- [IAcpClient.cs](file://Client/IAcpClient.cs)
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)
- [RequestPermissionRequest.cs](file://Models/RequestPermissionRequest.cs)
- [SessionUpdate.cs](file://Models/SessionUpdate.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Architecture Overview](#architecture-overview)
5. [Detailed Component Analysis](#detailed-component-analysis)
6. [Dependency Analysis](#dependency-analysis)
7. [Performance Considerations](#performance-considerations)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Conclusion](#conclusion)
10. [Appendices](#appendices)

## Introduction
This guide provides systematic troubleshooting for the ACP client library, focusing on agent connection problems, transport layer issues, JSON-RPC message parsing errors, permission handler failures, file system access problems, terminal execution issues, performance bottlenecks, memory leaks, resource exhaustion, logging strategies, diagnostic tools, profiling techniques, common error messages and resolutions, platform-specific issues, environment configuration problems, and compatibility across .NET runtime versions.

## Project Structure
The library is organized into clear layers:
- Client: high-level API and handler interfaces
- Transport: stdio-based process communication
- Protocol: JSON-RPC dispatching and request tracking
- JsonRpc: message types and converters
- Models: protocol models and enums
- Infrastructure: shared JSON serialization options

```mermaid
graph TB
subgraph "Client"
A["AcpClient"]
I1["IAcpClient"]
H1["IPermissionHandler"]
H2["IFileSystemHandler"]
H3["ITerminalHandler"]
end
subgraph "Transport"
T1["StdioAgentTransport"]
end
subgraph "Protocol"
D1["JsonRpcDispatcher"]
end
subgraph "JsonRpc"
C1["JsonRpcMessageConverter"]
O1["JsonOptions"]
end
subgraph "Models"
M1["RequestPermissionRequest"]
M2["SessionUpdate"]
end
A --> T1
A --> D1
A --> H1
A --> H2
A --> H3
D1 --> T1
D1 --> C1
A --> O1
A --> M1
A --> M2
```

**Diagram sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [IAcpClient.cs](file://Client/IAcpClient.cs)
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)
- [RequestPermissionRequest.cs](file://Models/RequestPermissionRequest.cs)
- [SessionUpdate.cs](file://Models/SessionUpdate.cs)

**Section sources**
- [README.md](file://README.md)
- [Agentic.ACPLibrary.csproj](file://Agentic.ACPLibrary.csproj)

## Core Components
- AcpClient: orchestrates initialization, session lifecycle, prompt sending, and eventing; wires built-in handlers for permissions, file system, and terminal operations.
- StdioAgentTransport: manages a child process with stdio pipes, reads lines from stdout/stderr, and exposes events for messages and process exit.
- JsonRpcDispatcher: serializes requests/notifications, tracks pending requests, deserializes incoming messages, and routes to registered handlers.
- JsonRpcMessageConverter: discriminates between request/notification/response based on JSON fields and avoids recursion by stripping itself from inner options.
- JsonOptions: centralizes System.Text.Json settings (case-insensitive properties, ignore nulls, out-of-order metadata, converters).
- Handler interfaces: define contracts for permission, file system, and terminal operations invoked by the agent.

Key responsibilities and failure points are mapped throughout this guide.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)

## Architecture Overview
The runtime flow connects the client to an external agent via a child process over stdio. The dispatcher handles JSON-RPC routing, while handlers implement domain-specific behaviors.

```mermaid
sequenceDiagram
participant App as "Your App"
participant Client as "AcpClient"
participant Dispatcher as "JsonRpcDispatcher"
participant Transport as "StdioAgentTransport"
participant Agent as "Agent Process"
App->>Client : InitializeAsync()
Client->>Transport : StartAsync()
Client->>Dispatcher : Connect(transport)
Client->>Dispatcher : RegisterNotificationHandler("session/update")
Client->>Dispatcher : RegisterRequestHandler("session/request_permission")
Client->>Dispatcher : RegisterRequestHandler("fs/*")
Client->>Dispatcher : RegisterRequestHandler("terminal/*")
Client->>Dispatcher : SendRequestAsync("initialize", payload)
Dispatcher-->>Transport : SendAsync(jsonLine)
Transport-->>Agent : Write line to stdin
Agent-->>Transport : Write response line to stdout
Transport-->>Dispatcher : MessageReceived(line)
Dispatcher-->>Client : Complete pending request
Client-->>App : InitializeResponse
```

**Diagram sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

## Detailed Component Analysis

### AcpClient
Responsibilities:
- Starts transport, connects dispatcher, subscribes to process exit and session updates.
- Registers built-in handlers for permissions, file system, and terminal methods.
- Performs initialize handshake and stores agent info.
- Manages sessions and prompts.

Common issues:
- Missing handlers cause default responses or errors.
- Session ID not set if CreateSessionAsync fails.
- Event subscriptions must be attached before InitializeAsync.

Diagnostics:
- Inspect IsInitialized and AgentInfo after InitializeAsync.
- Subscribe to AgentProcessExited to detect unexpected termination.
- Use ILogger to trace initialization steps.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [IAcpClient.cs](file://Client/IAcpClient.cs)

### StdioAgentTransport
Responsibilities:
- Spawns a child process with redirected stdio.
- Reads lines from stdout and stderr asynchronously.
- Exposes MessageReceived and ProcessExited events.
- Graceful shutdown with timeout and forced kill fallback.

Common issues:
- Invalid command path or arguments prevent process start.
- Working directory misconfiguration leads to permission or path errors.
- Encoding mismatches can corrupt messages.
- EOF or read exceptions terminate the loop silently unless handled.

Diagnostics:
- Check State transitions (Created → Starting → Running → Stopped).
- Monitor stderr output for agent-side logs.
- Ensure CancellationToken propagation during StopAsync.

**Section sources**
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

### JsonRpcDispatcher
Responsibilities:
- Tracks pending requests and completes them upon response arrival.
- Serializes outgoing requests/notifications and deserializes incoming messages.
- Routes requests and notifications to registered handlers.

Common issues:
- Sending without Connect throws invalid operation.
- Deserialization failures swallow exceptions and drop messages.
- Handler registration order matters; ensure required handlers are present.

Diagnostics:
- Verify transport is connected before sending.
- Add logging around OnMessageReceived to capture raw JSON lines.
- Validate that all expected handlers are registered.

**Section sources**
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)

### JsonRpcMessageConverter
Responsibilities:
- Discriminates message type by presence of method/id/result/error fields.
- Avoids recursive conversion by removing itself from inner options.

Common issues:
- Malformed JSON results in fallback or ignored messages.
- Custom converters must not re-introduce recursion.

Diagnostics:
- Inspect raw JSON lines before conversion.
- Confirm converter is registered in JsonOptions.Default.

**Section sources**
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)

### JsonOptions
Responsibilities:
- Centralizes JsonSerializerOptions (case-insensitive property names, ignore nulls, allow out-of-order metadata, add converters).

Common issues:
- Missing converters lead to unrecognized types.
- Case sensitivity differences break field mapping.

Diagnostics:
- Ensure JsonOptions.Default is used consistently across serialization.

**Section sources**
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)

### Handler Interfaces
- IPermissionHandler: respond to permission requests with outcomes.
- IFileSystemHandler: read/write text files.
- ITerminalHandler: create, query, wait, kill, release terminals.

Common issues:
- Null handlers return default error responses.
- Long-running handlers may block dispatching.

Diagnostics:
- Implement robust cancellation support.
- Log entry/exit of handler methods.

**Section sources**
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)

## Dependency Analysis
High-level dependencies and coupling:
- AcpClient depends on Transport, Dispatcher, and Handlers.
- Dispatcher depends on Transport and RequestTracker.
- JsonOptions centralizes serialization behavior used by both Dispatcher and AcpClient.
- Models are consumed by AcpClient and handlers.

```mermaid
classDiagram
class AcpClient {
+InitializeAsync()
+CreateSessionAsync()
+SendPromptAsync()
+CancelSessionAsync()
+ShutdownAsync()
+RegisterRequestHandler()
+RegisterNotificationHandler()
}
class StdioAgentTransport {
+StartAsync()
+SendAsync()
+StopAsync()
+MessageReceived
+ProcessExited
}
class JsonRpcDispatcher {
+Connect()
+SendRequestAsync()
+SendNotificationAsync()
+RegisterRequestHandler()
+RegisterNotificationHandler()
+DisconnectAsync()
}
class JsonRpcMessageConverter
class JsonOptions
class IPermissionHandler
class IFileSystemHandler
class ITerminalHandler
AcpClient --> StdioAgentTransport : "uses"
AcpClient --> JsonRpcDispatcher : "uses"
AcpClient --> IPermissionHandler : "delegates"
AcpClient --> IFileSystemHandler : "delegates"
AcpClient --> ITerminalHandler : "delegates"
JsonRpcDispatcher --> StdioAgentTransport : "sends/receives"
AcpClient --> JsonOptions : "serializes"
JsonRpcDispatcher --> JsonRpcMessageConverter : "deserializes"
```

**Diagram sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

## Performance Considerations
- Serialization overhead: System.Text.Json is efficient; avoid repeated option creation by using JsonOptions.Default.
- Async I/O: Ensure handlers do not perform blocking I/O; use async APIs and cancellation tokens.
- Stream throughput: Large outputs from terminals or file reads should be chunked and backpressure-aware.
- Memory usage: Reuse buffers where possible; avoid unnecessary string allocations in hot paths.
- Concurrency: Dispatcher processes messages sequentially per line; long-running handlers can delay processing—consider offloading work.

[No sources needed since this section provides general guidance]

## Troubleshooting Guide

### Agent Connection Problems
Symptoms:
- InitializeAsync hangs or returns early.
- No session created; CurrentSessionId remains null.
- AgentProcessExited fires immediately.

Root causes:
- Incorrect agent executable path or arguments.
- Wrong working directory causing permission or path errors.
- Transport not started or already stopped.
- Protocol version mismatch warnings.

Resolution steps:
- Validate command and arguments; test running the agent manually.
- Set explicit workingDirectory when constructing StdioAgentTransport.
- Ensure InitializeAsync is called once and only after transport is ready.
- Inspect AgentInfo.ProtocolVersion and log warnings accordingly.

Diagnostics:
- Log transport state transitions and process exit codes.
- Capture stderr lines from the agent process.
- Temporarily disable handlers to isolate handshake issues.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

### Transport Layer Issues
Symptoms:
- Messages not delivered; no MessageReceived events.
- Exceptions during SendAsync or ReadLoopAsync.
- Process does not terminate on ShutdownAsync.

Root causes:
- Sending while transport is not Running.
- EOF reached unexpectedly; reader loop exits.
- StandardInput/StandardOutput encoding mismatches.
- Timeout waiting for process exit leading to forced kill.

Resolution steps:
- Check Transport.State before sending.
- Ensure CancellationToken is propagated to StopAsync.
- Confirm UTF-8 encoding is configured for stdio streams.
- Investigate stderr for agent-side errors.

Diagnostics:
- Wrap SendAsync calls with try/catch and log exceptions.
- Add counters for lines received/sent.
- Use OS tools to verify process existence and pipe status.

**Section sources**
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

### JSON-RPC Message Parsing Errors
Symptoms:
- Requests or notifications ignored.
- Responses never complete pending requests.
- Unknown session update types fall back to base.

Root causes:
- Malformed JSON lines.
- Missing required fields (method, id, result, error).
- Custom converters not registered.
- Case-sensitive property name mismatches.

Resolution steps:
- Log raw JSON lines before deserialization.
- Ensure JsonOptions.Default includes JsonRpcMessageConverter and enum converter.
- Enable case-insensitive property names.
- Handle unknown derived types gracefully (fallback to base).

Diagnostics:
- Add a global message logger in OnMessageReceived to capture payloads.
- Validate JSON schema locally against known message shapes.

**Section sources**
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [JsonRpcMessageConverter.cs](file://JsonRpc/JsonRpcMessageConverter.cs)
- [JsonOptions.cs](file://Infrastructure/JsonOptions.cs)
- [SessionUpdate.cs](file://Models/SessionUpdate.cs)

### Permission Handler Failures
Symptoms:
- Default cancelled outcome returned.
- UI never prompted; agent stalls.

Root causes:
- PermissionHandler not assigned.
- Handler throws or blocks indefinitely.
- Incorrect request model mapping.

Resolution steps:
- Assign PermissionHandler before InitializeAsync.
- Implement non-blocking UI interactions with cancellation support.
- Validate RequestPermissionRequest fields and respond promptly.

Diagnostics:
- Log handler entry/exit and decision time.
- Return explicit outcomes (cancelled or selected) with correct optionId.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [IPermissionHandler.cs](file://Client/IPermissionHandler.cs)
- [RequestPermissionRequest.cs](file://Models/RequestPermissionRequest.cs)

### File System Access Problems
Symptoms:
- fs/read_text_file or fs/write_text_file fail with “File system not available”.
- UnauthorizedAccessException or FileNotFoundException.

Root causes:
- FileSystemHandler not assigned.
- Insufficient permissions or invalid paths.
- Content encoding issues.

Resolution steps:
- Assign IFileSystemHandler before InitializeAsync.
- Validate paths and check permissions prior to I/O.
- Use consistent encodings and handle large content efficiently.

Diagnostics:
- Log requested paths and outcomes.
- Wrap file operations in try/catch and return meaningful errors.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [IFileSystemHandler.cs](file://Client/IFileSystemHandler.cs)

### Terminal Execution Issues
Symptoms:
- terminal/create returns “Terminal handler not available”.
- Output empty or commands not executing.
- WaitForExit never completes.

Root causes:
- ITerminalHandler not assigned.
- Command not found or working directory incorrect.
- Terminal process not managed properly.

Resolution steps:
- Assign ITerminalHandler before InitializeAsync.
- Validate command availability and workingDirectory.
- Implement proper process lifecycle management.

Diagnostics:
- Log terminalId, command, workingDirectory, and exit codes.
- Provide GetOutputAsync streaming to inspect partial output.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [ITerminalHandler.cs](file://Client/ITerminalHandler.cs)

### Performance Bottlenecks
Indicators:
- High CPU during serialization/deserialization.
- Slow handler execution delaying other messages.
- Memory growth over time.

Approach:
- Profile serialization hotspots; reuse JsonOptions.Default.
- Offload heavy work in handlers to background tasks.
- Measure latency of SendRequestAsync and handler invocations.

Tools:
- .NET Profiler, dotnet-trace, dotnet-counters.
- Application Insights or OpenTelemetry for distributed tracing.

[No sources needed since this section provides general guidance]

### Memory Leaks and Resource Exhaustion
Indicators:
- Unbounded growth in memory or handles.
- Pending requests never completing.
- Streams or processes not closed.

Approach:
- Ensure DisposeAsync/ShutdownAsync called on client.
- Cancel pending requests on disconnect.
- Close streams and kill processes on failure paths.

Diagnostics:
- Track open resources and dispose patterns.
- Use GC diagnostics and handle leak analysis.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

### Logging Strategies
Recommendations:
- Use ILogger<AcpClient> for structured logs at key lifecycle points.
- Log raw JSON lines in a separate channel for debugging.
- Include correlation IDs for requests and sessions.

Implementation tips:
- Wrap critical sections with try/catch and log exceptions.
- Avoid excessive logging in hot loops; sample or throttle.

**Section sources**
- [AcpClient.cs](file://Client/AcpClient.cs)

### Diagnostic Tools
- Process inspection: task manager, procmon, strace/ltrace equivalents.
- Network-like analysis: capture stdio lines to files for replay.
- .NET tools: dotnet-trace, dotnet-dump, dotnet-gcdump.

[No sources needed since this section provides general guidance]

### Profiling Techniques
- CPU profiling: identify slow methods and hot paths.
- Memory profiling: detect allocations and leaks.
- I/O profiling: measure file and process I/O latency.

[No sources needed since this section provides general guidance]

### Common Error Messages, Causes, and Resolutions
- “Transport is not running.”
  - Cause: SendAsync called before StartAsync or after StopAsync.
  - Resolution: Ensure transport state is Running; call StartAsync first.
- “Dispatcher is not connected to a transport.”
  - Cause: SendRequestAsync/SendNotificationAsync without Connect.
  - Resolution: Call Connect before sending; verify initialization order.
- “File system not available” / “Terminal handler not available”
  - Cause: Handlers not assigned.
  - Resolution: Assign IFileSystemHandler/ITerminalHandler before InitializeAsync.
- Protocol version mismatch warning
  - Cause: Agent reports different protocol version than client.
  - Resolution: Update client or agent to align versions; log and proceed cautiously.
- Unknown session update type
  - Cause: New update kind not recognized.
  - Resolution: Library falls back to base type; update models to include new types.

**Section sources**
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [AcpClient.cs](file://Client/AcpClient.cs)
- [SessionUpdate.cs](file://Models/SessionUpdate.cs)

### Platform-Specific Issues
- Windows vs Unix path separators and quoting in command arguments.
- ShellExecute vs direct process invocation differences.
- Encoding defaults on different platforms; enforce UTF-8 explicitly.

Resolutions:
- Normalize paths and escape arguments appropriately.
- Test on target platforms; validate workingDirectory permissions.

[No sources needed since this section provides general guidance]

### Environment Configuration Problems
- Missing PATH entries for agent executable.
- Incorrect workingDirectory causing relative path failures.
- Environment variables not inherited by child process.

Resolutions:
- Provide absolute paths for agent executable.
- Set explicit workingDirectory in StdioAgentTransport.
- Pass necessary environment variables to the child process.

**Section sources**
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)

### Compatibility Across .NET Runtime Versions
- Target framework is net10.0; ensure runtime matches.
- System.Text.Json behavior may vary slightly across versions; rely on documented options.
- Microsoft.Extensions.Logging.Abstractions compatibility verified in project references.

Resolutions:
- Align runtime version with project target.
- Pin dependency versions to avoid breaking changes.

**Section sources**
- [Agentic.ACPLibrary.csproj](file://Agentic.ACPLibrary.csproj)

## Conclusion
Effective troubleshooting hinges on understanding the layered architecture: client orchestration, transport lifecycle, JSON-RPC dispatching, and handler implementations. By instrumenting logs, validating configurations, and following the diagnostic steps outlined above, most connection, parsing, handler, and performance issues can be resolved quickly. For persistent problems, leverage .NET profiling tools and capture raw JSON traces to pinpoint root causes.

## Appendices

### Quick Reference: Initialization Flow
```mermaid
flowchart TD
Start(["Start"]) --> Init["InitializeAsync()"]
Init --> StartTransport["Start transport"]
StartTransport --> ConnectDispatcher["Connect dispatcher"]
ConnectDispatcher --> RegisterHandlers["Register built-in handlers"]
RegisterHandlers --> SendInit["Send 'initialize' request"]
SendInit --> ReceiveResp{"Response received?"}
ReceiveResp --> |Yes| StoreInfo["Store AgentInfo"]
ReceiveResp --> |No| Error["Log error and retry/fail"]
StoreInfo --> Ready(["Ready"])
Error --> End(["End"])
Ready --> End
```

**Diagram sources**
- [AcpClient.cs](file://Client/AcpClient.cs)
- [JsonRpcDispatcher.cs](file://Protocol/JsonRpcDispatcher.cs)
- [StdioAgentTransport.cs](file://Transport/StdioAgentTransport.cs)