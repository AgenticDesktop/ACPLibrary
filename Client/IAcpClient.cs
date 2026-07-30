using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.JsonRpc;

namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Defines the contract for an ACP protocol client.
/// </summary>
public interface IAcpClient : IAsyncDisposable
{
    /// <summary>Agent info returned after initialization.</summary>
    InitializeResponse? AgentInfo { get; }

    /// <summary>Whether the client has completed the initialize handshake.</summary>
    bool IsInitialized { get; }

    /// <summary>Handler for Agent permission requests.</summary>
    IPermissionHandler? PermissionHandler { get; set; }

    /// <summary>Handler for Agent file system requests.</summary>
    IFileSystemHandler? FileSystemHandler { get; set; }

    /// <summary>Handler for Agent terminal requests.</summary>
    ITerminalHandler? TerminalHandler { get; set; }

    /// <summary>Current session ID.</summary>
    string? CurrentSessionId { get; }

    /// <summary>Raised when a session/update notification is received.</summary>
    event Func<SessionUpdate, Task>? SessionUpdated;

    /// <summary>Raised when the Agent process exits.</summary>
    event Func<int, Task>? AgentProcessExited;

    /// <summary>Start transport and perform the initialize handshake.</summary>
    Task<InitializeResponse> InitializeAsync(CancellationToken ct = default);

    /// <summary>Create a new session.</summary>
    Task<string> CreateSessionAsync(string cwd, CancellationToken ct = default);

    /// <summary>Load an existing session.</summary>
    Task<string> LoadSessionAsync(string sessionId, string cwd, CancellationToken ct = default);

    /// <summary>Send a prompt and wait for a response. Streaming updates arrive via SessionUpdated.</summary>
    Task<SessionPromptResponse> SendPromptAsync(string sessionId, List<ContentBlock> prompt, CancellationToken ct = default);

    /// <summary>Cancel an in-progress prompt.</summary>
    Task CancelSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Shut down the client (disconnect transport).</summary>
    Task ShutdownAsync();

    /// <summary>Register a custom JSON-RPC request handler for extensibility.</summary>
    void RegisterRequestHandler(string method, Func<JsonRpcRequest, Task<JsonRpcResponse>> handler);

    /// <summary>Register a custom JSON-RPC notification handler for extensibility.</summary>
    void RegisterNotificationHandler(string method, Func<JsonRpcNotification, Task> handler);
}
