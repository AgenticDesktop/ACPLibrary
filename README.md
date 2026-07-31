# Agentic.ACPLibrary

A .NET client library for the [Agent Client Protocol (ACP)](https://agentclientprotocol.com/).
Communicate with ACP-compliant agents over stdio using JSON-RPC.

## Get the Library

This library is available on [NuGet.org](https://www.nuget.org/packages/ShihaoShen.Agentic.ACPLibrary/) and [GitHub Packages](https://github.com/AgenticDesktop/ACPLibrary/pkgs/nuget/ShihaoShen.Agentic.ACPLibrary).

## Quick Start

```csharp
using Agentic.ACPLibrary.Client;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;
using Agentic.ACPLibrary.Models;
using Microsoft.Extensions.Logging;

// 1. Create transport
var transport = new StdioAgentTransport("path/to/agent", "--acp", workingDirectory: ".");

// 2. Create dispatcher and client
var dispatcher = new JsonRpcDispatcher();
var logger = loggerFactory.CreateLogger<AcpClient>();
IAcpClient client = new AcpClient(transport, dispatcher, logger);

// 3. Initialize (starts transport + handshake)
var info = await client.InitializeAsync();
Console.WriteLine($"Connected to: {info.AgentInfo?.Name}");

// 4. Create a session
var sessionId = await client.CreateSessionAsync(cwd: ".");

// 5. Send a prompt
var prompt = new List<ContentBlock> { new TextContent { Text = "Hello!" } };
var response = await client.SendPromptAsync(sessionId, prompt);
```

## Handler Interfaces

Implement these interfaces to handle requests from the Agent:

| Interface | Purpose |
|---|---|
| `IPermissionHandler` | Handle `session/request_permission` — prompt the user to approve/deny actions |
| `IFileSystemHandler` | Handle `fs/read_text_file` and `fs/write_text_file` |
| `ITerminalHandler` | Handle `terminal/create`, `terminal/output`, `terminal/wait_for_exit`, `terminal/kill`, `terminal/release` |

Assign them before calling `InitializeAsync`:

```csharp
client.PermissionHandler = myPermissionHandler;
client.FileSystemHandler = myFsHandler;
client.TerminalHandler = myTerminalHandler;
```

## Extensibility

Register custom handlers for methods not covered by the built-in protocol:

```csharp
// Handle a custom request method
client.RegisterRequestHandler("custom/method", async request =>
{
    // process request.Params ...
    return new JsonRpcResponse { Id = request.Id, Result = resultElement };
});

// Listen for a custom notification
client.RegisterNotificationHandler("custom/notify", async notification =>
{
    // handle notification
});
```

## API Overview

### Core Types

| Type | Namespace | Description |
|---|---|---|
| `IAcpClient` | `Agentic.ACPLibrary.Client` | Client contract |
| `AcpClient` | `Agentic.ACPLibrary.Client` | Default implementation |
| `IAgentTransport` | `Agentic.ACPLibrary.Transport` | Transport abstraction |
| `StdioAgentTransport` | `Agentic.ACPLibrary.Transport` | Stdio-based transport |
| `IJsonRpcDispatcher` | `Agentic.ACPLibrary.Protocol` | JSON-RPC dispatch abstraction |
| `JsonRpcDispatcher` | `Agentic.ACPLibrary.Protocol` | Default dispatcher |

### Models

| Type | Description |
|---|---|
| `ContentBlock` / `TextContent` / `ImageContent` | Prompt content types |
| `SessionUpdate` | Base class for streaming updates (`AgentMessageChunk`, `ToolCallNotification`, etc.) |
| `InitializeResponse` | Agent info from handshake |
| `SessionPromptResponse` | Response from `session/prompt` |
| `ToolCall` | Tool invocation details |

### Events on `IAcpClient`

- `SessionUpdated` — fires on each `session/update` notification (streaming text, tool calls, etc.)
- `AgentProcessExited` — fires when the agent process terminates
