# Agentic.ACPLibrary

A .NET library for the [Agent Client Protocol (ACP)](https://agentclientprotocol.com/).
Supports both sides of the protocol over stdio using JSON-RPC:

- **Client** — launch and drive an ACP-compliant agent (e.g. an IDE or desktop host)
- **Agent** — build your own ACP-compliant agent that clients can connect to

> A single process should act as either a Client or an Agent, not both.

## Get the Library

This library is available on [NuGet.org](https://www.nuget.org/packages/ShihaoShen.Agentic.ACPLibrary/) and [GitHub Packages](https://github.com/AgenticDesktop/ACPLibrary/pkgs/nuget/ShihaoShen.Agentic.ACPLibrary).

## Quick Start (Client)

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

## Quick Start (Agent)

Implement `AcpAgentHandlerBase` (only `HandlePromptAsync` is required), then run the agent
over its own stdin/stdout:

```csharp
using Agentic.ACPLibrary.Agent;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

public class MyAgentHandler : AcpAgentHandlerBase
{
    public override async Task<SessionPromptResponse> HandlePromptAsync(
        string sessionId, List<ContentBlock> prompt,
        IAcpAgentContext context, CancellationToken ct = default)
    {
        // Stream updates back to the Client
        await context.SendSessionUpdateAsync(sessionId, new AgentMessageChunk
        {
            SessionId = sessionId,
            Content = new TextContent { Text = "Hello from my agent!" }
        }, ct);

        return new SessionPromptResponse { StopReason = StopReason.EndTurn };
    }
}

// Program.cs
var services = new ServiceCollection()
    .AddAcpAgent<MyAgentHandler>()   // registers StdioHostTransport + dispatcher + agent
    .BuildServiceProvider();

await using var agent = services.GetRequiredService<IAcpAgent>();
await agent.RunAsync();

while (agent.IsRunning)
    await Task.Delay(100);           // exits when the Client disconnects (stdin EOF)
```

> **Warning:** an agent's stdout is reserved for the JSON-RPC channel. Write all logs and
> diagnostics to **stderr** (`Console.Error`) — any stray `Console.WriteLine` corrupts the protocol.

During `HandlePromptAsync`, use `IAcpAgentContext` to talk back to the Client:

| Method | Client-side counterpart |
|---|---|
| `SendSessionUpdateAsync` | `session/update` notification |
| `RequestPermissionAsync` | `IPermissionHandler` |
| `ReadTextFileAsync` / `WriteTextFileAsync` | `IFileSystemHandler` |
| `CreateTerminalAsync` / `GetTerminalOutputAsync` / `WaitForTerminalExitAsync` / `KillTerminalAsync` / `ReleaseTerminalAsync` | `ITerminalHandler` |

## Mock Agent for Testing

The repository ships a runnable mock agent under [`samples/MockAgent`](samples/MockAgent) for
testing ACP clients without a real AI backend. It replies to prompt text with a streamed
`agent_message_chunk` greeting and understands a few test directives (sent as the first text block):

| Directive | Behavior |
|---|---|
| `mock:refuse` | Returns `stopReason: refusal` without streaming |
| `mock:sleep=<ms>` | Delays before responding — useful for `session/cancel` tests (returns `cancelled` when cancelled) |
| anything else | Streams a thought chunk + `Hello! You said "<text>". What can I do for you?`, returns `end_turn` |

Point any ACP client at it as a subprocess command:

```csharp
var transport = new StdioAgentTransport("dotnet",
    "run --project samples/MockAgent", workingDirectory: ".");
```

Or test it by hand — pipe JSON-RPC lines into its stdin:

```powershell
'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1}}' |
    dotnet run --project samples/MockAgent
```

## Handler Interfaces (Client)

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
| `AcpClient` | `Agentic.ACPLibrary.Client` | Default client implementation |
| `IAcpAgent` | `Agentic.ACPLibrary.Agent` | Agent contract |
| `AcpAgent` | `Agentic.ACPLibrary.Agent` | Default agent implementation |
| `IAcpAgentHandler` | `Agentic.ACPLibrary.Agent` | Agent business-logic callbacks |
| `AcpAgentHandlerBase` | `Agentic.ACPLibrary.Agent` | Convenience base class (only `HandlePromptAsync` required) |
| `IAcpAgentContext` | `Agentic.ACPLibrary.Agent` | Agent → Client requests during prompt handling |
| `IAgentTransport` | `Agentic.ACPLibrary.Transport` | Transport abstraction |
| `StdioAgentTransport` | `Agentic.ACPLibrary.Transport` | Client side — launches the agent subprocess |
| `StdioHostTransport` | `Agentic.ACPLibrary.Transport` | Agent side — uses the process's own stdin/stdout |
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
